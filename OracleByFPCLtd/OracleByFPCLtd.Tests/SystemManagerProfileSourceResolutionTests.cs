using System.Collections.Generic;
using OracleByFPCLtd.DriverProfiles;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class SystemManagerProfileSourceResolutionTests
{
    [Fact]
    public void SetSource_UsesSystemManagerSourceCatalog_WhenPresent()
    {
        var bundle = BuildBundle(
            systemManagerSources: new[] { "AV Overview", "Climate Overview", "Camera Overview" },
            fallbackSources: new[] { "Fallback 1", "Fallback 2", "Fallback 3" });

        var raw = "[2026-02-23 06:43:31.661] Driver - Command:'System Manager\\[Hide]\\Set Source(1)' Sustain:NO";
        var mapped = SystemManagerProfile.Mapper!.TryMap(raw, bundle, out var mappedText, out var unresolved);

        Assert.True(mapped);
        Assert.False(unresolved);
        Assert.Contains("Set Source(Climate Overview)", mappedText);
    }

    [Fact]
    public void SetSourceByRoom_UsesSystemManagerSourceCatalog_WhenPresent()
    {
        var bundle = BuildBundle(
            systemManagerSources: new[] { "Source Zero", "Source One", "Source Two" },
            fallbackSources: new[] { "Fallback 1", "Fallback 2", "Fallback 3" });

        var raw = "Driver - Command:'System Manager\\[Hide]\\Set Source By Room(Kitchen, 2)' Sustain:NO";
        var mapped = SystemManagerProfile.Mapper!.TryMap(raw, bundle, out var mappedText, out var unresolved);

        Assert.True(mapped);
        Assert.False(unresolved);
        Assert.Contains("Set Source By Room(Kitchen, Source Two)", mappedText);
    }

    [Fact]
    public void SetSource_FallsBackToLegacySourceCatalog_WhenSystemManagerCatalogMissing()
    {
        var bundle = BuildBundle(
            systemManagerSources: new string[0],
            fallbackSources: new[] { "Legacy Zero", "Legacy One", "Legacy Two" });

        var raw = "Driver - Command:'System Manager\\[Hide]\\Set Source(2)' Sustain:NO";
        var mapped = SystemManagerProfile.Mapper!.TryMap(raw, bundle, out var mappedText, out var unresolved);

        Assert.True(mapped);
        Assert.False(unresolved);
        Assert.Contains("Set Source(Legacy Two)", mappedText);
    }

    [Fact]
    public void SetSource_MarksUnresolved_WhenIndexOutOfRange()
    {
        var bundle = BuildBundle(
            systemManagerSources: new[] { "Only Zero" },
            fallbackSources: new[] { "Legacy Zero" });

        var raw = "Driver - Command:'System Manager\\[Hide]\\Set Source(10)' Sustain:NO";
        var mapped = SystemManagerProfile.Mapper!.TryMap(raw, bundle, out var mappedText, out var unresolved);

        Assert.True(mapped);
        Assert.True(unresolved);
        Assert.Equal(raw, mappedText);
    }

    private static ProjectDataBundle BuildBundle(
        IReadOnlyList<string> systemManagerSources,
        IReadOnlyList<string> fallbackSources)
    {
        var result = new ProjectDataExtractionResult();
        for (var i = 0; i < fallbackSources.Count; i++)
        {
            result.ApexDiscoveryPreload.SourceCatalog.Add(new SourceCatalogEntry(
                1000 + i,
                0,
                6,
                fallbackSources[i],
                fallbackSources[i]));
        }

        for (var i = 0; i < systemManagerSources.Count; i++)
        {
            result.ApexDiscoveryPreload.SystemManagerSourceCatalog.Add(new SystemManagerSourceCatalogEntry(
                i,
                systemManagerSources[i]));
        }

        return ProjectDataBundle.FromExtractionResult(result);
    }
}
