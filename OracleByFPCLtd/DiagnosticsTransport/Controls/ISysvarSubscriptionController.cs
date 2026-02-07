using System.Threading.Tasks;

namespace OracleByFPCLtd.DiagnosticsTransport.Controls;

public interface ISysvarSubscriptionController
{
    Task SendSubscribeAsync(string resource, string value);
}
