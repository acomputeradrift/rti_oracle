using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class ActivitiesProfile
{
    private static readonly Regex DriverEventPattern = new Regex(
        "Driver event",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DriverAttributionPattern = new Regex(
        "happens on\\s*'[^'\\\\]+\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileResultMapper ResultMapper { get; } = new ActivitiesResultMapper();
    public static IDriverProfileMapper Mapper { get; } = new LegacyActivitiesMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "Activities",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        Mapper,
        ResultMapper);

    private sealed class LegacyActivitiesMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            var result = ResultMapper.TryMap(rawText, bundle);
            mappedText = result.Text;
            unresolved = IsUnresolvedStatus(result.Status);
            return result.Claimed;
        }
    }

    private sealed class ActivitiesResultMapper : IDriverProfileResultMapper
    {
        public DriverProfileMapResult TryMap(string rawText, ProjectDataBundle bundle)
        {
            var defaultText = rawText ?? "";
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
            }

            if (!DriverEventPattern.IsMatch(rawText))
            {
                return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
            }

            if (DriverAttributionPattern.IsMatch(rawText))
            {
                return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
            }

            return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.PassThrough);
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
