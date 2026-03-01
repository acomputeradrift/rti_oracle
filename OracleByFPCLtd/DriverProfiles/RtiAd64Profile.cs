using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class RtiAd64Profile
{
    private static readonly Regex WhitespacePattern = new Regex("\\s+", RegexOptions.Compiled);
    private static readonly Regex DriverCommandPattern = new Regex(
        "Driver - Command:\\s*'RTI AD-64\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DriverEventPattern = new Regex(
        "happens on\\s*'RTI AD-64\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UpdatePattern = new Regex(
        @"^(?:(?<timestamp>\[[^\]]+\])\s*)?Audio Matrix \(16 Zone\)\s*-\s*(?<update>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileResultMapper ResultMapper { get; } = new RtiAd64ResultMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "RTI AD-64",
        Array.Empty<string>(),
        new[] { "GroupCount", "SourceCount", "ZoneCount" },
        new[] { "GroupName", "SourceName", "ZoneName" },
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        ResultMapper);


    private sealed class RtiAd64ResultMapper : IDriverProfileResultMapper
    {
        public DriverProfileMapResult TryMap(string rawText, ProjectDataBundle bundle)
        {
            var defaultText = rawText ?? "";
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
            }

            if (DriverCommandPattern.IsMatch(rawText) || DriverEventPattern.IsMatch(rawText))
            {
                return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.PassThrough);
            }

            var updateMatch = UpdatePattern.Match(rawText);
            if (updateMatch.Success)
            {
                var timestamp = updateMatch.Groups["timestamp"].Value;
                var updateText = NormalizeUpdateText(updateMatch.Groups["update"].Value);
                var mappedText = string.IsNullOrWhiteSpace(timestamp)
                    ? $"Driver Update ({Definition.DeviceName}): '{updateText}'"
                    : $"{timestamp} Driver Update ({Definition.DeviceName}): '{updateText}'";
                return new DriverProfileMapResult(true, mappedText, DriverProfileProcessingStatus.Resolved);
            }

            return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
        }

        private static string NormalizeUpdateText(string value)
        {
            var text = WhitespacePattern.Replace((value ?? "").Trim(), " ");
            if (string.IsNullOrWhiteSpace(text))
            {
                return "";
            }

            return text.EndsWith(".", StringComparison.Ordinal) ? text : $"{text}.";
        }
    }

    private static bool IsUnresolvedStatus(DriverProfileProcessingStatus status) => status is DriverProfileProcessingStatus.NoFormat or DriverProfileProcessingStatus.NoMap or DriverProfileProcessingStatus.Unresolved or DriverProfileProcessingStatus.UnknownState;
}
