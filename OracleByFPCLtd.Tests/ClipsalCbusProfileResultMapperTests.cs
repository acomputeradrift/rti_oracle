using OracleByFPCLtd.DriverProfiles;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class ClipsalCbusProfileResultMapperTests
{
    [Fact]
    public void ResultMapperReturnsResolvedForImmediateSwitch()
    {
        var result = ClipsalCbusProfile.ResultMapper!.TryMap(
            "Driver - Command:'Clipsal C-Bus\\General\\Immediate Switch(121, 78, 56)' Sustain:NO",
            BuildBundle());

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.Resolved, result.Status);
        Assert.Equal("Driver - Command:'Clipsal C-Bus\\General\\Immediate Switch(On, Living Room Pendant)' Sustain:NO", result.Text);
    }

    [Fact]
    public void ResultMapperReturnsNoMapForMissingGroup()
    {
        var bundle = BuildBundle();
        bundle.Additional.Drivers["Clipsal C-Bus"].CbusGroups.Clear();

        var result = ClipsalCbusProfile.ResultMapper!.TryMap(
            "Driver event 'When 'App 56, Group 25 On' happens on 'Clipsal C-Bus\\App 56 Group On''",
            bundle);

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.NoMap, result.Status);
        Assert.Contains("Group 25 [No Map!]", result.Text);
    }

    [Fact]
    public void ResultMapperReturnsUnknownStateForImmediateSwitchWithUnsupportedState()
    {
        var result = ClipsalCbusProfile.ResultMapper!.TryMap(
            "Driver - Command:'Clipsal C-Bus\\General\\Immediate Switch(7, 78, 56)' Sustain:NO",
            BuildBundle());

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.UnknownState, result.Status);
        Assert.Contains("Immediate Switch(7 [Unknown State!], Living Room Pendant)", result.Text);
    }

    private static ProjectDataBundle BuildBundle()
    {
        var bundle = new ProjectDataBundle();
        var driverData = new AdditionalDriverData();
        driverData.CbusGroups[(56, 78)] = new CbusGroupEntry("Living Room", "Pendant");
        bundle.Additional.Drivers["Clipsal C-Bus"] = driverData;
        return bundle;
    }
}
