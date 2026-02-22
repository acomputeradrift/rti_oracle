using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OracleByFPCLtd.DriverProfiles.Services;

public static class DriverMessageTemplateFormatter
{
    private static readonly Regex TimestampRegex = new("^\\[(?<time>[^\\]]+)\\]\\s*(?<body>.*)$", RegexOptions.Compiled);
    private static readonly Regex CommandRegex = new("^Driver - Command:\\s*'(?<command>[^']+)'(?:\\s*(?<extra>.*))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex EventRegex = new("^Driver event\\s*'When\\s*'(?<event>[^']+)'\\s*happens on\\s*'(?<driver>[^'\\\\]+)\\\\(?<path>[^']*)''(?:\\s*(?<extra>.*))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool TryFormatDriverCommand(string mappedText, string driverName, out string formattedText)
    {
        formattedText = mappedText ?? "";
        if (string.IsNullOrWhiteSpace(mappedText))
        {
            return false;
        }

        if (!TryExtractTimestampAndBody(mappedText, out var timestamp, out var body))
        {
            return false;
        }

        if (!TryExtractCommandBody(body, out var command, out _))
        {
            return false;
        }

        if (!command.StartsWith(driverName + "\\", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!TryBuildSentence(driverName, command, out var sentence, out var extraInfo, out var skipPeriod))
        {
            return false;
        }

        if (!skipPeriod
            && !sentence.EndsWith(".", StringComparison.Ordinal)
            && !sentence.EndsWith("?", StringComparison.Ordinal)
            && !sentence.EndsWith("!", StringComparison.Ordinal))
        {
            sentence += ".";
        }

        formattedText = $"[{timestamp}] Driver Command ({driverName}): '{sentence}'";
        if (!string.IsNullOrWhiteSpace(extraInfo))
        {
            formattedText += $" {extraInfo}";
        }

        return true;
    }

    public static bool TryFormatDriverEvent(string mappedText, string driverName, out string formattedText)
    {
        formattedText = mappedText ?? "";
        if (string.IsNullOrWhiteSpace(mappedText))
        {
            return false;
        }

        if (!TryExtractTimestampAndBody(mappedText, out var timestamp, out var body))
        {
            return false;
        }

        var match = EventRegex.Match(body);
        if (!match.Success)
        {
            return false;
        }

        var eventDriverName = match.Groups["driver"].Value;
        if (!string.Equals(eventDriverName, driverName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var sentence = match.Groups["event"].Value.Trim();
        if (string.IsNullOrWhiteSpace(sentence))
        {
            return false;
        }

        if (!sentence.EndsWith(".", StringComparison.Ordinal))
        {
            sentence += ".";
        }

        formattedText = $"[{timestamp}] Driver Event ({driverName}): '{sentence}'";
        var extraInfo = match.Groups["extra"].Value;
        if (!string.IsNullOrWhiteSpace(extraInfo))
        {
            formattedText += $" {extraInfo}";
        }

        return true;
    }

    private static bool TryBuildSentence(string driverName, string command, out string sentence, out string extraInfo, out bool skipPeriod)
    {
        sentence = "";
        extraInfo = "";
        skipPeriod = false;

        if (!TryParseCommand(command, out var actionName, out var args))
        {
            return false;
        }

        switch (driverName)
        {
            case "Clipsal C-Bus":
                return TryBuildClipsalSentence(actionName, args, out sentence);
            case "Sonance 8130":
                return TryBuildSonanceSentence(actionName, args, out sentence);
            case "Yamaha AVENTAGE":
                return TryBuildYamahaSentence(actionName, args, out sentence);
            case "RTI Virtual Multiroom Amp":
                return TryBuildRtiVirtualMultiroomAmpSentence(command, actionName, args, out sentence);
            case "AVProEdge MXNet_1G":
                return TryBuildAvproSentence(actionName, args, out sentence);
            case "Two Way Strings":
                return TryBuildTwoWayStringsSentence(actionName, args, out sentence);
            case "System Manager":
                return TryBuildSystemManagerSentence(command, actionName, args, out sentence, out extraInfo, out skipPeriod);
            case "System Variables":
                return TryBuildSystemVariablesSentence(command, actionName, args, out sentence);
            case "Sonos":
                return TryBuildSonosSentence(actionName, args, out sentence);
            case "RTI AD-64":
                return TryBuildRtiAd64Sentence(actionName, args, out sentence);
            case "BijouSeries":
                return TryBuildBijouSentence(actionName, args, out sentence);
            case "Vaux Lattis Matrix":
                return TryBuildVauxSentence(actionName, args, out sentence);
            case "Layer Switch v2.x":
                return TryBuildLayerSwitchSentence(actionName, args, out sentence);
            case "Lutron Caseta / RA2 Select":
                return TryBuildLutronSentence(actionName, args, out sentence);
            case "Samsung Ex-Link":
                return TryBuildSamsungExLinkSentence(actionName, args, out sentence);
            case "QMotion QzHub3":
                return TryBuildQmotionSentence(actionName, args, out sentence);
            case "RTI VIP-UHD-CTRL":
                return TryBuildRtiVipSentence(command, actionName, args, out sentence);
            case "VHDx":
                return TryBuildVhdxSentence(actionName, args, out sentence);
            case "Jandy iAquaLink":
                return TryBuildJandySentence(actionName, out sentence);
            default:
                return false;
        }
    }

    private static bool TryBuildClipsalSentence(string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        if (actionName.Equals("Immediate Switch", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            sentence = $"{args[1]} switched to {args[0]}";
            return true;
        }

        if (actionName.Equals("Ramp to level", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            sentence = $"{args[1]} ramped over {args[0]}";
            return true;
        }

        if (actionName.Equals("HVAC Zone Setpoint Up", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            sentence = $"{args[0]} HVAC zone setpoint set to {args[1]}";
            return true;
        }

        return false;
    }

    private static bool TryBuildSonanceSentence(string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        if (actionName.Equals("Group Input Commands", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            sentence = $"{args[0]} input set to {args[1]}";
            return true;
        }

        if (actionName.Equals("Group Mute Commands", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            if (args[1].Equals("Mute Toggle", StringComparison.OrdinalIgnoreCase))
            {
                sentence = $"{args[0]} mute toggled";
                return true;
            }

            sentence = $"{args[0]} mute set to {args[1]}";
            return true;
        }

        if (actionName.Equals("Group Power Commands", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            sentence = $"{args[0]} power set to {NormalizePowerState(args[1])}";
            return true;
        }

        if (actionName.Equals("Master Power Command", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"Master power set to {NormalizePowerState(args[0])}";
            return true;
        }

        if (actionName.Equals("Group Volume Commands", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            var command = args[1];
            if (command.StartsWith("Volume Up +", StringComparison.OrdinalIgnoreCase))
            {
                sentence = $"{args[0]} volume increased by {command.Substring("Volume Up +".Length)}";
                return true;
            }

            if (command.Equals("Volume Up", StringComparison.OrdinalIgnoreCase))
            {
                sentence = $"{args[0]} volume increased";
                return true;
            }

            if (command.StartsWith("Volume Down -", StringComparison.OrdinalIgnoreCase))
            {
                sentence = $"{args[0]} volume decreased by {command.Substring("Volume Down -".Length)}";
                return true;
            }

            if (command.Equals("Volume Down", StringComparison.OrdinalIgnoreCase))
            {
                sentence = $"{args[0]} volume decreased";
                return true;
            }
        }

        return false;
    }

    private static bool TryBuildYamahaSentence(string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        if (actionName.Equals("Main Power", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"Main power set to {args[0]}";
            return true;
        }

        if (actionName.Equals("Main Input", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"Main input set to {args[0]}";
            return true;
        }

        if (actionName.Equals("Main Sound Program", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"Main sound program set to {args[0]}";
            return true;
        }

        if (actionName.Equals("Main Mute", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            if (args[0].Equals("Toggle", StringComparison.OrdinalIgnoreCase))
            {
                sentence = "Main mute toggled";
                return true;
            }

            sentence = $"Main mute set to {args[0]}";
            return true;
        }

        if (actionName.Equals("Volume Set", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"Main volume set to {args[0]}";
            return true;
        }

        if (actionName.Equals("Volume Up", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"Main volume adjusted by +{args[0]}";
            return true;
        }

        if (actionName.Equals("Volume Down", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"Main volume adjusted by -{args[0]}";
            return true;
        }

        return false;
    }

    private static bool TryBuildAvproSentence(string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        if (actionName.Equals("Switch Output (AV)", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            sentence = $"{args[1]} set to {args[0]}";
            return true;
        }

        if (actionName.Equals("CEC (Power)", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            sentence = $"CEC power for {args[0]} set to {NormalizePowerState(args[1])}";
            return true;
        }

        if (actionName.Equals("CEC (Hex)", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            sentence = $"CEC hex command {args[1]} send to {args[0]}";
            return true;
        }

        return false;
    }

    private static bool TryBuildRtiVirtualMultiroomAmpSentence(string command, string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        var parts = command.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        var room = parts[1].Trim();
        if (actionName.Equals("Power", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            if (args[0].Equals("Toggle", StringComparison.OrdinalIgnoreCase))
            {
                sentence = $"{room} power toggled";
                return true;
            }

            sentence = $"{room} power set to {args[0]}";
            return true;
        }

        return false;
    }

    private static bool TryBuildTwoWayStringsSentence(string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        if (actionName.EndsWith(" Power", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"{actionName[..^6]} power set to {NormalizePowerState(args[0])}";
            return true;
        }

        if (actionName.EndsWith(" Source", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"{actionName[..^7]} source set to {args[0]}";
            return true;
        }

        if (actionName.EndsWith(" Vol", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"{actionName[..^4]} volume set to {args[0]}";
            return true;
        }

        return false;
    }

    private static bool TryBuildSystemManagerSentence(string command, string actionName, IReadOnlyList<string> args, out string sentence, out string extraInfo, out bool skipPeriod)
    {
        sentence = "";
        extraInfo = "";
        skipPeriod = false;

        if (command.StartsWith("System Manager\\[Hide]\\Route Command(", StringComparison.OrdinalIgnoreCase)
            || command.Equals("System Manager\\[Hide]\\Room Off", StringComparison.OrdinalIgnoreCase))
        {
            sentence = command;
            extraInfo = "[No Format!]";
            skipPeriod = true;
            return true;
        }

        if (command.Equals("System Manager\\[Hide]\\System Off", StringComparison.OrdinalIgnoreCase))
        {
            sentence = "System set to Off";
            return true;
        }

        if (actionName.Equals("Set Source", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"Source set to {args[0]}";
            return true;
        }

        if (actionName.Equals("Set Selected Room", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"Selected room set to {args[0]}";
            return true;
        }

        if (actionName.Equals("Set Source By Room", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            sentence = $"Source for {args[0]} set to {args[1]}";
            return true;
        }

        if (actionName.Equals("Set Layer Visibility", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"Layer Visibility set to {args[0]}";
            return true;
        }

        return false;
    }

    private static bool TryBuildSystemVariablesSentence(string command, string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        if (command.StartsWith("System Variables\\Strings\\", StringComparison.OrdinalIgnoreCase))
        {
            if (actionName.Equals("Set", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
            {
                sentence = $"String {args[0]} set to {args[1]}";
                return true;
            }

            return false;
        }

        if (!command.StartsWith("System Variables\\Integers\\", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (actionName.Equals("Decrease", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            sentence = $"{FormatSystemVariableSubject(args[0])} decreased by {args[1]}";
            return true;
        }

        if (actionName.Equals("Increase", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            sentence = $"{FormatSystemVariableSubject(args[0])} increased by {args[1]}";
            return true;
        }

        if (actionName.Equals("Test", StringComparison.OrdinalIgnoreCase) && args.Count >= 3)
        {
            sentence = $"Testing: Is {FormatSystemVariableSubject(args[0])} {args[1]} to {args[2]}?";
            return true;
        }

        return false;
    }

    private static string FormatSystemVariableSubject(string value)
    {
        return int.TryParse(value, out var index) ? $"IntegerIndex {index}" : value;
    }

    private static bool TryBuildSonosSentence(string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        if ((actionName.Equals("Pause", StringComparison.OrdinalIgnoreCase) || actionName.Equals("Play", StringComparison.OrdinalIgnoreCase))
            && args.Count >= 1)
        {
            sentence = $"{StripParentheticalSuffix(args[0]).Trim()} transport set to {actionName}";
            return true;
        }

        return false;
    }

    private static bool TryBuildRtiAd64Sentence(string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        if (actionName.Equals("Power On", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"{NormalizeZoneName(args[0])} zone power set to On";
            return true;
        }

        if (actionName.Equals("Power Off", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"{NormalizeZoneName(args[0])} zone power set to Off";
            return true;
        }

        if (actionName.Equals("Source Select", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            sentence = $"{NormalizeZoneName(args[0])} zone source set to {NormalizeSourceName(args[1])}";
            return true;
        }

        if (actionName.Equals("Volume Up", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"{NormalizeZoneName(args[0])} zone volume adjusted Up";
            return true;
        }

        if (actionName.Equals("Volume Down", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"{NormalizeZoneName(args[0])} zone volume adjusted Down";
            return true;
        }

        if (actionName.Equals("Zone/Group Volume Up", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"{NormalizeZoneName(args[0])} zone/group volume adjusted Up";
            return true;
        }

        if (actionName.Equals("Zone/Group Volume Down", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"{NormalizeZoneName(args[0])} zone/group volume adjusted Down";
            return true;
        }

        return false;
    }

    private static bool TryBuildBijouSentence(string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        if (actionName.Equals("Power", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"Power set to {args[0]}";
            return true;
        }

        if (actionName.Equals("Set Input", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"Input set to {args[0]}";
            return true;
        }

        return false;
    }

    private static bool TryBuildVauxSentence(string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        if (actionName.Equals("Source Select", StringComparison.OrdinalIgnoreCase) && args.Count >= 3)
        {
            sentence = $"{args[1]} set to source {args[2]}";
            return true;
        }

        if (actionName.Equals("Volume Up", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"{args[0]} volume increased";
            return true;
        }

        if (actionName.Equals("Output Mute", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            if (args[0].Equals("Toggle", StringComparison.OrdinalIgnoreCase))
            {
                sentence = $"{args[1]} mute toggled";
                return true;
            }

            sentence = $"{args[1]} mute {args[0]}";
            return true;
        }

        if (actionName.Equals("Output Off", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"{args[0]} power set to Off";
            return true;
        }

        return false;
    }

    private static bool TryBuildLayerSwitchSentence(string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        var groupName = actionName.StartsWith("Ex. Group: ", StringComparison.OrdinalIgnoreCase)
            ? actionName.Substring("Ex. Group: ".Length)
            : actionName;

        if (args.Count == 1)
        {
            sentence = $"{groupName} set to {args[0]}";
            return true;
        }

        if (args.Count >= 2)
        {
            sentence = $"{groupName}({args[0]}) set to {args[1]}";
            return true;
        }

        return false;
    }

    private static bool TryBuildLutronSentence(string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        if (actionName.Equals("Set Dimmer Level", StringComparison.OrdinalIgnoreCase) && args.Count >= 3)
        {
            var name = StripIdSuffix(args[0]);
            var duration = TryFormatDuration(args[2], out var durationText) ? durationText : args[2];
            sentence = $"{name} dimmer level ramped to {args[1]} over {duration}";
            return true;
        }

        if (actionName.Equals("Switch Commands", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            var name = StripIdSuffix(args[0]);
            if (args[1].Equals("Toggle", StringComparison.OrdinalIgnoreCase))
            {
                sentence = $"{name} switch toggled";
                return true;
            }

            sentence = $"{name} switch set to {args[1]}";
            return true;
        }

        return false;
    }

    private static bool TryBuildSamsungExLinkSentence(string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        if (actionName.Equals("Discrete On", StringComparison.OrdinalIgnoreCase))
        {
            sentence = "Power set to On";
            return true;
        }

        if (actionName.Equals("Discrete Off", StringComparison.OrdinalIgnoreCase))
        {
            sentence = "Power set to Off";
            return true;
        }

        if (actionName.Equals("Discrete Inputs", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            sentence = $"Input set to {args[0]} {args[1]}";
            return true;
        }

        return false;
    }

    private static bool TryBuildQmotionSentence(string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        if ((actionName.Equals("Open", StringComparison.OrdinalIgnoreCase) || actionName.Equals("Close", StringComparison.OrdinalIgnoreCase))
            && args.Count >= 1)
        {
            sentence = $"Individual shade {args[0]} set to {actionName}";
            return true;
        }

        return false;
    }

    private static bool TryBuildRtiVipSentence(string command, string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        var parts = command.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        var roomSegment = parts[1];
        var room = roomSegment.Replace(" Commands", "", StringComparison.OrdinalIgnoreCase).Trim();

        if (actionName.Equals("RX On/Off", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"{room} RX power set to {args[0]}";
            return true;
        }

        if (actionName.Equals("RX Routing", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
        {
            sentence = $"{room} RX routing set to {NormalizeTxSource(args[0])}";
            return true;
        }

        return false;
    }

    private static bool TryBuildVhdxSentence(string actionName, IReadOnlyList<string> args, out string sentence)
    {
        sentence = "";
        if (actionName.Equals("Input Switching", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
        {
            sentence = $"{args[1]} input set to {args[0]}";
            return true;
        }

        return false;
    }

    private static bool TryBuildJandySentence(string actionName, out string sentence)
    {
        sentence = "";
        if (actionName.Equals("Spa Pump Off", StringComparison.OrdinalIgnoreCase))
        {
            sentence = "Spa pump set to Off";
            return true;
        }

        return false;
    }

    private static bool TryExtractTimestampAndBody(string rawText, out string timestamp, out string body)
    {
        timestamp = "";
        body = "";
        var match = TimestampRegex.Match(rawText);
        if (!match.Success)
        {
            return false;
        }

        timestamp = match.Groups["time"].Value.Trim();
        body = match.Groups["body"].Value.Trim();
        return !string.IsNullOrWhiteSpace(timestamp) && !string.IsNullOrWhiteSpace(body);
    }

    private static bool TryExtractCommandBody(string body, out string command, out string extra)
    {
        command = "";
        extra = "";
        var match = CommandRegex.Match(body);
        if (!match.Success)
        {
            return false;
        }

        command = match.Groups["command"].Value;
        extra = match.Groups["extra"].Value;
        return !string.IsNullOrWhiteSpace(command);
    }

    private static bool TryParseCommand(string command, out string actionName, out List<string> args)
    {
        actionName = "";
        args = new List<string>();

        var parts = command.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var tail = parts[^1];
        var closeIndex = tail.LastIndexOf(')');
        var openIndex = FindOpenParenIndexForTrailingArgs(tail, closeIndex);
        if (openIndex <= 0 || closeIndex <= openIndex)
        {
            actionName = tail.Trim();
            return !string.IsNullOrWhiteSpace(actionName);
        }

        actionName = tail.Substring(0, openIndex).Trim();
        var argsText = tail.Substring(openIndex + 1, closeIndex - openIndex - 1).Trim();
        args = SplitArgs(argsText);
        return !string.IsNullOrWhiteSpace(actionName);
    }

    private static int FindOpenParenIndexForTrailingArgs(string value, int closeIndex)
    {
        if (string.IsNullOrWhiteSpace(value) || closeIndex <= 0 || closeIndex >= value.Length)
        {
            return -1;
        }

        var depth = 0;
        for (var i = closeIndex; i >= 0; i--)
        {
            var ch = value[i];
            if (ch == ')')
            {
                depth++;
                continue;
            }

            if (ch == '(')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static List<string> SplitArgs(string argsText)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(argsText))
        {
            return result;
        }

        var builder = new StringBuilder();
        var depth = 0;
        foreach (var ch in argsText)
        {
            if (ch == '(')
            {
                depth++;
                builder.Append(ch);
                continue;
            }

            if (ch == ')')
            {
                if (depth > 0)
                {
                    depth--;
                }

                builder.Append(ch);
                continue;
            }

            if (ch == ',' && depth == 0)
            {
                result.Add(builder.ToString().Trim());
                builder.Clear();
                continue;
            }

            builder.Append(ch);
        }

        if (builder.Length > 0)
        {
            result.Add(builder.ToString().Trim());
        }

        return result;
    }

    private static string NormalizePowerState(string value)
    {
        if (value.StartsWith("Power ", StringComparison.OrdinalIgnoreCase))
        {
            return value.Substring("Power ".Length);
        }

        return value;
    }

    private static string StripParentheticalSuffix(string value)
    {
        var index = value.LastIndexOf(" (", StringComparison.Ordinal);
        if (index <= 0)
        {
            return value;
        }

        return value.Substring(0, index);
    }

    private static string NormalizeZoneName(string value)
    {
        var idx = value.IndexOf(" [Zone ", StringComparison.OrdinalIgnoreCase);
        return idx > 0 ? value.Substring(0, idx) : value;
    }

    private static string NormalizeSourceName(string value)
    {
        return Regex.Replace(value, "\\s*\\(Source\\s+\\d+\\)$", "", RegexOptions.IgnoreCase).Trim();
    }

    private static string StripIdSuffix(string value)
    {
        return Regex.Replace(value, "\\s*\\(ID\\s*\\d+\\)", "", RegexOptions.IgnoreCase).Trim();
    }

    private static string NormalizeTxSource(string value)
    {
        return Regex.Replace(value, "\\s*\\(TX\\s*\\d+\\)$", "", RegexOptions.IgnoreCase).Trim();
    }

    private static bool TryFormatDuration(string value, out string formatted)
    {
        formatted = value;
        if (!TimeSpan.TryParseExact(value, "c", CultureInfo.InvariantCulture, out var ts)
            && !TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out ts))
        {
            return false;
        }

        if (ts.TotalSeconds >= 1 && Math.Abs(ts.TotalSeconds - Math.Round(ts.TotalSeconds)) < 0.001)
        {
            var seconds = (int)Math.Round(ts.TotalSeconds);
            formatted = seconds == 1 ? "1 second" : $"{seconds} seconds";
            return true;
        }

        formatted = value;
        return false;
    }
}
