using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OracleByFPCLtd.DiagnosticsTransport;

namespace OracleByFPCLtd.DiagnosticsTransport.Connection;

public sealed class TcpCaptureConnectionManager : IConnectionManager
{
    private readonly TcpCaptureDiagnosticsTransport _inner;

    public TcpCaptureConnectionManager(TcpCaptureDiagnosticsTransport inner)
    {
        _inner = inner;
    }

    public event EventHandler<string>? TransportInfo
    {
        add => _inner.TransportInfo += value;
        remove => _inner.TransportInfo -= value;
    }

    public event EventHandler<string>? TransportError
    {
        add => _inner.TransportError += value;
        remove => _inner.TransportError -= value;
    }

    public bool IsConnected => _inner.IsConnected;

    public Task<List<string>> DiscoverAsync(TimeSpan timeout) => _inner.DiscoverAsync(timeout);

    public Task ConnectAsync(string ip) => _inner.ConnectAsync(ip);

    public Task DisconnectAsync() => _inner.DisconnectAsync();

    public Task<List<DriverInfo>> LoadDriversAsync(string ip) => _inner.LoadDriversAsync(ip);
}
