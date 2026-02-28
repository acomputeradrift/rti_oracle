using OracleByFPCLtd.DriverProfiles;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class RtiInternalProfileResultMapperTests
{
    [Fact]
    public void ResultMapperReturnsResolvedForMappedPage()
    {
        var result = RtiInternalProfile.ResultMapper!.TryMap(
            "[2026-01-24 10:00:00.000] Change to page 1 on device 'RTiPanel (iPhone X or newer)'",
            BuildBundle());

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.Resolved, result.Status);
        Assert.Contains("Change to page \"Room Select\"", result.Text);
        Assert.NotNull(result.MappingResolution);
        Assert.Equal("page", result.MappingResolution!.Kind);
        Assert.Equal("1", result.MappingResolution.MappedFrom);
        Assert.Equal("Room Select", result.MappingResolution.MappedTo);
        Assert.Equal("RTI Internal", result.MappingResolution.Profile);
        Assert.Equal("RTiPanel (iPhone X or newer)", result.MappingResolution.Device);
    }

    [Fact]
    public void ResultMapperReturnsUnresolvedWhenPageCannotResolve()
    {
        var bundle = BuildBundle();
        bundle.System.PageIndexMap.Clear();

        var result = RtiInternalProfile.ResultMapper!.TryMap(
            "[2026-01-24 10:00:00.000] Change to page 1 on device 'RTiPanel (iPhone X or newer)'",
            bundle);

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.Unresolved, result.Status);
        Assert.Equal("[2026-01-24 10:00:00.000] Change to page 1 on device 'RTiPanel (iPhone X or newer)'", result.Text);
        Assert.Null(result.MappingResolution);
    }

    [Fact]
    public void ResultMapperReturnsPassThroughForMacroEvent()
    {
        var result = RtiInternalProfile.ResultMapper!.TryMap(
            "Macro event",
            new ProjectDataBundle());

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.PassThrough, result.Status);
        Assert.Equal("Macro event", result.Text);
    }

    private static ProjectDataBundle BuildBundle()
    {
        var bundle = new ProjectDataBundle();
        bundle.System.DiagnosticsMapping.Add(new OracleByFPCLtd.ProjectData.DiagnosticsMappingEntry(
            81,
            "RTiPanel (iPhone X or newer)",
            "RTiPanel (iPhone X or newer)",
            0,
            0,
            0,
            0,
            "Room Select"));
        bundle.System.PageIndexMap["81|0"] = "Room Select";
        return bundle;
    }
}
