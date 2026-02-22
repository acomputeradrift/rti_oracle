using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
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

    public static IDriverProfileMapper Mapper { get; } = new VenstarColorTouchMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "Venstar ColorTouch",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        Mapper);

    private sealed class VenstarColorTouchMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            mappedText = rawText ?? "";
            unresolved = false;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }

            return DriverEventPattern.IsMatch(rawText) && VenstarAttributionPattern.IsMatch(rawText);
        }
    }
}
