using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SHPDiagnosticsViewer.ProcessingEngine;

public sealed record ProcessingResult(string Text, bool IsUnresolved);

public sealed record ProcessingContext(
    IReadOnlyDictionary<string, int> DeviceNameToId,
    IReadOnlyDictionary<string, string> PageIndexMap);

public sealed class ProcessingEngine
{
    private static readonly Regex PagePattern = new Regex(
        @"(?<prefix>.*?\bChange to page\s+)(?<page>\d+)(?<suffix>\s+on device\s+'(?<device>[^']+)'.*)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ProcessingContext _context;

    public ProcessingEngine(ProcessingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public ProcessingResult ProcessLine(string line, int rawLineNumber)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return new ProcessingResult($"{rawLineNumber} ", false);
        }

        var match = PagePattern.Match(line);
        if (!match.Success)
        {
            return new ProcessingResult($"{rawLineNumber} {line}", false);
        }

        var pageText = match.Groups["page"].Value;
        var deviceName = match.Groups["device"].Value;
        if (!int.TryParse(pageText, out var pageNumber) || pageNumber <= 0)
        {
            return new ProcessingResult($"{rawLineNumber} {line} [UNRESOLVED]", true);
        }

        if (!_context.DeviceNameToId.TryGetValue(deviceName, out var deviceId))
        {
            return new ProcessingResult($"{rawLineNumber} {line} [UNRESOLVED]", true);
        }

        var pageIndex = pageNumber - 1;
        var key = $"{deviceId}|{pageIndex}";
        if (!_context.PageIndexMap.TryGetValue(key, out var pageName) || string.IsNullOrWhiteSpace(pageName))
        {
            return new ProcessingResult($"{rawLineNumber} {line} [UNRESOLVED]", true);
        }

        var resolved = $"{match.Groups["prefix"].Value}\"{pageName}\"{match.Groups["suffix"].Value}";
        return new ProcessingResult($"{rawLineNumber} {resolved}", false);
    }
}
