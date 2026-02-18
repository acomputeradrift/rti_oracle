using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProcessingEngine.Mapping;

public sealed class AdditionalDataMappingService
{
    private static readonly CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public ProcessedLine Map(DiagnosticEvent evt, ProjectDataBundle bundle)
    {
        _ = bundle;
        if (evt is null)
        {
            LogStructuredEvent(
                SeverityLevel.Error,
                "Processing:Mapping",
                "Additional data mapping received null event.",
                new Dictionary<string, string> { ["error"] = "ArgumentNullException" });
            throw new System.ArgumentNullException(nameof(evt));
        }

        return new ProcessedLine($"{evt.RawLineNumber} {evt.RawText}", false);
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
            "AdditionalDataMappingService",
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
