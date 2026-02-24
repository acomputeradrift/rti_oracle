using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class ClipsalCbusProfile
{
    private static readonly Regex CommandPattern = new Regex(
        "Driver - Command:\\s*'(?<command>[^']+)'",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EventPattern = new Regex(
        "App\\s+(?<app>\\d+)\\s*,\\s*Group\\s+(?<group>\\d+)\\s+(?<state>On|Off)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ZoneIdPattern = new Regex("\\((?<zone>\\d+)\\)", RegexOptions.Compiled);

    public static IDriverProfileMapper Mapper { get; } = new ClipsalCbusMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "Clipsal C-Bus",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        new List<AdditionalInfoSheetSchema>
        {
            new("Clipsal C-Bus", new List<AdditionalInfoColumn>
            {
                new("AppID", AdditionalInfoColumnRole.AppId),
                new("GroupID", AdditionalInfoColumnRole.GroupId),
                new("GroupRoom", AdditionalInfoColumnRole.GroupRoom),
                new("GroupName", AdditionalInfoColumnRole.GroupName)
            }),
            new("Clipsal C-Bus Scenes", new List<AdditionalInfoColumn>
            {
                new("AppID", AdditionalInfoColumnRole.AppId),
                new("GroupID", AdditionalInfoColumnRole.GroupId),
                new("ActionSelector", AdditionalInfoColumnRole.ActionSelector),
                new("SceneName", AdditionalInfoColumnRole.SceneName)
            }),
            new("Clipsal C-Bus HVAC", new List<AdditionalInfoColumn>
            {
                new("GroupID", AdditionalInfoColumnRole.GroupId),
                new("ZoneID", AdditionalInfoColumnRole.ZoneId),
                new("ZoneName", AdditionalInfoColumnRole.ZoneName)
            })
        },
        Mapper);

    private sealed class ClipsalCbusMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            mappedText = rawText ?? "";
            unresolved = false;
            if (string.IsNullOrWhiteSpace(rawText) || bundle is null)
            {
                return false;
            }

            if (TryMapCommand(rawText, bundle, out mappedText, out unresolved))
            {
                return true;
            }

            if (TryMapEvent(rawText, bundle, out mappedText, out unresolved))
            {
                return true;
            }

            return false;
        }

        private static bool TryMapCommand(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            mappedText = rawText;
            unresolved = false;

            var match = CommandPattern.Match(rawText);
            if (!match.Success)
            {
                return false;
            }

            var commandText = match.Groups["command"].Value;
            if (!TryParseCommand(commandText, out var prefix, out var name, out var args))
            {
                return false;
            }

            if (!string.Equals(prefix, "Clipsal C-Bus\\General", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(prefix, "Clipsal C-Bus\\HVAC", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!bundle.Additional.Drivers.TryGetValue(Definition.DeviceName, out var driverData))
            {
                unresolved = true;
                return true;
            }

            var mappedArgs = args.ToList();
            if (prefix.EndsWith("\\General", StringComparison.OrdinalIgnoreCase)
                && name.Equals("Immediate Switch", StringComparison.OrdinalIgnoreCase))
            {
                if (mappedArgs.Count >= 3)
                {
                    mappedArgs[0] = MapImmediateSwitchState(mappedArgs[0], ref unresolved);
                    mappedArgs[1] = MapCbusGroup(driverData, mappedArgs[2], mappedArgs[1], ref unresolved);
                    mappedArgs.RemoveAt(2);
                }
            }
            else if (prefix.EndsWith("\\General", StringComparison.OrdinalIgnoreCase)
                && name.Equals("Ramp to level", StringComparison.OrdinalIgnoreCase))
            {
                if (mappedArgs.Count >= 4)
                {
                    mappedArgs[0] = MapRampRate(mappedArgs[0], ref unresolved);
                    mappedArgs[1] = MapCbusScene(driverData, mappedArgs[3], mappedArgs[1], mappedArgs[2], ref unresolved);
                    mappedArgs.RemoveAt(3);
                    mappedArgs.RemoveAt(2);
                }
            }
            else if (prefix.EndsWith("\\HVAC", StringComparison.OrdinalIgnoreCase)
                && name.Equals("HVAC Zone Setpoint Up", StringComparison.OrdinalIgnoreCase))
            {
                if (mappedArgs.Count >= 2)
                {
                    mappedArgs[0] = MapHvacZone(driverData, mappedArgs[0], mappedArgs[1], ref unresolved);
                    mappedArgs.RemoveAt(1);
                }
            }
            else
            {
                return false;
            }

            var updatedCommandBody = $"{name}({string.Join(", ", mappedArgs)})";
            var updatedCommandText = $"{prefix}\\{updatedCommandBody}";
            var replacementStart = match.Groups["command"].Index;
            mappedText = rawText.Substring(0, replacementStart)
                + updatedCommandText
                + rawText.Substring(replacementStart + match.Groups["command"].Length);
            return true;
        }

        private static bool TryMapEvent(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            mappedText = rawText;
            unresolved = false;

            if (!rawText.Contains("Driver event", StringComparison.OrdinalIgnoreCase)
                || !rawText.Contains("Clipsal C-Bus\\", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var match = EventPattern.Match(rawText);
            if (!match.Success)
            {
                return false;
            }

            if (!bundle.Additional.Drivers.TryGetValue(Definition.DeviceName, out var driverData))
            {
                unresolved = true;
                return true;
            }

            var appId = match.Groups["app"].Value;
            var groupId = match.Groups["group"].Value;
            var groupPhrase = $"Group {groupId}";
            var replacement = ResolveGroupReplacement(driverData, appId, groupId, ref unresolved);
            var updatedSegment = match.Value.Replace(groupPhrase, replacement, StringComparison.Ordinal);

            mappedText = rawText.Substring(0, match.Index)
                + updatedSegment
                + rawText.Substring(match.Index + match.Length);
            return true;
        }

        private static bool TryParseCommand(string commandText, out string prefix, out string name, out string[] args)
        {
            prefix = string.Empty;
            name = string.Empty;
            args = Array.Empty<string>();
            var lastSlash = commandText.LastIndexOf('\\');
            if (lastSlash <= 0)
            {
                return false;
            }

            prefix = commandText.Substring(0, lastSlash);
            var commandBody = commandText.Substring(lastSlash + 1);
            var openIndex = commandBody.IndexOf('(');
            var closeIndex = commandBody.LastIndexOf(')');
            if (openIndex <= 0 || closeIndex <= openIndex)
            {
                return false;
            }

            name = commandBody.Substring(0, openIndex).Trim();
            var argsText = commandBody.Substring(openIndex + 1, closeIndex - openIndex - 1);
            args = argsText
                .Split(',', StringSplitOptions.TrimEntries)
                .Where(arg => !string.IsNullOrWhiteSpace(arg))
                .ToArray();
            return !string.IsNullOrWhiteSpace(name);
        }

        private static string MapImmediateSwitchState(string stateText, ref bool unresolved)
        {
            if (int.TryParse(stateText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var state)
                && state == 1)
            {
                return "Off";
            }

            unresolved = true;
            return $"{stateText} [Unknown State!]";
        }

        private static string MapRampRate(string rateText, ref bool unresolved)
        {
            if (int.TryParse(rateText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rate))
            {
                if (rate == 2)
                {
                    return "Instantaneous";
                }
                if (rate == 10)
                {
                    return "4 seconds";
                }
            }

            unresolved = true;
            return $"{rateText} [Unknown State!]";
        }

        private static string MapCbusGroup(AdditionalDriverData data, string appIdText, string groupIdText, ref bool unresolved)
        {
            if (!TryParseIndex(appIdText, out var appId) || !TryParseIndex(groupIdText, out var groupId))
            {
                unresolved = true;
                return $"{groupIdText} [No Map!]";
            }

            if (data.CbusGroups.TryGetValue((appId, groupId), out var entry))
            {
                return FormatGroupName(entry);
            }

            unresolved = true;
            return $"{groupIdText} [No Map!]";
        }

        private static string ResolveGroupReplacement(AdditionalDriverData data, string appIdText, string groupIdText, ref bool unresolved)
        {
            if (!TryParseIndex(appIdText, out var appId) || !TryParseIndex(groupIdText, out var groupId))
            {
                unresolved = true;
                return $"Group {groupIdText} [No Map!]";
            }

            if (data.CbusGroups.TryGetValue((appId, groupId), out var entry))
            {
                return FormatGroupName(entry);
            }

            var sceneNames = data.CbusScenes
                .Where(entry => entry.Key.AppId == appId && entry.Key.GroupId == groupId && !string.IsNullOrWhiteSpace(entry.Value.SceneName))
                .Select(entry => entry.Value.SceneName.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (sceneNames.Count == 1)
            {
                return sceneNames[0];
            }

            unresolved = true;
            return $"Group {groupIdText} [No Map!]";
        }

        private static string MapCbusScene(
            AdditionalDriverData data,
            string appIdText,
            string groupIdText,
            string actionSelectorText,
            ref bool unresolved)
        {
            if (!TryParseIndex(appIdText, out var appId)
                || !TryParseIndex(groupIdText, out var groupId)
                || !TryParseIndex(actionSelectorText, out var actionSelector))
            {
                unresolved = true;
                return $"{groupIdText} [No Map!]";
            }

            if (data.CbusScenes.TryGetValue((appId, groupId, actionSelector), out var entry)
                && !string.IsNullOrWhiteSpace(entry.SceneName))
            {
                return entry.SceneName;
            }

            unresolved = true;
            return $"{groupIdText} [No Map!]";
        }

        private static string MapHvacZone(AdditionalDriverData data, string groupIdText, string zoneText, ref bool unresolved)
        {
            if (!TryParseIndex(groupIdText, out var groupId))
            {
                unresolved = true;
                return $"{groupIdText} [No Map!]";
            }

            var match = ZoneIdPattern.Match(zoneText);
            var zoneIdText = match.Success ? match.Groups["zone"].Value : zoneText;
            if (!TryParseIndex(zoneIdText, out var zoneId))
            {
                unresolved = true;
                return $"{zoneText} [No Map!]";
            }

            if (data.CbusHvacZones.TryGetValue((groupId, zoneId), out var entry))
            {
                var name = entry.ZoneName;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }

                unresolved = true;
                return $"{zoneText} [No Map!]";
            }

            unresolved = true;
            return $"{zoneText} [No Map!]";
        }

        private static string FormatGroupName(CbusGroupEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.GroupRoom))
            {
                return entry.GroupName;
            }

            if (string.IsNullOrWhiteSpace(entry.GroupName))
            {
                return entry.GroupRoom;
            }

            return $"{entry.GroupRoom} {entry.GroupName}";
        }

        private static bool TryParseIndex(string value, out int index)
        {
            index = 0;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
            {
                return true;
            }

            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                return false;
            }

            var rounded = Math.Round(number);
            if (Math.Abs(number - rounded) > 0.0001)
            {
                return false;
            }

            index = (int)rounded;
            return true;
        }
    }
}
