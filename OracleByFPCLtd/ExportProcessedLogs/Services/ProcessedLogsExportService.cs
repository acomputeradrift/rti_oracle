using System;
using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.ExportProcessedLogs.IO;
using OracleByFPCLtd.ExportProcessedLogs.Models;
using OracleByFPCLtd.ExportProcessedLogs.Rendering;

namespace OracleByFPCLtd.ExportProcessedLogs.Services;

public sealed class ProcessedLogsExportService
{
    private readonly IPdfRenderer _renderer;
    private readonly IExportFileWriter _writer;
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public ProcessedLogsExportService(IPdfRenderer renderer, IExportFileWriter writer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public void Export(ExportRequest request, string outputPath)
    {
        if (request is null)
        {
            LogStructuredEvent(
                SeverityLevel.Error,
                "Export",
                "Processed logs export request is null.",
                new Dictionary<string, string> { ["error"] = "ArgumentNullException" });
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            LogStructuredEvent(
                SeverityLevel.Error,
                "Export",
                "Processed logs export output path missing.",
                new Dictionary<string, string> { ["error"] = "ArgumentException" });
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }

        var bytes = _renderer.Render(request);
        _writer.Write(outputPath, bytes);
        LogStructuredEvent(
            SeverityLevel.Info,
            "Export",
            "Processed logs export completed.",
            new Dictionary<string, string>
            {
                ["path"] = outputPath,
                ["bytes"] = bytes.Length.ToString()
            });
    }

    private void LogStructuredEvent(
        SeverityLevel severity,
        string phase,
        string message,
        IReadOnlyDictionary<string, string>? details = null)
    {
        _centralLogger.LogEvent(new LogEntry(
            severity,
            CreateCorrelationId(),
            "ProcessedLogsExportService",
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
