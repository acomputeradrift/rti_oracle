using System;
using System.Collections.Generic;
using OracleByFPCLtd.DriverProfiles.Matching;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData;

namespace OracleByFPCLtd.DriverProfiles.Integration;

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

        var matcher = new DriverProfileMatcher();
        var matches = new List<DriverProfileMatch>();
        foreach (var entry in preload.DriverConfigMap)
        {
            var profile = matcher.Find(entry.Value.DeviceName, registry);
            if (profile is null)
            {
                continue;
            }

            matches.Add(new DriverProfileMatch(entry.Key, entry.Value, profile));
        }

        return new DriverProfileBundle(matches);
    }
}
