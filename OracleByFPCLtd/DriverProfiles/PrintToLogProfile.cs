using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class PrintToLogProfile
{
    private static readonly Regex DriverCommandPattern = new Regex(
        "^\\s*(?:\\[[^\\]]+\\]\\s*)?Driver - Command:\\s*'Print To Log\\\\",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IDriverProfileResultMapper ResultMapper { get; } = new PrintToLogResultMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "Print To Log",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Array.Empty<AdditionalInfoSheetSchema>(),
        ResultMapper);


    private sealed class PrintToLogResultMapper : IDriverProfileResultMapper
    {
        public DriverProfileMapResult TryMap(string rawText, ProjectDataBundle bundle)
        {
            var defaultText = rawText ?? "";
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
            }

            if (DriverCommandPattern.IsMatch(rawText))
            {
                return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.PassThrough);
            }

            return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
        }
    }

    private static bool IsUnresolvedStatus(DriverProfileProcessingStatus status) => status is DriverProfileProcessingStatus.NoFormat or DriverProfileProcessingStatus.NoMap or DriverProfileProcessingStatus.Unresolved or DriverProfileProcessingStatus.UnknownState;
}
