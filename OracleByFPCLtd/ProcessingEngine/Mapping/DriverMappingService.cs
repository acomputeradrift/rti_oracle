using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.DriverProfiles.Catalog;
using OracleByFPCLtd.DriverProfiles.Services;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProcessingEngine.Mapping;

public sealed class DriverMappingService
{
    private static readonly Regex CommandCapturePattern = new(
        "Driver - Command:\\s*'(?<command>[^']+)'",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

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

        var rawText = evt.RawText ?? "";
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return new ProcessedLine($"{evt.RawLineNumber} ", false);
        }

        foreach (var profile in DriverProfileCatalog.All())
        {
            var mapper = profile.Mapper;
            if (mapper is null)
            {
                continue;
            }

            if (!mapper.TryMap(rawText, bundle, out var mappedText, out var unresolved))
            {
                continue;
            }

            if (DriverMessageTemplateFormatter.TryFormatDriverCommand(mappedText, profile.DeviceName, out var formattedCommand))
            {
                mappedText = formattedCommand;
            }

            if (unresolved && IsDriverCommandLine(mappedText) && ShouldAppendNoMap(mappedText))
            {
                mappedText += " [No Map!]";
            }
            else if (unresolved && ShouldAppendUnresolved(mappedText))
            {
                mappedText += " [UNRESOLVED]";
            }

            LogStructuredEvent(
                SeverityLevel.Success,
                "Processing:Mapping",
                BuildApexMappingMessage(rawText, mappedText),
                new Dictionary<string, string>
                {
                    ["driver"] = profile.DeviceName,
                    ["line"] = evt.RawLineNumber.ToString()
                });

            if (HasAdditionalInfoData(bundle, profile.DeviceName))
            {
                LogStructuredEvent(
                    SeverityLevel.Success,
                    "Processing:Mapping",
                    "Processed log line mapped to Additional Info file (<Driver>:<id> -> <name>)",
                    new Dictionary<string, string>
                    {
                        ["driver"] = profile.DeviceName,
                        ["line"] = evt.RawLineNumber.ToString()
                    });
            }

            return new ProcessedLine($"{evt.RawLineNumber} {mappedText}", unresolved);
        }

        if (IsDriverLine(rawText))
        {
            LogStructuredEvent(
                SeverityLevel.Warn,
                "Processing:Mapping",
                "Driver profile not found.",
                new Dictionary<string, string> { ["rawText"] = rawText });
            return new ProcessedLine($"{evt.RawLineNumber} {rawText} [No Profile!]", true);
        }

        return new ProcessedLine($"{evt.RawLineNumber} {rawText}", false);
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
            "DriverMappingService",
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

    private static bool IsDriverLine(string text)
    {
        return text.Contains("Driver - Command:", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Driver event", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDriverCommandLine(string text)
    {
        return text.Contains("Driver - Command:", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Driver Command (", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldAppendNoMap(string text)
    {
        return !text.Contains("[No Map!]", StringComparison.Ordinal)
            && !text.Contains("[Unknown State!]", StringComparison.Ordinal)
            && !text.Contains("[No Profile!]", StringComparison.Ordinal);
    }

    private static bool ShouldAppendUnresolved(string text)
    {
        return !text.Contains("[UNRESOLVED]", StringComparison.Ordinal)
            && !text.Contains("[No Map!]", StringComparison.Ordinal)
            && !text.Contains("[Unknown State!]", StringComparison.Ordinal)
            && !text.Contains("[No Profile!]", StringComparison.Ordinal);
    }

    private static bool HasAdditionalInfoData(ProjectDataBundle bundle, string driverName)
    {
        if (!bundle.Additional.Drivers.TryGetValue(driverName, out var additional))
        {
            return false;
        }

        return additional.InputNames.Count > 0
            || additional.OutputNames.Count > 0
            || additional.CbusGroups.Count > 0
            || additional.CbusHvacZones.Count > 0
            || additional.CbusScenes.Count > 0;
    }

    private static string BuildApexMappingMessage(string rawText, string mappedText)
    {
        if (TryExtractMappingTransition(rawText, mappedText, out var transition))
        {
            return $"Processed log line mapped to Apex file ({transition})";
        }

        return "Processed log line mapped to Apex file";
    }

    private static bool TryExtractMappingTransition(string rawText, string mappedText, out string transition)
    {
        transition = "";
        if (!TryExtractCommandArgs(rawText, out var rawArgs) || !TryExtractCommandArgs(mappedText, out var mappedArgs))
        {
            return false;
        }

        var count = Math.Min(rawArgs.Count, mappedArgs.Count);
        for (var i = 0; i < count; i++)
        {
            var rawArg = rawArgs[i].Trim();
            var mappedArg = mappedArgs[i].Trim();
            if (string.Equals(rawArg, mappedArg, StringComparison.Ordinal))
            {
                continue;
            }

            transition = $"{rawArg} -> {mappedArg}";
            return true;
        }

        return false;
    }

    private static bool TryExtractCommandArgs(string text, out List<string> args)
    {
        args = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var commandMatch = CommandCapturePattern.Match(text);
        if (!commandMatch.Success)
        {
            return false;
        }

        var command = commandMatch.Groups["command"].Value;
        var tailIndex = command.LastIndexOf('\\');
        var tail = tailIndex >= 0 ? command[(tailIndex + 1)..] : command;
        var open = tail.LastIndexOf('(');
        var close = tail.LastIndexOf(')');
        if (open <= 0 || close <= open)
        {
            return false;
        }

        var argsText = tail.Substring(open + 1, close - open - 1);
        foreach (var arg in argsText.Split(',', StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(arg))
            {
                args.Add(arg);
            }
        }

        return args.Count > 0;
    }
}
