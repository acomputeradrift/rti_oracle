using OracleByFPCLtd.DriverProfiles;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class SystemVariablesProfileResultMapperTests
{
    [Fact]
    public void ResultMapperReturnsResolvedForIntegerNameMapping()
    {
        var bundle = BuildBundle();

        var result = SystemVariablesProfile.ResultMapper!.TryMap(
            "[2026-02-21 11:00:01.000] Driver - Command:'System Variables\\Integers\\Increase(1, 1)' Sustain:NO",
            bundle);

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.Resolved, result.Status);
        Assert.Contains("Increase(Room Count, 1)", result.Text);
    }

    [Fact]
    public void ResultMapperReturnsNoMapWhenIntegerNameIsMissing()
    {
        var result = SystemVariablesProfile.ResultMapper!.TryMap(
            "[2026-02-22 10:58:57.950] Driver - Command:'System Variables\\Integers\\Increase(1, 1)' Sustain:NO",
            new ProjectDataBundle());

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.NoMap, result.Status);
        Assert.Equal("[2026-02-22 10:58:57.950] Driver - Command:'System Variables\\Integers\\Increase(1, 1)' Sustain:NO", result.Text);
    }

    [Fact]
    public void ResultMapperReturnsPassThroughForStringCommands()
    {
        var result = SystemVariablesProfile.ResultMapper!.TryMap(
            "[2026-02-22 11:13:50.469] Driver - Command:'System Variables\\Strings\\Set(1, Room)' Sustain:NO",
            new ProjectDataBundle());

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.PassThrough, result.Status);
        Assert.Equal("[2026-02-22 11:13:50.469] Driver - Command:'System Variables\\Strings\\Set(1, Room)' Sustain:NO", result.Text);
    }

    private static ProjectDataBundle BuildBundle()
    {
        var bundle = new ProjectDataBundle();
        var driverData = new AdditionalDriverData();
        driverData.IntegerNames[1] = "Room Count";
        bundle.Additional.Drivers["System Variables"] = driverData;
        return bundle;
    }
}
