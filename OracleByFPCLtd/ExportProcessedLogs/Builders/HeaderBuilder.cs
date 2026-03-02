using System.Collections.Generic;
using OracleByFPCLtd.Formatting;
using OracleByFPCLtd.ExportProcessedLogs.Models;

namespace OracleByFPCLtd.ExportProcessedLogs.Builders;

public sealed class HeaderBuilder
{
    public IReadOnlyList<string> Build(ExportMetadata metadata)
    {
        var lines = new List<string>
        {
            $"Date: {DateTimeDisplayFormatter.FormatFilterDisplay(metadata.GeneratedAt.ToLocalTime())} (Local Time)",
            $"Apex File: {metadata.ApexFileName}"
        };

        var additional = string.IsNullOrWhiteSpace(metadata.AdditionalDataName)
            ? "Additional Info File: None"
            : $"Additional Info File: {metadata.AdditionalDataName}";
        lines.Add(additional);

        return lines;
    }
}
