using System;
using System.Collections.Generic;
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
                unresolved = HasUnresolvedSourceIndex(rawText);
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
    }
}
