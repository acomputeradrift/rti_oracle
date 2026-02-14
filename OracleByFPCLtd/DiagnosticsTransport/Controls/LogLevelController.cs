using System;
using System.Threading;
using System.Threading.Tasks;
using OracleByFPCLtd.DiagnosticsTransport;
using OracleByFPCLtd.Reliability;

namespace OracleByFPCLtd.DiagnosticsTransport.Controls;

public sealed class LogLevelController : ILogLevelController
{
    private readonly LegacyWebSocketDiagnosticsTransport _inner;

    public LogLevelController(LegacyWebSocketDiagnosticsTransport inner)
    {
        _inner = inner;
    }

    public event EventHandler<FeatureOperation>? OperationStateChanged
    {
        add => _inner.OperationStateChanged += value;
        remove => _inner.OperationStateChanged -= value;
    }

    public Task<CommandDispatchResult> SendLogLevelCommandAsync(string type, string level, CancellationToken token = default)
        => _inner.SendLogLevelCommandAsync(type, level, token);

    public Task SendLogLevelAsync(string type, string level) => _inner.SendLogLevelAsync(type, level);
}
