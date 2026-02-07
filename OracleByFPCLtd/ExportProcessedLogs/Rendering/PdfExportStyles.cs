using OracleByFPCLtd.ProcessingEngine;
using PdfSharpCore.Drawing;

namespace OracleByFPCLtd.ExportProcessedLogs.Rendering;

public static class PdfExportStyles
{
    public static XColor LogBackground => XColor.FromArgb(0, 0, 0);

    public static XColor GetCategoryColor(ProcessedLineCategory category)
    {
        return category switch
        {
            ProcessedLineCategory.Connect => XColor.FromArgb(0x39, 0xB5, 0x4A),
            ProcessedLineCategory.Disconnect => XColor.FromArgb(0xFF, 0x00, 0x00),
            ProcessedLineCategory.Button => XColor.FromArgb(0xFF, 0xFF, 0x00),
            ProcessedLineCategory.Page => XColor.FromArgb(0x1E, 0x90, 0xFF),
            ProcessedLineCategory.DriverEvent => XColor.FromArgb(0xFC, 0xB0, 0x40),
            ProcessedLineCategory.DriverCommand => XColor.FromArgb(0xFF, 0xFF, 0xFF),
            ProcessedLineCategory.Macro => XColor.FromArgb(0xA7, 0xA9, 0xAC),
            _ => XColor.FromArgb(0x58, 0x58, 0x5A)
        };
    }
}
