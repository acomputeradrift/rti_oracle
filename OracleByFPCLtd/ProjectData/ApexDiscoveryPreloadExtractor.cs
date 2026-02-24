using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Data.Sqlite;
using OracleByFPCLtd.DriverProfiles.Catalog;
using OracleByFPCLtd.DriverProfiles.Matching;
using OracleByFPCLtd.Logging;

namespace OracleByFPCLtd.ProjectData;

public sealed class ApexDiscoveryPreloadResult
{
    public Dictionary<string, string> PageIndexMap { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, SysVarRefEntry> SysVarRefMap { get; } = new(StringComparer.Ordinal);
    public Dictionary<int, DriverConfigEntry> DriverConfigMap { get; } = new();
    public List<PageMappingEntry> PageMappings { get; } = new();
    public List<RelayPortEntry> RelayPorts { get; } = new();
    public List<MpioIrPortEntry> MpioIrPorts { get; } = new();
    public List<SensePortEntry> SensePorts { get; } = new();
    public List<TriggerPortEntry> TriggerPorts { get; } = new();
    public List<Rs232PortEntry> Rs232Ports { get; } = new();
    public List<RoomMappingEntry> RoomMappings { get; } = new();
    public List<SourceCatalogEntry> SourceCatalog { get; } = new();
    public List<SystemManagerSourceCatalogEntry> SystemManagerSourceCatalog { get; } = new();
    public List<DriverTemplateVariableEntry> DriverTemplateVariables { get; } = new();
}

public sealed record SysVarRefEntry(int? DriverDeviceId, string? DriverName, string? VariableName, int? DeviceId);
public sealed record DriverConfigEntry(string DeviceName, string DeviceDisplayName, Dictionary<string, string> Config);
public sealed record PageMappingEntry(
    int DeviceId,
    string DeviceName,
    int? RoomId,
    string RoomName,
    int? SourceId,
    string SourceName,
    int PageNumber,
    string PageName);
public sealed record RelayPortEntry(
    string ControllerDeviceName,
    string ExpanderDeviceType,
    string ExpanderName,
    string RelayName,
    string RelayType,
    string RelayMode);
public sealed record MpioIrPortEntry(
    string ControllerDeviceName,
    string ExpanderDeviceType,
    string ExpanderName,
    int PortNumber,
    string PortName);
public sealed record SensePortEntry(
    string ControllerDeviceName,
    string ExpanderDeviceType,
    string ExpanderName,
    int PortNumber,
    string PortName,
    string SenseModeState);
public sealed record TriggerPortEntry(
    string ControllerDeviceName,
    string ExpanderDeviceType,
    string ExpanderName,
    int TriggerNumber,
    string TriggerName);
public sealed record Rs232PortEntry(
    string ControllerDeviceName,
    string ExpanderDeviceType,
    string ExpanderName,
    int PortNumber,
    string PortName);
public sealed record RoomMappingEntry(
    int RoomId,
    string RoomName,
    int? SourceId,
    string SourceName,
    int? ControllerDeviceId,
    string ControllerDeviceName,
    int? PageId,
    string PageName);
public sealed record SourceCatalogEntry(
    int DeviceId,
    int RoomId,
    int ControlType,
    string SourceName,
    string SourceDisplayName);
public sealed record SystemManagerSourceCatalogEntry(
    int SourceIndex,
    string SourceName);
public sealed record DriverTemplateVariableEntry(
    int DriverDeviceId,
    string DriverDeviceName,
    string DriverDisplayName,
    string SysVarRef,
    string SysVarToken,
    string SourceDriverId,
    string SourceDriverName,
    string VariableCategory,
    string VariableName,
    string VariableType,
    string Format);

public static class ApexDiscoveryPreloadExtractor
{
    private static readonly Regex SysVarGuidPattern = new Regex("\\{[A-F0-9\\-]+\\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VariablePattern = new Regex("<variable\\s+name='([^']+)'\\s+sysvar='([^']+)'", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ZoneNamePattern = new Regex("^ZoneName(\\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SourceNamePattern = new Regex("^SourceName(\\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex InputNamePattern = new Regex("^input(\\d+)name$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex OutputNamePattern = new Regex("^Output(\\d+)name$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public static ApexDiscoveryPreloadResult Extract(string apexPath)
    {
        if (!File.Exists(apexPath))
        {
            LogStructuredEvent(
                SeverityLevel.Error,
                "Extract",
                "APEX file not found.",
                new Dictionary<string, string>
                {
                    ["path"] = apexPath,
                    ["error"] = "FileNotFoundException"
                });
            throw new FileNotFoundException("APEX file not found.", apexPath);
        }

        var result = new ApexDiscoveryPreloadResult();
        using var connection = new SqliteConnection($"Data Source={apexPath};Mode=ReadOnly;Pooling=False");
        connection.Open();

        LoadPageIndexMap(connection, result.PageIndexMap);
        LoadDriverConfigMap(connection, result.DriverConfigMap);
        LoadSysVarRefMap(connection, result.SysVarRefMap);
        LoadPageMappings(connection, result.PageMappings);
        LoadRelayPorts(connection, result.RelayPorts);
        LoadMpioIrPorts(connection, result.MpioIrPorts);
        LoadSensePorts(connection, result.SensePorts);
        LoadTriggerPorts(connection, result.TriggerPorts);
        LoadRs232Ports(connection, result.Rs232Ports);
        LoadRoomMappings(connection, result.RoomMappings);
        LoadSourceCatalog(connection, result.SourceCatalog);
        LoadSystemManagerSourceCatalog(connection, result.DriverConfigMap, result.SourceCatalog, result.SystemManagerSourceCatalog);
        LoadDriverTemplateVariables(connection, result.DriverTemplateVariables);

        LogStructuredEvent(
            SeverityLevel.Info,
            "Extract",
            "APEX discovery preload completed.",
            new Dictionary<string, string>
            {
                ["pageIndexMap"] = result.PageIndexMap.Count.ToString(CultureInfo.InvariantCulture),
                ["driverConfigMap"] = result.DriverConfigMap.Count.ToString(CultureInfo.InvariantCulture),
                ["sysVarRefMap"] = result.SysVarRefMap.Count.ToString(CultureInfo.InvariantCulture),
                ["pageMappings"] = result.PageMappings.Count.ToString(CultureInfo.InvariantCulture),
                ["relayPorts"] = result.RelayPorts.Count.ToString(CultureInfo.InvariantCulture),
                ["mpioIrPorts"] = result.MpioIrPorts.Count.ToString(CultureInfo.InvariantCulture),
                ["sensePorts"] = result.SensePorts.Count.ToString(CultureInfo.InvariantCulture),
                ["triggerPorts"] = result.TriggerPorts.Count.ToString(CultureInfo.InvariantCulture),
                ["rs232Ports"] = result.Rs232Ports.Count.ToString(CultureInfo.InvariantCulture),
                ["roomMappings"] = result.RoomMappings.Count.ToString(CultureInfo.InvariantCulture),
                ["sourceCatalog"] = result.SourceCatalog.Count.ToString(CultureInfo.InvariantCulture),
                ["systemManagerSourceCatalog"] = result.SystemManagerSourceCatalog.Count.ToString(CultureInfo.InvariantCulture),
                ["driverTemplateVariables"] = result.DriverTemplateVariables.Count.ToString(CultureInfo.InvariantCulture)
            });
        return result;
    }

    private static void LogStructuredEvent(
        SeverityLevel severity,
        string phase,
        string message,
        IReadOnlyDictionary<string, string>? details = null)
    {
        CentralLogger.LogEvent(new LogEntry(
            severity,
            CreateCorrelationId(),
            "ApexDiscoveryPreloadExtractor",
            phase,
            message,
            details));
    }

    private static string CreateCorrelationId()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    private static string BuildStructuredLogPath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Oracle by FP&C",
            "Logs");
        return Path.Combine(folder, "oracle-structured.log");
    }

    private static void LoadPageIndexMap(SqliteConnection connection, Dictionary<string, string> map)
    {
        if (DriverProfileCatalog.Internal().Count == 0)
        {
            return;
        }

        var hasCloneAddress = HasColumn(connection, "RTIDeviceData", "CloneRTIAddress");
        using var command = connection.CreateCommand();
        command.CommandText = hasCloneAddress
            ? """
SELECT
  d.DeviceId AS DeviceId,
  p.PageOrder AS PageIndex,
  n.PageName AS PageName
FROM RTIDeviceData d
JOIN Devices dv ON d.DeviceId = dv.DeviceId
LEFT JOIN RTIDevicePageData p
  ON p.RTIAddress = CASE
      WHEN d.CloneRTIAddress IS NOT NULL AND d.CloneRTIAddress > 0
        THEN d.CloneRTIAddress
      ELSE d.RTIAddress
    END
LEFT JOIN PageNames n ON p.PageNameId = n.PageNameId
ORDER BY d.DeviceId, p.PageOrder;
"""
            : """
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

    private static bool HasColumn(SqliteConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(1))
            {
                continue;
            }

            var name = reader.GetString(1);
            if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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

        var registry = DriverProfileRegistryFactory.CreateDefault();
        var matcher = new DriverProfileMatcher();
        foreach (var (driverDeviceId, configs) in configsByDriver)
        {
            driverDeviceIds.TryGetValue(driverDeviceId, out var deviceId);
            deviceNames.TryGetValue(deviceId, out var deviceName);
            var profile = matcher.Find(deviceName ?? "", registry);

            var limits = ExtractLimits(configs);
            var filtered = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (profile is null || (profile.DiscoveryKeys.Count == 0 && profile.DiscoveryPrefixes.Count == 0))
            {
                if (string.Equals(deviceName, "System Variable Events", StringComparison.OrdinalIgnoreCase)
                    || (deviceName?.StartsWith("System Variable Events", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    foreach (var entry in FilterSystemVariableEvents(configs))
                    {
                        filtered[entry.Name] = entry.Value;
                    }
                }
                else
                {
                    foreach (var (name, value) in configs)
                    {
                        if (ShouldIncludeConfig(name, limits))
                        {
                            filtered[name] = value;
                        }
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

    private static void LoadPageMappings(SqliteConnection connection, List<PageMappingEntry> entries)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
SELECT
  d.DeviceId AS device_id,
  d.Name AS device_name,
  sd.RoomId AS room_id,
  r.Name AS room_name,
  p.SourceDeviceId AS source_id,
  sd.Name AS source_name,
  (p.PageOrder + 1) AS page_number,
  n.PageName AS page_name
FROM RTIDeviceData rd
JOIN Devices d ON rd.DeviceId = d.DeviceId
JOIN RTIDevicePageData p ON p.RTIAddress = rd.RTIAddress
LEFT JOIN Devices sd ON p.SourceDeviceId = sd.DeviceId
LEFT JOIN Rooms r ON sd.RoomId = r.RoomId
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

            entries.Add(new PageMappingEntry(
                reader.GetInt32(0),
                reader.IsDBNull(1) ? "" : reader.GetString(1),
                reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                reader.IsDBNull(3) ? "" : reader.GetString(3),
                reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                reader.IsDBNull(5) ? "" : reader.GetString(5),
                reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                reader.IsDBNull(7) ? "" : reader.GetString(7)));
        }
    }

    private static void LoadRelayPorts(SqliteConnection connection, List<RelayPortEntry> entries)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
WITH relay_labels AS (
  SELECT
    (LabelKey >> 16) AS expander_id,
    LabelName AS relay_name
  FROM PortLabels
  WHERE RTIAddress = 0
    AND (
      LabelKey BETWEEN -64768 AND -64761
      OR LabelName LIKE 'Relay %'
    )
),
controller AS (
  SELECT d.Name AS controller_device_name
  FROM RTIDeviceData rd
  JOIN Devices d ON rd.DeviceId = d.DeviceId
  WHERE rd.RTIAddress = 0
)
SELECT
  c.controller_device_name,
  CASE
    WHEN r.expander_id = -1 THEN 'Internal'
    WHEN e.DeviceType = 5 THEN 'RCM-4'
    WHEN e.DeviceType = 3 THEN 'ESC-2'
    WHEN e.DeviceType = 6 THEN 'XP-6'
    ELSE CAST(e.DeviceType AS TEXT)
  END AS expander_device_type,
  CASE
    WHEN r.expander_id = -1 THEN 'Internal'
    ELSE e.Name
  END AS expander_name,
  r.relay_name,
  CASE
    WHEN r.expander_id = -1 THEN 'Contact Closure'
    WHEN r.expander_id = 1 THEN 'Unknown'
    ELSE 'N/A'
  END AS relay_type,
  CASE
    WHEN r.expander_id = -1 THEN 'Normally Open'
    WHEN r.expander_id = 1 THEN 'Unknown'
    ELSE 'N/A'
  END AS relay_mode
FROM relay_labels r
CROSS JOIN controller c
LEFT JOIN ExpansionDevices e
  ON e.RTIAddress = 0 AND e.ExpanderId = r.expander_id
ORDER BY expander_device_type, expander_name, r.relay_name;
""";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new RelayPortEntry(
                reader.IsDBNull(0) ? "" : reader.GetString(0),
                reader.IsDBNull(1) ? "" : reader.GetString(1),
                reader.IsDBNull(2) ? "" : reader.GetString(2),
                reader.IsDBNull(3) ? "" : reader.GetString(3),
                reader.IsDBNull(4) ? "" : reader.GetString(4),
                reader.IsDBNull(5) ? "" : reader.GetString(5)));
        }
    }

    private static void LoadMpioIrPorts(SqliteConnection connection, List<MpioIrPortEntry> entries)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
WITH mpio_labels AS (
  SELECT
    (LabelKey >> 16) AS expander_id,
    (LabelKey & 65535) AS port_key,
    LabelName AS port_name
  FROM PortLabels
  WHERE RTIAddress = 0
    AND (
      LabelKey BETWEEN -65536 AND -65529
      OR LabelKey BETWEEN 65536 AND 65543
    )
),
controller AS (
  SELECT d.Name AS controller_device_name
  FROM RTIDeviceData rd
  JOIN Devices d ON rd.DeviceId = d.DeviceId
  WHERE rd.RTIAddress = 0
)
SELECT
  c.controller_device_name,
  CASE
    WHEN m.expander_id = -1 THEN 'Internal'
    WHEN e.DeviceType = 6 THEN 'XP-6'
    ELSE CAST(e.DeviceType AS TEXT)
  END AS expander_device_type,
  CASE
    WHEN m.expander_id = -1 THEN 'Internal'
    ELSE e.Name
  END AS expander_name,
  (m.port_key % 256) + 1 AS port_number,
  m.port_name
FROM mpio_labels m
CROSS JOIN controller c
LEFT JOIN ExpansionDevices e
  ON e.RTIAddress = 0 AND e.ExpanderId = m.expander_id
ORDER BY expander_device_type, expander_name, port_number;
""";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new MpioIrPortEntry(
                reader.IsDBNull(0) ? "" : reader.GetString(0),
                reader.IsDBNull(1) ? "" : reader.GetString(1),
                reader.IsDBNull(2) ? "" : reader.GetString(2),
                reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                reader.IsDBNull(4) ? "" : reader.GetString(4)));
        }
    }

    private static void LoadSensePorts(SqliteConnection connection, List<SensePortEntry> entries)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
WITH sense_labels AS (
  SELECT
    (LabelKey >> 16) AS expander_id,
    (LabelKey & 65535) AS port_key,
    LabelName AS port_name
  FROM PortLabels
  WHERE RTIAddress = 0
    AND (
      LabelKey BETWEEN -65024 AND -65017
      OR LabelKey BETWEEN 66048 AND 66055
    )
),
controller AS (
  SELECT d.Name AS controller_device_name
  FROM RTIDeviceData rd
  JOIN Devices d ON rd.DeviceId = d.DeviceId
  WHERE rd.RTIAddress = 0
),
sense_mask AS (
  SELECT Mask AS sense_mode_mask
  FROM SenseModeMap
  WHERE RTIAddress = 0 AND ExpanderId = -1
)
SELECT
  c.controller_device_name,
  CASE
    WHEN s.expander_id = -1 THEN 'Internal'
    WHEN e.DeviceType = 6 THEN 'XP-6'
    ELSE CAST(e.DeviceType AS TEXT)
  END AS expander_device_type,
  CASE
    WHEN s.expander_id = -1 THEN 'Internal'
    ELSE e.Name
  END AS expander_name,
  (s.port_key - 512) + 1 AS port_number,
  s.port_name,
  CASE
    WHEN s.expander_id = -1 THEN
      CASE
        WHEN ((sm.sense_mode_mask >> ((s.port_key - 512))) & 1) = 1 THEN 'Sense Closure'
        ELSE 'Sense Voltage'
      END
    ELSE 'N/A'
  END AS sense_mode_state
FROM sense_labels s
CROSS JOIN controller c
LEFT JOIN ExpansionDevices e
  ON e.RTIAddress = 0 AND e.ExpanderId = s.expander_id
LEFT JOIN sense_mask sm
  ON 1 = 1
ORDER BY expander_device_type, expander_name, port_number;
""";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new SensePortEntry(
                reader.IsDBNull(0) ? "" : reader.GetString(0),
                reader.IsDBNull(1) ? "" : reader.GetString(1),
                reader.IsDBNull(2) ? "" : reader.GetString(2),
                reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                reader.IsDBNull(4) ? "" : reader.GetString(4),
                reader.IsDBNull(5) ? "" : reader.GetString(5)));
        }
    }

    private static void LoadTriggerPorts(SqliteConnection connection, List<TriggerPortEntry> entries)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
WITH trig_labels AS (
  SELECT
    (LabelKey >> 16) AS expander_id,
    (LabelKey & 65535) AS port_key,
    LabelName AS trigger_name
  FROM PortLabels
  WHERE RTIAddress = 0
    AND LabelKey BETWEEN 66307 AND 66309
),
controller AS (
  SELECT d.Name AS controller_device_name
  FROM RTIDeviceData rd
  JOIN Devices d ON rd.DeviceId = d.DeviceId
  WHERE rd.RTIAddress = 0
)
SELECT
  c.controller_device_name,
  CASE
    WHEN e.DeviceType = 6 THEN 'XP-6'
    ELSE CAST(e.DeviceType AS TEXT)
  END AS expander_device_type,
  e.Name AS expander_name,
  (t.port_key - 770) AS trigger_number,
  t.trigger_name
FROM trig_labels t
CROSS JOIN controller c
LEFT JOIN ExpansionDevices e
  ON e.RTIAddress = 0 AND e.ExpanderId = t.expander_id
ORDER BY expander_device_type, expander_name, trigger_number;
""";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new TriggerPortEntry(
                reader.IsDBNull(0) ? "" : reader.GetString(0),
                reader.IsDBNull(1) ? "" : reader.GetString(1),
                reader.IsDBNull(2) ? "" : reader.GetString(2),
                reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                reader.IsDBNull(4) ? "" : reader.GetString(4)));
        }
    }

    private static void LoadRs232Ports(SqliteConnection connection, List<Rs232PortEntry> entries)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
WITH rs_labels AS (
  SELECT
    (LabelKey >> 16) AS expander_id,
    (LabelKey & 65535) AS port_key,
    LabelName AS port_name
  FROM PortLabels
  WHERE RTIAddress = 0
    AND (
      LabelKey BETWEEN -65280 AND -65273
      OR LabelKey BETWEEN 65792 AND 65799
    )
),
controller AS (
  SELECT d.Name AS controller_device_name
  FROM RTIDeviceData rd
  JOIN Devices d ON rd.DeviceId = d.DeviceId
  WHERE rd.RTIAddress = 0
)
SELECT
  c.controller_device_name,
  CASE
    WHEN r.expander_id = -1 THEN 'Internal'
    WHEN e.DeviceType = 6 THEN 'XP-6'
    ELSE CAST(e.DeviceType AS TEXT)
  END AS expander_device_type,
  CASE
    WHEN r.expander_id = -1 THEN 'Internal'
    ELSE e.Name
  END AS expander_name,
  (r.port_key - 256) + 1 AS port_number,
  r.port_name
FROM rs_labels r
CROSS JOIN controller c
LEFT JOIN ExpansionDevices e
  ON e.RTIAddress = 0 AND e.ExpanderId = r.expander_id
ORDER BY expander_device_type, expander_name, port_number;
""";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new Rs232PortEntry(
                reader.IsDBNull(0) ? "" : reader.GetString(0),
                reader.IsDBNull(1) ? "" : reader.GetString(1),
                reader.IsDBNull(2) ? "" : reader.GetString(2),
                reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                reader.IsDBNull(4) ? "" : reader.GetString(4)));
        }
    }

    private static void LoadRoomMappings(SqliteConnection connection, List<RoomMappingEntry> entries)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
SELECT
  r.RoomId AS room_id,
  r.Name AS room_name,
  s.DeviceId AS source_id,
  s.Name AS source_name,
  dv.DeviceId AS controller_device_id,
  dv.Name AS controller_device_name,
  p.PageId AS page_id,
  n.PageName AS page_name
FROM Rooms r
LEFT JOIN Devices s
  ON s.RoomId = r.RoomId
LEFT JOIN RTIDevicePageData p
  ON p.SourceDeviceId = s.DeviceId
LEFT JOIN RTIDeviceData rd
  ON p.RTIAddress = rd.RTIAddress
LEFT JOIN Devices dv
  ON rd.DeviceId = dv.DeviceId
LEFT JOIN PageNames n
  ON p.PageNameId = n.PageNameId
ORDER BY r.RoomId, s.DeviceId, dv.DeviceId, p.PageId;
""";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            entries.Add(new RoomMappingEntry(
                reader.GetInt32(0),
                reader.IsDBNull(1) ? "" : reader.GetString(1),
                reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                reader.IsDBNull(3) ? "" : reader.GetString(3),
                reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                reader.IsDBNull(5) ? "" : reader.GetString(5),
                reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                reader.IsDBNull(7) ? "" : reader.GetString(7)));
        }
    }

    private static void LoadSourceCatalog(SqliteConnection connection, List<SourceCatalogEntry> entries)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
SELECT
  DeviceId,
  RoomId,
  ControlType,
  Name,
  DisplayName
FROM Devices
WHERE ControlType IN (5, 6)
ORDER BY DeviceId;
""";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            var sourceName = reader.IsDBNull(3) ? "" : reader.GetString(3);
            var sourceDisplayName = reader.IsDBNull(4) ? "" : reader.GetString(4);
            if (string.IsNullOrWhiteSpace(sourceDisplayName))
            {
                sourceDisplayName = sourceName;
            }

            entries.Add(new SourceCatalogEntry(
                reader.GetInt32(0),
                reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                sourceName,
                sourceDisplayName));
        }
    }

    private static void LoadSystemManagerSourceCatalog(
        SqliteConnection connection,
        Dictionary<int, DriverConfigEntry> driverConfigMap,
        IReadOnlyList<SourceCatalogEntry> sourceCatalog,
        List<SystemManagerSourceCatalogEntry> entries)
    {
        var deviceSources = sourceCatalog
            .OrderBy(entry => entry.DeviceId)
            .Select(entry => string.IsNullOrWhiteSpace(entry.SourceDisplayName) ? entry.SourceName : entry.SourceDisplayName)
            .ToList();

        var tokenCount = GetSystemManagerTokenCount(connection);
        if (tokenCount <= 0)
        {
            tokenCount = deviceSources.Count;
        }

        var requiredPrefixCount = Math.Max(0, tokenCount - deviceSources.Count);
        var prefixByIndex = ExtractSystemManagerPrefixSources(driverConfigMap, requiredPrefixCount);

        var ordered = new List<string>(tokenCount);
        for (var i = 0; i < requiredPrefixCount; i++)
        {
            ordered.Add(prefixByIndex.TryGetValue(i, out var value) ? value : "");
        }

        ordered.AddRange(deviceSources);
        if (ordered.Count > tokenCount)
        {
            ordered = ordered.Take(tokenCount).ToList();
        }

        while (ordered.Count < tokenCount)
        {
            ordered.Add("");
        }

        for (var i = 0; i < ordered.Count; i++)
        {
            entries.Add(new SystemManagerSourceCatalogEntry(i, ordered[i]));
        }
    }

    private static int GetSystemManagerTokenCount(SqliteConnection connection)
    {
        var maxIndex = -1;
        using var command = connection.CreateCommand();
        command.CommandText = """
SELECT SysVarRef
FROM SystemVariableIds
WHERE SysVarRef LIKE '{20186C86-446C-4FC6-89E1-1931718A169B}#%@SourceInUse%'
   OR SysVarRef LIKE '{20186C86-446C-4FC6-89E1-1931718A169B}#%@SourceName%';
""";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            var sysVarRef = reader.GetString(0);
            var atIndex = sysVarRef.IndexOf('@');
            if (atIndex < 0 || atIndex + 1 >= sysVarRef.Length)
            {
                continue;
            }

            var token = sysVarRef[(atIndex + 1)..];
            int parsedIndex;
            if (token.StartsWith("SourceInUse", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(token.Substring("SourceInUse".Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedIndex))
            {
                maxIndex = Math.Max(maxIndex, parsedIndex);
                continue;
            }

            if (token.StartsWith("SourceName", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(token.Substring("SourceName".Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedIndex))
            {
                maxIndex = Math.Max(maxIndex, parsedIndex);
            }
        }

        return maxIndex + 1;
    }

    private static Dictionary<int, string> ExtractSystemManagerPrefixSources(
        Dictionary<int, DriverConfigEntry> driverConfigMap,
        int requiredPrefixCount)
    {
        if (requiredPrefixCount <= 0)
        {
            return new Dictionary<int, string>();
        }

        var candidates = new List<(int DriverDeviceId, Dictionary<int, string> Values, int ContiguousCount, int TotalCount)>();
        foreach (var driver in driverConfigMap)
        {
            var rawIndexValues = new Dictionary<int, string>();
            foreach (var entry in driver.Value.Config)
            {
                var match = SourceNamePattern.Match(entry.Key);
                if (!match.Success)
                {
                    continue;
                }

                if (!int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedIndex))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Value))
                {
                    continue;
                }

                rawIndexValues[parsedIndex] = entry.Value;
            }

            if (rawIndexValues.Count == 0)
            {
                continue;
            }

            var zeroBasedDriverConfig = rawIndexValues.ContainsKey(0);
            var mappedValues = new Dictionary<int, string>();
            foreach (var entry in rawIndexValues)
            {
                var mappedIndex = zeroBasedDriverConfig ? entry.Key : entry.Key - 1;
                if (mappedIndex < 0)
                {
                    continue;
                }

                if (!mappedValues.ContainsKey(mappedIndex))
                {
                    mappedValues[mappedIndex] = entry.Value;
                }
            }

            if (mappedValues.Count == 0)
            {
                continue;
            }

            var contiguousCount = 0;
            while (mappedValues.ContainsKey(contiguousCount))
            {
                contiguousCount++;
            }

            candidates.Add((driver.Key, mappedValues, contiguousCount, mappedValues.Count));
        }

        if (candidates.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var selected = candidates
            .OrderByDescending(candidate => candidate.ContiguousCount)
            .ThenByDescending(candidate => candidate.TotalCount)
            .ThenBy(candidate => candidate.DriverDeviceId)
            .First();

        var result = new Dictionary<int, string>();
        for (var i = 0; i < requiredPrefixCount; i++)
        {
            if (selected.Values.TryGetValue(i, out var name))
            {
                result[i] = name;
            }
        }

        return result;
    }

    private static void LoadDriverTemplateVariables(SqliteConnection connection, List<DriverTemplateVariableEntry> entries)
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

        var driverLookup = new Dictionary<string, DriverVariableCatalog>(StringComparer.OrdinalIgnoreCase);
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT DriverDeviceId, DeviceId, DriverId, SystemVariables FROM DriverData ORDER BY DriverDeviceId";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1))
                {
                    continue;
                }

                var driverDeviceId = reader.GetInt32(0);
                var deviceId = reader.GetInt32(1);
                var driverId = reader.IsDBNull(2) ? "" : reader.GetString(2);
                if (string.IsNullOrWhiteSpace(driverId))
                {
                    continue;
                }

                var xml = reader.IsDBNull(3) ? "" : reader.GetString(3);
                var variables = ParseDriverVariables(xml);
                deviceNames.TryGetValue(deviceId, out var driverName);
                driverLookup[driverId] = new DriverVariableCatalog(driverDeviceId, deviceId, driverName ?? "", variables);
            }
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
SELECT
  d.DeviceId,
  d.Name,
  d.DisplayName,
  dd.DriverId,
  dd.DriverDeviceId,
  sv.SysVarRef
FROM Devices d
JOIN DriverData dd ON dd.DeviceId = d.DeviceId
JOIN SystemVariableIds sv ON sv.SysVarRef LIKE '%' || dd.DriverId || '%'
ORDER BY d.DeviceId, sv.SysVarID;
""";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(3) || reader.IsDBNull(4) || reader.IsDBNull(5))
                {
                    continue;
                }

                var deviceId = reader.GetInt32(0);
                var deviceName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                var displayName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = deviceName;
                }

                var driverId = reader.GetString(3);
                var driverDeviceId = reader.GetInt32(4);
                var sysVarRef = reader.GetString(5);
                if (string.IsNullOrWhiteSpace(sysVarRef))
                {
                    continue;
                }

                var sysVarToken = "";
                var atIndex = sysVarRef.IndexOf('@');
                if (atIndex >= 0 && atIndex + 1 < sysVarRef.Length)
                {
                    sysVarToken = sysVarRef[(atIndex + 1)..];
                }

                var sourceDriverId = "";
                var sourceDriverName = "";
                var driverIdMatch = SysVarGuidPattern.Match(sysVarRef);
                if (driverIdMatch.Success)
                {
                    sourceDriverId = driverIdMatch.Value;
                    if (driverLookup.TryGetValue(sourceDriverId, out var catalog))
                    {
                        sourceDriverName = catalog.DriverName;
                    }
                }

                var variableCategory = "";
                var variableName = "";
                var variableType = "";
                var format = "";
                if (!string.IsNullOrWhiteSpace(sysVarToken)
                    && driverLookup.TryGetValue(driverId, out var driverCatalog)
                    && driverCatalog.Variables.TryGetValue(sysVarToken, out var details))
                {
                    variableCategory = details.Category;
                    variableName = details.Name;
                    variableType = details.Type;
                    format = details.Format;
                }

                entries.Add(new DriverTemplateVariableEntry(
                    driverDeviceId,
                    deviceName,
                    displayName,
                    sysVarRef,
                    sysVarToken,
                    sourceDriverId,
                    sourceDriverName,
                    variableCategory,
                    variableName,
                    variableType,
                    format));
            }
        }
    }

    private static Dictionary<string, DriverVariableDetails> ParseDriverVariables(string xml)
    {
        var variables = new Dictionary<string, DriverVariableDetails>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(xml))
        {
            return variables;
        }

        XDocument? document = null;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (Exception)
        {
            return variables;
        }

        foreach (var variable in document.Descendants("variable"))
        {
            var sysvar = variable.Attribute("sysvar")?.Value ?? "";
            if (string.IsNullOrWhiteSpace(sysvar))
            {
                continue;
            }

            if (variables.ContainsKey(sysvar))
            {
                continue;
            }

            var name = variable.Attribute("name")?.Value ?? "";
            var type = variable.Attribute("type")?.Value ?? variable.Attribute("datatype")?.Value ?? "";
            var format = variable.Attribute("format")?.Value ?? "";
            var category = variable.Ancestors("category").FirstOrDefault()?.Attribute("name")?.Value ?? "";
            variables[sysvar] = new DriverVariableDetails(category, name, type, format);
        }

        return variables;
    }

    private sealed record DriverVariableCatalog(
        int DriverDeviceId,
        int DeviceId,
        string DriverName,
        Dictionary<string, DriverVariableDetails> Variables);

    private sealed record DriverVariableDetails(string Category, string Name, string Type, string Format);

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

    private static IEnumerable<(string Name, string Value)> FilterSystemVariableEvents(List<(string Name, string Value)> configs)
    {
        var booleanMacro = new Dictionary<int, string>();
        var integerMacro = new Dictionary<int, string>();
        foreach (var (name, value) in configs)
        {
            if (TryParseConfigKey(name, "Config_Boolean", "Macro", out var booleanIndex))
            {
                booleanMacro[booleanIndex] = value ?? "";
                continue;
            }

            if (TryParseConfigKey(name, "Config_Integer", "Macro", out var integerIndex))
            {
                integerMacro[integerIndex] = value ?? "";
            }
        }

        foreach (var (name, value) in configs)
        {
            if (string.Equals(name, "Config_PersistEnabledStates", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(value, "(not set)", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryParseConfigIndex(name, "Config_Boolean", out var booleanIndex))
            {
                if (!booleanMacro.TryGetValue(booleanIndex, out var macro) || string.IsNullOrWhiteSpace(macro))
                {
                    continue;
                }

                yield return (name, value);
                continue;
            }

            if (TryParseConfigIndex(name, "Config_Integer", out var integerIndex))
            {
                if (!integerMacro.TryGetValue(integerIndex, out var macro)
                    || string.IsNullOrWhiteSpace(macro)
                    || string.Equals(macro, "0", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return (name, value);
                continue;
            }

            yield return (name, value);
        }
    }

    private static bool TryParseConfigKey(string name, string prefix, string? suffix, out int index)
    {
        index = 0;
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = name.Substring(prefix.Length);
        if (string.IsNullOrEmpty(remainder))
        {
            return false;
        }

        var suffixIndex = remainder.Length;
        if (!string.IsNullOrEmpty(suffix))
        {
            if (!remainder.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            suffixIndex -= suffix.Length;
        }

        if (suffixIndex <= 0)
        {
            return false;
        }

        var numberText = remainder.Substring(0, suffixIndex);
        return int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
    }

    private static bool TryParseConfigIndex(string name, string prefix, out int index)
    {
        index = 0;
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = name.Substring(prefix.Length);
        if (string.IsNullOrEmpty(remainder))
        {
            return false;
        }

        var digitCount = 0;
        while (digitCount < remainder.Length && char.IsDigit(remainder[digitCount]))
        {
            digitCount++;
        }

        if (digitCount == 0)
        {
            return false;
        }

        var numberText = remainder.Substring(0, digitCount);
        return int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
    }
}
