using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class RtiVipUhdCtrlProfile
{
    private static readonly Regex TimestampPrefixPattern = new Regex(
        @"^\s*(?<timestamp>\[[^\]]+\])\s*(?<rest>.+)$",
        RegexOptions.Compiled);
    private static readonly Regex DriverCommandPattern = new Regex(
        "Driver - Command:\\s*'RTI VIP-UHD-CTRL\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DriverEventPattern = new Regex(
        "happens on\\s*'RTI VIP-UHD-CTRL\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DriverUpdatePattern = new Regex(
        @"^RTI VIP-UHD-CTRL\s*-\s*(?<message>On(?:Connect|Disconnect)JSON)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileMapper Mapper { get; } = new RtiVipUhdCtrlMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "RTI VIP-UHD-CTRL",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        Mapper);

    private sealed class RtiVipUhdCtrlMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            mappedText = rawText ?? "";
            unresolved = false;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }

            if (DriverCommandPattern.IsMatch(rawText)
                || DriverEventPattern.IsMatch(rawText))
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
                ? $"Driver Update (RTI VIP-UHD-CTRL): '{message}'"
                : $"{timestamp} Driver Update (RTI VIP-UHD-CTRL): '{message}'";
            return true;
        }
    }
}
