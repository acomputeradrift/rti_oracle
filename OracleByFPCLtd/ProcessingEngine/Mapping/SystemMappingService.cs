using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProcessingEngine.Mapping;

public sealed class SystemMappingService
{
    private static readonly Regex PagePattern = new Regex(
        @"(?<prefix>.*?\bChange to page\s+)(?<page>\d+)(?<suffix>\s+on device\s+'(?<device>[^']+)'.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
            return new ProcessedLine($"{evt.RawLineNumber} {evt.RawText} [UNRESOLVED]", true);
        }

        if (!TryBuildDeviceNameMap(bundle.System.DiagnosticsMapping, out var deviceNameToId)
            || !deviceNameToId.TryGetValue(deviceName, out var deviceId))
        {
            return new ProcessedLine($"{evt.RawLineNumber} {evt.RawText} [UNRESOLVED]", true);
        }

        var pageIndex = pageNumber - 1;
        var key = $"{deviceId}|{pageIndex}";
        if (!bundle.System.PageIndexMap.TryGetValue(key, out var pageName) || string.IsNullOrWhiteSpace(pageName))
        {
            return new ProcessedLine($"{evt.RawLineNumber} {evt.RawText} [UNRESOLVED]", true);
        }

        var resolved = $"{match.Groups["prefix"].Value}\"{pageName}\"{match.Groups["suffix"].Value}";
        return new ProcessedLine($"{evt.RawLineNumber} {resolved}", false);
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
        }

        return deviceNameToId.Count > 0;
    }
}
