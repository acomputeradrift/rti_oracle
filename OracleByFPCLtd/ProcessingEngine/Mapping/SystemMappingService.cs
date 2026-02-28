using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProcessingEngine.Mapping;

public sealed class SystemMappingService
{
    private static readonly Regex PagePattern = new Regex(
        @"(?<prefix>.*?\bChange to page\s+)(?<page>\d+)(?<suffix>\s+on device\s+'(?<device>[^']+)'.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildEventLogFilePathHint()
    });

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

        if (string.IsNullOrWhiteSpace(evt.RawText))
        {
            return new ProcessedLine($"{evt.RawLineNumber} ", false);
        }

        var match = PagePattern.Match(evt.RawText);
        if (!match.Success)
        {
            return new ProcessedLine($"{evt.RawLineNumber} {evt.RawText}", false);
        }

        var pageText = match.Groups["page"].Value;
        var deviceName = match.Groups["device"].Value;
        if (!int.TryParse(pageText, out var pageNumber) || pageNumber <= 0)
        {
            WriteEventLogEntry(
                SeverityLevel.Warn,
                "Processing:Mapping",
                "Page number parse failed.",
                new Dictionary<string, string>
                {
                    ["pageText"] = pageText,
                    ["device"] = deviceName
                });
            return new ProcessedLine($"{evt.RawLineNumber} {evt.RawText} [Unresolved!]", true);
        }

        if (!TryBuildDeviceNameMap(bundle.System.DiagnosticsMapping, out var deviceNameToId)
            || !deviceNameToId.TryGetValue(deviceName, out var deviceId))
        {
            WriteEventLogEntry(
                SeverityLevel.Warn,
                "Processing:Mapping",
                "Device mapping not found.",
                new Dictionary<string, string> { ["device"] = deviceName });
            return new ProcessedLine($"{evt.RawLineNumber} {evt.RawText} [Unresolved!]", true);
        }

        var pageIndex = pageNumber - 1;
        var key = $"{deviceId}|{pageIndex}";
        if (!bundle.System.PageIndexMap.TryGetValue(key, out var pageName) || string.IsNullOrWhiteSpace(pageName))
        {
            WriteEventLogEntry(
                SeverityLevel.Warn,
                "Processing:Mapping",
                "Page index mapping not found.",
                new Dictionary<string, string>
                {
                    ["deviceId"] = deviceId.ToString(),
                    ["pageIndex"] = pageIndex.ToString()
                });
            return new ProcessedLine($"{evt.RawLineNumber} {evt.RawText} [Unresolved!]", true);
        }

        var resolved = $"{match.Groups["prefix"].Value}\"{pageName}\"{match.Groups["suffix"].Value}";
        WriteEventLogEntry(
            SeverityLevel.Success,
            "Processing",
            $"mapped page {pageNumber} for {pageName}",
            new Dictionary<string, string>
            {
                ["line"] = evt.RawLineNumber.ToString(),
                ["device"] = deviceName,
                ["mappedFrom"] = $"Page {pageNumber}",
                ["mappedTo"] = pageName,
                ["source"] = "Apex"
            });
        return new ProcessedLine($"{evt.RawLineNumber} {resolved}", false);
    }

    private static void WriteEventLogEntry(
        SeverityLevel severity,
        string phase,
        string message,
        IReadOnlyDictionary<string, string>? details = null)
    {
        CentralLogger.LogEvent(new LogEntry(
            severity,
            CreateCorrelationId(),
            "SystemMappingService",
            phase,
            message,
            details));
    }

    private static string CreateCorrelationId()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    private static string BuildEventLogFilePathHint()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Oracle by FP&C",
            "Logs");
        return Path.Combine(folder, "oracle-structured.log");
    }

    private static void OverrideCentralLoggerForTesting(CentralLogger logger)
    {
        CentralLogger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static bool TryBuildDeviceNameMap(
        IEnumerable<OracleByFPCLtd.ProjectData.DiagnosticsMappingEntry> mapping,
        out Dictionary<string, int> deviceNameToId)
    {
        deviceNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in mapping)
        {
            if (!deviceNameToId.ContainsKey(entry.DeviceName))
            {
                deviceNameToId[entry.DeviceName] = entry.DeviceId;
            }

            if (!string.IsNullOrWhiteSpace(entry.DeviceDisplayName)
                && !deviceNameToId.ContainsKey(entry.DeviceDisplayName))
            {
                deviceNameToId[entry.DeviceDisplayName] = entry.DeviceId;
            }
        }

        return deviceNameToId.Count > 0;
    }
}

