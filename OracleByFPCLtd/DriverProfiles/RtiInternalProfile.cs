using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class RtiInternalProfile
{
    private static readonly Regex PagePattern = new Regex(
        @"(?<prefix>.*?\bChange to page\s+)(?<page>\d+)(?<suffix>\s+on device\s+'(?<device>[^']+)'.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileMapper Mapper { get; } = new RtiInternalMapper();

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
        new List<string>(),
        Mapper: Mapper);

    private sealed class RtiInternalMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            mappedText = rawText ?? "";
            unresolved = false;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }

            var match = PagePattern.Match(rawText);
            if (!match.Success)
            {
                return false;
            }

            if (!int.TryParse(match.Groups["page"].Value, out var pageNumber) || pageNumber <= 0)
            {
                unresolved = true;
                return true;
            }

            if (!TryBuildDeviceNameMap(bundle.System.DiagnosticsMapping, out var deviceNameToId)
                || !deviceNameToId.TryGetValue(match.Groups["device"].Value, out var deviceId))
            {
                unresolved = true;
                return true;
            }

            var pageIndex = pageNumber - 1;
            var key = $"{deviceId}|{pageIndex}";
            if (!bundle.System.PageIndexMap.TryGetValue(key, out var pageName) || string.IsNullOrWhiteSpace(pageName))
            {
                unresolved = true;
                return true;
            }

            mappedText = $"{match.Groups["prefix"].Value}\"{pageName}\"{match.Groups["suffix"].Value}";
            return true;
        }

        private static bool TryBuildDeviceNameMap(
            IEnumerable<OracleByFPCLtd.ProjectData.DiagnosticsMappingEntry> mapping,
            out Dictionary<string, int> deviceNameToId)
        {
            deviceNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in mapping)
            {
                if (!deviceNameToId.ContainsKey(entry.DeviceName))
                {
                    deviceNameToId[entry.DeviceName] = entry.DeviceId;
                }

                if (!string.IsNullOrWhiteSpace(entry.DeviceDisplayName)
                    && !deviceNameToId.ContainsKey(entry.DeviceDisplayName))
                {
                    deviceNameToId[entry.DeviceDisplayName] = entry.DeviceId;
                }
            }

            return deviceNameToId.Count > 0;
        }
    }
}
