using System;
using System.IO;
using OracleByFPCLtd.Settings.Models;
using OracleByFPCLtd.Settings.Services;
using OracleByFPCLtd.Settings.Storage;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class SettingsIsolationTests
{
    [Fact]
    public void RecentProjectServiceKeepsUniqueFileNames()
    {
        var service = new RecentProjectService();
        var settings = new OracleSettings();

        service.RecordProjectSelection(settings, @"C:\A\Project.apex");
        service.RecordProjectSelection(settings, @"D:\B\Project.apex");

        Assert.Single(settings.RecentProjects);
        Assert.Equal(@"D:\B\Project.apex", settings.RecentProjects[0].FilePath);
    }

    [Fact]
    public void RecentIpServiceAllowsDuplicates()
    {
        var service = new RecentIpService();
        var settings = new OracleSettings();

        service.RecordRecentIp(settings, "192.168.1.10");
        service.RecordRecentIp(settings, "192.168.1.10");

        Assert.Equal(2, settings.RecentIps.Count);
    }

    [Fact]
    public void StoreRoundTripPreservesRecentProjects()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        var store = new OracleSettingsStore(path);
        var settings = new OracleSettings();
        var service = new RecentProjectService();
        service.RecordProjectSelection(settings, @"C:\Project.apex");

        store.Save(settings);
        var reloaded = store.Load();

        Assert.Single(reloaded.RecentProjects);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
