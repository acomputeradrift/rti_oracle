using System;
using System.Collections.Generic;
using OracleByFPCLtd.ProcessingEngine.Formatting;
using OracleByFPCLtd.ProcessingEngine.Parsing;

namespace OracleByFPCLtd.ProcessingEngine;

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
            if (!RawLogParser.TryParseNumberedLine(line, out var evt))
            {
                continue;
            }

            var processed = engine.ProcessEvent(evt);
            results.Add(ProcessedLineFormatter.Format(processed));
        }

        return results;
    }
}
