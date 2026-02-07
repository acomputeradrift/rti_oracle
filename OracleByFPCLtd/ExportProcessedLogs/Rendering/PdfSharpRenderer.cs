using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.ExportProcessedLogs.Builders;
using OracleByFPCLtd.ExportProcessedLogs.Models;
using OracleByFPCLtd.ProcessingEngine;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace OracleByFPCLtd.ExportProcessedLogs.Rendering;

public sealed class PdfSharpRenderer : IPdfRenderer
{
    private const double Margin = 40;
    private const double LineSpacing = 1;
    private const double LogPadding = 6;
    private const double LogoSize = 56;
    private const double HeaderGap = 10;
    private const double HeaderBlockSpacing = 2;
    private readonly HeaderBuilder _headerBuilder = new();
    private readonly FilterSummaryBuilder _filterSummaryBuilder = new();
    private readonly LogSectionBuilder _logSectionBuilder = new();

    public byte[] Render(ExportRequest request)
    {
        var headerLines = _headerBuilder.Build(request.Metadata);
        var filterLine = _filterSummaryBuilder.Build(request.FilterSummary);
        var logLines = _logSectionBuilder.Build(request);

        var document = new PdfDocument();
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var titleFont = new XFont("Segoe UI", 12, XFontStyle.Bold);
        var headerFont = new XFont("Segoe UI", 10, XFontStyle.Regular);
        var bodyFont = new XFont("Segoe UI", 8, XFontStyle.Regular);

        var y = Margin;
        var headerLeft = Margin;
        var headerTop = y;
        DrawLogo(gfx, headerLeft, headerTop);
        var headerTextLeft = headerLeft + LogoSize + HeaderGap;
        y = headerTop;
        DrawText(gfx, "Oracle by FP&C Ltd Export", titleFont, XBrushes.Black, headerTextLeft, y);
        y += GetLineHeight(gfx, titleFont) + HeaderBlockSpacing;
        y = DrawLines(gfx, headerLines, headerFont, XBrushes.Black, headerTextLeft, y, 1);
        y = DrawLines(gfx, new[] { filterLine }, headerFont, XBrushes.Black, headerTextLeft, y, 1);
        y = Math.Max(y, headerTop + LogoSize);
        y += 10;

        gfx = DrawLogSection(document, page, gfx, logLines, bodyFont, y);
        gfx.Dispose();
        DrawPageNumbers(document);

        using var stream = new MemoryStream();
        document.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    private static XGraphics DrawLogSection(PdfDocument document, PdfPage page, XGraphics gfx, IReadOnlyList<string> lines, XFont font, double startY)
    {
        var pageWidth = page.Width;
        var pageHeight = page.Height;
        var lineHeight = GetLineHeight(gfx, font);
        var logAreaTop = startY;
        var logAreaBottom = pageHeight - Margin;

        DrawLogBackground(gfx, pageWidth, logAreaTop, logAreaBottom);

        var y = logAreaTop + LogPadding;
        foreach (var line in lines)
        {
            if (y + lineHeight > logAreaBottom - LogPadding)
            {
                gfx.Dispose();
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                pageWidth = page.Width;
                pageHeight = page.Height;
                logAreaTop = Margin;
                logAreaBottom = pageHeight - Margin;
                DrawLogBackground(gfx, pageWidth, logAreaTop, logAreaBottom);
                y = logAreaTop + LogPadding;
            }

            var category = ProcessedLineClassifier.DetermineCategory(line);
            var color = PdfExportStyles.GetCategoryColor(category);
            DrawText(gfx, line, font, new XSolidBrush(color), Margin + LogPadding, y);
            y += lineHeight + LineSpacing;
        }

        return gfx;
    }

    private static void DrawLogBackground(XGraphics gfx, double pageWidth, double logAreaTop, double logAreaBottom)
    {
        var logHeight = logAreaBottom - logAreaTop;
        if (logHeight <= 0)
        {
            return;
        }

        gfx.DrawRectangle(new XSolidBrush(PdfExportStyles.LogBackground), Margin, logAreaTop, pageWidth - (Margin * 2), logHeight);
    }

    private static void DrawLogo(XGraphics gfx, double x, double y)
    {
        var logoBytes = EmbeddedLogo.LoadBytes();
        if (logoBytes == null || logoBytes.Length == 0)
        {
            return;
        }

        using var image = XImage.FromStream(() => new MemoryStream(logoBytes));
        gfx.DrawImage(image, x, y, LogoSize, LogoSize);
    }

    private static void DrawPageNumbers(PdfDocument document)
    {
        var font = new XFont("Segoe UI", 6, XFontStyle.Regular);
        for (var index = 0; index < document.Pages.Count; index++)
        {
            var page = document.Pages[index];
            using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            var label = $"Page {index + 1} of {document.Pages.Count}";
            var size = gfx.MeasureString(label, font);
            var x = page.Width - Margin - size.Width;
            var y = page.Height - Margin + 4;
            gfx.DrawString(label, font, XBrushes.Gray, new XPoint(x, y), XStringFormats.TopLeft);
        }
    }

    private static double DrawLines(XGraphics gfx, IEnumerable<string> lines, XFont font, XBrush brush, double x, double y, double lineSpacing)
    {
        var lineHeight = GetLineHeight(gfx, font);
        foreach (var line in lines)
        {
            DrawText(gfx, line, font, brush, x, y);
            y += lineHeight + lineSpacing;
        }

        return y;
    }

    private static void DrawText(XGraphics gfx, string text, XFont font, XBrush brush, double x, double y)
    {
        gfx.DrawString(text, font, brush, new XPoint(x, y), XStringFormats.TopLeft);
    }

    private static double GetLineHeight(XGraphics gfx, XFont font)
    {
        _ = gfx;
        return font.GetHeight();
    }

    private static class EmbeddedLogo
    {
        private const string LogoPath = "feeny-logo-100-circle-black-back.png";

        public static byte[]? LoadBytes()
        {
            var uri = new Uri($"pack://application:,,,/{LogoPath}", UriKind.Absolute);
            var streamInfo = System.Windows.Application.GetResourceStream(uri);
            if (streamInfo?.Stream == null)
            {
                return null;
            }

            using var stream = streamInfo.Stream;
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }
}
