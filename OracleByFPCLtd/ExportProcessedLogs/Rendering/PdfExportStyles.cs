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
            ProcessedLineCategory.Connect => XColor.FromArgb(50, 205, 50),
            ProcessedLineCategory.Disconnect => XColor.FromArgb(255, 0, 0),
            ProcessedLineCategory.DriverCommand => XColor.FromArgb(211, 211, 211),
            ProcessedLineCategory.Macro => XColor.FromArgb(255, 165, 0),
            ProcessedLineCategory.DriverEvent => XColor.FromArgb(255, 255, 0),
            _ => XColor.FromArgb(255, 255, 255)
        };
    }
}
