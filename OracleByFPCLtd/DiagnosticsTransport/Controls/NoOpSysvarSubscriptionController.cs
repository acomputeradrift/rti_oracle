using System.Threading.Tasks;

namespace OracleByFPCLtd.DiagnosticsTransport.Controls;

public sealed class NoOpSysvarSubscriptionController : ISysvarSubscriptionController
{
    public Task SendSubscribeAsync(string resource, string value) => Task.CompletedTask;
}
