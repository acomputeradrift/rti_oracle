using System.Collections.Generic;
using OracleByFPCLtd.ExportProcessedLogs.Models;

namespace OracleByFPCLtd.ExportProcessedLogs.Builders;

public sealed class HeaderBuilder
{
    public IReadOnlyList<string> Build(ExportMetadata metadata)
    {
        var lines = new List<string>
        {
            $"Date: {metadata.GeneratedAt:yyyy-MM-dd HH:mm}",
            $"Apex File: {metadata.ApexFileName}"
        };

        var additional = string.IsNullOrWhiteSpace(metadata.AdditionalDataName)
            ? "Additional Info File: None"
            : $"Additional Info File: {metadata.AdditionalDataName}";
        lines.Add(additional);

        return lines;
    }
}
