using System;
using System.Collections.Generic;
using OracleByFPCLtd.ProcessingEngine.Mapping;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProcessingEngine;

public sealed record ProcessingResult(string Text, bool IsUnresolved);

public sealed record ProcessingContext(
    IReadOnlyDictionary<string, int> DeviceNameToId,
    IReadOnlyDictionary<string, string> PageIndexMap);

public sealed class ProcessingEngine
{
    private readonly ProjectDataBundle _bundle;
    private readonly SystemMappingService _systemMappingService = new();
    private readonly DriverMappingService _driverMappingService = new();
    private readonly AdditionalDataMappingService _additionalDataMappingService = new();

    public ProcessingEngine(ProcessingContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        _bundle = BuildBundleFromContext(context);
    }

    public ProcessingEngine(ProjectDataBundle bundle)
    {
        _bundle = bundle ?? throw new ArgumentNullException(nameof(bundle));
    }

    public ProcessedLine ProcessEvent(DiagnosticEvent evt)
    {
        var systemLine = _systemMappingService.Map(evt, _bundle);
        _ = _driverMappingService.Map(evt, _bundle);
        _ = _additionalDataMappingService.Map(evt, _bundle);
        return systemLine;
    }

    public ProcessingResult ProcessLine(string line, int rawLineNumber)
    {
        var processed = ProcessEvent(new DiagnosticEvent(rawLineNumber, line));
        return new ProcessingResult(processed.Text, processed.IsUnresolved);
    }

    private static ProjectDataBundle BuildBundleFromContext(ProcessingContext context)
    {
        var result = new ProjectDataExtractionResult();
        foreach (var entry in context.PageIndexMap)
        {
            result.ApexDiscoveryPreload.PageIndexMap[entry.Key] = entry.Value;
        }

        foreach (var entry in context.DeviceNameToId)
        {
            result.DiagnosticsMapping.Add(new DiagnosticsMappingEntry(
                entry.Value,
                entry.Key,
                0,
                0,
                0,
                0,
                ""));
        }

        return ProjectDataBundle.FromExtractionResult(result);
    }
}
