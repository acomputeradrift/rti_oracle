using System;
using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.ProcessingEngine.Models;

namespace OracleByFPCLtd.ProcessingEngine.Formatting;

public static class ProcessedLineFormatter
{
    private static readonly CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public static string Format(ProcessedLine line)
    {
        if (line is null)
        {
            LogStructuredEvent(
                SeverityLevel.Error,
                "Processing:Formatting",
                "Processed line formatter received null.",
                new Dictionary<string, string> { ["error"] = "ArgumentNullException" });
            throw new ArgumentNullException(nameof(line));
        }

        return line.Text;
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
            "ProcessedLineFormatter",
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
