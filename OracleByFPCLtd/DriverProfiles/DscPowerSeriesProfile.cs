using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class DscPowerSeriesProfile
{
    private static readonly Regex DriverEventPattern = new Regex(
        "Driver event",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DscAttributionPattern = new Regex(
        "happens on\\s*'DSC PowerSeries\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileMapper Mapper { get; } = new DscPowerSeriesMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "DSC PowerSeries",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        Mapper);

    private sealed class DscPowerSeriesMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            mappedText = rawText ?? "";
            unresolved = false;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }

            return DriverEventPattern.IsMatch(rawText) && DscAttributionPattern.IsMatch(rawText);
        }
    }
}
