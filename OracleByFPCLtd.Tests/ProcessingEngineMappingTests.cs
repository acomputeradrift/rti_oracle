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
}
