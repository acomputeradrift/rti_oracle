using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OracleByFPCLtd.DiagnosticsTransport;
using OracleByFPCLtd.Logging;

namespace OracleByFPCLtd.DiagnosticsTransport.Messaging;

public sealed class WebSocketMessageSender : IMessageSender
{
    private readonly LegacyWebSocketDiagnosticsTransport _inner;
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildEventLogFilePathHint()
    });

    public WebSocketMessageSender(LegacyWebSocketDiagnosticsTransport inner)
    {
        _inner = inner;
    }

    public Task SendJsonAsync<T>(T payload)
    {
        WriteEventLogEntry(
            SeverityLevel.Info,
            "SendJsonAsync",
            "WebSocket message send requested.",
            new Dictionary<string, string> { ["payloadType"] = typeof(T).Name });
        return _inner.SendJsonAsync(payload);
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
            "WebSocketMessageSender",
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

