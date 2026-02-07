using System;
using OracleByFPCLtd.ProcessingEngine.Models;

namespace OracleByFPCLtd.ProcessingEngine.Parsing;

public static class RawLogParser
{
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
            return false;
        }

        var numberText = line.Substring(0, delimiterIndex);
        if (!int.TryParse(numberText, out var rawLineNumber))
        {
            return false;
        }

        var content = line[(delimiterIndex + 1)..];
        diagnosticEvent = new DiagnosticEvent(rawLineNumber, content);
        return true;
    }
}
