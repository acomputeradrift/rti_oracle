using OracleByFPCLtd.ExportProcessedLogs.Rendering;
using OracleByFPCLtd.ProcessingEngine;
using PdfSharpCore.Drawing;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class PdfExportStylesTests
{
    [Fact]
    public void LogBackgroundIsBlack()
    {
        var color = PdfExportStyles.LogBackground;

        Assert.Equal((byte)0, color.R);
        Assert.Equal((byte)0, color.G);
        Assert.Equal((byte)0, color.B);
    }

    [Theory]
    [InlineData(ProcessedLineCategory.Connect, 0x39, 0xB5, 0x4A)]
    [InlineData(ProcessedLineCategory.Disconnect, 0xFF, 0x00, 0x00)]
    [InlineData(ProcessedLineCategory.DriverEvent, 0xFC, 0xB0, 0x40)]
    [InlineData(ProcessedLineCategory.DriverCommand, 0xFF, 0xFF, 0xFF)]
    [InlineData(ProcessedLineCategory.Button, 0xFF, 0xFF, 0x00)]
    [InlineData(ProcessedLineCategory.Page, 0x1E, 0x90, 0xFF)]
    [InlineData(ProcessedLineCategory.Macro, 0xA7, 0xA9, 0xAC)]
    [InlineData(ProcessedLineCategory.Default, 0x58, 0x58, 0x5A)]
    public void CategoryColorsMatchProcessedOutputPalette(ProcessedLineCategory category, byte red, byte green, byte blue)
    {
        var color = PdfExportStyles.GetCategoryColor(category);

        Assert.Equal(red, color.R);
        Assert.Equal(green, color.G);
        Assert.Equal(blue, color.B);
    }
}
