using System.Collections.Generic;
using System.Reflection;
using OracleByFPCLtd.DriverProfiles;
using OracleByFPCLtd.ProjectData;
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
    public void SystemMappingServiceResolvesDisplayName()
    {
        var bundle = BuildBundleWithDisplayName();
        var service = new SystemMappingService();
        var evt = new DiagnosticEvent(12, "[2026-01-24 10:00:00.000] Change to page 1 on device 'iPad'");

        var line = service.Map(evt, bundle);

        Assert.Equal("12 [2026-01-24 10:00:00.000] Change to page \"Main\" on device 'iPad'", line.Text);
        Assert.False(line.IsUnresolved);
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
    public void DriverMappingServiceFormatsTimestampedCbusRampToLevelScene()
    {
        var bundle = BuildBundleWithCbus();
        bundle.Additional.Drivers["Clipsal C-Bus"].CbusScenes[(202, 0, 32)] = new CbusSceneEntry("Landscape Lighting On");
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(10, "[2026-02-10 20:03:30.112] Driver - Command:'Clipsal C-Bus\\General\\Ramp to level(10, 0, 32, 202)' Sustain:YES");

        var line = service.Map(evt, bundle);

        Assert.Equal("10 [2026-02-10 20:03:30.112] Driver Command (Clipsal C-Bus): 'Landscape Lighting On ramped over 4 seconds.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceTransitionExtractionDoesNotAppendTrailingParenForNestedSystemManagerCommand()
    {
        const string rawText = "[2026-02-12 19:21:00.001] Driver - Command:'System Manager\\[Hide]\\Route Command(2, Set Selected Room(Room One))' Sustain:NO";
        const string mappedText = "[2026-02-12 19:21:00.001] Driver - Command:'System Manager\\[Hide]\\Route Command(2, Set Selected Room(Room 1))' Sustain:NO";

        var method = typeof(DriverMappingService).GetMethod(
            "TryExtractMappingTransition",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var args = new object[] { rawText, mappedText, "" };
        var extracted = (bool)method!.Invoke(null, args)!;

        Assert.True(extracted);
        Assert.Equal("Room One -> Room 1", args[2]);
    }

    [Fact]
    public void DriverMappingServiceApexMappingMessagePrefixesSource()
    {
        var method = typeof(DriverMappingService).GetMethod(
            "BuildApexMappingMessage",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var message = method!.Invoke(null, new object[] { "1 -> Video Source (Global)" }) as string;

        Assert.Equal("Processed log line mapped to Apex file (Source 1 -> Video Source (Global))", message);
    }

    [Fact]
    public void DriverMappingServiceKeepsNoFormatTagForTimestampedSystemManagerRouteCommand()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(15, "[2026-02-10 19:15:33.701] Driver - Command:'System Manager\\[Hide]\\Route Command(2, 1, 3)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("15 [2026-02-10 19:15:33.701] Driver Command (System Manager): 'System Manager\\[Hide]\\Route Command(2, 1, 3)' [No Format!]", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceMarksNoMapForSystemManagerSetSourceByRoomWithNumericSource()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(4, "[2026-02-11 13:45:16.196] Driver - Command:'System Manager\\Routing\\Set Source By Room(Room Three, 26)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("4 [2026-02-11 13:45:16.196] Driver Command (System Manager): 'Source for Room Three set to 26.' [No Map!]", line.Text);
        Assert.True(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceResolvesSystemManagerSetSourceByRoomWithSourceCatalogOffset()
    {
        var bundle = BuildBundleWithSystemManagerSourceCatalog();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(34, "[2026-02-11 14:34:26.358] Driver - Command:'System Manager\\Routing\\Set Source By Room(Room Five, 28)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("34 [2026-02-11 14:34:26.358] Driver Command (System Manager): 'Source for Room Five set to Other Source (Room 5).'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceResolvesSystemManagerSetSourceWithSourceCatalogOffset()
    {
        var bundle = BuildBundleWithSystemManagerSourceCatalog();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(3, "[2026-02-11 14:29:23.662] Driver - Command:'System Manager\\Routing\\Set Source(7)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("3 [2026-02-11 14:29:23.662] Driver Command (System Manager): 'Source set to Video Source (Global).'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceMarksNoMapForSystemManagerSetSourceWithNumericSource()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(22, "[2026-02-11 13:45:51.332] Driver - Command:'System Manager\\Routing\\Set Source(7)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("22 [2026-02-11 13:45:51.332] Driver Command (System Manager): 'Source set to 7.' [No Map!]", line.Text);
        Assert.True(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceAppendsNoMapForUnresolvedCommandAfterFormatting()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(16, "[2026-02-10 19:00:39.485] Driver - Command:'Vaux Lattis Matrix\\Output Settings\\Source Select(Route All, 13, 1)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("16 [2026-02-10 19:00:39.485] Driver Command (Vaux Lattis Matrix): '13 set to source 1.' [No Map!]", line.Text);
        Assert.True(line.IsUnresolved);
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
    public void DriverMappingServiceMarksNoProfileForNonDriverLine()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(2, "[2026-01-24 10:00:00.000] Random non-profile line");

        var line = service.Map(evt, bundle);

        Assert.Equal("2 [2026-01-24 10:00:00.000] Random non-profile line [No Profile!]", line.Text);
        Assert.True(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceMapsPageLineViaRtiInternalProfile()
    {
        var bundle = BuildBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(27, "[2026-01-24 10:00:00.000] Change to page 1 on device 'RTiPanel (iPhone X or newer)'");

        var line = service.Map(evt, bundle);

        Assert.Equal("27 [2026-01-24 10:00:00.000] Change to page \"Room Select\" on device 'RTiPanel (iPhone X or newer)'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceMapsRtiInternalIrPortCommand()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(28, "IR - Port:'XP-8v','ECB5 #1' Command:'POWER OFF [ / / ]' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("28 IR Command (Internal): 'POWER OFF -> XP-8v: ECB5 #1'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceMapsRtiInternalRelayTriggerAction()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(29, "Relay/Trigger - Port:'XP-8v','Garage Door West' Action:OFF");

        var line = service.Map(evt, bundle);

        Assert.Equal("29 Relay/Trigger Command (Internal): 'OFF -> XP-8v: Garage Door West'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceMapsRtiInternalRcm12RelayNameFromAdditionalInfo()
    {
        var bundle = new ProjectDataBundle();
        var driverData = new AdditionalDriverData();
        driverData.RelayNames[2] = "Boiler Pump";
        bundle.Additional.Drivers["RTI Internal"] = driverData;
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(30, "IR - Port:'XP-8v','RTI RCM-12 Relay Module' Command:'RELAY 2 CLOSE [ / / ]' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("30 IR Command (Internal): 'Boiler Pump CLOSE -> XP-8v: RTI RCM-12 Relay Module'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Theory]
    [InlineData("Macro - End")]
    [InlineData("Macro - Start")]
    [InlineData("Button Up")]
    [InlineData("Device 'RTiPanel (iPhone X or newer)' has connected")]
    [InlineData("Device 'RTiPanel (iPhone X or newer)' has disconnected")]
    [InlineData("System macro 'LIGHTS - LUTRON Master Bath ALL ON' - Start")]
    [InlineData("System macro 'LIGHTS - LUTRON Master Bath ALL ON' - End")]
    public void DriverMappingServiceClaimsRtiInternalLifecycleLines(string raw)
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(31, raw);

        var line = service.Map(evt, bundle);

        Assert.Equal($"31 {raw}", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceStripsTransportFromRtiInternalButtonDownLine()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var raw = "Button Down - Device:'RTiPanel (iPhone X or newer)' Transport:Ethernet TCP";
        var evt = new DiagnosticEvent(32, raw);

        var line = service.Map(evt, bundle);

        Assert.Equal("32 Button Down - Device:'RTiPanel (iPhone X or newer)'", line.Text);
        Assert.False(line.IsUnresolved);
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

    [Fact]
    public void DriverMappingServiceTreatsLutronCasetaRa2SelectAsProfile()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(14, "Driver - Command:'Lutron Caseta / RA2 Select\\Switches\\Switch Commands(Master - East Pendant (ID 55), Toggle)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("14 Driver - Command:'Lutron Caseta / RA2 Select\\Switches\\Switch Commands(Master - East Pendant (ID 55), Toggle)' Sustain:NO", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsDscPowerSeriesDriverEvent()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(21, "[2026-02-21 10:12:44.112] Driver event 'When 'Garage West DOOR Opened' happens on 'DSC PowerSeries\\Zone Open''");

        var line = service.Map(evt, bundle);

        Assert.Equal("21 [2026-02-21 10:12:44.112] Driver Event (DSC PowerSeries): 'Garage West DOOR Opened.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsVenstarColorTouchDriverEvent()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(22, "[2026-02-21 10:14:44.112] Driver event 'When 'Garage (Stat 2) - Operating State Change' happens on 'Venstar ColorTouch\\Garage (Stat 2) Events''");

        var line = service.Map(evt, bundle);

        Assert.Equal("22 [2026-02-21 10:14:44.112] Driver Event (Venstar ColorTouch): 'Garage (Stat 2) - Operating State Change.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsSystemVariablesDecreaseWithIntegerNameMap()
    {
        var bundle = new ProjectDataBundle();
        var driverData = new AdditionalDriverData();
        driverData.IntegerNames[1] = "Room Count";
        bundle.Additional.Drivers["System Variables"] = driverData;

        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(23, "[2026-02-21 11:00:00.000] Driver - Command:'System Variables\\Integers\\Decrease(1, 1)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("23 [2026-02-21 11:00:00.000] Driver Command (System Variables): 'Room Count decreased by 1.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsSystemVariablesIncreaseWithIntegerNameMap()
    {
        var bundle = new ProjectDataBundle();
        var driverData = new AdditionalDriverData();
        driverData.IntegerNames[1] = "Room Count";
        bundle.Additional.Drivers["System Variables"] = driverData;

        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(24, "[2026-02-21 11:00:01.000] Driver - Command:'System Variables\\Integers\\Increase(1, 1)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("24 [2026-02-21 11:00:01.000] Driver Command (System Variables): 'Room Count increased by 1.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsSystemVariablesTestEqualWithIntegerNameMap()
    {
        var bundle = new ProjectDataBundle();
        var driverData = new AdditionalDriverData();
        driverData.IntegerNames[1] = "Room Count";
        bundle.Additional.Drivers["System Variables"] = driverData;

        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(25, "[2026-02-21 11:00:02.000] Driver - Command:'System Variables\\Integers\\Test(1, equal, 0, 1)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("25 [2026-02-21 11:00:02.000] Driver Command (System Variables): 'Testing: Is Room Count equal to 0?'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsSystemVariablesTestEqualSecondVariantWithIntegerNameMap()
    {
        var bundle = new ProjectDataBundle();
        var driverData = new AdditionalDriverData();
        driverData.IntegerNames[1] = "Room Count";
        bundle.Additional.Drivers["System Variables"] = driverData;

        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(26, "[2026-02-21 11:00:03.000] Driver - Command:'System Variables\\Integers\\Test(1, equal, 1, 2)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("26 [2026-02-21 11:00:03.000] Driver Command (System Variables): 'Testing: Is Room Count equal to 1?'", line.Text);
        Assert.False(line.IsUnresolved);
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

    private static ProjectDataBundle BuildBundleWithDisplayName()
    {
        var bundle = new ProjectDataBundle();
        bundle.System.DiagnosticsMapping.Add(new OracleByFPCLtd.ProjectData.DiagnosticsMappingEntry(
            15,
            "RTiPanel (iPad)",
            "iPad",
            0,
            0,
            0,
            0,
            "Main"));
        bundle.System.PageIndexMap["15|0"] = "Main";
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

    private static ProjectDataBundle BuildBundleWithSystemManagerSourceCatalog()
    {
        var bundle = new ProjectDataBundle();
        bundle.System.SourceCatalog.AddRange(new[]
        {
            new SourceCatalogEntry(1, 0, 5, "Home (Global)", "Home (Global)"),
            new SourceCatalogEntry(7, 1, 5, "Home (Room 1)", "Home (Room 1)"),
            new SourceCatalogEntry(8, 2, 5, "Home (Room 2)", "Home (Room 2)"),
            new SourceCatalogEntry(9, 3, 5, "Home (Room 3)", "Home (Room 3)"),
            new SourceCatalogEntry(10, 4, 5, "Home (Room 4)", "Home (Room 4)"),
            new SourceCatalogEntry(11, 5, 5, "Home (Room 5)", "Home (Room 5)"),
            new SourceCatalogEntry(12, 0, 5, "Audio Source (Global)", "Audio Source (Global)"),
            new SourceCatalogEntry(13, 0, 5, "Video Source (Global)", "Video Source (Global)"),
            new SourceCatalogEntry(14, 1, 5, "Audio Source (Room 1)", "Audio Source (Room 1)"),
            new SourceCatalogEntry(15, 1, 5, "Video Source (Room 1)", "Video Source (Room 1)"),
            new SourceCatalogEntry(16, 2, 5, "Audio Source (Room 2)", "Audio Source (Room 2)"),
            new SourceCatalogEntry(17, 2, 5, "Video Source (Room 2)", "Video Source (Room 2)"),
            new SourceCatalogEntry(18, 3, 5, "Audio Source (Room 3)", "Audio Source (Room 3)"),
            new SourceCatalogEntry(19, 3, 5, "Video Source (Room 3)", "Video Source (Room 3)"),
            new SourceCatalogEntry(20, 4, 5, "Audio Source (Room 4)", "Audio Source (Room 4)"),
            new SourceCatalogEntry(21, 4, 5, "Video Source (Room 4)", "Video Source (Room 4)"),
            new SourceCatalogEntry(22, 5, 5, "Audio Source (Room 5)", "Audio Source (Room 5)"),
            new SourceCatalogEntry(23, 5, 5, "Video Source (Room 5)", "Video Source (Room 5)"),
            new SourceCatalogEntry(25, 1, 6, "Audio - Zone 1 (Room 1)", "Audio - Zone 1 (Room 1)"),
            new SourceCatalogEntry(26, 2, 6, "Audio - Zone 2 (Room 2)", "Audio - Zone 2 (Room 2)"),
            new SourceCatalogEntry(27, 3, 6, "Audio - Zone 3 (Room 3)", "Audio - Zone 3 (Room 3)"),
            new SourceCatalogEntry(28, 4, 6, "Audio - Zone 4 (Room 4)", "Audio - Zone 4 (Room 4)"),
            new SourceCatalogEntry(29, 5, 6, "Audio - Zone 5 (Room 5)", "Audio - Zone 5 (Room 5)"),
            new SourceCatalogEntry(31, 1, 6, "Video - Zone 1 (Room 1)", "Video - Zone 1 (Room 1)"),
            new SourceCatalogEntry(32, 1, 5, "Other Source (Room 1)", "Other Source (Room 1)"),
            new SourceCatalogEntry(33, 2, 5, "Other Source (Room 2)", "Other Source (Room 2)"),
            new SourceCatalogEntry(34, 3, 5, "Other Source (Room 3)", "Other Source (Room 3)"),
            new SourceCatalogEntry(35, 4, 5, "Other Source (Room 4)", "Other Source (Room 4)"),
            new SourceCatalogEntry(36, 5, 5, "Other Source (Room 5)", "Other Source (Room 5)")
        });
        return bundle;
    }
}
