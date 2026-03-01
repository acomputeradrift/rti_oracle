using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class LutronCasetaRa2SelectProfile
{
    private static readonly Regex DriverCommandPattern = new Regex(
        "Driver - Command:\\s*'Lutron Caseta / RA2 Select\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DriverEventPattern = new Regex(
        "happens on\\s*'Lutron Caseta / RA2 Select\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DriverUpdatePattern = new Regex(
        "^Lutron\\s+(Upper|Lower)\\s+-\\s+ID\\s+\\d+\\s*,",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileResultMapper ResultMapper { get; } = new LutronCasetaRa2SelectResultMapper();
    public static IDriverProfileMapper Mapper { get; } = new LegacyLutronCasetaRa2SelectMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "Lutron Caseta / RA2 Select",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        Mapper,
        ResultMapper);

    private sealed class LegacyLutronCasetaRa2SelectMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            var result = ResultMapper.TryMap(rawText, bundle);
            mappedText = result.Text;
            unresolved = IsUnresolvedStatus(result.Status);
            return result.Claimed;
        }
    }

    private sealed class LutronCasetaRa2SelectResultMapper : IDriverProfileResultMapper
    {
        public DriverProfileMapResult TryMap(string rawText, ProjectDataBundle bundle)
        {
            var defaultText = rawText ?? "";
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
            }

            if (DriverCommandPattern.IsMatch(rawText)
                || DriverEventPattern.IsMatch(rawText)
                || DriverUpdatePattern.IsMatch(rawText))
            {
                return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.PassThrough);
            }

            return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
        }
    }

    private static bool IsUnresolvedStatus(DriverProfileProcessingStatus status) => status is DriverProfileProcessingStatus.NoFormat or DriverProfileProcessingStatus.NoMap or DriverProfileProcessingStatus.Unresolved or DriverProfileProcessingStatus.UnknownState;
}
