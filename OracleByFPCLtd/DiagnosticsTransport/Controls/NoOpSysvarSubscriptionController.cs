using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OracleByFPCLtd.Logging;

namespace OracleByFPCLtd.DiagnosticsTransport.Controls;

public sealed class NoOpSysvarSubscriptionController : ISysvarSubscriptionController
{
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public Task SendSubscribeAsync(string resource, string value)
    {
        LogStructuredEvent(
            SeverityLevel.Info,
            "SendSubscribeAsync",
            "Sysvar subscribe ignored (noop).",
            new Dictionary<string, string>
            {
                ["resource"] = resource,
                ["value"] = value
            });
        return Task.CompletedTask;
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
            "NoOpSysvarSubscriptionController",
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
