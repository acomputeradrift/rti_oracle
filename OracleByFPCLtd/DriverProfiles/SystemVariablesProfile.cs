using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class SystemVariablesProfile
{
    private static readonly Regex CommandCapturePattern = new Regex(
        "Driver - Command:\\s*'(?<command>[^']+)'",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileResultMapper ResultMapper { get; } = new SystemVariablesResultMapper();
    public static IDriverProfileMapper Mapper { get; } = new LegacySystemVariablesMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "System Variables",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        new List<AdditionalInfoSheetSchema>
        {
            new("System Variables", new List<AdditionalInfoColumn>
            {
                new("IntegerIndex", AdditionalInfoColumnRole.IntegerIndex),
                new("IntegerName", AdditionalInfoColumnRole.IntegerName)
            })
        },
        Mapper,
        ResultMapper);

    private sealed class LegacySystemVariablesMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            var result = ResultMapper.TryMap(rawText, bundle);
            mappedText = result.Text;
            unresolved = IsUnresolvedStatus(result.Status);
            return result.Claimed;
        }
    }

    private sealed class SystemVariablesResultMapper : IDriverProfileResultMapper
    {
        public DriverProfileMapResult TryMap(string rawText, ProjectDataBundle bundle)
        {
            var defaultText = rawText ?? "";
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
            }

            var match = CommandCapturePattern.Match(rawText);
            if (!match.Success)
            {
                return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
            }

            var command = match.Groups["command"].Value;
            if (!command.StartsWith("System Variables\\", StringComparison.OrdinalIgnoreCase))
            {
                return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
            }

            if (!TryParseCommand(command, out var category, out var actionName, out var args))
            {
                return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.PassThrough);
            }

            if (!category.Equals("Integers", StringComparison.OrdinalIgnoreCase))
            {
                return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.PassThrough);
            }

            if (!actionName.Equals("Decrease", StringComparison.OrdinalIgnoreCase)
                && !actionName.Equals("Increase", StringComparison.OrdinalIgnoreCase)
                && !actionName.Equals("Test", StringComparison.OrdinalIgnoreCase))
            {
                return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.PassThrough);
            }

            if (args.Count < 2)
            {
                return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.NoMap);
            }

            if (!int.TryParse(args[0], out var index))
            {
                return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.NoMap);
            }

            if (!bundle.Additional.Drivers.TryGetValue(Definition.DeviceName, out var driverData)
                || !driverData.IntegerNames.TryGetValue(index, out var integerName)
                || string.IsNullOrWhiteSpace(integerName))
            {
                return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.NoMap);
            }

            args[0] = integerName;
            var updatedCommand = RebuildCommand(command, actionName, args);
            var replacementStart = match.Groups["command"].Index;
            var mappedText = rawText.Substring(0, replacementStart)
                + updatedCommand
                + rawText.Substring(replacementStart + match.Groups["command"].Length);
            return new DriverProfileMapResult(true, mappedText, DriverProfileProcessingStatus.Resolved);
        }

        private static bool TryParseCommand(string command, out string category, out string actionName, out List<string> args)
        {
            category = "";
            actionName = "";
            args = new List<string>();

            var parts = command.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                return false;
            }

            category = parts[1];
            var tail = parts[^1];
            var closeIndex = tail.LastIndexOf(')');
            var openIndex = tail.LastIndexOf('(');
            if (openIndex <= 0 || closeIndex <= openIndex)
            {
                actionName = tail.Trim();
                return !string.IsNullOrWhiteSpace(actionName);
            }

            actionName = tail.Substring(0, openIndex).Trim();
            var argsText = tail.Substring(openIndex + 1, closeIndex - openIndex - 1).Trim();
            foreach (var arg in argsText.Split(',', StringSplitOptions.TrimEntries))
            {
                if (!string.IsNullOrWhiteSpace(arg))
                {
                    args.Add(arg);
                }
            }

            return !string.IsNullOrWhiteSpace(actionName);
        }

        private static string RebuildCommand(string originalCommand, string actionName, IReadOnlyList<string> args)
        {
            var parts = originalCommand.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
            {
                return originalCommand;
            }

            parts[^1] = $"{actionName}({string.Join(", ", args)})";
            return string.Join("\\", parts);
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
