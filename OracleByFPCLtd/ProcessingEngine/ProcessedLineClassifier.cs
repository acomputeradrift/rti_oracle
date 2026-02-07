using System;
using System.Text.RegularExpressions;
using System.Windows.Media;

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
    DriverEvent
}

public static class ProcessedLineClassifier
{
    private static readonly Regex NumberPrefix = new Regex("^\\s*\\d+\\s+", RegexOptions.Compiled);

    public static ProcessedLineCategory DetermineCategory(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
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

        if (content.Contains("Button", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessedLineCategory.Button;
        }

        if (content.Contains("Page", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessedLineCategory.Page;
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

        if (content.Contains("Event", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessedLineCategory.DriverEvent;
        }

        return ProcessedLineCategory.Default;
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
