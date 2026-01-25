using System;
using System.Collections.Generic;

namespace SHPDiagnosticsViewer.ProcessingEngine;

public static class ProcessingEngineRunner
{
    public static List<string> ProcessNumberedLines(IEnumerable<string> lines, ProcessingEngine engine)
    {
        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }
        if (engine is null)
        {
            throw new ArgumentNullException(nameof(engine));
        }

        var results = new List<string>();
        foreach (var line in lines)
        {
            if (!TryParseNumberedLine(line, out var rawLineNumber, out var content))
            {
                continue;
            }

            var processed = engine.ProcessLine(content, rawLineNumber);
            results.Add(processed.Text);
        }

        return results;
    }

    private static bool TryParseNumberedLine(string line, out int rawLineNumber, out string content)
    {
        rawLineNumber = 0;
        content = "";
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var delimiterIndex = line.IndexOf('\t');
        if (delimiterIndex <= 0)
        {
            delimiterIndex = line.IndexOf(' ');
        }

        if (delimiterIndex <= 0)
        {
            return false;
        }

        var numberText = line.Substring(0, delimiterIndex);
        if (!int.TryParse(numberText, out rawLineNumber))
        {
            return false;
        }

        content = line[(delimiterIndex + 1)..];
        return true;
    }
}
