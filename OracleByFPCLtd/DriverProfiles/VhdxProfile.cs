using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class VhdxProfile
{
    private static readonly Regex TimestampPrefixPattern = new Regex(
        @"^\s*(?<timestamp>\[[^\]]+\])\s*(?<rest>.+)$",
        RegexOptions.Compiled);
    private static readonly Regex DriverCommandPattern = new Regex(
        "Driver - Command:\\s*'VHDx\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DriverEventPattern = new Regex(
        "happens on\\s*'VHDx\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DriverUpdatePattern = new Regex(
        @"^VHDx\s*-\s*(?<message>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileMapper Mapper { get; } = new VhdxMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "VHDx",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        Mapper);

    private sealed class VhdxMapper : IDriverProfileMapper
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
                return true;
            }

            if (TryBuildDriverUpdate(rawText, out var driverUpdate))
            {
                mappedText = driverUpdate;
                return true;
            }

            return false;
        }

        private static bool TryBuildDriverUpdate(string rawText, out string mappedText)
        {
            mappedText = rawText;
            var candidate = rawText.Trim();
            var timestamp = "";

            var timestampMatch = TimestampPrefixPattern.Match(candidate);
            if (timestampMatch.Success)
            {
                timestamp = timestampMatch.Groups["timestamp"].Value.Trim();
                candidate = timestampMatch.Groups["rest"].Value.Trim();
            }

            var updateMatch = DriverUpdatePattern.Match(candidate);
            if (!updateMatch.Success)
            {
                return false;
            }

            var message = updateMatch.Groups["message"].Value.Trim();
            mappedText = string.IsNullOrWhiteSpace(timestamp)
                ? $"Driver Update (VHDx): '{message}'"
                : $"{timestamp} Driver Update (VHDx): '{message}'";
            return true;
        }
    }
}
