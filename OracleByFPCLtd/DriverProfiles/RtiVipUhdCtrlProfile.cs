using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProcessingEngine.Models;
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

    public static IDriverProfileResultMapper ResultMapper { get; } = new RtiVipUhdCtrlResultMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "RTI VIP-UHD-CTRL",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        ResultMapper);


    private sealed class RtiVipUhdCtrlResultMapper : IDriverProfileResultMapper
    {
        public DriverProfileMapResult TryMap(string rawText, ProjectDataBundle bundle)
        {
            var defaultText = rawText ?? "";
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
            }

            if (DriverCommandPattern.IsMatch(rawText)
                || DriverEventPattern.IsMatch(rawText))
            {
                return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.PassThrough);
            }

            if (TryBuildDriverUpdate(rawText, out var driverUpdate))
            {
                return new DriverProfileMapResult(true, driverUpdate, DriverProfileProcessingStatus.Resolved);
            }

            return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
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

    private static bool IsUnresolvedStatus(DriverProfileProcessingStatus status) => status is DriverProfileProcessingStatus.NoFormat or DriverProfileProcessingStatus.NoMap or DriverProfileProcessingStatus.Unresolved or DriverProfileProcessingStatus.UnknownState;
}
