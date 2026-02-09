using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
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

    public static IDriverProfileMapper Mapper { get; } = new ActivitiesMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "Activities",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        Mapper);

    private sealed class ActivitiesMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            mappedText = rawText ?? "";
            unresolved = false;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }

            if (!DriverEventPattern.IsMatch(rawText))
            {
                return false;
            }

            if (DriverAttributionPattern.IsMatch(rawText))
            {
                return false;
            }

            return true;
        }
    }
}
