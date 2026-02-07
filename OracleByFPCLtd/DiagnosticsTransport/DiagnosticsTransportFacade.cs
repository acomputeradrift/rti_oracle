using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OracleByFPCLtd.DiagnosticsTransport.Connection;
using OracleByFPCLtd.DiagnosticsTransport.Controls;
using OracleByFPCLtd.DiagnosticsTransport.Messaging;

namespace OracleByFPCLtd.DiagnosticsTransport;

public sealed class DiagnosticsTransportFacade : IDiagnosticsTransport
{
    private readonly IConnectionManager _connection;
    private readonly IMessageReceiver _receiver;
    private readonly ILogLevelController _logLevelController;
    private readonly ISysvarSubscriptionController _sysvarController;

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

        _connection.TransportInfo += (_, message) => TransportInfo?.Invoke(this, message);
        _connection.TransportError += (_, message) => TransportError?.Invoke(this, message);
        _receiver.RawMessageReceived += (_, message) => RawMessageReceived?.Invoke(this, message);
    }

    public event EventHandler<string>? RawMessageReceived;
    public event EventHandler<string>? TransportInfo;
    public event EventHandler<string>? TransportError;

    public bool IsConnected => _connection.IsConnected;

    public Task<List<string>> DiscoverAsync(TimeSpan timeout) => _connection.DiscoverAsync(timeout);

    public Task ConnectAsync(string ip) => _connection.ConnectAsync(ip);

    public Task DisconnectAsync() => _connection.DisconnectAsync();

    public Task SendLogLevelAsync(string type, string level) => _logLevelController.SendLogLevelAsync(type, level);

    public Task<List<DriverInfo>> LoadDriversAsync(string ip) => _connection.LoadDriversAsync(ip);
}
