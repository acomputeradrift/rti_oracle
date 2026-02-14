using System.Threading.Tasks;
using OracleByFPCLtd.DiagnosticsTransport;
using OracleByFPCLtd.Reliability;
using Xunit;

namespace OracleByFPCLtd.Tests.DiagnosticsTransport;

public sealed class LegacyWebSocketDiagnosticsTransportTests
{
    [Fact]
    public async Task SendLogLevelCommandAsyncReturnsFailureWhenDisconnected()
    {
        var transport = new LegacyWebSocketDiagnosticsTransport();

        var result = await transport.SendLogLevelCommandAsync("DRIVER//1", "3");

        Assert.False(result.Dispatched);
        Assert.NotNull(result.Failure);
        Assert.Equal(FailureCodes.LogLevelDispatchFailed, result.Failure!.Code);
    }
}
