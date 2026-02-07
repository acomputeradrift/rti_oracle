using System;

namespace OracleByFPCLtd.DiagnosticsTransport.Messaging;

public interface IMessageReceiver
{
    event EventHandler<string>? RawMessageReceived;
}
