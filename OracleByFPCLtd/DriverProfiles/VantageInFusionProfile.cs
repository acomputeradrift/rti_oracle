using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProcessingEngine.Models;
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

    public static IDriverProfileResultMapper ResultMapper { get; } = new VantageInFusionResultMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "Vantage InFusion",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        ResultMapper);


    private sealed class VantageInFusionResultMapper : IDriverProfileResultMapper
    {
        public DriverProfileMapResult TryMap(string rawText, ProjectDataBundle bundle)
        {
            var defaultText = rawText ?? "";
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
            }

            if (CommandPattern.IsMatch(rawText) || EventAttributionPattern.IsMatch(rawText))
            {
                return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.PassThrough);
            }

            return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
        }
    }

    private static bool IsUnresolvedStatus(DriverProfileProcessingStatus status) => status is DriverProfileProcessingStatus.NoFormat or DriverProfileProcessingStatus.NoMap or DriverProfileProcessingStatus.Unresolved or DriverProfileProcessingStatus.UnknownState;
}
