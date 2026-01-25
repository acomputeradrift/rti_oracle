using System;
using System.IO;
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
}
