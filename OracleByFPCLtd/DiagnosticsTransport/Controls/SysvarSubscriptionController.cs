using System.Threading.Tasks;
using OracleByFPCLtd.DiagnosticsTransport;

namespace OracleByFPCLtd.DiagnosticsTransport.Controls;

public sealed class SysvarSubscriptionController : ISysvarSubscriptionController
{
    private readonly LegacyWebSocketDiagnosticsTransport _inner;

    public SysvarSubscriptionController(LegacyWebSocketDiagnosticsTransport inner)
    {
        _inner = inner;
    }

    public Task SendSubscribeAsync(string resource, string value) => _inner.SendSubscribeAsync(resource, value);
}
