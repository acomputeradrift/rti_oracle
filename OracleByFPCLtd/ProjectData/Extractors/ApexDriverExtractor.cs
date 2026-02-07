using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProjectData.Extractors;

public static class ApexDriverExtractor
{
    public static DriverData Extract(ProjectDataExtractionResult result)
    {
        return DriverData.FromExtractionResult(result);
    }
}
