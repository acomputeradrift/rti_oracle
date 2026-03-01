using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProcessingEngine.Models;
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
    private static readonly Regex SerialPortPattern = new Regex(
        @"^(?:\[(?<timestamp>[^\]]+)\]\s*)?Serial - Port:'(?<processor>[^']+)','(?<port>[^']+)' Command:'(?<command>[^']+)'(?:\s+Sustain:(?<sustain>\S+))?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SerialPortConfigPattern = new Regex(
        @"^(?:\[(?<timestamp>[^\]]+)\]\s*)?Serial - Port:'(?<processor>[^']+)','(?<port>[^']+)'\s+Baud:(?<baud>\S+)\s+StopBits:(?<stopBits>\S+)\s+DataBits:(?<dataBits>\S+)\s+Parity:(?<parity>\S+)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SerialPortDataPattern = new Regex(
        @"^(?:\[(?<timestamp>[^\]]+)\]\s*)?Serial - Port:'(?<processor>[^']+)','(?<port>[^']+)'\s+Data:(?<data>.+)\s*$",
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
    private static readonly Regex SenseEventPattern = new Regex(
        @"^(?:\[(?<timestamp>[^\]]+)\]\s*)?Sense event\s+'(?<event>When\s+[^']+)'\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ScheduledEventPattern = new Regex(
        @"^(?:\[(?<timestamp>[^\]]+)\]\s*)?Scheduled event\s+'(?<event>[^']+)'\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DelayPattern = new Regex(
        @"^(?:\[(?<timestamp>[^\]]+)\]\s*)?Delay\s+(?<duration>\d+ms)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex MacroEventPattern = new Regex(
        @"^(?:\[[^\]]+\]\s*)?Macro event$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DeviceSendFailedNotConnectedPattern = new Regex(
        @"^(?:\[[^\]]+\]\s*)?'[^']+','[^']+'\s*-\s*Send failed,\s*device not connected$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RelayIndexPattern = new Regex(
        @"\bRELAY\s+(?<index>\d+)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex IrNoiseSuffixPattern = new Regex(
        @"\s*\[\s*/\s*/\s*\]\s*$",
        RegexOptions.Compiled);

    public static IDriverProfileResultMapper ResultMapper { get; } = new RtiInternalResultMapper();

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
        ResultMapper: ResultMapper);


    private sealed class RtiInternalResultMapper : IDriverProfileResultMapper
    {
        public DriverProfileMapResult TryMap(string rawText, ProjectDataBundle bundle)
        {
            var defaultText = rawText ?? "";
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
            }

            var match = PagePattern.Match(rawText);
            if (match.Success)
            {
                if (!int.TryParse(match.Groups["page"].Value, out var pageNumber) || pageNumber <= 0)
                {
                    return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.Unresolved);
                }

                if (!TryBuildDeviceNameMap(bundle.System.DiagnosticsMapping, out var deviceNameToId)
                    || !deviceNameToId.TryGetValue(match.Groups["device"].Value, out var deviceId))
                {
                    return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.Unresolved);
                }

                var pageIndex = pageNumber - 1;
                var key = $"{deviceId}|{pageIndex}";
                if (!bundle.System.PageIndexMap.TryGetValue(key, out var pageName) || string.IsNullOrWhiteSpace(pageName))
                {
                    return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.Unresolved);
                }

                var mappedText = $"{match.Groups["prefix"].Value}\"{pageName}\"{match.Groups["suffix"].Value}";
                return new DriverProfileMapResult(
                    true,
                    mappedText,
                    DriverProfileProcessingStatus.Resolved,
                    new MappingResolution(
                        "page",
                        pageNumber.ToString(),
                        pageName,
                        "Apex",
                        Profile: "RTI Internal",
                        Device: match.Groups["device"].Value));
            }

            if (TryMapPortCommand(rawText, bundle, out var portMappedText, out var portUnresolved))
            {
                return new DriverProfileMapResult(
                    true,
                    portMappedText,
                    portUnresolved ? DriverProfileProcessingStatus.Unresolved : DriverProfileProcessingStatus.Resolved);
            }

            if (TryMapInternalLifecycle(rawText, out var lifecycleMappedText, out var lifecycleUnresolved))
            {
                return new DriverProfileMapResult(
                    true,
                    lifecycleMappedText,
                    DetermineLifecycleStatus(rawText, lifecycleMappedText, lifecycleUnresolved));
            }

            return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
        }

        private static bool TryMapInternalLifecycle(string rawText, out string mappedText, out bool unresolved)
        {
            mappedText = rawText;
            unresolved = false;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }

            var senseMatch = SenseEventPattern.Match(rawText);
            if (senseMatch.Success)
            {
                var timestampPrefix = BuildTimestampPrefix(senseMatch.Groups["timestamp"].Value);
                var senseEventText = senseMatch.Groups["event"].Value.Trim();
                mappedText = $"{timestampPrefix}Sense Event (Internal): '{senseEventText}.'";
                return true;
            }

            var scheduledMatch = ScheduledEventPattern.Match(rawText);
            if (scheduledMatch.Success)
            {
                var timestampPrefix = BuildTimestampPrefix(scheduledMatch.Groups["timestamp"].Value);
                var scheduleText = scheduledMatch.Groups["event"].Value.Trim();
                mappedText = $"{timestampPrefix}Scheduled Event (Internal): 'When {scheduleText} happened.'";
                return true;
            }

            var delayMatch = DelayPattern.Match(rawText);
            if (delayMatch.Success)
            {
                var timestampPrefix = BuildTimestampPrefix(delayMatch.Groups["timestamp"].Value);
                var duration = delayMatch.Groups["duration"].Value.Trim();
                mappedText = $"{timestampPrefix}Driver Command (Internal): 'Delay {duration}.'";
                return true;
            }

            if (ButtonDownPattern.IsMatch(rawText))
            {
                mappedText = ButtonDownTransportSuffixPattern.Replace(rawText, "");
                return true;
            }

            if (MacroStartEndPattern.IsMatch(rawText)
                || ButtonUpPattern.IsMatch(rawText)
                || DeviceConnectedDisconnectedPattern.IsMatch(rawText)
                || DeviceSendFailedNotConnectedPattern.IsMatch(rawText)
                || SystemMacroStartEndPattern.IsMatch(rawText)
                || StopMacroPattern.IsMatch(rawText)
                || MacroEventPattern.IsMatch(rawText))
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
                var timestampPrefix = BuildTimestampPrefix(irMatch.Groups["timestamp"].Value);
                var processor = irMatch.Groups["processor"].Value.Trim();
                var port = irMatch.Groups["port"].Value.Trim();
                var command = NormalizeIrCommand(irMatch.Groups["command"].Value);
                if (string.Equals(port, RtiRcm12RelayModule, StringComparison.OrdinalIgnoreCase))
                {
                    command = MapRcm12RelayCommand(command, bundle, ref unresolved);
                }

                mappedText = $"{timestampPrefix}IR Command (Internal): '{command} -> {processor}: {port}'";
                return true;
            }

            var relayMatch = RelayTriggerPattern.Match(rawText);
            if (relayMatch.Success)
            {
                var timestampPrefix = BuildTimestampPrefix(relayMatch.Groups["timestamp"].Value);
                var processor = relayMatch.Groups["processor"].Value.Trim();
                var port = relayMatch.Groups["port"].Value.Trim();
                var action = relayMatch.Groups["action"].Value.Trim();
                mappedText = $"{timestampPrefix}Relay/Trigger Command (Internal): '{action} -> {processor}: {port}'";
                return true;
            }

            var serialMatch = SerialPortPattern.Match(rawText);
            if (serialMatch.Success)
            {
                var timestampPrefix = BuildTimestampPrefix(serialMatch.Groups["timestamp"].Value);
                var processor = serialMatch.Groups["processor"].Value.Trim();
                var port = serialMatch.Groups["port"].Value.Trim();
                var command = NormalizeSerialCommand(serialMatch.Groups["command"].Value);
                mappedText = $"{timestampPrefix}Serial Command (Internal): '{command} -> {processor}: {port}'";
                return true;
            }

            var serialConfigMatch = SerialPortConfigPattern.Match(rawText);
            if (serialConfigMatch.Success)
            {
                var timestampPrefix = BuildTimestampPrefix(serialConfigMatch.Groups["timestamp"].Value);
                var processor = serialConfigMatch.Groups["processor"].Value.Trim();
                var port = serialConfigMatch.Groups["port"].Value.Trim();
                var baud = serialConfigMatch.Groups["baud"].Value.Trim();
                var stopBits = serialConfigMatch.Groups["stopBits"].Value.Trim();
                var dataBits = serialConfigMatch.Groups["dataBits"].Value.Trim();
                var parity = serialConfigMatch.Groups["parity"].Value.Trim();
                mappedText =
                    $"{timestampPrefix}Serial Command (Internal): 'Port set to Baud {baud}, StopBits {stopBits}, DataBits {dataBits}, Parity {parity} -> {processor}: {port}'";
                return true;
            }

            var serialDataMatch = SerialPortDataPattern.Match(rawText);
            if (serialDataMatch.Success)
            {
                var timestampPrefix = BuildTimestampPrefix(serialDataMatch.Groups["timestamp"].Value);
                var processor = serialDataMatch.Groups["processor"].Value.Trim();
                var port = serialDataMatch.Groups["port"].Value.Trim();
                var payload = NormalizeSerialDataPayload(serialDataMatch.Groups["data"].Value);
                mappedText = $"{timestampPrefix}Serial Command (Internal): '{payload} -> {processor}: {port}'";
                return true;
            }

            return false;
        }

        private static string BuildTimestampPrefix(string timestamp)
        {
            timestamp = timestamp?.Trim() ?? "";
            return string.IsNullOrWhiteSpace(timestamp) ? "" : $"[{timestamp}] ";
        }

        private static string NormalizeIrCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return command;
            }

            return IrNoiseSuffixPattern.Replace(command.Trim(), "").Trim();
        }

        private static string NormalizeSerialCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return command;
            }

            return command
                .Trim()
                .Replace("\\r", "", StringComparison.OrdinalIgnoreCase)
                .Replace("\r", "", StringComparison.Ordinal)
                .Trim();
        }

        private static string NormalizeSerialDataPayload(string data)
        {
            var raw = (data ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return raw;
            }

            var tokens = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var bytes = new List<byte>();
            foreach (var token in tokens)
            {
                var value = token.Trim();
                if (!value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    || value.Length != 4
                    || !byte.TryParse(value.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out var parsed))
                {
                    return raw;
                }

                bytes.Add(parsed);
            }

            if (bytes.Count == 0)
            {
                return raw;
            }

            var decoded = Encoding.ASCII.GetString(bytes.ToArray())
                .Replace("\r", "", StringComparison.Ordinal)
                .Replace("\n", "", StringComparison.Ordinal)
                .Trim();

            return string.IsNullOrWhiteSpace(decoded) ? raw : decoded;
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

        private static DriverProfileProcessingStatus DetermineLifecycleStatus(
            string rawText,
            string mappedText,
            bool unresolved)
        {
            if (unresolved)
            {
                return DriverProfileProcessingStatus.Unresolved;
            }

            return string.Equals(rawText, mappedText, StringComparison.Ordinal)
                ? DriverProfileProcessingStatus.PassThrough
                : DriverProfileProcessingStatus.Resolved;
        }
    }

    private static bool IsUnresolvedStatus(DriverProfileProcessingStatus status)
    {
        return status is DriverProfileProcessingStatus.NoFormat
            or DriverProfileProcessingStatus.NoMap
            or DriverProfileProcessingStatus.Unresolved
            or DriverProfileProcessingStatus.UnknownState;
    }
}
