using OracleByFPCLtd.ExportProcessedLogs.Models;

namespace OracleByFPCLtd.ExportProcessedLogs.Rendering;

public interface IPdfRenderer
{
    byte[] Render(ExportRequest request);
}
