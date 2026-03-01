using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class VenstarColorTouchProfile
{
    private static readonly Regex DriverEventPattern = new Regex(
        "Driver event",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VenstarAttributionPattern = new Regex(
        "happens on\\s*'Venstar ColorTouch\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DriverUpdatePattern = new Regex(
        "^Venstar ColorTouch\\s+-\\s+.+\\s+is\\s+connected\\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileResultMapper ResultMapper { get; } = new VenstarColorTouchResultMapper();
    public static IDriverProfileMapper Mapper { get; } = new LegacyVenstarColorTouchMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "Venstar ColorTouch",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        Mapper,
        ResultMapper);

    private sealed class LegacyVenstarColorTouchMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            var result = ResultMapper.TryMap(rawText, bundle);
            mappedText = result.Text;
            unresolved = IsUnresolvedStatus(result.Status);
            return result.Claimed;
        }
    }

    private sealed class VenstarColorTouchResultMapper : IDriverProfileResultMapper
    {
        public DriverProfileMapResult TryMap(string rawText, ProjectDataBundle bundle)
        {
            var defaultText = rawText ?? "";
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
            }

            if ((DriverEventPattern.IsMatch(rawText) && VenstarAttributionPattern.IsMatch(rawText))
                || DriverUpdatePattern.IsMatch(rawText))
            {
                return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.PassThrough);
            }

            return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
        }
    }

    private static bool IsUnresolvedStatus(DriverProfileProcessingStatus status) => status is DriverProfileProcessingStatus.NoFormat or DriverProfileProcessingStatus.NoMap or DriverProfileProcessingStatus.Unresolved or DriverProfileProcessingStatus.UnknownState;
}
