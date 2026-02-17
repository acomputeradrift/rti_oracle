using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.Settings.Models;

namespace OracleByFPCLtd.Settings.Services;

public sealed class RecentProjectService
{
    private const int MaxRecentItems = 5;
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public void RecordProjectSelection(OracleSettings settings, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var fileName = Path.GetFileName(filePath);
        var existing = settings.RecentProjects.FirstOrDefault(entry =>
            string.Equals(entry.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        var lastIp = existing?.LastSuccessfulIp;
        var lastConnected = existing?.LastConnectedAt;

        settings.RecentProjects.RemoveAll(entry =>
            string.Equals(entry.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        settings.RecentProjects.Insert(0, new RecentProjectEntry
        {
            FilePath = filePath,
            LastSuccessfulIp = lastIp,
            LastConnectedAt = lastConnected
        });

        Trim(settings.RecentProjects);
        LogStructuredEvent(
            SeverityLevel.Info,
            "RecordProjectSelection",
            "Recent project updated.",
            new Dictionary<string, string>
            {
                ["path"] = filePath,
                ["fileName"] = fileName
            });
    }

    public void RecordSuccessfulConnection(OracleSettings settings, string filePath, string ip)
    {
        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(ip))
        {
            return;
        }

        RecordProjectSelection(settings, filePath);
        settings.RecentProjects[0].LastSuccessfulIp = ip;
        settings.RecentProjects[0].LastConnectedAt = DateTime.Now;
        LogStructuredEvent(
            SeverityLevel.Info,
            "RecordSuccessfulConnection",
            "Recent project connection updated.",
            new Dictionary<string, string>
            {
                ["path"] = filePath,
                ["ip"] = ip
            });
    }

    private static void Trim(List<RecentProjectEntry> items)
    {
        if (items.Count > MaxRecentItems)
        {
            items.RemoveRange(MaxRecentItems, items.Count - MaxRecentItems);
        }
    }

    private void LogStructuredEvent(
        SeverityLevel severity,
        string phase,
        string message,
        IReadOnlyDictionary<string, string>? details = null)
    {
        _centralLogger.LogEvent(new LogEntry(
            severity,
            CreateCorrelationId(),
            "RecentProjectService",
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
