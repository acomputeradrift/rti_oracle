using System;

namespace OracleByFPCLtd.ExportProcessedLogs.Models;

public sealed record ExportMetadata(DateTime GeneratedAt, string ApexFileName, string? AdditionalDataName);
