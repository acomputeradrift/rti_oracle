using System;
using OracleByFPCLtd.DiagnosticsTransport;

namespace OracleByFPCLtd.DiagnosticsTransport.Messaging;

public sealed class TcpCaptureMessageReceiver : IMessageReceiver
{
    private readonly TcpCaptureDiagnosticsTransport _inner;

    public TcpCaptureMessageReceiver(TcpCaptureDiagnosticsTransport inner)
    {
        _inner = inner;
    }

    public event EventHandler<string>? RawMessageReceived
    {
        add => _inner.RawMessageReceived += value;
        remove => _inner.RawMessageReceived -= value;
    }
}
