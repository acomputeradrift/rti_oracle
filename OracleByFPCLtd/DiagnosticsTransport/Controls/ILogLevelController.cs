using System;
using System.Threading;
using System.Threading.Tasks;
using OracleByFPCLtd.Reliability;

namespace OracleByFPCLtd.DiagnosticsTransport.Controls;

public interface ILogLevelController
{
    event EventHandler<FeatureOperation>? OperationStateChanged;
    Task<CommandDispatchResult> SendLogLevelCommandAsync(string type, string level, CancellationToken token = default);
    Task SendLogLevelAsync(string type, string level);
}
