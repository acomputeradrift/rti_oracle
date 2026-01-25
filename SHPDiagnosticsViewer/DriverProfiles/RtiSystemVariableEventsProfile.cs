using System;
using System.Collections.Generic;

namespace SHPDiagnosticsViewer.DriverProfiles;

public static class RtiSystemVariableEventsProfile
{
    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "System Variable Events",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>());
}
