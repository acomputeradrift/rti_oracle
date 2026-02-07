using System;
using System.IO;
using OracleByFPCLtd.Settings.Models;

namespace OracleByFPCLtd.Settings.Services;

public sealed class AdditionalInfoService
{
    private const int MaxRecentItems = 5;

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
    }
}
