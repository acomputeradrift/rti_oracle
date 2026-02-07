using System.Collections.Generic;
using OracleByFPCLtd.ExportProcessedLogs.Models;

namespace OracleByFPCLtd.ExportProcessedLogs.Builders;

public sealed class LogSectionBuilder
{
    public IReadOnlyList<string> Build(ExportRequest request)
    {
        return new List<string>(request.Lines);
    }
}
