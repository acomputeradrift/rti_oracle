using System;
using System.IO;
using OracleByFPCLtd.Settings.Models;
using OracleByFPCLtd.Settings.Services;
using OracleByFPCLtd.Settings.Storage;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public void RecordProjectSelectionKeepsUniqueFileNames()
    {
        var store = new OracleSettingsStore(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json"));
        var settings = new OracleSettings();
        var projectService = new RecentProjectService();

        projectService.RecordProjectSelection(settings, @"C:\A\Project.apex");
        projectService.RecordProjectSelection(settings, @"D:\B\Project.apex");

        Assert.Single(settings.RecentProjects);
        Assert.Equal(@"D:\B\Project.apex", settings.RecentProjects[0].FilePath);
    }

    [Fact]
    public void RecordSuccessfulConnectionUpdatesAssociation()
    {
        var store = new OracleSettingsStore(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json"));
        var settings = new OracleSettings();
        var projectService = new RecentProjectService();
        var ipService = new RecentIpService();

        projectService.RecordSuccessfulConnection(settings, @"C:\Project.apex", "192.168.1.10");
        ipService.RecordRecentIp(settings, "192.168.1.10");

        Assert.Single(settings.RecentProjects);
        Assert.Equal("192.168.1.10", settings.RecentProjects[0].LastSuccessfulIp);
        Assert.NotNull(settings.RecentProjects[0].LastConnectedAt);
    }

    [Fact]
    public void RecordRecentIpAllowsDuplicates()
    {
        var store = new OracleSettingsStore(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json"));
        var settings = new OracleSettings();
        var ipService = new RecentIpService();

        ipService.RecordRecentIp(settings, "192.168.1.10");
        ipService.RecordRecentIp(settings, "192.168.1.10");

        Assert.Equal(2, settings.RecentIps.Count);
        Assert.Equal("192.168.1.10", settings.RecentIps[0]);
        Assert.Equal("192.168.1.10", settings.RecentIps[1]);
    }

    [Fact]
    public void SaveAndLoadRoundTrip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        var store = new OracleSettingsStore(path);
        var settings = new OracleSettings();
        var projectService = new RecentProjectService();
        var ipService = new RecentIpService();
        projectService.RecordProjectSelection(settings, @"C:\Project.apex");
        ipService.RecordRecentIp(settings, "192.168.1.10");

        store.Save(settings);
        var reloaded = store.Load();

        Assert.Single(reloaded.RecentProjects);
        Assert.Single(reloaded.RecentIps);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
