using System;
using System.IO;
using System.Linq;
using SHPDiagnosticsViewer.ProjectData;
using Xunit;

namespace SHPDiagnosticsViewer.Tests;

public sealed class ProjectDataExtractorTests
{
    [Fact]
    public void ExtractProducesDeterministicOrdering()
    {
        var result = CreateExtractor().Extract(GetFixturePath());

        Assert.True(IsSorted(result.DiagnosticsMapping.Select(entry => (entry.DeviceId, entry.PageId))));
        Assert.True(IsSorted(result.ProjectReport.Select(entry => (entry.EntityType, entry.PrimaryId))));
        Assert.True(IsSorted(result.ProjectTest.Select(entry => (entry.DeviceId, entry.PageId, entry.ButtonId))));
    }

    [Fact]
    public void DiagnosticsMappingIncludesRtiPanelHomePage()
    {
        var result = CreateExtractor().Extract(GetFixturePath());

        Assert.Contains(result.DiagnosticsMapping, entry =>
            entry.DeviceId == 6
            && entry.DeviceName == "RTiPanel (iPhone X or newer)"
            && entry.RtiAddress == 1
            && entry.PageId == 1
            && entry.PageNameId == 258
            && entry.PageName == "Home (Global)");
    }

    [Fact]
    public void ProjectReportIncludesRoomsDevicesAndPorts()
    {
        var result = CreateExtractor().Extract(GetFixturePath());

        Assert.Contains(result.ProjectReport, entry =>
            entry.EntityType == "Room"
            && entry.PrimaryId == 2
            && entry.SecondaryId == 8
            && entry.TertiaryId == 2
            && entry.Name == "Room 2");

        Assert.Contains(result.ProjectReport, entry =>
            entry.EntityType == "Device"
            && entry.PrimaryId == 6
            && entry.SecondaryId == 0
            && entry.Name == "RTiPanel (iPhone X or newer)");

        Assert.Contains(result.ProjectReport, entry =>
            entry.EntityType == "Port"
            && entry.PrimaryId == 1
            && entry.SecondaryId == -65536
            && entry.TertiaryId == 0
            && entry.Name == "Port 1");
    }

    [Fact]
    public void ProjectTestIncludesDeviceSourcePageAndButton()
    {
        var result = CreateExtractor().Extract(GetFixturePath());

        Assert.Contains(result.ProjectTest, entry =>
            entry.DeviceId == 6
            && entry.DeviceName == "RTiPanel (iPhone X or newer)"
            && entry.RtiAddress == 1
            && entry.SourceLabelId == 18
            && entry.SourceLabelIndex == 1
            && entry.PageId == 1
            && entry.PageNameId == 258
            && entry.PageName == "Home (Global)"
            && entry.ButtonId == 1
            && entry.ButtonTagId == 2
            && entry.ButtonText == "$%TAG!Room: Room 1%$");
    }

    [Fact]
    public void MissingFileFailsClearly()
    {
        var path = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.apex");
        var extractor = CreateExtractor();

        Assert.Throws<FileNotFoundException>(() => extractor.Extract(path));
    }

    private static string GetFixturePath()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "TEST - System Manager v10.apex");
        Assert.True(File.Exists(path), $"Fixture missing at {path}");
        return path;
    }

    private static IProjectDataExtractor CreateExtractor()
    {
        return new ProjectDataExtractor();
    }

    private static bool IsSorted<T>(System.Collections.Generic.IEnumerable<T> items) where T : IComparable<T>
    {
        var previous = default(T);
        var first = true;
        foreach (var item in items)
        {
            if (!first && previous!.CompareTo(item) > 0)
            {
                return false;
            }

            previous = item;
            first = false;
        }

        return true;
    }
}
