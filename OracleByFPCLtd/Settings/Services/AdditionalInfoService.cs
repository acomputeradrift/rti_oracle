using System;
using System.IO;
using System.Collections.Generic;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.Settings.Models;

namespace OracleByFPCLtd.Settings.Services;

public sealed class AdditionalInfoService
{
    private const int MaxRecentItems = 5;
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildEventLogFilePathHint()
    });

    public void RecordAdditionalInfo(OracleSettings settings, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var fileName = Path.GetFileName(filePath);
        settings.RecentAdditionalInfo.RemoveAll(entry =>
            string.Equals(Path.GetFileName(entry), fileName, StringComparison.OrdinalIgnoreCase));
        settings.RecentAdditionalInfo.Insert(0, filePath);
        if (settings.RecentAdditionalInfo.Count > MaxRecentItems)
        {
            settings.RecentAdditionalInfo.RemoveRange(MaxRecentItems, settings.RecentAdditionalInfo.Count - MaxRecentItems);
        }

        WriteEventLogEntry(
            SeverityLevel.Info,
            "RecordAdditionalInfo",
            "Additional info file recorded.",
            new Dictionary<string, string>
            {
                ["path"] = filePath,
                ["fileName"] = fileName
            });
    }

    private void WriteEventLogEntry(
        SeverityLevel severity,
        string phase,
        string message,
        IReadOnlyDictionary<string, string>? details = null)
    {
        _centralLogger.LogEvent(new LogEntry(
            severity,
            CreateCorrelationId(),
            "AdditionalInfoService",
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

