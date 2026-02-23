using System.Windows.Media;
using OracleByFPCLtd.ProcessingEngine;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class ProcessedLineClassifierColorTests
{
    [Theory]
    [InlineData("Driver Command: volume up", ProcessedLineCategory.DriverCommand)]
    [InlineData("driver command: power on", ProcessedLineCategory.DriverCommand)]
    [InlineData("IR Command: SAT CH+", ProcessedLineCategory.DriverCommand)]
    [InlineData("ir command: Apple TV Play", ProcessedLineCategory.DriverCommand)]
    [InlineData("Driver Event: Trigger fired", ProcessedLineCategory.DriverEvent)]
    [InlineData("driver event: Zone changed", ProcessedLineCategory.DriverEvent)]
    [InlineData("Command: volume up", ProcessedLineCategory.Default)]
    [InlineData("Event: Trigger fired", ProcessedLineCategory.Default)]
    [InlineData("Button: Volume Up", ProcessedLineCategory.Button)]
    [InlineData("button: Power", ProcessedLineCategory.Button)]
    [InlineData("Page: Room Select", ProcessedLineCategory.Page)]
    [InlineData("page change", ProcessedLineCategory.Page)]
    [InlineData("System macro", ProcessedLineCategory.SystemMacro)]
    [InlineData("Stop macro", ProcessedLineCategory.SystemMacro)]
    public void DetermineCategoryUsesStrictDriverKeywords(string line, ProcessedLineCategory expected)
    {
        var category = ProcessedLineClassifier.DetermineCategory($"1 [2026-01-24 10:00] {line}");

        Assert.Equal(expected, category);
    }

    [Theory]
    [InlineData(ProcessedLineCategory.Connect, 0x39, 0xB5, 0x4A)]
    [InlineData(ProcessedLineCategory.Disconnect, 0xFF, 0x00, 0x00)]
    [InlineData(ProcessedLineCategory.DriverEvent, 0xFC, 0xB0, 0x40)]
    [InlineData(ProcessedLineCategory.DriverCommand, 0xFF, 0xFF, 0xFF)]
    [InlineData(ProcessedLineCategory.Button, 0xFF, 0xFF, 0x00)]
    [InlineData(ProcessedLineCategory.Page, 0x1E, 0x90, 0xFF)]
    [InlineData(ProcessedLineCategory.Macro, 0xA7, 0xA9, 0xAC)]
    [InlineData(ProcessedLineCategory.SystemMacro, 0x9E, 0x1E, 0x9E)]
    [InlineData(ProcessedLineCategory.Default, 0x58, 0x58, 0x5A)]
    public void CategoriesUseBrandColors(ProcessedLineCategory category, byte red, byte green, byte blue)
    {
        var brush = ProcessedLineClassifier.GetBrush(category) as SolidColorBrush;

        Assert.NotNull(brush);
        Assert.Equal(red, brush!.Color.R);
        Assert.Equal(green, brush.Color.G);
        Assert.Equal(blue, brush.Color.B);
    }
}
