using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.DriverProfiles.Catalog;
using OracleByFPCLtd.DriverProfiles.Services;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProcessingEngine.Mapping;

public sealed class DriverMappingService
{
    private static readonly Regex CommandCapturePattern = new(
        "Driver - Command:\\s*'(?<command>[^']+)'",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DriverCommandAttributionPattern = new(
        "^\\s*(?:\\[[^\\]]+\\]\\s*)?Driver - Command:\\s*'(?<driver>[^'\\\\]+)\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DriverEventAttributionPattern = new(
        "happens on\\s*'(?<driver>[^'\\\\]+)\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RawPagePattern = new(
        @"(?<prefix>.*?\bChange to page\s+)(?<page>\d+)(?<suffix>\s+on device\s+'(?<device>[^']+)'.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ResolvedPagePattern = new(
        @"(?<prefix>.*?\bChange to page\s+"")(?<pageName>[^""]+)(?<suffix>""\s+on device\s+'(?<device>[^']+)'.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildEventLogFilePathHint()
    });

    public ProcessedLine Map(DiagnosticEvent evt, ProjectDataBundle bundle)
    {
        if (evt is null)
        {
            throw new ArgumentNullException(nameof(evt));
        }

        if (bundle is null)
        {
            throw new ArgumentNullException(nameof(bundle));
        }

        var rawText = evt.RawText ?? "";
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new ProcessedLine($"{evt.RawLineNumber} ", false);
        }

        foreach (var profile in DriverProfileCatalog.All())
        {
            var mapper = profile.Mapper;
            if (mapper is null)
            {
                continue;
            }

            if (!mapper.TryMap(rawText, bundle, out var mappedText, out var unresolved))
            {
                continue;
            }

            // Capture mapping transition from the profile-mapped command before
            // readability formatting changes the command shape.
            var hasTransition = TryExtractMappingTransition(rawText, mappedText, out var transition);

            var formatterDriverName = ResolveFormatterDriverName(profile.DeviceName, mappedText);
            if (DriverMessageTemplateFormatter.TryFormatDriverCommand(mappedText, formatterDriverName, out var formattedCommand))
            {
                mappedText = formattedCommand;
            }
            else if (DriverMessageTemplateFormatter.TryFormatDriverEvent(mappedText, formatterDriverName, out var formattedEvent))
            {
                mappedText = formattedEvent;
            }

            if (unresolved && IsDriverCommandLine(mappedText) && ShouldAppendNoMap(mappedText))
            {
                mappedText += " [No Map!]";
            }
            else if (unresolved && ShouldAppendUnresolved(mappedText))
            {
                mappedText += " [Unresolved!]";
            }

            if (!unresolved
                && string.Equals(mappedText, rawText, StringComparison.Ordinal)
                && !IsAllowedClaimedPassthrough(profile.DeviceName, rawText)
                && IsClaimedDriverMessage(rawText))
            {
                if (!mappedText.Contains("[No Format!]", StringComparison.Ordinal))
                {
                    mappedText += " [No Format!]";
                }

                unresolved = true;
                WriteEventLogEntry(
                    SeverityLevel.Warn,
                    "Processing:Formatting",
                    "Driver profile claimed line but no format rule matched.",
                    new Dictionary<string, string>
                    {
                        ["line"] = evt.RawLineNumber.ToString(),
                        ["profile"] = profile.DeviceName,
                        ["rawText"] = rawText
                    });
            }

            var resolvedSubstitution = hasTransition && !unresolved;

            MappingResolution? mappingResolution = null;
            if (resolvedSubstitution)
            {
                var transitionParts = transition.Split(" -> ", 2, StringSplitOptions.None);
                var mappedFrom = transitionParts.Length > 0 ? transitionParts[0] : "";
                var mappedTo = transitionParts.Length > 1 ? transitionParts[1] : "";
                var mappingSource = HasAdditionalInfoData(bundle, profile.DeviceName) ? "Additional Info" : "Apex";
                mappingResolution = new MappingResolution(
                    DetermineMappingKind(profile.DeviceName, mappingSource),
                    mappedFrom,
                    mappedTo,
                    mappingSource,
                    Profile: profile.DeviceName);
            }
            else if (!unresolved
                && TryBuildInlineMappingResolution(profile.DeviceName, rawText, mappedText, out var inlineMappingResolution))
            {
                mappingResolution = inlineMappingResolution;
            }

            return new ProcessedLine($"{evt.RawLineNumber} {mappedText}", unresolved, mappingResolution);
        }

        WriteEventLogEntry(
            SeverityLevel.Warn,
            "Processing:Mapping",
            "Driver profile not found.",
            new Dictionary<string, string>
            {
                ["line"] = evt.RawLineNumber.ToString(),
                ["rawText"] = rawText
            });
        if (TryExtractAttributedDriverName(rawText, out var attributedDriverName)
            && HasKnownDriverProfile(attributedDriverName))
        {
            return new ProcessedLine($"{evt.RawLineNumber} {rawText} [Incomplete Profile!]", true);
        }

        return new ProcessedLine($"{evt.RawLineNumber} {rawText} [No Profile!]", true);
    }

    private static void WriteEventLogEntry(
        SeverityLevel severity,
        string phase,
        string message,
        IReadOnlyDictionary<string, string>? details = null)
    {
        CentralLogger.LogEvent(new LogEntry(
            severity,
            CreateCorrelationId(),
            "DriverMappingService",
            phase,
            message,
            details));
    }

    private static string CreateCorrelationId()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    private static string BuildEventLogFilePathHint()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Oracle by FP&C",
            "Logs");
        return Path.Combine(folder, "oracle-structured.log");
    }

    private static void OverrideCentralLoggerForTesting(CentralLogger logger)
    {
        CentralLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static string DetermineMappingKind(string profileName, string mappingSource)
    {
        if (string.Equals(mappingSource, "Apex", StringComparison.OrdinalIgnoreCase)
            && string.Equals(profileName, "System Manager", StringComparison.OrdinalIgnoreCase))
        {
            return "source";
        }

        return "value";
    }

    private static bool TryBuildInlineMappingResolution(
        string profileName,
        string rawText,
        string mappedText,
        out MappingResolution mappingResolution)
    {
        mappingResolution = null!;

        if (!string.Equals(profileName, "RTI Internal", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rawPageMatch = RawPagePattern.Match(rawText ?? "");
        var resolvedPageMatch = ResolvedPagePattern.Match(mappedText ?? "");
        if (!rawPageMatch.Success || !resolvedPageMatch.Success)
        {
            return false;
        }

        var rawPage = rawPageMatch.Groups["page"].Value.Trim();
        var pageName = resolvedPageMatch.Groups["pageName"].Value.Trim();
        var deviceName = resolvedPageMatch.Groups["device"].Value.Trim();
        if (string.IsNullOrWhiteSpace(rawPage)
            || string.IsNullOrWhiteSpace(pageName)
            || string.IsNullOrWhiteSpace(deviceName))
        {
            return false;
        }

        mappingResolution = new MappingResolution(
            "page",
            rawPage,
            pageName,
            "Apex",
            Profile: "RTI Internal",
            Device: deviceName);
        return true;
    }

    private static bool IsDriverCommandLine(string text)
    {
        return text.Contains("Driver - Command:", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Driver Command (", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsClaimedDriverMessage(string rawText)
    {
        return rawText.Contains("Driver - Command:", StringComparison.OrdinalIgnoreCase)
            || rawText.Contains("Driver event", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllowedClaimedPassthrough(string profileName, string rawText)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return false;
        }

        if (string.Equals(profileName, "RTI Diagnostics", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(profileName, "RTI Virtual Multiroom Amp", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(profileName, "Activities", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(profileName, "Lutron Caseta / RA2 Select", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(profileName, "Venstar ColorTouch", StringComparison.OrdinalIgnoreCase)
            && rawText.StartsWith("Venstar ColorTouch -", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(profileName, "RTI Internal", StringComparison.OrdinalIgnoreCase)
            && rawText.Equals("Macro event", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(profileName, "System Manager", StringComparison.OrdinalIgnoreCase)
            && rawText.Contains("Driver - Command:", StringComparison.OrdinalIgnoreCase)
            && !rawText.Contains("System Manager\\[Hide]\\", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool ShouldAppendNoMap(string text)
    {
        return !text.Contains("[No Map!]", StringComparison.Ordinal)
            && !text.Contains("[Unknown State!]", StringComparison.Ordinal)
            && !text.Contains("[No Profile!]", StringComparison.Ordinal);
    }

    private static bool ShouldAppendUnresolved(string text)
    {
        return !text.Contains("[Unresolved!]", StringComparison.Ordinal)
            && !text.Contains("[UNRESOLVED]", StringComparison.Ordinal)
            && !text.Contains("[No Map!]", StringComparison.Ordinal)
            && !text.Contains("[Unknown State!]", StringComparison.Ordinal)
            && !text.Contains("[No Profile!]", StringComparison.Ordinal);
    }

    private static bool TryExtractAttributedDriverName(string rawText, out string driverName)
    {
        driverName = "";
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return false;
        }

        var commandMatch = DriverCommandAttributionPattern.Match(rawText);
        if (commandMatch.Success)
        {
            driverName = commandMatch.Groups["driver"].Value.Trim();
            return !string.IsNullOrWhiteSpace(driverName);
        }

        var eventMatch = DriverEventAttributionPattern.Match(rawText);
        if (eventMatch.Success)
        {
            driverName = eventMatch.Groups["driver"].Value.Trim();
            return !string.IsNullOrWhiteSpace(driverName);
        }

        return false;
    }

    private static bool HasKnownDriverProfile(string driverName)
    {
        if (string.IsNullOrWhiteSpace(driverName))
        {
            return false;
        }

        foreach (var profile in DriverProfileCatalog.All())
        {
            if (string.Equals(profile.DeviceName, driverName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var alias in profile.Aliases)
            {
                if (string.Equals(alias, driverName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (string.Equals(profile.DeviceName, "System Variable Events", StringComparison.OrdinalIgnoreCase)
                && driverName.StartsWith("System Variable Events", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string ResolveFormatterDriverName(string profileDeviceName, string mappedText)
    {
        if (!profileDeviceName.Equals("System Variable Events", StringComparison.OrdinalIgnoreCase))
        {
            return profileDeviceName;
        }

        if (!TryExtractAttributedDriverName(mappedText, out var attributedDriverName))
        {
            return profileDeviceName;
        }

        return attributedDriverName.StartsWith("System Variable Events", StringComparison.OrdinalIgnoreCase)
            ? attributedDriverName
            : profileDeviceName;
    }

    private static bool HasAdditionalInfoData(ProjectDataBundle bundle, string driverName)
    {
        if (!bundle.Additional.Drivers.TryGetValue(driverName, out var additional))
        {
            return false;
        }

        return additional.InputNames.Count > 0
            || additional.OutputNames.Count > 0
            || additional.IntegerNames.Count > 0
            || additional.CbusGroups.Count > 0
            || additional.CbusHvacZones.Count > 0
            || additional.CbusScenes.Count > 0;
    }

    private static bool TryExtractMappingTransition(string rawText, string mappedText, out string transition)
    {
        transition = "";
        if (!TryExtractCommandArgs(rawText, out var rawArgs) || !TryExtractCommandArgs(mappedText, out var mappedArgs))
        {
            return false;
        }

        var count = Math.Min(rawArgs.Count, mappedArgs.Count);
        for (var i = 0; i < count; i++)
        {
            var rawArg = rawArgs[i].Trim();
            var mappedArg = mappedArgs[i].Trim();
            if (string.Equals(rawArg, mappedArg, StringComparison.Ordinal))
            {
                continue;
            }

            if (!TryExtractLeafTransition(rawArg, mappedArg, out transition))
            {
                transition = $"{rawArg} -> {mappedArg}";
            }
            return true;
        }

        return false;
    }

    private static bool TryExtractCommandArgs(string text, out List<string> args)
    {
        args = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var commandMatch = CommandCapturePattern.Match(text);
        if (!commandMatch.Success)
        {
            return false;
        }

        var command = commandMatch.Groups["command"].Value;
        var tailIndex = command.LastIndexOf('\\');
        var tail = tailIndex >= 0 ? command[(tailIndex + 1)..] : command;
        var close = tail.LastIndexOf(')');
        var open = FindOpenParenIndexForTrailingArgs(tail, close);
        if (open <= 0 || close <= open)
        {
            return false;
        }

        var argsText = tail.Substring(open + 1, close - open - 1);
        foreach (var arg in SplitArgs(argsText))
        {
            if (!string.IsNullOrWhiteSpace(arg))
            {
                args.Add(arg);
            }
        }

        return args.Count > 0;
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

    private static IEnumerable<string> SplitArgs(string argsText)
    {
        if (string.IsNullOrWhiteSpace(argsText))
        {
            yield break;
        }

        var depth = 0;
        var start = 0;
        for (var i = 0; i < argsText.Length; i++)
        {
            var ch = argsText[i];
            if (ch == '(')
            {
                depth++;
                continue;
            }

            if (ch == ')')
            {
                if (depth > 0)
                {
                    depth--;
                }
                continue;
            }

            if (ch == ',' && depth == 0)
            {
                yield return argsText.Substring(start, i - start).Trim();
                start = i + 1;
            }
        }

        yield return argsText[start..].Trim();
    }

    private static bool TryExtractLeafTransition(string rawValue, string mappedValue, out string transition)
    {
        transition = "";
        if (string.Equals(rawValue, mappedValue, StringComparison.Ordinal))
        {
            return false;
        }

        if (!TryParseCall(rawValue, out var rawName, out var rawArgs)
            || !TryParseCall(mappedValue, out var mappedName, out var mappedArgs)
            || !string.Equals(rawName, mappedName, StringComparison.OrdinalIgnoreCase))
        {
            transition = $"{rawValue} -> {mappedValue}";
            return true;
        }

        var count = Math.Min(rawArgs.Count, mappedArgs.Count);
        for (var i = 0; i < count; i++)
        {
            var rawArg = rawArgs[i];
            var mappedArg = mappedArgs[i];
            if (string.Equals(rawArg, mappedArg, StringComparison.Ordinal))
            {
                continue;
            }

            if (TryExtractLeafTransition(rawArg, mappedArg, out transition))
            {
                return true;
            }

            transition = $"{rawArg} -> {mappedArg}";
            return true;
        }

        transition = $"{rawValue} -> {mappedValue}";
        return true;
    }

    private static bool TryParseCall(string value, out string name, out List<string> args)
    {
        name = "";
        args = new List<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        var close = trimmed.LastIndexOf(')');
        var open = FindOpenParenIndexForTrailingArgs(trimmed, close);
        if (open <= 0 || close <= open || close != trimmed.Length - 1)
        {
            return false;
        }

        name = trimmed[..open].Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var argsText = trimmed.Substring(open + 1, close - open - 1);
        args = SplitArgs(argsText).ToList();
        return args.Count > 0;
    }
}

