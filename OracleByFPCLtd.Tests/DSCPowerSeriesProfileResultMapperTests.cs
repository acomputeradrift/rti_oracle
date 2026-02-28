using OracleByFPCLtd.DriverProfiles;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class DscPowerSeriesProfileResultMapperTests
{
    [Fact]
    public void ResultMapperReturnsPassThroughForDriverCommand()
    {
        var result = DscPowerSeriesProfile.ResultMapper!.TryMap(
            "[2026-02-23 07:36:52.423] Driver - Command:'DSC PowerSeries\\Keypad\\Number Keys(2)' Sustain:YES Rate:100",
            new ProjectDataBundle());

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.PassThrough, result.Status);
        Assert.Equal("[2026-02-23 07:36:52.423] Driver - Command:'DSC PowerSeries\\Keypad\\Number Keys(2)' Sustain:YES Rate:100", result.Text);
    }

    [Fact]
    public void ResultMapperReturnsPassThroughForDriverEvent()
    {
        var result = DscPowerSeriesProfile.ResultMapper!.TryMap(
            "[2026-02-21 10:12:44.112] Driver event 'When 'Garage West DOOR Opened' happens on 'DSC PowerSeries\\Zone Open''",
            new ProjectDataBundle());

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.PassThrough, result.Status);
        Assert.Equal("[2026-02-21 10:12:44.112] Driver event 'When 'Garage West DOOR Opened' happens on 'DSC PowerSeries\\Zone Open''", result.Text);
    }

    [Fact]
    public void ResultMapperReturnsNoProfileForNonDriverSenseEvent()
    {
        var result = DscPowerSeriesProfile.ResultMapper!.TryMap(
            "[2026-02-23 07:36:52.423] Sense event 'When 'Garage West DOOR Opened' happens on 'DSC PowerSeries\\Zone Open''",
            new ProjectDataBundle());

        Assert.False(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.NoProfile, result.Status);
    }
}
