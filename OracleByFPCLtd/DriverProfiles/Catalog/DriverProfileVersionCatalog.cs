using System;
using System.Collections.Generic;

namespace OracleByFPCLtd.DriverProfiles.Catalog;

public static class DriverProfileVersionCatalog
{
    // Manual version stamps for driver profiles. Update the timestamp when a profile's
    // mapping or formatting behavior changes.
    private static readonly Dictionary<string, DateTimeOffset> LastUpdatedUtcByDriver = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Activities"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["AVProEdge MXNet_1G"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["BijouSeries"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["Clipsal C-Bus"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["DSC PowerSeries"] = DateTimeOffset.Parse("2026-02-21T00:00:00Z"),
        ["Jandy iAquaLink"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["Layer Switch v2.x"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["Lutron Caseta / RA2 Select"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["QMotion QzHub3"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["RTI AD DSP Matrix"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["RTI AD-64"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["RTI Internal"] = DateTimeOffset.Parse("2026-02-22T00:00:00Z"),
        ["RTI Music"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["RTI System Variable Events"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["RTI VIP-UHD-CTRL"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["RTI Virtual Multiroom Amp"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["Samsung Ex-Link"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["Sonance 8130"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["Sonos"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["System Manager"] = DateTimeOffset.Parse("2026-02-23T00:00:00Z"),
        ["System Variables"] = DateTimeOffset.Parse("2026-02-21T00:00:00Z"),
        ["Two Way Strings"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["Vaux Lattis Matrix"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["VHDx"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["Yamaha AVENTAGE"] = DateTimeOffset.Parse("2026-02-11T00:00:00Z"),
        ["Venstar ColorTouch"] = DateTimeOffset.Parse("2026-02-21T00:00:00Z")
    };

    public static bool TryGetLastUpdatedUtc(string driverName, out DateTimeOffset lastUpdatedUtc)
    {
        if (string.IsNullOrWhiteSpace(driverName))
        {
            lastUpdatedUtc = default;
            return false;
        }

        return LastUpdatedUtcByDriver.TryGetValue(driverName, out lastUpdatedUtc);
    }
}
