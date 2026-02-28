using OracleByFPCLtd.DriverProfiles;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class ActivitiesProfileResultMapperTests
{
    [Fact]
    public void ResultMapperReturnsPassThroughForPlainDriverEvent()
    {
        var result = ActivitiesProfile.ResultMapper!.TryMap(
            "Driver event 'Audio OFF in Room Three'",
            new ProjectDataBundle());

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.PassThrough, result.Status);
        Assert.Equal("Driver event 'Audio OFF in Room Three'", result.Text);
    }

    [Fact]
    public void ResultMapperReturnsNoProfileForAttributedDriverEvent()
    {
        var result = ActivitiesProfile.ResultMapper!.TryMap(
            "Driver event 'When something happens on 'System Manager\\Routing\\Set Source''",
            new ProjectDataBundle());

        Assert.False(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.NoProfile, result.Status);
    }
}
