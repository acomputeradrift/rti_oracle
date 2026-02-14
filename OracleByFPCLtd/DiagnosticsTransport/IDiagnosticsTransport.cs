using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OracleByFPCLtd.Reliability;

namespace OracleByFPCLtd.DiagnosticsTransport;

public interface IDiagnosticsTransport
{
    event EventHandler<string>? RawMessageReceived;
    event EventHandler<string>? TransportInfo;
    event EventHandler<string>? TransportError;
    event EventHandler<FeatureOperation>? OperationStateChanged;

    bool IsConnected { get; }

    Task<List<string>> DiscoverAsync(TimeSpan timeout);
    Task ConnectAsync(string ip);
    Task DisconnectAsync();
    Task<CommandDispatchResult> SendLogLevelCommandAsync(string type, string level, CancellationToken token = default);
    Task SendLogLevelAsync(string type, string level);
    Task<List<DriverInfo>> LoadDriversAsync(string ip);
}
