using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProjectData.Extractors;

public static class AdditionalDataExtractor
{
    public static AdditionalData Extract(ProjectDataExtractionResult result)
    {
        return AdditionalData.FromExtractionResult(result);
    }
}
