using System;
using System.Collections.Generic;
using System.Linq;
using SHPDiagnosticsViewer.DriverProfiles;
using SHPDiagnosticsViewer.ProjectData;
using Xunit;

namespace SHPDiagnosticsViewer.Tests;

public sealed class DriverProfileModuleTests
{
    [Fact]
    public void IntegrateMatchesProfilesByNameAndAlias()
    {
        // Requirement: mission.md - Core Capabilities #3/#5; invariants.md - Explicit Mapping.
        var preload = new ApexDiscoveryPreloadResult();
        preload.DriverConfigMap[1] = new DriverConfigEntry("System Variable Events", "SVE - 1", new Dictionary<string, string>());
        preload.DriverConfigMap[2] = new DriverConfigEntry("Temp Driver", "Temp Driver", new Dictionary<string, string>());

        var profiles = new[]
        {
            new DriverProfileDefinition(
                "System Variable Events",
                new[] { "System Variable Events #2" },
                new List<string>(),
                new List<string>(),
                new List<DriverProfileDiscoveryRule>(),
                new List<DriverProfileAnalysisRule>(),
                new List<string>()),
            new DriverProfileDefinition(
                "Clipsal C-Bus",
                new[] { "Temp Driver" },
                new List<string>(),
                new List<string>(),
                new List<DriverProfileDiscoveryRule>(),
                new List<DriverProfileAnalysisRule>(),
                new List<string>())
        };

        var registry = new DriverProfileRegistry(profiles);
        var bundle = DriverProfileModule.Integrate(preload, registry);

        Assert.Equal(2, bundle.Matches.Count);
        Assert.Contains(bundle.Matches, match => match.DriverDeviceId == 1 && match.Profile.DeviceName == "System Variable Events");
        Assert.Contains(bundle.Matches, match => match.DriverDeviceId == 2 && match.Profile.DeviceName == "Clipsal C-Bus");
    }

    [Fact]
    public void Ad64ProfileUsesCountAndNameKeys()
    {
        // Requirement: mission.md - Core Capabilities #3/#5; invariants.md - Explicit Mapping.
        var profile = RtiAd64Profile.Definition;

        Assert.Equal("RTI AD-64", profile.DeviceName);
        Assert.Contains("GroupCount", profile.DiscoveryKeys);
        Assert.Contains("SourceCount", profile.DiscoveryKeys);
        Assert.Contains("ZoneCount", profile.DiscoveryKeys);
        Assert.Contains("GroupName", profile.DiscoveryPrefixes);
        Assert.Contains("SourceName", profile.DiscoveryPrefixes);
        Assert.Contains("ZoneName", profile.DiscoveryPrefixes);
    }

    [Fact]
    public void InternalProfileIsAlwaysAvailable()
    {
        // Requirement: mission.md - Core Capabilities #3/#4; invariants.md - Explicit Mapping.
        var profile = DriverProfileCatalog.Internal().Single();

        Assert.Equal("RTI Internal", profile.DeviceName);
        Assert.Contains(profile.DiscoveryRules, rule => rule.Description.Contains("Device Page Mapping", StringComparison.Ordinal));
    }
}
