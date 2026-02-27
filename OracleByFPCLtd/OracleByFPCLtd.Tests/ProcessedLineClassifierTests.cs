using OracleByFPCLtd.ProcessingEngine;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class ProcessedLineClassifierTests
{
    [Fact]
    public void DetermineCategory_ClassifiesNotConnectedAsDisconnect()
    {
        var line = "101 [2026-02-26 15:43:12.804] 'Ensuite Tv ESC-2','Port 1' - Send failed, device not connected";

        var category = ProcessedLineClassifier.DetermineCategory(line);

        Assert.Equal(ProcessedLineCategory.Disconnect, category);
    }

    [Fact]
    public void DetermineCategory_ClassifiesHasDisconnectedAsDisconnect()
    {
        var line = "102 [2026-02-26 15:43:15.100] Device 'iPhone (Sean)' has disconnected";

        var category = ProcessedLineClassifier.DetermineCategory(line);

        Assert.Equal(ProcessedLineCategory.Disconnect, category);
    }
}
