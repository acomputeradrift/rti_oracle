using System.Collections.Generic;
using OracleByFPCLtd.DriverProfiles;
using OracleByFPCLtd.ProcessingEngine.Mapping;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProcessingEngine.Parsing;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class ProcessingEngineMappingTests
{
    [Fact]
    public void RawLogParserParsesNumberedLine()
    {
        var line = "7 [2026-01-24 10:00:00.000] Change to page 1 on device 'RTiPanel (iPhone X or newer)'";

        var parsed = RawLogParser.TryParseNumberedLine(line, out var evt);

        Assert.True(parsed);
        Assert.Equal(7, evt.RawLineNumber);
        Assert.Equal("[2026-01-24 10:00:00.000] Change to page 1 on device 'RTiPanel (iPhone X or newer)'", evt.RawText);
    }

    [Fact]
    public void RawLogParserRejectsNonNumberedLine()
    {
        var parsed = RawLogParser.TryParseNumberedLine("no line number", out _);

        Assert.False(parsed);
    }

    [Fact]
    public void SystemMappingServiceMapsPageName()
    {
        var bundle = BuildBundle();
        var service = new SystemMappingService();
        var evt = new DiagnosticEvent(7, "[2026-01-24 10:00:00.000] Change to page 1 on device 'RTiPanel (iPhone X or newer)'");

        var line = service.Map(evt, bundle);

        Assert.Equal("7 [2026-01-24 10:00:00.000] Change to page \"Room Select\" on device 'RTiPanel (iPhone X or newer)'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void SystemMappingServiceMarksUnresolvedMappings()
    {
        var bundle = BuildBundle();
        bundle.System.PageIndexMap.Clear();
        var service = new SystemMappingService();
        var evt = new DiagnosticEvent(7, "[2026-01-24 10:00:00.000] Change to page 1 on device 'RTiPanel (iPhone X or newer)'");

        var line = service.Map(evt, bundle);

        Assert.Contains("[UNRESOLVED]", line.Text);
        Assert.True(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceMapsVauxSourceSelect()
    {
        var bundle = BuildBundleWithVaux();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(5, "Driver - Command:'Vaux Lattis Matrix\\Output Settings\\Source Select(Route All, 13, 1)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("5 Driver - Command:'Vaux Lattis Matrix\\Output Settings\\Source Select(Route All, Gym, Shaw 1)' Sustain:NO", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void VauxProfileMapsSourceSelect()
    {
        var bundle = BuildBundleWithVaux();
        var mapper = VauxLattisMatrixProfile.Mapper;
        var rawText = "Driver - Command:'Vaux Lattis Matrix\\Output Settings\\Source Select(Route All, 13, 1)' Sustain:NO";

        var mapped = mapper.TryMap(rawText, bundle, out var mappedText, out var unresolved);

        Assert.True(mapped);
        Assert.Equal("Driver - Command:'Vaux Lattis Matrix\\Output Settings\\Source Select(Route All, Gym, Shaw 1)' Sustain:NO", mappedText);
        Assert.False(unresolved);
    }

    [Theory]
    [InlineData("Output Mute(Toggle, 13)", "Output Mute(Toggle, Gym)")]
    [InlineData("Output Off(13)", "Output Off(Gym)")]
    [InlineData("Volume Up(13)", "Volume Up(Gym)")]
    public void DriverMappingServiceMapsVauxOutputIndex(string input, string expected)
    {
        var bundle = BuildBundleWithVaux();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(3, $"Driver - Command:'Vaux Lattis Matrix\\Output Settings\\{input}' Sustain:YES");

        var line = service.Map(evt, bundle);

        Assert.Equal($"3 Driver - Command:'Vaux Lattis Matrix\\Output Settings\\{expected}' Sustain:YES", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceMapsCbusImmediateSwitchUnknownState()
    {
        var bundle = BuildBundleWithCbus();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(4, "Driver - Command:'Clipsal C-Bus\\General\\Immediate Switch(121, 78, 56)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("4 Driver - Command:'Clipsal C-Bus\\General\\Immediate Switch(121 [Unknown State!], Living Room Pendant, 56)' Sustain:NO", line.Text);
        Assert.True(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceMarksCbusMissingMap()
    {
        var bundle = BuildBundleWithCbus();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(2, "Driver event 'When 'App 56, Group 25 On' happens on 'Clipsal C-Bus\\App 56 Group On''");

        bundle.Additional.Drivers["Clipsal C-Bus"].CbusGroups.Clear();
        var line = service.Map(evt, bundle);

        Assert.Equal("2 Driver event 'When 'App 56, Group 25 [No Map!] On' happens on 'Clipsal C-Bus\\App 56 Group On''", line.Text);
        Assert.True(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceMapsCbusHvacSetpointUpUnknownState()
    {
        var bundle = BuildBundleWithCbus();
        bundle.Additional.Drivers["Clipsal C-Bus"].CbusHvacZones[(1, 0)] = new CbusHvacEntry("Garage", "Unswitched");
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(6, "Driver - Command:'Clipsal C-Bus\\HVAC\\HVAC Zone Setpoint Up(1, Unswitched (0))' Sustain:NO  Sent to 'WorkShop Slave'");

        var line = service.Map(evt, bundle);

        Assert.Equal("6 Driver - Command:'Clipsal C-Bus\\HVAC\\HVAC Zone Setpoint Up(Garage, Unswitched (0 [Unknown State!]))' Sustain:NO  Sent to 'WorkShop Slave'", line.Text);
        Assert.True(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceMapsCbusRampToLevelScene()
    {
        var bundle = BuildBundleWithCbus();
        bundle.Additional.Drivers["Clipsal C-Bus"].CbusScenes[(202, 0, 33)] = new CbusSceneEntry("Lower Floor On");
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(9, "Driver - Command:'Clipsal C-Bus\\General\\Ramp to level(10, 0, 33, 202)' Sustain:YES");

        var line = service.Map(evt, bundle);

        Assert.Equal("9 Driver - Command:'Clipsal C-Bus\\General\\Ramp to level(4 seconds, Lower Floor On)' Sustain:YES", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceMarksNoProfile()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(1, "Driver - Command:'Some Driver\\General\\Action(1)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("1 Driver - Command:'Some Driver\\General\\Action(1)' Sustain:NO [No Profile!]", line.Text);
        Assert.True(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceTreatsSystemManagerAsProfile()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(11, "Driver - Command:'System Manager\\Layer Visibility\\Set Layer Visibility(Source List)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("11 Driver - Command:'System Manager\\Layer Visibility\\Set Layer Visibility(Source List)' Sustain:NO", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceTreatsRtiVirtualMultiroomAmpAsProfile()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(12, "Driver - Command:'RTI Virtual Multiroom Amp\\Room Three\\Power(Off)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("12 Driver - Command:'RTI Virtual Multiroom Amp\\Room Three\\Power(Off)' Sustain:NO", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceTreatsActivitiesDriverEventsAsProfile()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(13, "Driver event 'Audio OFF in Room Three'");

        var line = service.Map(evt, bundle);

        Assert.Equal("13 Driver event 'Audio OFF in Room Three'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    private static ProjectDataBundle BuildBundle()
    {
        var bundle = new ProjectDataBundle();
        bundle.System.DiagnosticsMapping.Add(new OracleByFPCLtd.ProjectData.DiagnosticsMappingEntry(
            81,
            "RTiPanel (iPhone X or newer)",
            0,
            0,
            0,
            0,
            "Room Select"));
        bundle.System.PageIndexMap["81|0"] = "Room Select";
        return bundle;
    }

    private static ProjectDataBundle BuildBundleWithVaux()
    {
        var bundle = new ProjectDataBundle();
        var driverData = new AdditionalDriverData();
        driverData.InputNames[1] = "Shaw 1";
        driverData.OutputNames[13] = "Gym";
        bundle.Additional.Drivers["Vaux Lattis Matrix"] = driverData;
        return bundle;
    }

    private static ProjectDataBundle BuildBundleWithCbus()
    {
        var bundle = new ProjectDataBundle();
        var driverData = new AdditionalDriverData();
        driverData.CbusGroups[(56, 78)] = new CbusGroupEntry("Living Room", "Pendant");
        bundle.Additional.Drivers["Clipsal C-Bus"] = driverData;
        return bundle;
    }
}
