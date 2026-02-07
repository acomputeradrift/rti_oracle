using System;
using OracleByFPCLtd.DriverProfiles.Models;

namespace OracleByFPCLtd.DriverProfiles.Matching;

public sealed class DriverProfileMatcher
{
    public DriverProfileDefinition? Find(string deviceName, DriverProfileRegistry registry)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return null;
        }

        foreach (var profile in registry.Profiles)
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
