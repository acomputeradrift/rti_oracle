using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.Settings.Models;

namespace OracleByFPCLtd.Settings.Services;

public sealed class RecentIpService
{
    private const int MaxRecentItems = 5;
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public void RecordRecentIp(OracleSettings settings, string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return;
        }

        settings.RecentIps.Insert(0, ip);
        if (settings.RecentIps.Count > MaxRecentItems)
        {
            settings.RecentIps.RemoveRange(MaxRecentItems, settings.RecentIps.Count - MaxRecentItems);
        }

        LogStructuredEvent(
            SeverityLevel.Info,
            "RecordRecentIp",
            "Recent IP recorded.",
            new Dictionary<string, string> { ["ip"] = ip });
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
            "RecentIpService",
            phase,
            message,
            details));
    }

    private static string CreateCorrelationId()
    {
        return System.Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    private static string BuildStructuredLogPath()
    {
        var folder = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "Oracle by FP&C",
            "Logs");
        return Path.Combine(folder, "oracle-structured.log");
    }
}
