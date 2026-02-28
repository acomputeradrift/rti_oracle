using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.DriverProfiles.Catalog;
using OracleByFPCLtd.DriverProfiles.Matching;
using OracleByFPCLtd.DriverProfiles.Models;

namespace OracleByFPCLtd.ProjectData;

public static class AdditionalInfoTemplatePlanner
{
    private const string RtiRcm12RelayModuleSheet = "RTI RCM-12 Relay Module";
    private const int Rcm12DeviceType = 7;
    private static readonly CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildEventLogFilePathHint()
    });

    public static IReadOnlyList<AdditionalInfoSheetSchema> DetermineSchemas(
        IEnumerable<DriverConfigEntry> drivers,
        IEnumerable<int>? expansionDeviceTypes = null,
        IEnumerable<RelayPortEntry>? relayPorts = null)
    {
        if (drivers is null)
        {
            WriteEventLogEntry(
                SeverityLevel.Warn,
                "DetermineSchemas",
                "Additional info schemas skipped; drivers missing.",
                new Dictionary<string, string> { ["error"] = "ArgumentNullException" });
            return Array.Empty<AdditionalInfoSheetSchema>();
        }

        var registry = new DriverProfileRegistry(DriverProfileCatalog.All());
        var matcher = new DriverProfileMatcher();
        var schemas = new List<AdditionalInfoSheetSchema>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var expansionTypes = expansionDeviceTypes == null
            ? new HashSet<int>()
            : new HashSet<int>(expansionDeviceTypes);
        var hasRcm12RelayPort = HasRcm12RelayPorts(relayPorts);

        foreach (var driver in drivers)
        {
            if (driver == null)
            {
                continue;
            }

            var profile = matcher.Find(driver.DeviceName, registry)
                ?? matcher.Find(driver.DeviceDisplayName, registry);
            if (profile?.AdditionalInfoSchemas == null || profile.AdditionalInfoSchemas.Count == 0)
            {
                continue;
            }

            foreach (var schema in profile.AdditionalInfoSchemas)
            {
                if (schema == null)
                {
                    continue;
                }

                if (!ShouldIncludeInternalSchema(schema.SheetName, expansionTypes, hasRcm12RelayPort))
                {
                    continue;
                }

                if (seen.Add(schema.SheetName))
                {
                    schemas.Add(schema);
                }
            }
        }

        // Internal profiles (for example RTI Internal) do not reliably surface via
        // DriverConfigMap but can still require Additional Info schemas.
        foreach (var profile in DriverProfileCatalog.Internal())
        {
            if (profile.AdditionalInfoSchemas == null || profile.AdditionalInfoSchemas.Count == 0)
            {
                continue;
            }

            foreach (var schema in profile.AdditionalInfoSchemas)
            {
                if (schema == null)
                {
                    continue;
                }

                if (!ShouldIncludeInternalSchema(schema.SheetName, expansionTypes, hasRcm12RelayPort))
                {
                    continue;
                }

                if (seen.Add(schema.SheetName))
                {
                    schemas.Add(schema);
                }
            }
        }

        WriteEventLogEntry(
            SeverityLevel.Info,
            "DetermineSchemas",
            "Additional info schemas determined.",
            new Dictionary<string, string> { ["count"] = schemas.Count.ToString() });
        return schemas;
    }

    private static bool ShouldIncludeInternalSchema(string sheetName, ISet<int> expansionTypes, bool hasRcm12RelayPort)
    {
        if (string.Equals(sheetName, RtiRcm12RelayModuleSheet, StringComparison.OrdinalIgnoreCase))
        {
            return expansionTypes.Contains(Rcm12DeviceType) || hasRcm12RelayPort;
        }

        return true;
    }

    private static bool HasRcm12RelayPorts(IEnumerable<RelayPortEntry>? relayPorts)
    {
        if (relayPorts is null)
        {
            return false;
        }

        foreach (var relayPort in relayPorts)
        {
            if (relayPort is null)
            {
                continue;
            }

            if (string.Equals(relayPort.ExpanderDeviceType, Rcm12DeviceType.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void WriteEventLogEntry(
        SeverityLevel severity,
        string phase,
        string message,
        IReadOnlyDictionary<string, string>? details = null)
    {
        CentralLogger.LogEvent(new LogEntry(
            severity,
            CreateCorrelationId(),
            "AdditionalInfoTemplatePlanner",
            phase,
            message,
            details));
    }

    private static string CreateCorrelationId()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    private static string BuildEventLogFilePathHint()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Oracle by FP&C",
            "Logs");
        return Path.Combine(folder, "oracle-structured.log");
    }
}

