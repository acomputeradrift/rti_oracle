using System.Collections.Generic;
using OracleByFPCLtd.ExportProcessedLogs.Models;

namespace OracleByFPCLtd.ExportProcessedLogs.Builders;

public sealed class LogSectionBuilder
{
    public IReadOnlyList<string> Build(ExportRequest request)
    {
        var lines = new List<string>
        {
            $"Filters: keywords={request.FilterSummary.Keywords} start={request.FilterSummary.Start} end={request.FilterSummary.End}"
        };

        lines.AddRange(request.Lines);
        return lines;
    }
}
