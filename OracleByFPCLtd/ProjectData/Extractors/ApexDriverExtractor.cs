using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProjectData.Extractors;

public static class ApexDriverExtractor
{
    private static readonly CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildEventLogFilePathHint()
    });

    public static DriverData Extract(ProjectDataExtractionResult result)
    {
        if (result is null)
        {
            WriteEventLogEntry(
                SeverityLevel.Error,
                "Extract",
                "Driver data extraction received null result.",
                new Dictionary<string, string> { ["error"] = "ArgumentNullException" });
            throw new System.ArgumentNullException(nameof(result));
        }

        return DriverData.FromExtractionResult(result);
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
            "ApexDriverExtractor",
            phase,
            message,
            details));
    }

    private static string CreateCorrelationId()
    {
        return System.Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    private static string BuildEventLogFilePathHint()
    {
        var folder = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "Oracle by FP&C",
            "Logs");
        return Path.Combine(folder, "oracle-structured.log");
    }
}

