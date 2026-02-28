using OracleByFPCLtd.DriverProfiles;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class VauxLattisMatrixProfileResultMapperTests
{
    [Fact]
    public void ResultMapperReturnsResolvedForSourceSelect()
    {
        var result = VauxLattisMatrixProfile.ResultMapper!.TryMap(
            "Driver - Command:'Vaux Lattis Matrix\\Output Settings\\Source Select(Route All, 13, 1)' Sustain:NO",
            BuildBundle());

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.Resolved, result.Status);
        Assert.Equal("Driver - Command:'Vaux Lattis Matrix\\Output Settings\\Source Select(Route All, Gym, Shaw 1)' Sustain:NO", result.Text);
    }

    [Fact]
    public void ResultMapperReturnsNoMapWhenAdditionalInfoIsMissing()
    {
        var result = VauxLattisMatrixProfile.ResultMapper!.TryMap(
            "[2026-02-10 19:00:39.485] Driver - Command:'Vaux Lattis Matrix\\Output Settings\\Source Select(Route All, 13, 1)' Sustain:NO",
            new ProjectDataBundle());

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.NoMap, result.Status);
        Assert.Equal("[2026-02-10 19:00:39.485] Driver - Command:'Vaux Lattis Matrix\\Output Settings\\Source Select(Route All, 13, 1)' Sustain:NO", result.Text);
    }

    [Fact]
    public void ResultMapperReturnsResolvedForOutputMute()
    {
        var result = VauxLattisMatrixProfile.ResultMapper!.TryMap(
            "[2026-02-11 14:00:01.000] Driver - Command:'Vaux Lattis Matrix\\Output Settings\\Output Mute(Toggle, 13)' Sustain:NO",
            BuildBundle());

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.Resolved, result.Status);
        Assert.Contains("Output Mute(Toggle, Gym)", result.Text);
    }

    private static ProjectDataBundle BuildBundle()
    {
        var bundle = new ProjectDataBundle();
        var driverData = new AdditionalDriverData();
        driverData.InputNames[1] = "Shaw 1";
        driverData.OutputNames[13] = "Gym";
        bundle.Additional.Drivers["Vaux Lattis Matrix"] = driverData;
        return bundle;
    }
}
