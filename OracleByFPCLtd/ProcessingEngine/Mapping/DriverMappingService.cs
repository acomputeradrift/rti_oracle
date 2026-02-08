using System;
using OracleByFPCLtd.DriverProfiles.Catalog;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProcessingEngine.Mapping;

public sealed class DriverMappingService
{
    public ProcessedLine Map(DiagnosticEvent evt, ProjectDataBundle bundle)
    {
        if (evt is null)
        {
            throw new ArgumentNullException(nameof(evt));
        }

        if (bundle is null)
        {
            throw new ArgumentNullException(nameof(bundle));
        }

        var rawText = evt.RawText ?? "";
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new ProcessedLine($"{evt.RawLineNumber} ", false);
        }

        foreach (var profile in DriverProfileCatalog.All())
        {
            var mapper = profile.Mapper;
            if (mapper is null)
            {
                continue;
            }

            if (!mapper.TryMap(rawText, bundle, out var mappedText, out var unresolved))
            {
                continue;
            }

            if (unresolved && !mappedText.Contains("[UNRESOLVED]", StringComparison.Ordinal))
            {
                mappedText += " [UNRESOLVED]";
            }

            return new ProcessedLine($"{evt.RawLineNumber} {mappedText}", unresolved);
        }

        return new ProcessedLine($"{evt.RawLineNumber} {rawText}", false);
    }
}
