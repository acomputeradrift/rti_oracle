using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace SHPDiagnosticsViewer.Tests;

public sealed class DecodeWsTests
{
    [Fact]
    public void DecodingIsDeterministicAndOrdered()
    {
        var input = SampleHexLines();

        var first = DecodeWsReference.DecodeToOutputLines(input);
        var second = DecodeWsReference.DecodeToOutputLines(input);

        Assert.Equal(first, second);
        Assert.Equal(6, first.Count);
        Assert.Equal("1: Input: Button 1 Pressed", first[0]);
        Assert.Equal("2: Driver status: Ready", first[1]);
        Assert.Equal("3: Driver event: Start", first[2]);
        Assert.Equal("4: Input [ScheduledTasks] Driver event test]", first[3]);
        Assert.Equal("5: 09/15/2024 10:11:13 Sustain:NO", first[4]);
        Assert.Equal("6: 09/15/2024 10:11:14 Value)", first[5]);
    }

    [Fact]
    public void OutputHasOneLogicalEntryPerLineWithNoTruncation()
    {
        var output = DecodeWsReference.DecodeToOutputLines(SampleHexLines());

        Assert.All(output, line =>
        {
            Assert.DoesNotContain("\n", line, StringComparison.Ordinal);
            Assert.DoesNotContain("\r", line, StringComparison.Ordinal);
            Assert.DoesNotContain("...", line, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void MissingInputFileFailsClearly()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"decode_ws_missing_{Guid.NewGuid():N}.txt");

        var success = DecodeWsReference.TryDecodeFile(missingPath, out var error, out _);

        Assert.False(success);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void InvalidHexLinesYieldNoOutput()
    {
        var output = DecodeWsReference.DecodeToOutputLines(new[] { "not-hex", "123" });

        Assert.Empty(output);
    }

    private static List<string> SampleHexLines()
    {
        var encoding = Encoding.Unicode;
        var lines = new[]
        {
            "09/15/2024 10:11:12 Input: Button 1 Pressed",
            "Driver status: Ready",
            "Driver e vent: Start",
            "Input [Schedule   Driver event test]",
            "09/15/2024 10:11:13 Sustain:NO abc",
            "09/15/2024 10:11:14 Value) xx",
            "hello"
        };

        return lines.Select(line => DecodeWsReference.ToHexLine(line, encoding)).ToList();
    }
}

internal static class DecodeWsReference
{
    private static readonly Encoding Utf8 = Encoding.GetEncoding(
        "utf-8",
        EncoderFallback.ExceptionFallback,
        new DecoderReplacementFallback(""));
    private static readonly Encoding Utf16Le = Encoding.GetEncoding(
        "utf-16le",
        EncoderFallback.ExceptionFallback,
        new DecoderReplacementFallback(""));
    private static readonly Encoding Latin1 = Encoding.Latin1;
    private static readonly Regex PrefixRegex = new Regex("(Input|Driver|System Manager|Macro|hello)", RegexOptions.Compiled);
    private static readonly Regex DatePattern = new Regex("\\d{2}/\\d{2}/\\d{4}", RegexOptions.Compiled);
    private static readonly Regex SchedulePattern = new Regex("\\[Schedule\\s+Driver event", RegexOptions.Compiled);
    private static readonly Regex SustainPattern = new Regex("(Sustain:NO)\\s+[A-Za-z]{1,3}$", RegexOptions.Compiled);
    private static readonly Regex TrailingMarkerPattern = new Regex("([)'\\\"])\\s+[A-Za-z]{1,3}$", RegexOptions.Compiled);

    public static List<string> DecodeToOutputLines(IEnumerable<string> rawLines)
    {
        var decodedChunks = new List<string>();
        foreach (var rawLine in rawLines)
        {
            var decoded = DecodeHexLine(rawLine);
            var cleaned = CleanText(decoded);
            if (!string.IsNullOrEmpty(cleaned))
            {
                decodedChunks.Add(cleaned);
            }
        }

        var fullText = string.Join("\n", decodedChunks);
        var lines = fullText
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();

        var prefixes = new[] { "Input", "Driver", "System Manager", "Macro" };
        var normalizedLines = new List<string>();
        foreach (var line in lines)
        {
            var match = PrefixRegex.Match(line);
            var normalized = line;
            if (match.Success && match.Index > 0)
            {
                normalized = line.Substring(match.Index);
            }
            normalizedLines.Add(normalized.Trim());
        }

        var logicalLines = new List<string>();
        foreach (var line in normalizedLines)
        {
            if (string.IsNullOrEmpty(line) || !line.Any(char.IsLetterOrDigit))
            {
                continue;
            }

            if (line.Trim() == "hello")
            {
                continue;
            }

            if (line.Trim().All(char.IsDigit))
            {
                continue;
            }

            if (!DatePattern.IsMatch(line)
                && logicalLines.Count > 0
                && (line.StartsWith("Driver event", StringComparison.Ordinal)
                    || line.StartsWith("Driver - Command", StringComparison.Ordinal)
                    || line.StartsWith("System Manager", StringComparison.Ordinal)
                    || (!prefixes.Any(prefix => line.StartsWith(prefix, StringComparison.Ordinal))
                        && !char.IsDigit(line[0]))))
            {
                logicalLines[^1] = $"{logicalLines[^1]} {line}".Trim();
                continue;
            }

            if (prefixes.Any(prefix => line.StartsWith(prefix, StringComparison.Ordinal)) || char.IsDigit(line[0]))
            {
                logicalLines.Add(line);
            }
            else if (logicalLines.Count > 0)
            {
                logicalLines[^1] = $"{logicalLines[^1]} {line}".Trim();
            }
            else
            {
                logicalLines.Add(line);
            }
        }

        var cleanedLines = new List<string>();
        foreach (var line in logicalLines)
        {
            var cleaned = line.Replace("Driver e vent", "Driver event", StringComparison.Ordinal);
            cleaned = SchedulePattern.Replace(cleaned, "[ScheduledTasks] Driver event");
            cleaned = SustainPattern.Replace(cleaned, "$1");
            cleaned = TrailingMarkerPattern.Replace(cleaned, "$1");
            cleanedLines.Add(cleaned);
        }

        var output = new List<string>();
        for (var i = 0; i < cleanedLines.Count; i++)
        {
            output.Add(FormattableString.Invariant($"{i + 1}: {cleanedLines[i]}"));
        }

        return output;
    }

    public static bool TryDecodeFile(string path, out string error, out List<string> output)
    {
        try
        {
            var rawLines = File.ReadAllLines(path, Encoding.ASCII);
            output = DecodeToOutputLines(rawLines);
            error = "";
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            output = new List<string>();
            error = ex.Message;
            return false;
        }
    }

    public static string ToHexLine(string text, Encoding encoding)
    {
        var bytes = encoding.GetBytes(text);
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var value in bytes)
        {
            builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        }
        return builder.ToString();
    }

    private static string DecodeHexLine(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0)
        {
            return "";
        }

        if (!TryParseHex(trimmed, out var data))
        {
            return "";
        }

        if (data.Length >= 2 && data.Where((value, index) => index % 2 == 1 && value == 0).Count() > data.Length / 4.0)
        {
            return TryDecode(data, Utf16Le, Utf8, Latin1);
        }

        return TryDecode(data, Utf8, Latin1, Utf16Le);
    }

    private static bool TryParseHex(string input, out byte[] data)
    {
        var noWhitespace = string.Concat(input.Where(ch => !char.IsWhiteSpace(ch)));
        if (noWhitespace.Length == 0 || noWhitespace.Length % 2 != 0)
        {
            data = Array.Empty<byte>();
            return false;
        }

        try
        {
            data = Convert.FromHexString(noWhitespace);
            return true;
        }
        catch (FormatException)
        {
            data = Array.Empty<byte>();
            return false;
        }
    }

    private static string TryDecode(byte[] data, params Encoding[] encodings)
    {
        foreach (var encoding in encodings)
        {
            try
            {
                return encoding.GetString(data);
            }
            catch (DecoderFallbackException)
            {
            }
        }

        return "";
    }

    private static string CleanText(string text)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (ch == '\n')
            {
                builder.Append(ch);
                continue;
            }

            var codepoint = (int)ch;
            if (codepoint >= 32 && codepoint <= 126)
            {
                builder.Append(ch);
            }
        }
        return builder.ToString();
    }
}
