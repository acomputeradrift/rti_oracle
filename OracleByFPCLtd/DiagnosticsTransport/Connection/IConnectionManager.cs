using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OracleByFPCLtd.DiagnosticsTransport.Connection;

public interface IConnectionManager
{
    event EventHandler<string>? TransportInfo;
    event EventHandler<string>? TransportError;

    bool IsConnected { get; }

    Task<List<string>> DiscoverAsync(TimeSpan timeout);
    Task ConnectAsync(string ip);
    Task DisconnectAsync();
    Task<List<DriverInfo>> LoadDriversAsync(string ip);
}
