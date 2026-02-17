using System;
using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.Logging;
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
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

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
        var defaultText = $"{evt.RawLineNumber} {evt.RawText}";
        var driverLine = _driverMappingService.Map(evt, _bundle);
        if (driverLine.IsUnresolved || !string.Equals(driverLine.Text, defaultText, StringComparison.Ordinal))
        {
            return driverLine;
        }

        var systemLine = _systemMappingService.Map(evt, _bundle);
        if (systemLine.IsUnresolved || !string.Equals(systemLine.Text, defaultText, StringComparison.Ordinal))
        {
            return systemLine;
        }

        _ = _additionalDataMappingService.Map(evt, _bundle);
        if (driverLine.IsUnresolved && systemLine.IsUnresolved)
        {
            LogStructuredEvent(
                SeverityLevel.Warn,
                "ProcessEvent",
                "Unresolved diagnostic line.",
                new Dictionary<string, string>
                {
                    ["rawLineNumber"] = evt.RawLineNumber.ToString(),
                    ["rawText"] = evt.RawText
                });
        }
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
                entry.Key,
                0,
                0,
                0,
                0,
                ""));
        }

        return ProjectDataBundle.FromExtractionResult(result);
    }

    private void LogStructuredEvent(
        SeverityLevel severity,
        string phase,
        string message,
        IReadOnlyDictionary<string, string>? details = null)
    {
        _centralLogger.LogEvent(new LogEntry(
            severity,
            CreateCorrelationId(),
            "ProcessingEngine",
            phase,
            message,
            details));
    }

    private static string CreateCorrelationId()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    private static string BuildStructuredLogPath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Oracle by FP&C",
            "Logs");
        return Path.Combine(folder, "oracle-structured.log");
    }
}
