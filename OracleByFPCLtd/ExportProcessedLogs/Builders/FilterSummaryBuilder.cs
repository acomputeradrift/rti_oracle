using OracleByFPCLtd.ExportProcessedLogs.Models;

namespace OracleByFPCLtd.ExportProcessedLogs.Builders;

public sealed class FilterSummaryBuilder
{
    public string Build(FilterSummary summary)
    {
        var keywords = FormatOrNone(summary.Keywords);
        var start = FormatOrNone(summary.Start);
        var end = FormatOrNone(summary.End);
        return $"Filter: Keywords = {keywords}, Start Date/Time = {start}, End Date/Time = {end}";
    }

    private static string FormatOrNone(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "None" : value.Trim();
    }
}
