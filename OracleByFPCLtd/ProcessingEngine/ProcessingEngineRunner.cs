using System;
using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.ProcessingEngine.Formatting;
using OracleByFPCLtd.ProcessingEngine.Parsing;
using OracleByFPCLtd.Logging;

namespace OracleByFPCLtd.ProcessingEngine;

public static class ProcessingEngineRunner
{
    private static readonly CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public static List<string> ProcessNumberedLines(IEnumerable<string> lines, ProcessingEngine engine)
    {
        return ProcessNumberedLines(lines, engine, progress: null);
    }

    public static List<string> ProcessNumberedLines(
        IEnumerable<string> lines,
        ProcessingEngine engine,
        Action<int, int>? progress)
    {
        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }
        if (engine is null)
        {
            throw new ArgumentNullException(nameof(engine));
        }

        var lineList = lines as IList<string> ?? new List<string>(lines);
        var total = lineList.Count;
        var processedCount = 0;
        var results = new List<string>(total);
        foreach (var line in lineList)
        {
            if (!RawLogParser.TryParseNumberedLine(line, out var evt))
            {
                processedCount++;
                progress?.Invoke(processedCount, total);
                continue;
            }

            LogStructuredEvent(
                SeverityLevel.Info,
                "Processing:Formatting",
                "Raw log line formatted (line number, date/time stamp)",
                new Dictionary<string, string>
                {
                    ["line"] = evt.RawLineNumber.ToString()
                });

            var processed = engine.ProcessEvent(evt);
            results.Add(ProcessedLineFormatter.Format(processed));

            LogStructuredEvent(
                SeverityLevel.Info,
                "Processing:Formatting",
                "Processed log line formatted (line number, date/time stamp, readablility)",
                new Dictionary<string, string>
                {
                    ["line"] = evt.RawLineNumber.ToString()
                });

            processedCount++;
            progress?.Invoke(processedCount, total);
        }

        if (total == 0)
        {
            progress?.Invoke(0, 0);
        }

        return results;
    }

    private static void LogStructuredEvent(
        SeverityLevel severity,
        string phase,
        string message,
        IReadOnlyDictionary<string, string>? details = null)
    {
        CentralLogger.LogEvent(new LogEntry(
            severity,
            CreateCorrelationId(),
            "ProcessingEngineRunner",
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
