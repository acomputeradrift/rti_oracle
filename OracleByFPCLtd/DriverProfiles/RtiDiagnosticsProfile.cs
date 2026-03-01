using System;
using System.Collections.Generic;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles;

public static class RtiDiagnosticsProfile
{
    private const string DiagnosticsPrimaryProcessorName = "Diagnostics: Primary Processor";
    public static IDriverProfileResultMapper ResultMapper { get; } = new RtiDiagnosticsResultMapper();
    public static IDriverProfileMapper Mapper { get; } = new LegacyRtiDiagnosticsMapper();

    public static DriverProfileDefinition Definition { get; } = new DriverProfileDefinition(
        "RTI Diagnostics",
        Array.Empty<string>(),
        Array.Empty<string>(),
        Array.Empty<string>(),
        new List<DriverProfileDiscoveryRule>(),
        new List<DriverProfileAnalysisRule>(),
        new List<string>(),
        Mapper: Mapper,
        ResultMapper: ResultMapper);

    private sealed class LegacyRtiDiagnosticsMapper : IDriverProfileMapper
    {
        public bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved)
        {
            var result = ResultMapper.TryMap(rawText, bundle);
            mappedText = result.Text;
            unresolved = IsUnresolvedStatus(result.Status);
            return result.Claimed;
        }
    }

    private sealed class RtiDiagnosticsResultMapper : IDriverProfileResultMapper
    {
        public DriverProfileMapResult TryMap(string rawText, ProjectDataBundle bundle)
        {
            _ = bundle;
            var defaultText = rawText ?? "";
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
            }

            if (rawText.Contains(DiagnosticsPrimaryProcessorName, StringComparison.OrdinalIgnoreCase))
            {
                return new DriverProfileMapResult(true, rawText, DriverProfileProcessingStatus.PassThrough);
            }

            return new DriverProfileMapResult(false, defaultText, DriverProfileProcessingStatus.NoProfile);
        }
    }

    private static bool IsUnresolvedStatus(DriverProfileProcessingStatus status) => status is DriverProfileProcessingStatus.NoFormat or DriverProfileProcessingStatus.NoMap or DriverProfileProcessingStatus.Unresolved or DriverProfileProcessingStatus.UnknownState;
}
