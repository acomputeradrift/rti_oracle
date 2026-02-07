using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProjectData.Extractors;

public static class ApexSystemExtractor
{
    public static SystemData Extract(ProjectDataExtractionResult result)
    {
        return SystemData.FromExtractionResult(result);
    }
}
