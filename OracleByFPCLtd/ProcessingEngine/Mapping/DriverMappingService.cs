using System;
using OracleByFPCLtd.DriverProfiles.Catalog;
using OracleByFPCLtd.DriverProfiles.Services;
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

            if (DriverMessageTemplateFormatter.TryFormatDriverCommand(mappedText, profile.DeviceName, out var formattedCommand))
            {
                mappedText = formattedCommand;
            }

            if (unresolved && IsDriverCommandLine(mappedText) && ShouldAppendNoMap(mappedText))
            {
                mappedText += " [No Map!]";
            }
            else if (unresolved && ShouldAppendUnresolved(mappedText))
            {
                mappedText += " [UNRESOLVED]";
            }

            return new ProcessedLine($"{evt.RawLineNumber} {mappedText}", unresolved);
        }

        if (IsDriverLine(rawText))
        {
            return new ProcessedLine($"{evt.RawLineNumber} {rawText} [No Profile!]", true);
        }

        return new ProcessedLine($"{evt.RawLineNumber} {rawText}", false);
    }

    private static bool IsDriverLine(string text)
    {
        return text.Contains("Driver - Command:", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Driver event", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDriverCommandLine(string text)
    {
        return text.Contains("Driver - Command:", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Driver Command (", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldAppendNoMap(string text)
    {
        return !text.Contains("[No Map!]", StringComparison.Ordinal)
            && !text.Contains("[Unknown State!]", StringComparison.Ordinal)
            && !text.Contains("[No Profile!]", StringComparison.Ordinal);
    }

    private static bool ShouldAppendUnresolved(string text)
    {
        return !text.Contains("[UNRESOLVED]", StringComparison.Ordinal)
            && !text.Contains("[No Map!]", StringComparison.Ordinal)
            && !text.Contains("[Unknown State!]", StringComparison.Ordinal)
            && !text.Contains("[No Profile!]", StringComparison.Ordinal);
    }
}
