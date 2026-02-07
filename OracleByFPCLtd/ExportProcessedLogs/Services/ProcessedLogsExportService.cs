using System;
using OracleByFPCLtd.ExportProcessedLogs.IO;
using OracleByFPCLtd.ExportProcessedLogs.Models;
using OracleByFPCLtd.ExportProcessedLogs.Rendering;

namespace OracleByFPCLtd.ExportProcessedLogs.Services;

public sealed class ProcessedLogsExportService
{
    private readonly IPdfRenderer _renderer;
    private readonly IExportFileWriter _writer;

    public ProcessedLogsExportService(IPdfRenderer renderer, IExportFileWriter writer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public void Export(ExportRequest request, string outputPath)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }

        var bytes = _renderer.Render(request);
        _writer.Write(outputPath, bytes);
    }
}
