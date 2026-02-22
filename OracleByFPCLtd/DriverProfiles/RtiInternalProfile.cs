using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class RtiInternalProfile
{
    private const string RtiRcm12RelayModule = "RTI RCM-12 Relay Module";
    private static readonly Regex PagePattern = new Regex(
        @"(?<prefix>.*?\bChange to page\s+)(?<page>\d+)(?<suffix>\s+on device\s+'(?<device>[^']+)'.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex IrPortPattern = new Regex(
        @"^(?:\[(?<timestamp>[^\]]+)\]\s*)?IR - Port:'(?<processor>[^']+)','(?<port>[^']+)' Command:'(?<command>[^']+)'(?:\s+Sustain:(?<sustain>\S+))?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RelayTriggerPattern = new Regex(
        @"^(?:\[(?<timestamp>[^\]]+)\]\s*)?Relay/Trigger - Port:'(?<processor>[^']+)','(?<port>[^']+)' Action:(?<action>\S+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MacroStartEndPattern = new Regex(
        @"^(?:\[[^\]]+\]\s*)?Macro - (?:Start|End)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ButtonDownPattern = new Regex(
        @"^(?:\[[^\]]+\]\s*)?Button Down - Device:'[^']+'(?:\s+Transport:.*)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ButtonDownTransportSuffixPattern = new Regex(
        @"\s+Transport:.*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ButtonUpPattern = new Regex(
        @"^(?:\[[^\]]+\]\s*)?Button Up(?:\s|$).*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DeviceConnectedDisconnectedPattern = new Regex(
        @"^(?:\[[^\]]+\]\s*)?Device\s+'[^']+'\s+has\s+(?:connected|disconnected)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SystemMacroStartEndPattern = new Regex(
        @"^(?:\[[^\]]+\]\s*)?System macro '(?<macro>[^']+)' - (?<phase>Start|End)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StopMacroPattern = new Regex(
        @"^(?:\[[^\]]+\]\s*)?Stop macro$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RelayIndexPattern = new Regex(
        @"\bRELAY\s+(?<index>\d+)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex IrNoiseSuffixPattern = new Regex(
        @"\s*\[\s*/\s*/\s*\]\s*$",
        RegexOptions.Compiled);

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
        new List<AdditionalInfoSheetSchema>
        {
            new("RTI RCM-12 Relay Module", new List<AdditionalInfoColumn>
            {
                new("RelayIndex", AdditionalInfoColumnRole.RelayIndex),
                new("RelayName", AdditionalInfoColumnRole.RelayName)
            })
        },
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
                if (TryMapPortCommand(rawText, bundle, out mappedText, out unresolved))
                {
                    return true;
                }

                return TryMapInternalLifecycle(rawText, out mappedText, out unresolved);
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

        private static bool TryMapInternalLifecycle(string rawText, out string mappedText, out bool unresolved)
        {
            mappedText = rawText;
            unresolved = false;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }

            if (ButtonDownPattern.IsMatch(rawText))
            {
                mappedText = ButtonDownTransportSuffixPattern.Replace(rawText, "");
                return true;
            }

            if (MacroStartEndPattern.IsMatch(rawText)
                || ButtonUpPattern.IsMatch(rawText)
                || DeviceConnectedDisconnectedPattern.IsMatch(rawText)
                || SystemMacroStartEndPattern.IsMatch(rawText)
                || StopMacroPattern.IsMatch(rawText))
            {
                return true;
            }

            return false;
        }

        private static bool TryMapPortCommand(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            mappedText = rawText;
            unresolved = false;

            var irMatch = IrPortPattern.Match(rawText);
            if (irMatch.Success)
            {
                var processor = irMatch.Groups["processor"].Value.Trim();
                var port = irMatch.Groups["port"].Value.Trim();
                var command = NormalizeIrCommand(irMatch.Groups["command"].Value);
                if (string.Equals(port, RtiRcm12RelayModule, StringComparison.OrdinalIgnoreCase))
                {
                    command = MapRcm12RelayCommand(command, bundle, ref unresolved);
                }

                mappedText = $"IR Command (Internal): '{command} -> {processor}: {port}'";
                return true;
            }

            var relayMatch = RelayTriggerPattern.Match(rawText);
            if (relayMatch.Success)
            {
                var processor = relayMatch.Groups["processor"].Value.Trim();
                var port = relayMatch.Groups["port"].Value.Trim();
                var action = relayMatch.Groups["action"].Value.Trim();
                mappedText = $"Relay/Trigger Command (Internal): '{action} -> {processor}: {port}'";
                return true;
            }

            return false;
        }

        private static string NormalizeIrCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return command;
            }

            return IrNoiseSuffixPattern.Replace(command.Trim(), "").Trim();
        }

        private static string MapRcm12RelayCommand(string command, ProjectDataBundle bundle, ref bool unresolved)
        {
            var relayMatch = RelayIndexPattern.Match(command);
            if (!relayMatch.Success || !int.TryParse(relayMatch.Groups["index"].Value, out var relayIndex))
            {
                return command;
            }

            if (!bundle.Additional.Drivers.TryGetValue(Definition.DeviceName, out var data)
                || !data.RelayNames.TryGetValue(relayIndex, out var relayName)
                || string.IsNullOrWhiteSpace(relayName))
            {
                unresolved = true;
                return command;
            }

            return RelayIndexPattern.Replace(command, relayName, 1);
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
