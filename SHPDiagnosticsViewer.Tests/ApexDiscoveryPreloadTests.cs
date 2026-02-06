using System;
using System.Collections;
using System.IO;
using System.Linq;
using SHPDiagnosticsViewer.ProjectData;
using Xunit;

namespace SHPDiagnosticsViewer.Tests;

public sealed class ApexDiscoveryPreloadTests
{
    private static string ApexPath =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ApexDiscovery", "Verrier Home FEENY EDIT v49.apex"));

    [Fact]
    public void PageIndexMapIncludesDevicePageNames()
    {
        // Requirement: mission.md - Core Capabilities #3/#5; invariants.md - No-Inference + Traceability.
        var result = ApexDiscoveryPreloadExtractor.Extract(ApexPath);

        Assert.True(result.PageIndexMap.TryGetValue("81|0", out var pageName));
        Assert.Equal("Room Select", pageName);
    }

    [Fact]
    public void SysVarRefMapResolvesDriverAndVariableName()
    {
        // Requirement: mission.md - Core Capabilities #3/#5; invariants.md - Explicit Mapping.
        var result = ApexDiscoveryPreloadExtractor.Extract(ApexPath);
        var key = "SYSVARREF:{EC82485C-AF0B-4BF0-9DB1-22B290C8B814}#24@App38Group00";

        Assert.True(result.SysVarRefMap.TryGetValue(key, out var entry));
        Assert.Equal(1, entry.DriverDeviceId);
        Assert.Equal("Clipsal C-Bus", entry.DriverName);
        Assert.Equal(0, entry.DeviceId);
        Assert.Equal("App ID 56, Group 0 state", entry.VariableName);
    }

    [Fact]
    public void DriverConfigMapExcludesDebugKeys()
    {
        // Requirement: mission.md - Core Capabilities #3; invariants.md - Output Honesty.
        var result = ApexDiscoveryPreloadExtractor.Extract(ApexPath);

        Assert.True(result.DriverConfigMap.TryGetValue(1, out var driver));
        Assert.Equal("Clipsal C-Bus", driver.DeviceName);
        Assert.Equal("Clipsal C-Bus", driver.DeviceDisplayName);
        Assert.Equal("TCP", driver.Config["ConnectionType"]);
        Assert.False(driver.Config.ContainsKey("DebugTrace"));
    }

    [Fact]
    public void Ad64ConfigUsesCountsToLimitNames()
    {
        // Requirement: mission.md - Core Capabilities #3/#5; invariants.md - Explicit Mapping.
        var result = ApexDiscoveryPreloadExtractor.Extract(ApexPath);

        Assert.True(result.DriverConfigMap.TryGetValue(4, out var driver));
        Assert.Equal("RTI AD-64", driver.DeviceName);
        Assert.False(driver.Config.ContainsKey("GroupCount"));
        Assert.True(driver.Config.ContainsKey("GroupName8"));
        Assert.False(driver.Config.ContainsKey("Connection0"));
        Assert.False(driver.Config.ContainsKey("ZoneName17"));
        Assert.False(driver.Config.ContainsKey("SourceName10"));
    }

    [Fact]
    public void SystemVariableEventsSkipsEmptyAndNotSetConfigGroups()
    {
        // Requirement: mission.md - Core Capabilities #3/#5; invariants.md - Output Honesty.
        var result = ApexDiscoveryPreloadExtractor.Extract(ApexPath);

        Assert.True(result.DriverConfigMap.TryGetValue(54, out var driver));
        Assert.Equal("System Variable Events", driver.DeviceName);
        Assert.False(driver.Config.ContainsKey("Config_Boolean1Macro"));
        Assert.False(driver.Config.ContainsKey("Config_Boolean1Sysvar"));
        Assert.False(driver.Config.ContainsKey("Config_Boolean1Type"));
        Assert.False(driver.Config.ContainsKey("Config_Integer5Macro"));
        Assert.False(driver.Config.ContainsKey("Config_Integer5Sysvar"));
        Assert.False(driver.Config.ContainsKey("Config_Integer5Type"));
        Assert.False(driver.Config.ContainsKey("Config_Integer5Value"));
        Assert.False(driver.Config.ContainsKey("Config_Combo1VarA"));
        Assert.False(driver.Config.ContainsKey("Config_PersistEnabledStates"));
        Assert.True(driver.Config.ContainsKey("Config_Integer1Sysvar"));
    }

    [Fact]
    public void PageExtractionIncludesSourceAndRoomContext()
    {
        var result = ApexDiscoveryPreloadExtractor.Extract(ApexPath);
        var entries = GetEntries(result, "PageMappings");

        Assert.Contains(entries, entry => EntryMatches(entry,
            ("DeviceId", 81),
            ("DeviceName", "RTiPanel (iPhone X or newer)"),
            ("RoomId", 0),
            ("RoomName", "Global"),
            ("SourceId", 1),
            ("SourceName", "Home"),
            ("PageNumber", 1),
            ("PageName", "Room Select")));
    }

    [Fact]
    public void RelayPortsIncludeInternalRelayState()
    {
        var result = ApexDiscoveryPreloadExtractor.Extract(ApexPath);
        var entries = GetEntries(result, "RelayPorts");

        Assert.Contains(entries, entry => EntryMatches(entry,
            ("ControllerDeviceName", "XP-8v"),
            ("ExpanderDeviceType", "Internal"),
            ("ExpanderName", "Internal"),
            ("RelayName", "De-humidifier"),
            ("RelayType", "Contact Closure"),
            ("RelayMode", "Normally Open")));
    }

    [Fact]
    public void MpioIrPortsIncludeInternalPortNames()
    {
        var result = ApexDiscoveryPreloadExtractor.Extract(ApexPath);
        var entries = GetEntries(result, "MpioIrPorts");

        Assert.Contains(entries, entry => EntryMatches(entry,
            ("ControllerDeviceName", "XP-8v"),
            ("ExpanderDeviceType", "Internal"),
            ("ExpanderName", "Internal"),
            ("PortNumber", 1),
            ("PortName", "Sat Box 1 / Apple TV 1")));
    }

    [Fact]
    public void SensePortsIncludeModeState()
    {
        var result = ApexDiscoveryPreloadExtractor.Extract(ApexPath);
        var entries = GetEntries(result, "SensePorts");

        Assert.Contains(entries, entry => EntryMatches(entry,
            ("ControllerDeviceName", "XP-8v"),
            ("ExpanderDeviceType", "Internal"),
            ("ExpanderName", "Internal"),
            ("PortNumber", 1),
            ("PortName", "Door Chime"),
            ("SenseModeState", "Sense Closure")));
    }

    [Fact]
    public void TriggerPortsIncludeExpanderContext()
    {
        var result = ApexDiscoveryPreloadExtractor.Extract(ApexPath);
        var entries = GetEntries(result, "TriggerPorts");

        Assert.Contains(entries, entry => EntryMatches(entry,
            ("ControllerDeviceName", "XP-8v"),
            ("ExpanderDeviceType", "XP-6"),
            ("ExpanderName", "WorkShop Slave"),
            ("TriggerNumber", 1),
            ("TriggerName", "Trigger Out 1")));
    }

    [Fact]
    public void Rs232PortsIncludeInternalPortNames()
    {
        var result = ApexDiscoveryPreloadExtractor.Extract(ApexPath);
        var entries = GetEntries(result, "Rs232Ports");

        Assert.Contains(entries, entry => EntryMatches(entry,
            ("ControllerDeviceName", "XP-8v"),
            ("ExpanderDeviceType", "Internal"),
            ("ExpanderName", "Internal"),
            ("PortNumber", 1),
            ("PortName", "CP-1650 Zones 1-8")));
    }

    [Fact]
    public void RoomExtractionLinksPagesToSourcesAndControllers()
    {
        var result = ApexDiscoveryPreloadExtractor.Extract(ApexPath);
        var entries = GetEntries(result, "RoomMappings");

        Assert.Contains(entries, entry => EntryMatches(entry,
            ("RoomId", 0),
            ("RoomName", "Global"),
            ("SourceId", 1),
            ("SourceName", "Home"),
            ("ControllerDeviceId", 81),
            ("ControllerDeviceName", "RTiPanel (iPhone X or newer)"),
            ("PageId", 513),
            ("PageName", "Room Select")));
    }

    [Fact]
    public void DriverTemplateVariablesExposeCategoryAndFormat()
    {
        var result = ApexDiscoveryPreloadExtractor.Extract(ApexPath);
        var entries = GetEntries(result, "DriverTemplateVariables");

        Assert.Contains(entries, entry => EntryMatches(entry,
            ("DriverDeviceId", 1),
            ("DriverDeviceName", "Clipsal C-Bus"),
            ("DriverDisplayName", "Clipsal C-Bus"),
            ("SysVarRef", "{EC82485C-AF0B-4BF0-9DB1-22B290C8B814}#24@App38Group00"),
            ("SysVarToken", "App38Group00"),
            ("SourceDriverId", "{EC82485C-AF0B-4BF0-9DB1-22B290C8B814}"),
            ("SourceDriverName", "Clipsal C-Bus"),
            ("VariableCategory", "App ID 56, Group State"),
            ("VariableName", "App ID 56, Group 0 state"),
            ("VariableType", "boolean"),
            ("Format", "B:Off:On")));
    }

    private static object[] GetEntries(ApexDiscoveryPreloadResult result, string propertyName)
    {
        var property = typeof(ApexDiscoveryPreloadResult).GetProperty(propertyName);
        Assert.NotNull(property);

        var value = property.GetValue(result);
        Assert.NotNull(value);

        var enumerable = Assert.IsAssignableFrom<IEnumerable>(value);
        return enumerable.Cast<object>().ToArray();
    }

    private static bool EntryMatches(object entry, params (string Name, object? Value)[] matches)
    {
        foreach (var (name, expected) in matches)
        {
            var property = entry.GetType().GetProperty(name);
            if (property is null)
            {
                return false;
            }

            var actual = property.GetValue(entry);
            if (!Equals(actual, expected))
            {
                return false;
            }
        }

        return true;
    }
}
