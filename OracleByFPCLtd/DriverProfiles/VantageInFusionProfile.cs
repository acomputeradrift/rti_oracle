using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class VantageInFusionProfile
{
    private static readonly Regex CommandPattern = new(
        "Driver - Command:\\s*'Vantage InFusion\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EventAttributionPattern = new(
        "Driver event\\s*'When\\s*'[^']+'\\s*happens on\\s*'Vantage InFusion\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileMapper Mapper { get; } = new VantageInFusionMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "Vantage InFusion",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        Mapper);

    private sealed class VantageInFusionMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            mappedText = rawText ?? "";
            unresolved = false;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }

            return CommandPattern.IsMatch(rawText) || EventAttributionPattern.IsMatch(rawText);
        }
    }
}
