using System.Threading.Tasks;
using OracleByFPCLtd.DiagnosticsTransport;

namespace OracleByFPCLtd.DiagnosticsTransport.Controls;

public sealed class TcpCaptureLogLevelController : ILogLevelController
{
    private readonly TcpCaptureDiagnosticsTransport _inner;

    public TcpCaptureLogLevelController(TcpCaptureDiagnosticsTransport inner)
    {
        _inner = inner;
    }

    public Task SendLogLevelAsync(string type, string level) => _inner.SendLogLevelAsync(type, level);
}
