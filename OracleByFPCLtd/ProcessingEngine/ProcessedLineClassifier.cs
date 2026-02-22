using System;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.Logging;

namespace OracleByFPCLtd.ProcessingEngine;

public enum ProcessedLineCategory
{
    Default,
    Connect,
    Disconnect,
    Button,
    Page,
    DriverCommand,
    Macro,
    SystemMacro,
    DriverEvent
}

public static class ProcessedLineClassifier
{
    private static readonly Regex NumberPrefix = new Regex("^\\s*\\d+\\s+", RegexOptions.Compiled);
    private static readonly CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public static ProcessedLineCategory DetermineCategory(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            LogStructuredEvent(
                SeverityLevel.Warn,
                "DetermineCategory",
                "Processed line is empty.",
                new Dictionary<string, string> { ["line"] = line ?? "" });
            return ProcessedLineCategory.Default;
        }

        var content = NumberPrefix.Replace(line, "");
        if (content.Contains("has connected", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessedLineCategory.Connect;
        }

        if (content.Contains("has disconnected", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessedLineCategory.Disconnect;
        }

        if (content.Contains("Command", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessedLineCategory.DriverCommand;
        }

        if (content.Contains("Macro - Start", StringComparison.OrdinalIgnoreCase)
            || content.Contains("Macro - End", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessedLineCategory.Macro;
        }

        if (content.Contains("System macro", StringComparison.OrdinalIgnoreCase)
            || content.Contains("Stop macro", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessedLineCategory.SystemMacro;
        }

        if (content.Contains("Event", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessedLineCategory.DriverEvent;
        }

        if (content.Contains("Button", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessedLineCategory.Button;
        }

        if (content.Contains("Page", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessedLineCategory.Page;
        }

        return ProcessedLineCategory.Default;
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
            "ProcessedLineClassifier",
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

    public static Brush GetBrush(ProcessedLineCategory category)
    {
        return category switch
        {
            ProcessedLineCategory.Connect => CreateBrush(0x39, 0xB5, 0x4A),
            ProcessedLineCategory.Disconnect => CreateBrush(0xFF, 0x00, 0x00),
            ProcessedLineCategory.Button => CreateBrush(0xFF, 0xFF, 0x00),
            ProcessedLineCategory.Page => CreateBrush(0x1E, 0x90, 0xFF),
            ProcessedLineCategory.DriverEvent => CreateBrush(0xFC, 0xB0, 0x40),
            ProcessedLineCategory.DriverCommand => CreateBrush(0xFF, 0xFF, 0xFF),
            ProcessedLineCategory.Macro => CreateBrush(0xA7, 0xA9, 0xAC),
            ProcessedLineCategory.SystemMacro => CreateBrush(0x9E, 0x1E, 0x9E),
            _ => CreateBrush(0x58, 0x58, 0x5A)
        };
    }

    private static Brush CreateBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
