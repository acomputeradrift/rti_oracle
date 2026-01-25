using System;
using System.Collections.Generic;

namespace SHPDiagnosticsViewer.DriverProfiles;

public static class RtiInternalProfile
{
    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "RTI Internal",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>
        {
            new DriverProfileDiscoveryRule(
                "Device Page Mapping",
                """
SELECT
  d.DeviceId AS DeviceId,
  p.PageOrder AS PageIndex,
  n.PageName AS PageName
FROM RTIDeviceData d
JOIN Devices dv ON d.DeviceId = dv.DeviceId
LEFT JOIN RTIDevicePageData p ON p.RTIAddress = d.RTIAddress
LEFT JOIN PageNames n ON p.PageNameId = n.PageNameId
ORDER BY d.DeviceId, p.PageOrder;
""")
        },
        new List<DriverProfileAnalysisRule>(),
        new List<string>());
}
