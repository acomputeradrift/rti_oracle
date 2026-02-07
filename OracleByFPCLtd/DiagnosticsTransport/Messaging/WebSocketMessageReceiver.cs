using System;
using OracleByFPCLtd.DiagnosticsTransport;

namespace OracleByFPCLtd.DiagnosticsTransport.Messaging;

public sealed class WebSocketMessageReceiver : IMessageReceiver
{
    private readonly LegacyWebSocketDiagnosticsTransport _inner;

    public WebSocketMessageReceiver(LegacyWebSocketDiagnosticsTransport inner)
    {
        _inner = inner;
    }

    public event EventHandler<string>? RawMessageReceived
    {
        add => _inner.RawMessageReceived += value;
        remove => _inner.RawMessageReceived -= value;
    }
}
