using System.Threading.Tasks;
using OracleByFPCLtd.DiagnosticsTransport;

namespace OracleByFPCLtd.DiagnosticsTransport.Messaging;

public sealed class WebSocketMessageSender : IMessageSender
{
    private readonly LegacyWebSocketDiagnosticsTransport _inner;

    public WebSocketMessageSender(LegacyWebSocketDiagnosticsTransport inner)
    {
        _inner = inner;
    }

    public Task SendJsonAsync<T>(T payload) => _inner.SendJsonAsync(payload);
}
