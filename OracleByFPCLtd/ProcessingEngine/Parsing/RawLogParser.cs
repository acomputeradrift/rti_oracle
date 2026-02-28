using System;
using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.ProcessingEngine.Models;

namespace OracleByFPCLtd.ProcessingEngine.Parsing;

public static class RawLogParser
{
    private static readonly CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildEventLogFilePathHint()
    });

    public static bool TryParseNumberedLine(string line, out DiagnosticEvent diagnosticEvent)
    {
        diagnosticEvent = new DiagnosticEvent(0, "");
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
            WriteEventLogEntry(
                SeverityLevel.Warn,
                "TryParseNumberedLine",
                "Missing line number delimiter.",
                new Dictionary<string, string> { ["line"] = line });
            return false;
        }

        var numberText = line.Substring(0, delimiterIndex);
        if (!int.TryParse(numberText, out var rawLineNumber))
        {
            WriteEventLogEntry(
                SeverityLevel.Warn,
                "TryParseNumberedLine",
                "Line number parse failed.",
                new Dictionary<string, string> { ["line"] = line });
            return false;
        }

        var content = line[(delimiterIndex + 1)..];
        diagnosticEvent = new DiagnosticEvent(rawLineNumber, content);
        return true;
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
            "RawLogParser",
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
}

