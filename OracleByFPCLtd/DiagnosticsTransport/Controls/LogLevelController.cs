using System.Threading.Tasks;
using OracleByFPCLtd.DiagnosticsTransport;

namespace OracleByFPCLtd.DiagnosticsTransport.Controls;

public sealed class LogLevelController : ILogLevelController
{
    private readonly LegacyWebSocketDiagnosticsTransport _inner;

    public LogLevelController(LegacyWebSocketDiagnosticsTransport inner)
    {
        _inner = inner;
    }

    public Task SendLogLevelAsync(string type, string level) => _inner.SendLogLevelAsync(type, level);
}
