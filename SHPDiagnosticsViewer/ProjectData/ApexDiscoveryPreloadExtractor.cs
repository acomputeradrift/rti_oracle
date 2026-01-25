using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace SHPDiagnosticsViewer.ProjectData;

public sealed class ApexDiscoveryPreloadResult
{
    public Dictionary<string, string> PageIndexMap { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, SysVarRefEntry> SysVarRefMap { get; } = new(StringComparer.Ordinal);
    public Dictionary<int, DriverConfigEntry> DriverConfigMap { get; } = new();
}

public sealed record SysVarRefEntry(int? DriverDeviceId, string? DriverName, string? VariableName, int? DeviceId);
public sealed record DriverConfigEntry(string DeviceName, string DeviceDisplayName, Dictionary<string, string> Config);

public static class ApexDiscoveryPreloadExtractor
{
    private static readonly Regex SysVarGuidPattern = new Regex("\\{[A-F0-9\\-]+\\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VariablePattern = new Regex("<variable\\s+name='([^']+)'\\s+sysvar='([^']+)'", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ZoneNamePattern = new Regex("^ZoneName(\\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SourceNamePattern = new Regex("^SourceName(\\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex InputNamePattern = new Regex("^input(\\d+)name$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex OutputNamePattern = new Regex("^Output(\\d+)name$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static ApexDiscoveryPreloadResult Extract(string apexPath)
    {
        if (!File.Exists(apexPath))
        {
            throw new FileNotFoundException("APEX file not found.", apexPath);
        }

        var result = new ApexDiscoveryPreloadResult();
        using var connection = new SqliteConnection($"Data Source={apexPath};Mode=ReadOnly");
        connection.Open();

        LoadPageIndexMap(connection, result.PageIndexMap);
        LoadDriverConfigMap(connection, result.DriverConfigMap);
        LoadSysVarRefMap(connection, result.SysVarRefMap);

        return result;
    }

    private static void LoadPageIndexMap(SqliteConnection connection, Dictionary<string, string> map)
    {
        if (DriverProfiles.DriverProfileCatalog.Internal().Count == 0)
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = """
SELECT
  d.DeviceId AS DeviceId,
  p.PageOrder AS PageIndex,
  n.PageName AS PageName
FROM RTIDeviceData d
JOIN Devices dv ON d.DeviceId = dv.DeviceId
LEFT JOIN RTIDevicePageData p ON p.RTIAddress = d.RTIAddress
LEFT JOIN PageNames n ON p.PageNameId = n.PageNameId
ORDER BY d.DeviceId, p.PageOrder;
""";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            if (reader.IsDBNull(1))
            {
                continue;
            }

            var deviceId = reader.GetInt32(0);
            var pageIndex = reader.GetInt32(1);
            var pageName = reader.IsDBNull(2) ? "" : reader.GetString(2);
            map[$"{deviceId}|{pageIndex}"] = pageName;
        }
    }

    private static void LoadDriverConfigMap(SqliteConnection connection, Dictionary<int, DriverConfigEntry> map)
    {
        var deviceNames = new Dictionary<int, string>();
        var deviceDisplayNames = new Dictionary<int, string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT DeviceId, Name, DisplayName FROM Devices ORDER BY DeviceId";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                var deviceId = reader.GetInt32(0);
                var name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                var displayName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                deviceNames[deviceId] = name;
                deviceDisplayNames[deviceId] = string.IsNullOrWhiteSpace(displayName) ? name : displayName;
            }
        }

        var driverDeviceIds = new Dictionary<int, int>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT DriverDeviceId, DeviceId FROM DriverData ORDER BY DriverDeviceId";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                driverDeviceIds[reader.GetInt32(0)] = reader.GetInt32(1);
            }
        }

        var configsByDriver = new Dictionary<int, List<(string Name, string Value)>>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT DriverDeviceId, Name, Value FROM DriverConfig ORDER BY DriverDeviceId, Name";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                var name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                if (name.StartsWith("Debug", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var driverDeviceId = reader.GetInt32(0);
                var value = reader.IsDBNull(2) ? "" : reader.GetString(2);
                if (!configsByDriver.TryGetValue(driverDeviceId, out var list))
                {
                    list = new List<(string, string)>();
                    configsByDriver[driverDeviceId] = list;
                }
                list.Add((name, value));
            }
        }

        var registry = DriverProfiles.DriverProfileRegistryFactory.CreateDefault();
        foreach (var (driverDeviceId, configs) in configsByDriver)
        {
            driverDeviceIds.TryGetValue(driverDeviceId, out var deviceId);
            deviceNames.TryGetValue(deviceId, out var deviceName);
            var profile = registry.Find(deviceName ?? "");

            var limits = ExtractLimits(configs);
            var filtered = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (profile is null || (profile.DiscoveryKeys.Count == 0 && profile.DiscoveryPrefixes.Count == 0))
            {
                foreach (var (name, value) in configs)
                {
                    if (ShouldIncludeConfig(name, limits))
                    {
                        filtered[name] = value;
                    }
                }
            }
            else
            {
                var counts = ExtractCounts(configs, profile.DiscoveryKeys);
                foreach (var (name, value) in configs)
                {
                    if (profile.DiscoveryKeys.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (TryIncludeByPrefix(profile.DiscoveryPrefixes, counts, name))
                    {
                        filtered[name] = value;
                    }
                }
            }

            deviceDisplayNames.TryGetValue(deviceId, out var deviceDisplayName);
            map[driverDeviceId] = new DriverConfigEntry(deviceName ?? "", deviceDisplayName ?? "", filtered);
        }
    }

    private static void LoadSysVarRefMap(SqliteConnection connection, Dictionary<string, SysVarRefEntry> map)
    {
        var deviceNames = new Dictionary<int, string>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT DeviceId, Name FROM Devices ORDER BY DeviceId";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                deviceNames[reader.GetInt32(0)] = reader.IsDBNull(1) ? "" : reader.GetString(1);
            }
        }

        var drivers = new Dictionary<string, (int DriverDeviceId, int DeviceId, string DriverName, Dictionary<string, string> Variables)>(StringComparer.OrdinalIgnoreCase);
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT DriverDeviceId, DeviceId, DriverId, SystemVariables FROM DriverData ORDER BY DriverDeviceId";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                var driverDeviceId = reader.GetInt32(0);
                if (reader.IsDBNull(1))
                {
                    continue;
                }

                var deviceId = reader.GetInt32(1);
                var driverId = reader.IsDBNull(2) ? "" : reader.GetString(2);
                if (string.IsNullOrWhiteSpace(driverId))
                {
                    continue;
                }

                var xml = reader.IsDBNull(3) ? "" : reader.GetString(3);
                var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match match in VariablePattern.Matches(xml))
                {
                    var name = match.Groups[1].Value;
                    var sysvar = match.Groups[2].Value;
                    if (!variables.ContainsKey(sysvar))
                    {
                        variables[sysvar] = name;
                    }
                }

                var driverName = deviceNames.TryGetValue(deviceId, out var deviceName) ? deviceName : "";
                drivers[driverId] = (driverDeviceId, deviceId, driverName, variables);
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT SysVarRef, DeviceId FROM SystemVariableIds ORDER BY SysVarID";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                var sysVarRef = reader.GetString(0);
                if (string.IsNullOrWhiteSpace(sysVarRef))
                {
                    continue;
                }

                var normalizedKey = sysVarRef.StartsWith("SYSVARREF:", StringComparison.OrdinalIgnoreCase)
                    ? sysVarRef
                    : $"SYSVARREF:{sysVarRef}";

                var driverIdMatch = SysVarGuidPattern.Match(sysVarRef);
                var driverDeviceId = (int?)null;
                var driverName = "";
                Dictionary<string, string>? variableLookup = null;
                if (driverIdMatch.Success && drivers.TryGetValue(driverIdMatch.Value, out var driver))
                {
                    driverDeviceId = driver.DriverDeviceId;
                    driverName = driver.DriverName;
                    variableLookup = driver.Variables;
                }

                var sysvarToken = "";
                var atIndex = sysVarRef.IndexOf('@');
                if (atIndex >= 0 && atIndex + 1 < sysVarRef.Length)
                {
                    sysvarToken = sysVarRef[(atIndex + 1)..];
                }

                string? variableName = null;
                if (!string.IsNullOrEmpty(sysvarToken) && variableLookup != null)
                {
                    variableLookup.TryGetValue(sysvarToken, out variableName);
                }

                int? deviceId = reader.IsDBNull(1) ? null : reader.GetInt32(1);
                map[normalizedKey] = new SysVarRefEntry(driverDeviceId, driverName, variableName, deviceId);
            }
        }
    }

    private static ConfigLimits ExtractLimits(List<(string Name, string Value)> configs)
    {
        var limits = new ConfigLimits();
        foreach (var (name, value) in configs)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            {
                continue;
            }

            if (string.Equals(name, "MaxZones", StringComparison.OrdinalIgnoreCase))
            {
                limits.MaxZones = intValue;
            }
            else if (string.Equals(name, "MaxSources", StringComparison.OrdinalIgnoreCase))
            {
                limits.MaxSources = intValue;
            }
            else if (string.Equals(name, "Inputs", StringComparison.OrdinalIgnoreCase))
            {
                limits.Inputs = intValue;
            }
            else if (string.Equals(name, "Outputs", StringComparison.OrdinalIgnoreCase))
            {
                limits.Outputs = intValue;
            }
        }

        return limits;
    }

    private static Dictionary<string, int> ExtractCounts(List<(string Name, string Value)> configs, IReadOnlyList<string> keys)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in configs)
        {
            if (!keys.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            {
                counts[name] = intValue;
            }
        }
        return counts;
    }

    private static bool TryIncludeByPrefix(IReadOnlyList<string> prefixes, Dictionary<string, int> counts, string name)
    {
        foreach (var prefix in prefixes)
        {
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var suffix = name[prefix.Length..];
            if (!int.TryParse(suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            {
                return false;
            }

            var countKey = prefix.EndsWith("Name", StringComparison.OrdinalIgnoreCase)
                ? $"{prefix[..^4]}Count"
                : $"{prefix}Count";
            if (counts.TryGetValue(countKey, out var maxIndex))
            {
                return index <= maxIndex;
            }

            return false;
        }

        return false;
    }

    private static bool ShouldIncludeConfig(string name, ConfigLimits limits)
    {
        if (TryGetIndex(ZoneNamePattern, name, out var zoneIndex))
        {
            return !limits.MaxZones.HasValue || zoneIndex < limits.MaxZones.Value;
        }

        if (TryGetIndex(SourceNamePattern, name, out var sourceIndex))
        {
            return !limits.MaxSources.HasValue || sourceIndex < limits.MaxSources.Value;
        }

        if (TryGetIndex(InputNamePattern, name, out var inputIndex))
        {
            return !limits.Inputs.HasValue || inputIndex <= limits.Inputs.Value;
        }

        if (TryGetIndex(OutputNamePattern, name, out var outputIndex))
        {
            return !limits.Outputs.HasValue || outputIndex <= limits.Outputs.Value;
        }

        return true;
    }

    private static bool TryGetIndex(Regex pattern, string name, out int index)
    {
        index = 0;
        var match = pattern.Match(name);
        if (!match.Success)
        {
            return false;
        }

        return int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
    }

    private sealed class ConfigLimits
    {
        public int? MaxZones { get; set; }
        public int? MaxSources { get; set; }
        public int? Inputs { get; set; }
        public int? Outputs { get; set; }
    }
}
