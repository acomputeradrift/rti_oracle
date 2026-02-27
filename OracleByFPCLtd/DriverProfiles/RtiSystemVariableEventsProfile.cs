using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class RtiSystemVariableEventsProfile
{
    private static readonly Regex DriverCommandPattern = new Regex(
        "^\\s*(?:\\[[^\\]]+\\]\\s*)?Driver - Command:\\s*'System Variable Events(?:\\s*#\\d+)?\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileMapper Mapper { get; } = new SystemVariableEventsMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "System Variable Events",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        Mapper);

    private sealed class SystemVariableEventsMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            mappedText = rawText ?? "";
            unresolved = false;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }

            return DriverCommandPattern.IsMatch(rawText);
        }
    }
}
