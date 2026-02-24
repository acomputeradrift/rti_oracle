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

        Assert.Contains("[Unresolved!]", line.Text);
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

        Assert.Equal("4 Driver - Command:'Clipsal C-Bus\\General\\Immediate Switch(121 [Unknown State!], Living Room Pendant)' Sustain:NO", line.Text);
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
    public void DriverMappingServiceFormatsTimestampedCbusSceneEvent()
    {
        var bundle = BuildBundleWithCbus();
        bundle.Additional.Drivers["Clipsal C-Bus"].CbusScenes[(202, 33, 0)] = new CbusSceneEntry("West Bedroom North Recessed");
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(92, "[2026-02-23 07:25:34.196] Driver event 'When 'App 202, Group 33 Off' happens on 'Clipsal C-Bus\\App 202 Group Off''");

        var line = service.Map(evt, bundle);

        Assert.Equal("92 [2026-02-23 07:25:34.196] Driver Event (Clipsal C-Bus): 'When West Bedroom North Recessed turns Off.'", line.Text);
        Assert.False(line.IsUnresolved);
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
    public void DriverMappingServiceFormatsTimestampedSystemManagerRouteCommandAsDriverUpdateWithoutNoFormatTag()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(15, "[2026-02-10 19:15:33.701] Driver - Command:'System Manager\\[Hide]\\Route Command(2, 1, 3)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("15 [2026-02-10 19:15:33.701] Driver Update (System Manager): 'System Manager\\[Hide]\\Route Command(2, 1, 3)'", line.Text);
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
    public void DriverMappingServiceUsesSystemManagerSourceCatalogForSetSourceWhenBothCatalogsExist()
    {
        var bundle = BuildBundleWithConflictingSystemManagerCatalogs();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(3, "[2026-02-11 14:29:23.662] Driver - Command:'System Manager\\Routing\\Set Source(7)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("3 [2026-02-11 14:29:23.662] Driver Command (System Manager): 'Source set to New Method Source 7.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceUsesSourceCatalogForSetSourceByRoomWhenBothCatalogsExist()
    {
        var bundle = BuildBundleWithConflictingSystemManagerCatalogs();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(5, "[2026-02-11 14:29:23.662] Driver - Command:'System Manager\\Routing\\Set Source By Room(Gym, 7)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("5 [2026-02-11 14:29:23.662] Driver Command (System Manager): 'Source for Gym set to Old Method Source 7.'", line.Text);
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
    public void DriverMappingServiceMarksIncompleteProfileWhenDriverExistsButLineTypeIsUnsupported()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(203, "[2026-02-23 07:36:52.423] Sense event 'When 'Garage West DOOR Opened' happens on 'DSC PowerSeries\\Zone Open''");

        var line = service.Map(evt, bundle);

        Assert.Equal("203 [2026-02-23 07:36:52.423] Sense event 'When 'Garage West DOOR Opened' happens on 'DSC PowerSeries\\Zone Open'' [Incomplete Profile!]", line.Text);
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
    public void DriverMappingServiceMapsRtiInternalIrPortCommandWithTimestamp()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(28, "[2026-02-22 08:19:10.123] IR - Port:'XP-8v','ECB5 #1' Command:'POWER OFF [ / / ]' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("28 [2026-02-22 08:19:10.123] IR Command (Internal): 'POWER OFF -> XP-8v: ECB5 #1'", line.Text);
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
    public void DriverMappingServiceMapsRtiInternalRelayTriggerActionWithTimestamp()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(29, "[2026-02-22 08:19:10.456] Relay/Trigger - Port:'XP-8v','Garage Door West' Action:OFF");

        var line = service.Map(evt, bundle);

        Assert.Equal("29 [2026-02-22 08:19:10.456] Relay/Trigger Command (Internal): 'OFF -> XP-8v: Garage Door West'", line.Text);
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
    [InlineData("Stop macro")]
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
    public void DriverMappingServiceFormatsRtiInternalSenseEvent()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var raw = "Sense event 'When [Sense 1] Gate opens'";
        var evt = new DiagnosticEvent(33, raw);

        var line = service.Map(evt, bundle);

        Assert.Equal("33 Sense Event (Internal): 'When [Sense 1] Gate opens.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsTimestampedRtiInternalSenseEvent()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var raw = "[2026-02-23 07:25:34.196] Sense event 'When [Sense 1] Gate opens'";
        var evt = new DiagnosticEvent(34, raw);

        var line = service.Map(evt, bundle);

        Assert.Equal("34 [2026-02-23 07:25:34.196] Sense Event (Internal): 'When [Sense 1] Gate opens.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsRtiInternalScheduledEvent()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var raw = "Scheduled event 'Every day at sunrise'";
        var evt = new DiagnosticEvent(35, raw);

        var line = service.Map(evt, bundle);

        Assert.Equal("35 Scheduled Event (Internal): 'When Every day at sunrise happens.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsTimestampedRtiInternalScheduledEvent()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var raw = "[2026-02-23 07:25:34.196] Scheduled event 'Every day at sunset'";
        var evt = new DiagnosticEvent(36, raw);

        var line = service.Map(evt, bundle);

        Assert.Equal("36 [2026-02-23 07:25:34.196] Scheduled Event (Internal): 'When Every day at sunset happens.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsRtiInternalDelayCommand()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(361, "Delay 200ms");

        var line = service.Map(evt, bundle);

        Assert.Equal("361 Driver Command (Internal): 'Delay 200ms.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsTimestampedRtiInternalDelayCommand()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(362, "[2026-02-23 07:36:52.423] Delay 200ms");

        var line = service.Map(evt, bundle);

        Assert.Equal("362 [2026-02-23 07:36:52.423] Driver Command (Internal): 'Delay 200ms.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceMapsRtiInternalSerialCommand()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(37, "Serial - Port:'XP-8v','CP-1650 Zones 1-8' Command:'POWER ON\\r' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("37 Serial Command (Internal): 'POWER ON -> XP-8v: CP-1650 Zones 1-8'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceMapsTimestampedRtiInternalSerialCommand()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(38, "[2026-02-23 07:25:34.196] Serial - Port:'XP-8v','CP-1650 Zones 1-8' Command:'POWER ON\\r' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("38 [2026-02-23 07:25:34.196] Serial Command (Internal): 'POWER ON -> XP-8v: CP-1650 Zones 1-8'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceClaimsRtiDiagnosticsPrimaryProcessorLine()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var raw = "Diagnostics: Primary Processor - OnHTTPServerData() data.websocket = {\"type\":\"Subscribe\"}";
        var evt = new DiagnosticEvent(33, raw);

        var line = service.Map(evt, bundle);

        Assert.Equal($"33 {raw}", line.Text);
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

    [Theory]
    [InlineData("System Manager -  Variable Stats: Added 5 with 0 of those subscribed, 1 were skipped. Removed 6 from old view with 0 of those unsubscribed and 5 spliced. 6 Total considered.", "Driver Update (System Manager): 'Variable Stats: Added 5 with 0 of those subscribed, 1 were skipped. Removed 6 from old view with 0 of those unsubscribed and 5 spliced. 6 Total considered.'")]
    [InlineData("System Manager - Changing selected room for RTiPanel (iPhone X or newer) to GLOBAL", "Driver Update (System Manager): 'Changing selected room for RTiPanel (iPhone X or newer) to GLOBAL'")]
    [InlineData("System Manager - Clock: UpdateTimeSysVars at Sun Feb 22 2026 10:36:00 GMT+0000", "Driver Update (System Manager): 'Clock: UpdateTimeSysVars at Sun Feb 22 2026 10:36:00 GMT+0000'")]
    [InlineData("System Manager - newRoom -> 0", "Driver Update (System Manager): 'newRoom -> 0'")]
    [InlineData("System Manager - oldRoom -> 0", "Driver Update (System Manager): 'oldRoom -> 0'")]
    [InlineData("System Manager - strView -> %1", "Driver Update (System Manager): 'strView -> %1'")]
    public void DriverMappingServiceFormatsSystemManagerUpdateLines(string raw, string expectedBody)
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(34, raw);

        var line = service.Map(evt, bundle);

        Assert.Equal($"34 {expectedBody}", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsSystemManagerUpdateLinesWithTimestamp()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var raw = "[2026-02-22 10:36:00.000] System Manager - Clock: UpdateTimeSysVars at Sun Feb 22 2026 10:36:00 GMT+0000";
        var evt = new DiagnosticEvent(35, raw);

        var line = service.Map(evt, bundle);

        Assert.Equal("35 [2026-02-22 10:36:00.000] Driver Update (System Manager): 'Clock: UpdateTimeSysVars at Sun Feb 22 2026 10:36:00 GMT+0000'", line.Text);
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
    public void DriverMappingServiceTreatsLutronUpdateLinesAsProfilePassthrough()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(15, "Lutron Upper - ID 10, Set Dimmer Level to 100, with a fade rate of 00:00:02");

        var line = service.Map(evt, bundle);

        Assert.Equal("15 Lutron Upper - ID 10, Set Dimmer Level to 100, with a fade rate of 00:00:02", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsDscPowerSeriesDriverEvent()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(21, "[2026-02-21 10:12:44.112] Driver event 'When 'Garage West DOOR Opened' happens on 'DSC PowerSeries\\Zone Open''");

        var line = service.Map(evt, bundle);

        Assert.Equal("21 [2026-02-21 10:12:44.112] Driver Event (DSC PowerSeries): 'When Garage West DOOR Opened.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsDscPowerSeriesNumberKeyCommand()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(204, "[2026-02-23 07:36:52.423] Driver - Command:'DSC PowerSeries\\Keypad\\Number Keys(2)' Sustain:YES Rate:100");

        var line = service.Map(evt, bundle);

        Assert.Equal("204 [2026-02-23 07:36:52.423] Driver Command (DSC PowerSeries): 'Number 2 key pressed on keypad.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsVenstarColorTouchDriverEvent()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(22, "[2026-02-21 10:14:44.112] Driver event 'When 'Garage (Stat 2) - Operating State Change' happens on 'Venstar ColorTouch\\Garage (Stat 2) Events''");

        var line = service.Map(evt, bundle);

        Assert.Equal("22 [2026-02-21 10:14:44.112] Driver Event (Venstar ColorTouch): 'When Garage (Stat 2) - Operating State Change.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceTreatsVenstarConnectedUpdateAsProfilePassthrough()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(23, "Venstar ColorTouch - Master Bed is connected");

        var line = service.Map(evt, bundle);

        Assert.Equal("23 Venstar ColorTouch - Master Bed is connected", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsJandyAqualinkRsSpaOffCommand()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(24, "[2026-02-24 09:00:00.000] Driver - Command:'Jandy AquaLink RS\\Spa Control\\Spa Off' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("24 [2026-02-24 09:00:00.000] Driver Command (Jandy iAquaLink): 'Spa turned Off.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsVhdxDriverUpdate()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(24, "VHDx - Failed to reach the matrix");

        var line = service.Map(evt, bundle);

        Assert.Equal("24 Driver Update (VHDx): 'Failed to reach the matrix'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsRtiVipUhdCtrlConnectDriverUpdate()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(25, "RTI VIP-UHD-CTRL - OnConnectJSON");

        var line = service.Map(evt, bundle);

        Assert.Equal("25 Driver Update (RTI VIP-UHD-CTRL): 'OnConnectJSON'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsRtiVipUhdCtrlDisconnectDriverUpdateWithTimestamp()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(26, "[2026-02-23 07:44:14.286] RTI VIP-UHD-CTRL - OnDisconnectJSON");

        var line = service.Map(evt, bundle);

        Assert.Equal("26 [2026-02-23 07:44:14.286] Driver Update (RTI VIP-UHD-CTRL): 'OnDisconnectJSON'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsVantageInFusionDriverCommand()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(36, "[2026-02-22 12:10:00.000] Driver - Command:'Vantage InFusion\\Tasks\\Execute Task(AUDIO - Hallway Button LED OFF, Press)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("36 [2026-02-22 12:10:00.000] Driver Command (Vantage InFusion): 'Task AUDIO - Hallway Button LED OFF is executed (Press).'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsVantageInFusionDriverEvent()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(37, "[2026-02-22 12:11:00.000] Driver event 'When 'SE Lamp (VID 178) On' happens on 'Vantage InFusion\\Button LEDs (1-100)''");

        var line = service.Map(evt, bundle);

        Assert.Equal("37 [2026-02-22 12:11:00.000] Driver Event (Vantage InFusion): 'When SE Lamp (VID 178) is turned ON.'", line.Text);
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

    [Fact]
    public void DriverMappingServiceFormatsSystemVariablesStringSetWithoutMapping()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(102, "[2026-02-22 11:13:50.469] Driver - Command:'System Variables\\Strings\\Set(1, Room)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("102 [2026-02-22 11:13:50.469] Driver Command (System Variables): 'String 1 set to Room.'", line.Text);
        Assert.False(line.IsUnresolved);
    }

    [Fact]
    public void DriverMappingServiceFormatsSystemVariablesIncreaseNoMapWithIntegerIndexLabel()
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(13, "[2026-02-22 10:58:57.950] Driver - Command:'System Variables\\Integers\\Increase(1, 1)' Sustain:NO");

        var line = service.Map(evt, bundle);

        Assert.Equal("13 [2026-02-22 10:58:57.950] Driver Command (System Variables): 'IntegerIndex 1 increased by 1.' [No Map!]", line.Text);
        Assert.True(line.IsUnresolved);
    }

    [Theory]
    [InlineData(14, "[2026-02-22 10:58:58.116] Driver - Command:'System Variables\\Integers\\Test(1, equal, 0, 1)' Sustain:NO", "14 [2026-02-22 10:58:58.116] Driver Command (System Variables): 'Testing: Is IntegerIndex 1 equal to 0?' [No Map!]")]
    [InlineData(15, "[2026-02-22 10:58:58.225] Driver - Command:'System Variables\\Integers\\Test(1, equal, 1, 2)' Sustain:NO", "15 [2026-02-22 10:58:58.225] Driver Command (System Variables): 'Testing: Is IntegerIndex 1 equal to 1?' [No Map!]")]
    public void DriverMappingServiceFormatsSystemVariablesTestNoMapWithIntegerIndexLabel(int lineNumber, string raw, string expected)
    {
        var bundle = new ProjectDataBundle();
        var service = new DriverMappingService();
        var evt = new DiagnosticEvent(lineNumber, raw);

        var line = service.Map(evt, bundle);

        Assert.Equal(expected, line.Text);
        Assert.True(line.IsUnresolved);
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
        var sourceEntries = new[]
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
        };

        bundle.System.SourceCatalog.AddRange(sourceEntries);
        var ordered = sourceEntries.OrderBy(entry => entry.DeviceId).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            bundle.System.SystemManagerSourceCatalog.Add(new SystemManagerSourceCatalogEntry(
                i,
                string.IsNullOrWhiteSpace(ordered[i].SourceDisplayName) ? ordered[i].SourceName : ordered[i].SourceDisplayName));
        }

        return bundle;
    }

    private static ProjectDataBundle BuildBundleWithConflictingSystemManagerCatalogs()
    {
        var bundle = new ProjectDataBundle();
        for (var i = 0; i <= 10; i++)
        {
            bundle.System.SourceCatalog.Add(new SourceCatalogEntry(100 + i, 0, 5, $"Old Method Source {i}", $"Old Method Source {i}"));
            bundle.System.SystemManagerSourceCatalog.Add(new SystemManagerSourceCatalogEntry(i, $"New Method Source {i}"));
        }

        return bundle;
    }
}
