using System.Threading.Tasks;

namespace OracleByFPCLtd.DiagnosticsTransport.Messaging;

public interface IMessageSender
{
    Task SendJsonAsync<T>(T payload);
}
