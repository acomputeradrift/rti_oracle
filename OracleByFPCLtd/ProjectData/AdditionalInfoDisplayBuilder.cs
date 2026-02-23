using System.Collections.Generic;
using System.IO;
using System.Linq;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProjectData;

public static class AdditionalInfoDisplayBuilder
{
    private static readonly CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public static IEnumerable<AdditionalInfoDisplayEntry> Build(AdditionalData data)
    {
        if (data is null)
        {
            LogStructuredEvent(
                SeverityLevel.Warn,
                "Build",
                "Additional info display data missing.",
                new Dictionary<string, string> { ["error"] = "ArgumentNullException" });
            yield break;
        }

        foreach (var driver in data.Drivers.OrderBy(entry => entry.Key))
        {
            if (driver.Value.PreviewEntries.Count > 0)
            {
                foreach (var entry in driver.Value.PreviewEntries
                    .OrderBy(entry => entry.MapType, System.StringComparer.Ordinal)
                    .ThenBy(entry => entry.Index)
                    .ThenBy(entry => entry.Name, System.StringComparer.Ordinal))
                {
                    yield return new AdditionalInfoDisplayEntry(driver.Key, entry.MapType, entry.Index, entry.Name);
                }

                continue;
            }

            foreach (var entry in driver.Value.InputNames.OrderBy(entry => entry.Key))
            {
                yield return new AdditionalInfoDisplayEntry(driver.Key, "Input", entry.Key, entry.Value);
            }

            foreach (var entry in driver.Value.OutputNames.OrderBy(entry => entry.Key))
            {
                yield return new AdditionalInfoDisplayEntry(driver.Key, "Output", entry.Key, entry.Value);
            }

            foreach (var entry in driver.Value.IntegerNames.OrderBy(entry => entry.Key))
            {
                yield return new AdditionalInfoDisplayEntry(driver.Key, "Integer", entry.Key, entry.Value);
            }

            foreach (var entry in driver.Value.RelayNames.OrderBy(entry => entry.Key))
            {
                yield return new AdditionalInfoDisplayEntry(driver.Key, "Relay", entry.Key, entry.Value);
            }

            foreach (var entry in driver.Value.CbusGroups.OrderBy(entry => entry.Key.GroupId))
            {
                var name = FormatCbusGroup(entry.Value);
                yield return new AdditionalInfoDisplayEntry(driver.Key, "C-Bus Group", entry.Key.GroupId, name);
            }

            foreach (var entry in driver.Value.CbusHvacZones.OrderBy(entry => entry.Key.ZoneId))
            {
                var name = FormatCbusHvac(entry.Value);
                yield return new AdditionalInfoDisplayEntry(driver.Key, "C-Bus HVAC", entry.Key.ZoneId, name);
            }

            foreach (var entry in driver.Value.CbusScenes.OrderBy(entry => entry.Key.GroupId))
            {
                var name = entry.Value.SceneName;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    yield return new AdditionalInfoDisplayEntry(driver.Key, "C-Bus Scene", entry.Key.GroupId, name);
                }
            }
        }
    }

    private static string FormatCbusGroup(CbusGroupEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.GroupRoom))
        {
            return entry.GroupName;
        }

        if (string.IsNullOrWhiteSpace(entry.GroupName))
        {
            return entry.GroupRoom;
        }

        return $"{entry.GroupRoom} {entry.GroupName}";
    }

    private static string FormatCbusHvac(CbusHvacEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.GroupName))
        {
            return entry.ZoneName;
        }

        if (string.IsNullOrWhiteSpace(entry.ZoneName))
        {
            return entry.GroupName;
        }

        return $"{entry.GroupName} {entry.ZoneName}";
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
            "AdditionalInfoDisplayBuilder",
            phase,
            message,
            details));
    }

    private static string CreateCorrelationId()
    {
        return System.Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    private static string BuildStructuredLogPath()
    {
        var folder = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "Oracle by FP&C",
            "Logs");
        return Path.Combine(folder, "oracle-structured.log");
    }
}

public sealed record AdditionalInfoDisplayEntry(string DriverName, string MapType, int Index, string Name);
