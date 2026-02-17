using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OracleByFPCLtd.DiagnosticsTransport;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.Reliability;

namespace OracleByFPCLtd.DiagnosticsTransport.Controls;

public sealed class TcpCaptureLogLevelController : ILogLevelController
{
    private readonly TcpCaptureDiagnosticsTransport _inner;
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public TcpCaptureLogLevelController(TcpCaptureDiagnosticsTransport inner)
    {
        _inner = inner;
    }

    public event EventHandler<FeatureOperation>? OperationStateChanged
    {
        add => _inner.OperationStateChanged += value;
        remove => _inner.OperationStateChanged -= value;
    }

    public Task<CommandDispatchResult> SendLogLevelCommandAsync(string type, string level, CancellationToken token = default)
    {
        LogStructuredEvent(
            SeverityLevel.Info,
            "SendLogLevelCommandAsync",
            "TCP capture log level command sent.",
            new Dictionary<string, string>
            {
                ["type"] = type,
                ["level"] = level
            });
        return _inner.SendLogLevelCommandAsync(type, level, token);
    }

    public Task SendLogLevelAsync(string type, string level)
    {
        LogStructuredEvent(
            SeverityLevel.Info,
            "SendLogLevelAsync",
            "TCP capture log level command sent.",
            new Dictionary<string, string>
            {
                ["type"] = type,
                ["level"] = level
            });
        return _inner.SendLogLevelAsync(type, level);
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
            "TcpCaptureLogLevelController",
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
