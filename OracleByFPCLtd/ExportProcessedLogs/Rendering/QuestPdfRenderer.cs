using System.Collections.Generic;
using OracleByFPCLtd.ExportProcessedLogs.Builders;
using OracleByFPCLtd.ExportProcessedLogs.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace OracleByFPCLtd.ExportProcessedLogs.Rendering;

public sealed class QuestPdfRenderer : IPdfRenderer
{
    private readonly HeaderBuilder _headerBuilder = new();
    private readonly LogSectionBuilder _logSectionBuilder = new();

    public byte[] Render(ExportRequest request)
    {
        var headerLines = _headerBuilder.Build(request.Metadata);
        var logLines = _logSectionBuilder.Build(request);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text("Processed Logs Export").SemiBold().FontSize(14);
                    AddLines(column, headerLines);
                    column.Item().LineHorizontal(1);
                    AddLines(column, logLines);
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void AddLines(ColumnDescriptor column, IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            column.Item().Text(line);
        }
    }
}
