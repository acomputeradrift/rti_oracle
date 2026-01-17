using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SHPDiagnosticsViewer.DiagnosticsTransport;

public interface IDiagnosticsTransport
{
    event EventHandler<string>? RawMessageReceived;
    event EventHandler<string>? TransportInfo;
    event EventHandler<string>? TransportError;

    bool IsConnected { get; }

    Task<List<string>> DiscoverAsync(TimeSpan timeout);
    Task ConnectAsync(string ip);
    Task DisconnectAsync();
    Task SendLogLevelAsync(string type, string level);
    Task<List<DriverInfo>> LoadDriversAsync(string ip);
}
