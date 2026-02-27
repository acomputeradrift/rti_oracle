using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using OracleByFPCLtd.ProcessingEngine.Parsing;

namespace OracleByFPCLtd.Reliability;

public sealed record UnhandledTaggedReport(
    int SchemaVersion,
    DateTime CreatedUtc,
    string AppVersion,
    List<UnhandledDriverReport> Drivers);

public sealed record UnhandledDriverReport(
    string DriverName,
    List<UnhandledTagReport> Tags);

public sealed record UnhandledTagReport(
    string Tag,
    List<UnhandledEntryReport> Entries);

public sealed record UnhandledEntryReport(
    string ProcessedMessage,
    List<UnhandledRawSample> RawSamples);

public sealed record UnhandledRawSample(
    int RawLineNumber,
    string RawText);

public static class UnhandledTaggedReportBuilder
{
    private const int SchemaVersion = 2;
    private static readonly Regex TaggedDriverCommandPattern = new("Driver - Command:\\s*'(?<driver>[^\\\\']+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TaggedDriverEventPattern = new("happens on\\s*'(?<driver>[^\\\\']+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TaggedFormattedDriverCommandPattern = new("^Driver Command\\s*\\((?<driver>[^\\)]+)\\):", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TaggedFormattedDriverEventPattern = new("^Driver Event\\s*\\((?<driver>[^\\)]+)\\):", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TaggedFormattedDriverUpdatePattern = new("^Driver Update\\s*\\((?<driver>[^\\)]+)\\):", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly string[] DiagnosticTags =
    {
        "[No Profile!]",
        "[Incomplete Profile!]",
        "[No Map!]",
        "[Unknown State!]",
        "[No Format!]",
        "[Unresolved!]",
        "[UNRESOLVED]"
    };

    public static UnhandledTaggedReport Build(
        IReadOnlyDictionary<string, Dictionary<string, HashSet<string>>> taggedMessagesByDriver,
        IEnumerable<string> processedLines,
        IEnumerable<string> rawLines,
        string appVersion,
        DateTime? createdUtc = null)
    {
        var tagged = taggedMessagesByDriver ?? new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
        var processed = processedLines ?? Array.Empty<string>();
        var raw = rawLines ?? Array.Empty<string>();
        var created = createdUtc ?? DateTime.UtcNow;
        var version = string.IsNullOrWhiteSpace(appVersion) ? "unknown" : appVersion;

        var rawByLine = BuildRawLineLookup(raw);
        var evidenceIndex = BuildEvidenceIndex(processed, rawByLine);

        var drivers = tagged
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(driver => new UnhandledDriverReport(
                driver.Key,
                driver.Value
                    .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(group => new UnhandledTagReport(
                        group.Key,
                        group.Value
                            .OrderBy(message => message, StringComparer.Ordinal)
                            .Select(message => new UnhandledEntryReport(
                                message,
                                GetRawSamples(evidenceIndex, driver.Key, group.Key, message)))
                            .ToList()))
                    .ToList()))
            .ToList();

        return new UnhandledTaggedReport(SchemaVersion, created, version, drivers);
    }

    private static Dictionary<int, string> BuildRawLineLookup(IEnumerable<string> rawLines)
    {
        var map = new Dictionary<int, string>();
        foreach (var line in rawLines)
        {
            if (!RawLogParser.TryParseNumberedLine(line, out var evt))
            {
                continue;
            }

            map[evt.RawLineNumber] = evt.RawText;
        }

        return map;
    }

    private static Dictionary<(string Driver, string Tag, string Message), HashSet<UnhandledRawSample>> BuildEvidenceIndex(
        IEnumerable<string> processedLines,
        IReadOnlyDictionary<int, string> rawByLine)
    {
        var index = new Dictionary<(string Driver, string Tag, string Message), HashSet<UnhandledRawSample>>();
        foreach (var line in processedLines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var tags = ExtractDiagnosticTags(line);
            if (tags.Count == 0)
            {
                continue;
            }

            var normalized = NormalizeTaggedLine(line, tags);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            var driver = ExtractTaggedDriverName(normalized);
            if (!TryExtractLeadingLineNumber(line, out var rawLineNumber))
            {
                continue;
            }

            if (!rawByLine.TryGetValue(rawLineNumber, out var rawText))
            {
                continue;
            }

            var sample = new UnhandledRawSample(rawLineNumber, rawText);
            foreach (var tag in tags)
            {
                var key = (driver, tag, normalized);
                if (!index.TryGetValue(key, out var samples))
                {
                    samples = new HashSet<UnhandledRawSample>();
                    index[key] = samples;
                }

                samples.Add(sample);
            }
        }

        return index;
    }

    private static List<UnhandledRawSample> GetRawSamples(
        IReadOnlyDictionary<(string Driver, string Tag, string Message), HashSet<UnhandledRawSample>> evidenceIndex,
        string driver,
        string tag,
        string message)
    {
        if (!evidenceIndex.TryGetValue((driver, tag, message), out var samples))
        {
            return new List<UnhandledRawSample>();
        }

        return samples
            .OrderBy(sample => sample.RawLineNumber)
            .ThenBy(sample => sample.RawText, StringComparer.Ordinal)
            .ToList();
    }

    private static bool TryExtractLeadingLineNumber(string text, out int lineNumber)
    {
        lineNumber = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var delimiterIndex = text.IndexOf('\t');
        if (delimiterIndex <= 0)
        {
            delimiterIndex = text.IndexOf(' ');
        }

        if (delimiterIndex <= 0)
        {
            return false;
        }

        var numberText = text.Substring(0, delimiterIndex);
        return int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out lineNumber);
    }

    private static string StripLeadingLineNumber(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var spaceIndex = text.IndexOf(' ');
        if (spaceIndex <= 0)
        {
            return text;
        }

        var prefix = text.Substring(0, spaceIndex);
        return int.TryParse(prefix, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ? text.Substring(spaceIndex + 1) : text;
    }

    private static string StripLeadingTimestamp(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var trimmed = text.TrimStart();
        if (!trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            return text;
        }

        var closeIndex = trimmed.IndexOf(']');
        if (closeIndex < 0)
        {
            return text;
        }

        var remainder = trimmed.Substring(closeIndex + 1).TrimStart();
        return string.IsNullOrWhiteSpace(remainder) ? text : remainder;
    }

    private static string ExtractTaggedDriverName(string rawText)
    {
        var match = TaggedFormattedDriverCommandPattern.Match(rawText);
        if (match.Success)
        {
            return match.Groups["driver"].Value.Trim();
        }

        match = TaggedFormattedDriverEventPattern.Match(rawText);
        if (match.Success)
        {
            return match.Groups["driver"].Value.Trim();
        }

        match = TaggedFormattedDriverUpdatePattern.Match(rawText);
        if (match.Success)
        {
            return match.Groups["driver"].Value.Trim();
        }

        match = TaggedDriverCommandPattern.Match(rawText);
        if (match.Success)
        {
            return match.Groups["driver"].Value.Trim();
        }

        match = TaggedDriverEventPattern.Match(rawText);
        if (match.Success)
        {
            return match.Groups["driver"].Value.Trim();
        }

        return "Uncategorized";
    }

    private static List<string> ExtractDiagnosticTags(string line)
    {
        var tags = new List<string>();
        if (string.IsNullOrWhiteSpace(line))
        {
            return tags;
        }

        foreach (var tag in DiagnosticTags)
        {
            if (!line.Contains(tag, StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(tag, "[UNRESOLVED]", StringComparison.Ordinal))
            {
                if (!tags.Contains("[Unresolved!]", StringComparer.Ordinal))
                {
                    tags.Add("[Unresolved!]");
                }
                continue;
            }

            if (!tags.Contains(tag, StringComparer.Ordinal))
            {
                tags.Add(tag);
            }
        }

        return tags;
    }

    private static string NormalizeTaggedLine(string line, IReadOnlyList<string> tags)
    {
        var raw = StripLeadingLineNumber(line);
        raw = StripLeadingTimestamp(raw);

        foreach (var tag in tags)
        {
            raw = raw.Replace(" " + tag, "", StringComparison.Ordinal);
            raw = raw.Replace(tag, "", StringComparison.Ordinal);
        }

        raw = raw.Replace(" [UNRESOLVED]", "", StringComparison.Ordinal);
        return raw.Trim();
    }
}
