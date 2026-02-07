using System.Collections.Generic;

namespace OracleByFPCLtd.ExportProcessedLogs.Models;

public sealed record ExportRequest(
    IReadOnlyList<string> Lines,
    ExportMetadata Metadata,
    FilterSummary FilterSummary);
