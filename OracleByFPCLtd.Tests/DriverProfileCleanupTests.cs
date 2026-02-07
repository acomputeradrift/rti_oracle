using System;
using System.Collections.Generic;
using System.Linq;
using OracleByFPCLtd.DriverProfiles.Catalog;
using OracleByFPCLtd.DriverProfiles.Integration;
using OracleByFPCLtd.DriverProfiles.Matching;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class DriverProfileCleanupTests
{
    [Fact]
    public void MatcherFindsByNameAliasAndSystemVariablePrefix()
    {
        var profiles = new[]
        {
            new DriverProfileDefinition(
                "System Variable Events",
                new[] { "System Variable Events #2" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<DriverProfileDiscoveryRule>(),
                Array.Empty<DriverProfileAnalysisRule>(),
                Array.Empty<string>()),
            new DriverProfileDefinition(
                "Clipsal C-Bus",
                new[] { "Temp Driver" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<DriverProfileDiscoveryRule>(),
                Array.Empty<DriverProfileAnalysisRule>(),
                Array.Empty<string>())
        };

        var registry = new DriverProfileRegistry(profiles);
        var matcher = new DriverProfileMatcher();

        Assert.Equal("System Variable Events", matcher.Find("System Variable Events", registry)!.DeviceName);
        Assert.Equal("Clipsal C-Bus", matcher.Find("Temp Driver", registry)!.DeviceName);
        Assert.Equal("System Variable Events", matcher.Find("System Variable Events (Room)", registry)!.DeviceName);
    }

    [Fact]
    public void ModuleIntegratesMatchesFromRegistry()
    {
        var preload = new ApexDiscoveryPreloadResult();
        preload.DriverConfigMap[1] = new DriverConfigEntry("System Variable Events", "SVE - 1", new Dictionary<string, string>());
        preload.DriverConfigMap[2] = new DriverConfigEntry("Temp Driver", "Temp Driver", new Dictionary<string, string>());

        var profiles = new[]
        {
            new DriverProfileDefinition(
                "System Variable Events",
                new[] { "System Variable Events #2" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<DriverProfileDiscoveryRule>(),
                Array.Empty<DriverProfileAnalysisRule>(),
                Array.Empty<string>()),
            new DriverProfileDefinition(
                "Clipsal C-Bus",
                new[] { "Temp Driver" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<DriverProfileDiscoveryRule>(),
                Array.Empty<DriverProfileAnalysisRule>(),
                Array.Empty<string>())
        };

        var registry = new DriverProfileRegistry(profiles);
        var bundle = DriverProfileModule.Integrate(preload, registry);

        Assert.Equal(2, bundle.Matches.Count);
        Assert.Contains(bundle.Matches, match => match.DriverDeviceId == 1 && match.Profile.DeviceName == "System Variable Events");
        Assert.Contains(bundle.Matches, match => match.DriverDeviceId == 2 && match.Profile.DeviceName == "Clipsal C-Bus");
    }

    [Fact]
    public void InternalCatalogContainsInternalProfile()
    {
        var profile = DriverProfileCatalog.Internal().Single();

        Assert.Equal("RTI Internal", profile.DeviceName);
        Assert.Contains(profile.DiscoveryRules, rule => rule.Description.Contains("Device Page Mapping", StringComparison.Ordinal));
    }
}
