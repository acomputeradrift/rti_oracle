using System;
using System.Collections.Generic;

namespace SHPDiagnosticsViewer.DriverProfiles;

public static class RtiAd64Profile
{
    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "RTI AD-64",
        Array.Empty<string>(),
        new[] { "GroupCount", "SourceCount", "ZoneCount" },
        new[] { "GroupName", "SourceName", "ZoneName" },
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>());
}
