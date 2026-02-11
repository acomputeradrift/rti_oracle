using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class RtiAdDspMatrixProfile
{
    private static readonly Regex DriverCommandPattern = new Regex(
        "Driver - Command:\\s*'RTI AD DSP Matrix\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DriverEventPattern = new Regex(
        "happens on\\s*'RTI AD DSP Matrix\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileMapper Mapper { get; } = new RtiAdDspMatrixMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "RTI AD DSP Matrix",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        Mapper);

    private sealed class RtiAdDspMatrixMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            mappedText = rawText ?? "";
            unresolved = false;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }

            if (DriverCommandPattern.IsMatch(rawText) || DriverEventPattern.IsMatch(rawText))
            {
                return true;
            }

            return false;
        }
    }
}
