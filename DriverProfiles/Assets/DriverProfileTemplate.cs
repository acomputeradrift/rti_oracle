using System;
using System.Collections.Generic;
using SHPDiagnosticsViewer.DriverProfiles;

namespace SHPDiagnosticsViewer.DriverProfiles;

public static class DriverProfileTemplate
{
    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "<DeviceName>",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>
        {
            new DriverProfileDiscoveryRule(
                "<Discovery Rule Description>",
                """
SELECT ...
""")
        },
        new List<DriverProfileAnalysisRule>
        {
            new DriverProfileAnalysisRule(
                "<Analysis Rule Description>",
                "<Example Input>",
                "<Example Output>")
        },
        new List<string>
        {
            "<Notes>"
        });
}
