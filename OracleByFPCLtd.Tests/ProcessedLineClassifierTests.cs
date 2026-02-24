using OracleByFPCLtd.ProcessingEngine;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class ProcessedLineClassifierTests
{
    [Theory]
    [InlineData("1 [2026-01-24 10:00:00.000] Device 'RTiPanel' has connected", ProcessedLineCategory.Connect)]
    [InlineData("1 [2026-01-24 10:00:00.000] Device 'RTiPanel' has disconnected", ProcessedLineCategory.Disconnect)]
    [InlineData("1 [2026-01-24 10:00:00.000] Driver Command (Foo): 'Bar'", ProcessedLineCategory.DriverCommand)]
    [InlineData("1 [2026-01-24 10:00:00.000] IR Command (Internal): 'Power -> XP-8v: Port 1'", ProcessedLineCategory.DriverCommand)]
    [InlineData("1 [2026-01-24 10:00:00.000] Relay/Trigger Command (Internal): 'OFF -> XP-8v: Garage Door West'", ProcessedLineCategory.DriverCommand)]
    [InlineData("1 [2026-01-24 10:00:00.000] Serial Command (Internal): 'POWER ON -> XP-8v: CP-1650 Zones 1-8'", ProcessedLineCategory.DriverCommand)]
    [InlineData("1 [2026-01-24 10:00:00.000] Macro - Start", ProcessedLineCategory.Macro)]
    [InlineData("1 [2026-01-24 10:00:00.000] Macro - End", ProcessedLineCategory.Macro)]
    [InlineData("1 [2026-01-24 10:00:00.000] System macro", ProcessedLineCategory.SystemMacro)]
    [InlineData("1 [2026-01-24 10:00:00.000] Stop macro", ProcessedLineCategory.SystemMacro)]
    [InlineData("1 [2026-01-24 10:00:00.000] Macro event", ProcessedLineCategory.DriverEvent)]
    [InlineData("1 [2026-01-24 10:00:00.000] Driver Event (Foo): 'Activity Ready.'", ProcessedLineCategory.DriverEvent)]
    [InlineData("1 [2026-01-24 10:00:00.000] Command sent to socket", ProcessedLineCategory.Default)]
    [InlineData("1 [2026-01-24 10:00:00.000] Event callback fired", ProcessedLineCategory.Default)]
    [InlineData("1 [2026-01-24 10:00:00.000] Something else", ProcessedLineCategory.Default)]
    public void ClassifiesLines(string line, ProcessedLineCategory expected)
    {
        var actual = ProcessedLineClassifier.DetermineCategory(line);

        Assert.Equal(expected, actual);
    }
}
