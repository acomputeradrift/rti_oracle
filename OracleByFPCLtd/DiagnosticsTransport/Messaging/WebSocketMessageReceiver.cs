using System;
using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.DiagnosticsTransport;
using OracleByFPCLtd.Logging;

namespace OracleByFPCLtd.DiagnosticsTransport.Messaging;

public sealed class WebSocketMessageReceiver : IMessageReceiver
{
    private readonly LegacyWebSocketDiagnosticsTransport _inner;
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildEventLogFilePathHint()
    });

    public WebSocketMessageReceiver(LegacyWebSocketDiagnosticsTransport inner)
    {
        _inner = inner;
    }

    public event EventHandler<string>? RawMessageReceived
    {
        add
        {
            WriteEventLogEntry(
                SeverityLevel.Info,
                "RawMessageReceived",
                "WebSocket receiver subscribed.",
                new Dictionary<string, string> { ["action"] = "add" });
            _inner.RawMessageReceived += value;
        }
        remove
        {
            WriteEventLogEntry(
                SeverityLevel.Info,
                "RawMessageReceived",
                "WebSocket receiver unsubscribed.",
                new Dictionary<string, string> { ["action"] = "remove" });
            _inner.RawMessageReceived -= value;
        }
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
            "WebSocketMessageReceiver",
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

