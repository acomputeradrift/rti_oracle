using System;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace SHPDiagnosticsViewer.ProcessingEngine;

public enum ProcessedLineCategory
{
    Default,
    Connect,
    Disconnect,
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

        if (content.Contains("Driver - Command:", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessedLineCategory.DriverCommand;
        }

        if (content.Contains("Macro - Start", StringComparison.OrdinalIgnoreCase)
            || content.Contains("Macro - End", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessedLineCategory.Macro;
        }

        if (content.Contains("Driver event:", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessedLineCategory.DriverEvent;
        }

        return ProcessedLineCategory.Default;
    }

    public static Brush GetBrush(ProcessedLineCategory category)
    {
        return category switch
        {
            ProcessedLineCategory.Connect => Brushes.LimeGreen,
            ProcessedLineCategory.Disconnect => Brushes.Red,
            ProcessedLineCategory.DriverCommand => Brushes.LightGray,
            ProcessedLineCategory.Macro => Brushes.Orange,
            ProcessedLineCategory.DriverEvent => Brushes.Yellow,
            _ => Brushes.White
        };
    }
}
