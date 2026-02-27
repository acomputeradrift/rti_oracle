using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class SystemManagerProfile
{
    private static readonly Regex CommandCapturePattern = new Regex(
        "Driver - Command:\\s*'(?<command>[^']+)'",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DriverCommandPattern = new Regex(
        "Driver - Command:\\s*'System Manager\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DriverEventPattern = new Regex(
        "happens on\\s*'System Manager\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UpdatePattern = new Regex(
        @"^(?:(?<timestamp>\[[^\]]+\])\s*)?System Manager -\s*(?<update>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileMapper Mapper { get; } = new SystemManagerMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "System Manager",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        Mapper);

    private sealed class SystemManagerMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            mappedText = rawText ?? "";
            unresolved = false;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }

            if (DriverCommandPattern.IsMatch(rawText) || DriverEventPattern.IsMatch(rawText))
            {
                if (TryResolveSourceIndexCommands(rawText, bundle, out var resolvedText, out var sourceUnresolved))
                {
                    mappedText = resolvedText;
                    unresolved = sourceUnresolved;
                    return true;
                }

                unresolved = HasUnresolvedSourceIndex(rawText);
                return true;
            }

            var updateMatch = UpdatePattern.Match(rawText);
            if (updateMatch.Success)
            {
                var timestamp = updateMatch.Groups["timestamp"].Value;
                var updateText = updateMatch.Groups["update"].Value.Trim();
                updateText = updateText.TrimEnd('\r', '\n');
                if (updateText.StartsWith("Variable Stats:", StringComparison.Ordinal))
                {
                    updateText = updateText.TrimStart();
                }

                mappedText = string.IsNullOrWhiteSpace(timestamp)
                    ? $"Driver Update (System Manager): '{updateText}'"
                    : $"{timestamp} Driver Update (System Manager): '{updateText}'";
                return true;
            }

            return false;
        }

        private static bool HasUnresolvedSourceIndex(string rawText)
        {
            var match = CommandCapturePattern.Match(rawText);
            if (!match.Success)
            {
                return false;
            }

            var command = match.Groups["command"].Value;
            if (!command.StartsWith("System Manager\\", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!TryParseCommand(command, out var actionName, out var args))
            {
                return false;
            }

            if (actionName.Equals("Set Source", StringComparison.OrdinalIgnoreCase) && args.Count >= 1)
            {
                return IsNumericArg(args[0]);
            }

            if (actionName.Equals("Set Source By Room", StringComparison.OrdinalIgnoreCase) && args.Count >= 2)
            {
                return IsNumericArg(args[1]);
            }

            return false;
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
            var openIndex = tail.LastIndexOf('(');
            var closeIndex = tail.LastIndexOf(')');
            if (openIndex <= 0 || closeIndex <= openIndex)
            {
                actionName = tail.Trim();
                return !string.IsNullOrWhiteSpace(actionName);
            }

            actionName = tail.Substring(0, openIndex).Trim();
            var argsText = tail.Substring(openIndex + 1, closeIndex - openIndex - 1).Trim();
            if (string.IsNullOrWhiteSpace(argsText))
            {
                return !string.IsNullOrWhiteSpace(actionName);
            }

            foreach (var arg in argsText.Split(',', StringSplitOptions.TrimEntries))
            {
                if (!string.IsNullOrWhiteSpace(arg))
                {
                    args.Add(arg);
                }
            }

            return !string.IsNullOrWhiteSpace(actionName);
        }

        private static bool IsNumericArg(string value)
        {
            return int.TryParse(value, out _);
        }

        private static bool TryResolveSourceIndexCommands(string rawText, ProjectDataBundle bundle, out string resolvedText, out bool unresolved)
        {
            resolvedText = rawText;
            unresolved = false;

            var match = CommandCapturePattern.Match(rawText);
            if (!match.Success)
            {
                return false;
            }

            var command = match.Groups["command"].Value;
            if (!TryParseCommand(command, out var actionName, out var args))
            {
                return false;
            }

            var isSetSource = actionName.Equals("Set Source", StringComparison.OrdinalIgnoreCase) && args.Count >= 1;
            var isSetSourceByRoom = actionName.Equals("Set Source By Room", StringComparison.OrdinalIgnoreCase) && args.Count >= 2;
            if (!isSetSource && !isSetSourceByRoom)
            {
                return false;
            }

            var sourceArgIndex = isSetSource ? 0 : 1;
            if (!IsNumericArg(args[sourceArgIndex]))
            {
                return false;
            }

            var resolved = TryResolveSystemManagerSourceIndex(
                bundle,
                args[sourceArgIndex],
                out var sourceName);

            if (!resolved)
            {
                unresolved = true;
                return true;
            }

            args[sourceArgIndex] = sourceName;
            var updatedCommand = RebuildCommandWithArgs(command, args);
            var replacementStart = match.Groups["command"].Index;
            resolvedText = rawText.Substring(0, replacementStart)
                + updatedCommand
                + rawText.Substring(replacementStart + match.Groups["command"].Length);
            return true;
        }

        private static bool TryResolveSystemManagerSourceIndexFromSystemManagerCatalog(ProjectDataBundle bundle, string rawIndex, out string sourceName)
        {
            sourceName = "";
            if (!int.TryParse(rawIndex, out var zeroBasedIndex))
            {
                return false;
            }

            if (zeroBasedIndex < 0)
            {
                return false;
            }

            var indexedSources = bundle.System.SystemManagerSourceCatalog.OrderBy(entry => entry.SourceIndex).ToList();
            if (indexedSources.Count == 0)
            {
                return false;
            }

            if (zeroBasedIndex >= indexedSources.Count)
            {
                return false;
            }

            sourceName = indexedSources[zeroBasedIndex].SourceName;
            return !string.IsNullOrWhiteSpace(sourceName);
        }

        private static bool TryResolveSystemManagerSourceIndexFromSourceCatalog(ProjectDataBundle bundle, string rawIndex, out string sourceName)
        {
            sourceName = "";
            if (!int.TryParse(rawIndex, out var zeroBasedIndex))
            {
                return false;
            }

            if (zeroBasedIndex < 0)
            {
                return false;
            }

            var orderedSources = bundle.System.SourceCatalog.OrderBy(entry => entry.DeviceId).ToList();
            if (zeroBasedIndex >= orderedSources.Count)
            {
                return false;
            }

            var entry = orderedSources[zeroBasedIndex];
            sourceName = string.IsNullOrWhiteSpace(entry.SourceDisplayName) ? entry.SourceName : entry.SourceDisplayName;
            return !string.IsNullOrWhiteSpace(sourceName);
        }

        private static bool TryResolveSystemManagerSourceIndex(ProjectDataBundle bundle, string rawIndex, out string sourceName)
        {
            if (TryResolveSystemManagerSourceIndexFromSystemManagerCatalog(bundle, rawIndex, out sourceName))
            {
                return true;
            }

            return TryResolveSystemManagerSourceIndexFromSourceCatalog(bundle, rawIndex, out sourceName);
        }

        private static string RebuildCommandWithArgs(string originalCommand, IReadOnlyList<string> args)
        {
            var parts = originalCommand.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return originalCommand;
            }

            var tail = parts[^1];
            var openIndex = tail.LastIndexOf('(');
            var closeIndex = tail.LastIndexOf(')');
            if (openIndex <= 0 || closeIndex <= openIndex)
            {
                return originalCommand;
            }

            var actionName = tail.Substring(0, openIndex).Trim();
            parts[^1] = $"{actionName}({string.Join(", ", args)})";
            return string.Join("\\", parts);
        }
    }
}
