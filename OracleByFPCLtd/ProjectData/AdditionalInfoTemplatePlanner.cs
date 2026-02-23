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
    private static readonly CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public static IReadOnlyList<AdditionalInfoSheetSchema> DetermineSchemas(IEnumerable<DriverConfigEntry> drivers)
    {
        if (drivers is null)
        {
            LogStructuredEvent(
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

                if (seen.Add(schema.SheetName))
                {
                    schemas.Add(schema);
                }
            }
        }

        LogStructuredEvent(
            SeverityLevel.Info,
            "DetermineSchemas",
            "Additional info schemas determined.",
            new Dictionary<string, string> { ["count"] = schemas.Count.ToString() });
        return schemas;
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
            "AdditionalInfoTemplatePlanner",
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
}
