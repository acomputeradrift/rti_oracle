using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OracleByFPCLtd.DiagnosticsTransport.Connection;
using OracleByFPCLtd.DiagnosticsTransport.Controls;
using OracleByFPCLtd.DiagnosticsTransport.Messaging;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.Reliability;

namespace OracleByFPCLtd.DiagnosticsTransport;

public sealed class DiagnosticsTransportFacade : IDiagnosticsTransport
{
    private readonly IConnectionManager _connection;
    private readonly IMessageReceiver _receiver;
    private readonly ILogLevelController _logLevelController;
    private readonly ISysvarSubscriptionController _sysvarController;
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public DiagnosticsTransportFacade(
        IConnectionManager connection,
        IMessageReceiver receiver,
        ILogLevelController logLevelController,
        ISysvarSubscriptionController sysvarController)
    {
        _connection = connection;
        _receiver = receiver;
        _logLevelController = logLevelController;
        _sysvarController = sysvarController;

        _connection.TransportInfo += (_, message) =>
        {
            if (!IsConnectedSuccessMessage(message))
            {
                LogStructuredEvent(SeverityLevel.Info, "TransportInfo", message);
            }
            TransportInfo?.Invoke(this, message);
        };
        _connection.TransportError += (_, message) =>
        {
            LogStructuredEvent(SeverityLevel.Error, "TransportError", message);
            TransportError?.Invoke(this, message);
        };
        _receiver.RawMessageReceived += (_, message) => RawMessageReceived?.Invoke(this, message);
        _logLevelController.OperationStateChanged += (_, operation) => OperationStateChanged?.Invoke(this, operation);
    }

    public event EventHandler<string>? RawMessageReceived;
    public event EventHandler<string>? TransportInfo;
    public event EventHandler<string>? TransportError;
    public event EventHandler<FeatureOperation>? OperationStateChanged;

    public bool IsConnected => _connection.IsConnected;

    public Task<List<string>> DiscoverAsync(TimeSpan timeout) => _connection.DiscoverAsync(timeout);

    public Task ConnectAsync(string ip) => _connection.ConnectAsync(ip);

    public Task DisconnectAsync() => _connection.DisconnectAsync();

    public Task<CommandDispatchResult> SendLogLevelCommandAsync(string type, string level, CancellationToken token = default)
        => _logLevelController.SendLogLevelCommandAsync(type, level, token);

    public Task SendLogLevelAsync(string type, string level) => _logLevelController.SendLogLevelAsync(type, level);

    public Task<List<DriverInfo>> LoadDriversAsync(string ip) => _connection.LoadDriversAsync(ip);

    private void LogStructuredEvent(SeverityLevel severity, string phase, string message)
    {
        var correlationId = CreateCorrelationId();
        _centralLogger.LogEvent(new LogEntry(
            severity,
            correlationId,
            "DiagnosticsTransportFacade",
            phase,
            message,
            new Dictionary<string, string> { ["message"] = message }));
    }

    private static bool IsConnectedSuccessMessage(string message)
    {
        return !string.IsNullOrWhiteSpace(message)
            && message.StartsWith("[success]", StringComparison.OrdinalIgnoreCase)
            && message.Contains("Connected to WebSocket", StringComparison.OrdinalIgnoreCase);
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
