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
    [InlineData(ProcessedLineCategory.Connect, 50, 205, 50)]
    [InlineData(ProcessedLineCategory.Disconnect, 255, 0, 0)]
    [InlineData(ProcessedLineCategory.DriverCommand, 211, 211, 211)]
    [InlineData(ProcessedLineCategory.Macro, 255, 165, 0)]
    [InlineData(ProcessedLineCategory.DriverEvent, 255, 255, 0)]
    [InlineData(ProcessedLineCategory.Default, 255, 255, 255)]
    public void CategoryColorsMatchProcessedOutputPalette(ProcessedLineCategory category, byte red, byte green, byte blue)
    {
        var color = PdfExportStyles.GetCategoryColor(category);

        Assert.Equal(red, color.R);
        Assert.Equal(green, color.G);
        Assert.Equal(blue, color.B);
    }
}
