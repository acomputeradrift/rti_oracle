using OracleByFPCLtd.DriverProfiles;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class ClipsalCbusProfileImmediateSwitchStateTests
{
    [Fact]
    public void ImmediateSwitch_MapsState121ToOn()
    {
        var bundle = BuildBundle(appId: 48, groupId: 1, groupName: "Garage Door 1");
        var raw = "Driver - Command:'Clipsal C-Bus\\General\\Immediate Switch(121, 1, 48)' Sustain:NO";

        var mapped = ClipsalCbusProfile.Mapper.TryMap(raw, bundle, out var mappedText, out var unresolved);

        Assert.True(mapped);
        Assert.False(unresolved);
        Assert.Contains("Immediate Switch(On, Garage Door 1)", mappedText);
    }

    [Fact]
    public void ImmediateSwitch_MapsState255ToOn()
    {
        var bundle = BuildBundle(appId: 56, groupId: 29, groupName: "Garage Main 1");
        var raw = "Driver - Command:'Clipsal C-Bus\\General\\Immediate Switch(255, 29, 56)' Sustain:NO";

        var mapped = ClipsalCbusProfile.Mapper.TryMap(raw, bundle, out var mappedText, out var unresolved);

        Assert.True(mapped);
        Assert.False(unresolved);
        Assert.Contains("Immediate Switch(On, Garage Main 1)", mappedText);
    }

    private static ProjectDataBundle BuildBundle(int appId, int groupId, string groupName)
    {
        var bundle = new ProjectDataBundle();
        var driverData = new AdditionalDriverData();
        driverData.CbusGroups[(appId, groupId)] = new CbusGroupEntry("", groupName);
        bundle.Additional.Drivers[ClipsalCbusProfile.Definition.DeviceName] = driverData;
        return bundle;
    }
}
