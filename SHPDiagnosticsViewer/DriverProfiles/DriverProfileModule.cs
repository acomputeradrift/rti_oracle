using System;
using System.Collections.Generic;
using System.Linq;
using SHPDiagnosticsViewer.ProjectData;

namespace SHPDiagnosticsViewer.DriverProfiles;

public sealed record DriverProfileDiscoveryRule(string Description, string Sql);
public sealed record DriverProfileAnalysisRule(string Description, string ExampleInput, string ExampleOutput);

public sealed record DriverProfileDefinition(
    string DeviceName,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> DiscoveryKeys,
    IReadOnlyList<string> DiscoveryPrefixes,
    IReadOnlyList<DriverProfileDiscoveryRule> DiscoveryRules,
    IReadOnlyList<DriverProfileAnalysisRule> AnalysisRules,
    IReadOnlyList<string> Notes);

public sealed record DriverProfileMatch(int DriverDeviceId, DriverConfigEntry DriverConfig, DriverProfileDefinition Profile);

public sealed class DriverProfileRegistry
{
    private readonly IReadOnlyList<DriverProfileDefinition> _profiles;

    public DriverProfileRegistry(IEnumerable<DriverProfileDefinition> profiles)
    {
        _profiles = profiles?.ToList() ?? new List<DriverProfileDefinition>();
    }

    public IReadOnlyList<DriverProfileDefinition> Profiles => _profiles;

    public DriverProfileDefinition? Find(string deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return null;
        }

        foreach (var profile in _profiles)
        {
            if (string.Equals(profile.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase))
            {
                return profile;
            }

            foreach (var alias in profile.Aliases)
            {
                if (string.Equals(alias, deviceName, StringComparison.OrdinalIgnoreCase))
                {
                    return profile;
                }
            }

            if (string.Equals(profile.DeviceName, "System Variable Events", StringComparison.OrdinalIgnoreCase)
                && deviceName.StartsWith("System Variable Events", StringComparison.OrdinalIgnoreCase))
            {
                return profile;
            }
        }

        return null;
    }
}

public sealed record DriverProfileBundle(IReadOnlyList<DriverProfileMatch> Matches);

public static class DriverProfileModule
{
    public static DriverProfileBundle Integrate(ApexDiscoveryPreloadResult preload, DriverProfileRegistry registry)
    {
        if (preload is null)
        {
            throw new ArgumentNullException(nameof(preload));
        }
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        var matches = new List<DriverProfileMatch>();
        foreach (var entry in preload.DriverConfigMap)
        {
            var profile = registry.Find(entry.Value.DeviceName);
            if (profile is null)
            {
                continue;
            }

            matches.Add(new DriverProfileMatch(entry.Key, entry.Value, profile));
        }

        return new DriverProfileBundle(matches);
    }
}

public static class DriverProfileCatalog
{
    public static IReadOnlyList<DriverProfileDefinition> All()
    {
        return new[] { RtiAd64Profile.Definition, RtiInternalProfile.Definition, RtiSystemVariableEventsProfile.Definition };
    }

    public static IReadOnlyList<DriverProfileDefinition> Internal()
    {
        return new[] { RtiInternalProfile.Definition };
    }
}

public static class DriverProfileRegistryFactory
{
    public static DriverProfileRegistry CreateDefault()
    {
        return new DriverProfileRegistry(DriverProfileCatalog.All());
    }
}
