using SHPDiagnosticsViewer.ProcessingEngine;
using Xunit;

namespace SHPDiagnosticsViewer.Tests;

public sealed class ProcessedLineClassifierTests
{
    [Theory]
    [InlineData("1 [2026-01-24 10:00:00.000] Device 'RTiPanel' has connected", ProcessedLineCategory.Connect)]
    [InlineData("1 [2026-01-24 10:00:00.000] Device 'RTiPanel' has disconnected", ProcessedLineCategory.Disconnect)]
    [InlineData("1 [2026-01-24 10:00:00.000] Driver - Command: 'Foo'", ProcessedLineCategory.DriverCommand)]
    [InlineData("1 [2026-01-24 10:00:00.000] Macro - Start", ProcessedLineCategory.Macro)]
    [InlineData("1 [2026-01-24 10:00:00.000] Macro - End", ProcessedLineCategory.Macro)]
    [InlineData("1 [2026-01-24 10:00:00.000] Driver event: Activity Ready", ProcessedLineCategory.DriverEvent)]
    [InlineData("1 [2026-01-24 10:00:00.000] Something else", ProcessedLineCategory.Default)]
    public void ClassifiesLines(string line, ProcessedLineCategory expected)
    {
        var actual = ProcessedLineClassifier.DetermineCategory(line);

        Assert.Equal(expected, actual);
    }
}
