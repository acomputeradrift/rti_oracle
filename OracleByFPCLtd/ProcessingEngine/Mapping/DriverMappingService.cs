using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProcessingEngine.Mapping;

public sealed class DriverMappingService
{
    public ProcessedLine Map(DiagnosticEvent evt, ProjectDataBundle bundle)
    {
        _ = bundle;
        return new ProcessedLine($"{evt.RawLineNumber} {evt.RawText}", false);
    }
}
