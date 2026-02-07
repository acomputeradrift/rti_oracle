using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OracleByFPCLtd.Settings.Models;

namespace OracleByFPCLtd.Settings.Services;

public sealed class RecentProjectService
{
    private const int MaxRecentItems = 5;

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
    }

    private static void Trim(List<RecentProjectEntry> items)
    {
        if (items.Count > MaxRecentItems)
        {
            items.RemoveRange(MaxRecentItems, items.Count - MaxRecentItems);
        }
    }
}
