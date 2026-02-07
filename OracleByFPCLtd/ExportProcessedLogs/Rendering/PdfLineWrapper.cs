using System.Collections.Generic;

namespace OracleByFPCLtd.ExportProcessedLogs.Rendering;

public static class PdfLineWrapper
{
    public static IReadOnlyList<string> Wrap(string line, Func<string, double> measure, double maxWidth)
    {
        if (maxWidth <= 0 || string.IsNullOrEmpty(line))
        {
            return new[] { line ?? string.Empty };
        }

        var indent = GetLeadingWhitespace(line);
        var trimmed = line.TrimStart();
        if (measure(line) <= maxWidth)
        {
            return new[] { line };
        }

        var words = trimmed.Split(' ');
        var result = new List<string>();
        var current = indent;

        foreach (var word in words)
        {
            if (string.IsNullOrEmpty(word))
            {
                continue;
            }

            var candidate = string.IsNullOrEmpty(current.Trim())
                ? indent + word
                : current + " " + word;

            if (measure(candidate) > maxWidth && current.Length > indent.Length)
            {
                result.Add(current);
                current = indent + word;
                continue;
            }

            current = candidate;
        }

        if (!string.IsNullOrEmpty(current))
        {
            result.Add(current);
        }

        return result;
    }

    private static string GetLeadingWhitespace(string line)
    {
        var count = 0;
        while (count < line.Length && char.IsWhiteSpace(line[count]))
        {
            count++;
        }

        return count == 0 ? string.Empty : line.Substring(0, count);
    }
}
