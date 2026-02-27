using OracleByFPCLtd.ProcessingEngine.Mapping;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class RtiInternalProfileConnectivityTests
{
    [Fact]
    public void SendFailedNotConnected_LineIsHandledAsInternalPassthrough()
    {
        var service = new DriverMappingService();
        var bundle = new ProjectDataBundle();
        var evt = new DiagnosticEvent(
            1452,
            "[2026-02-26 15:43:12.804] 'Ensuite Tv ESC-2','Port 1' - Send failed, device not connected");

        var result = service.Map(evt, bundle);

        Assert.False(result.IsUnresolved);
        Assert.Equal(
            "1452 [2026-02-26 15:43:12.804] 'Ensuite Tv ESC-2','Port 1' - Send failed, device not connected",
            result.Text);
    }
}
