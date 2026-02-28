using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OracleByFPCLtd.DiagnosticsTransport;
using OracleByFPCLtd.Logging;

namespace OracleByFPCLtd.DiagnosticsTransport.Controls;

public sealed class SysvarSubscriptionController : ISysvarSubscriptionController
{
    private readonly LegacyWebSocketDiagnosticsTransport _inner;
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildEventLogFilePathHint()
    });

    public SysvarSubscriptionController(LegacyWebSocketDiagnosticsTransport inner)
    {
        _inner = inner;
    }

    public Task SendSubscribeAsync(string resource, string value)
    {
        WriteEventLogEntry(
            SeverityLevel.Info,
            "SendSubscribeAsync",
            "Sysvar subscribe requested.",
            new Dictionary<string, string>
            {
                ["resource"] = resource,
                ["value"] = value
            });
        return _inner.SendSubscribeAsync(resource, value);
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
            "SysvarSubscriptionController",
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

