using System;
using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.Logging;

namespace OracleByFPCLtd.ExportProcessedLogs.IO;

public sealed class ExportFileWriter : IExportFileWriter
{
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildEventLogFilePathHint()
    });

    public void Write(string outputPath, byte[] bytes)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            WriteEventLogEntry(
                SeverityLevel.Error,
                "Write",
                "Export file path missing.",
                new Dictionary<string, string> { ["error"] = "ArgumentException" });
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }

        if (bytes is null)
        {
            WriteEventLogEntry(
                SeverityLevel.Error,
                "Write",
                "Export data buffer missing.",
                new Dictionary<string, string> { ["error"] = "ArgumentNullException" });
            throw new ArgumentNullException(nameof(bytes));
        }

        File.WriteAllBytes(outputPath, bytes);
        WriteEventLogEntry(
            SeverityLevel.Info,
            "Write",
            "Export file write completed.",
            new Dictionary<string, string>
            {
                ["path"] = outputPath,
                ["bytes"] = bytes.Length.ToString()
            });
    }

    private void WriteEventLogEntry(
        SeverityLevel severity,
        string phase,
        string message,
        IReadOnlyDictionary<string, string>? details = null)
    {
        _centralLogger.LogEvent(new LogEntry(
            severity,
            CreateCorrelationId(),
            "ExportFileWriter",
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

