using System.Collections.Generic;
using OracleByFPCLtd.Settings.Models;

namespace OracleByFPCLtd.Settings.Services;

public sealed class RecentIpService
{
    private const int MaxRecentItems = 5;

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
    }
}
