using System.Collections.Generic;
using OracleByFPCLtd.DriverProfiles;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class SystemManagerProfileResultMapperTests
{
    [Fact]
    public void ResultMapperReturnsNoFormatForUnhandledHiddenCommand()
    {
        var result = SystemManagerProfile.ResultMapper!.TryMap(
            "Driver - Command:'System Manager\\[Hide]\\Source Return' Sustain:NO",
            new ProjectDataBundle());

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.NoFormat, result.Status);
        Assert.Equal("Driver - Command:'System Manager\\[Hide]\\Source Return' Sustain:NO", result.Text);
        Assert.Equal("Driver profile claimed line but no format rule matched.", result.WarningMessage);
    }

    [Fact]
    public void ResultMapperReturnsPassThroughForVisibleCommandWithoutFormatter()
    {
        var result = SystemManagerProfile.ResultMapper!.TryMap(
            "Driver - Command:'System Manager\\Layer Visibility\\Set Layer Visibility(Source List)' Sustain:NO",
            new ProjectDataBundle());

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.PassThrough, result.Status);
        Assert.Equal("Driver - Command:'System Manager\\Layer Visibility\\Set Layer Visibility(Source List)' Sustain:NO", result.Text);
    }

    [Fact]
    public void ResultMapperReturnsResolvedForSetSourceWithCatalogMatch()
    {
        var result = SystemManagerProfile.ResultMapper!.TryMap(
            "Driver - Command:'System Manager\\[Hide]\\Set Source(1)' Sustain:NO",
            BuildBundle(new[] { "AV Overview", "Climate Overview", "Camera Overview" }));

        Assert.True(result.Claimed);
        Assert.Equal(DriverProfileProcessingStatus.Resolved, result.Status);
        Assert.Contains("Set Source(Climate Overview)", result.Text);
        Assert.NotNull(result.MappingResolution);
        Assert.Equal("source", result.MappingResolution!.Kind);
        Assert.Equal("1", result.MappingResolution.MappedFrom);
        Assert.Equal("Climate Overview", result.MappingResolution.MappedTo);
    }

    private static ProjectDataBundle BuildBundle(IReadOnlyList<string> systemManagerSources)
    {
        var result = new ProjectDataExtractionResult();
        for (var i = 0; i < systemManagerSources.Count; i++)
        {
            result.ApexDiscoveryPreload.SystemManagerSourceCatalog.Add(new SystemManagerSourceCatalogEntry(
                i,
                systemManagerSources[i]));
        }

        return ProjectDataBundle.FromExtractionResult(result);
    }
}
