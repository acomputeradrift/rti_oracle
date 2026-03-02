using System;
using System.Collections.Generic;
using OracleByFPCLtd.ExportProcessedLogs.Builders;
using OracleByFPCLtd.ExportProcessedLogs.IO;
using OracleByFPCLtd.ExportProcessedLogs.Models;
using OracleByFPCLtd.ExportProcessedLogs.Rendering;
using OracleByFPCLtd.ExportProcessedLogs.Services;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class ExportProcessedLogsTests
{
    [Fact]
    public void HeaderBuilderBuildsMetadataLines()
    {
        var metadata = new ExportMetadata(
            new DateTime(2026, 1, 24, 10, 30, 0, DateTimeKind.Local),
            "Project.apex",
            "Additional.xlsx");
        var builder = new HeaderBuilder();

        var lines = builder.Build(metadata);

        Assert.Contains("Date: 26-01-24 10:30 AM (Local Time)", lines);
        Assert.Contains("Apex File: Project.apex", lines);
        Assert.Contains("Additional Info File: Additional.xlsx", lines);
    }

    [Fact]
    public void LogSectionBuilderIncludesLines()
    {
        var filter = new FilterSummary("Driver", "2026-01-24 10:00", "2026-01-24 11:00");
        var request = new ExportRequest(
            new List<string> { "1 Line one", "2 Line two" },
            new ExportMetadata(DateTime.UnixEpoch, "Project.apex", null),
            filter);
        var builder = new LogSectionBuilder();

        var lines = builder.Build(request);

        Assert.Contains("1 Line one", lines);
        Assert.Contains("2 Line two", lines);
    }

    [Fact]
    public void FilterSummaryBuilderUsesNoneDefaults()
    {
        var summary = new FilterSummary("", "", "");
        var builder = new FilterSummaryBuilder();

        var line = builder.Build(summary);

        Assert.Equal("Filter: Keywords = None, Start Date/Time = None, End Date/Time = None", line);
    }

    [Fact]
    public void ExportServiceRendersAndWrites()
    {
        var request = new ExportRequest(
            new List<string> { "1 Line one" },
            new ExportMetadata(DateTime.UnixEpoch, "Project.apex", null),
            new FilterSummary("", "", ""));
        var renderer = new FakePdfRenderer();
        var writer = new FakeExportFileWriter();
        var service = new ProcessedLogsExportService(renderer, writer);

        service.Export(request, "output.pdf");

        Assert.Same(request, renderer.LastRequest);
        Assert.Equal("output.pdf", writer.LastPath);
        Assert.Equal(renderer.LastBytes, writer.LastBytes);
    }

    [Fact]
    public void PdfWrapRespectsWordBoundariesAndIndentation()
    {
        var indent = "  ";
        var line = indent + "This is a long line that should wrap on word boundaries.";

        var wrapped = PdfLineWrapper.Wrap(line, s => s.Length, maxWidth: 20);

        Assert.Equal(4, wrapped.Count);
        Assert.Equal(indent + "This is a long", wrapped[0]);
        Assert.Equal(indent + "line that should", wrapped[1]);
        Assert.Equal(indent + "wrap on word", wrapped[2]);
        Assert.Equal(indent + "boundaries.", wrapped[3]);
    }

    [Fact]
    public void PdfWrapAllowsShortLinesUnchanged()
    {
        var line = "Short line";

        var wrapped = PdfLineWrapper.Wrap(line, s => s.Length, maxWidth: 40);

        Assert.Single(wrapped);
        Assert.Equal("Short line", wrapped[0]);
    }

    private sealed class FakePdfRenderer : IPdfRenderer
    {
        public ExportRequest? LastRequest { get; private set; }
        public byte[] LastBytes { get; } = { 0x25, 0x50, 0x44, 0x46 };

        public byte[] Render(ExportRequest request)
        {
            LastRequest = request;
            return LastBytes;
        }
    }

    private sealed class FakeExportFileWriter : IExportFileWriter
    {
        public string? LastPath { get; private set; }
        public byte[]? LastBytes { get; private set; }

        public void Write(string outputPath, byte[] bytes)
        {
            LastPath = outputPath;
            LastBytes = bytes;
        }
    }
}
