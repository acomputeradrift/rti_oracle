using System;
using System.Collections.Generic;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class ProjectDataBundleTests
{
    [Fact]
    public void AdapterMapsLegacyExtractionToBundle()
    {
        var extraction = BuildLegacyResult();

        var bundle = ProjectDataBundle.FromExtractionResult(extraction);

        Assert.Single(bundle.System.DiagnosticsMapping);
        Assert.Single(bundle.System.ProjectReport);
        Assert.Single(bundle.System.ProjectTest);
        Assert.Equal("Room Select", bundle.System.PageIndexMap["81|0"]);
        Assert.Equal("Driver 1", bundle.Drivers.DriverConfigMap[1].DeviceDisplayName);
        Assert.Single(bundle.Drivers.DriverTemplateVariables);
        Assert.NotNull(bundle.Additional);
    }

    [Fact]
    public void AdapterRoundTripsToLegacyResult()
    {
        var extraction = BuildLegacyResult();

        var bundle = ProjectDataBundle.FromExtractionResult(extraction);
        var roundTrip = bundle.ToExtractionResult();

        Assert.Single(roundTrip.DiagnosticsMapping);
        Assert.Single(roundTrip.ProjectReport);
        Assert.Single(roundTrip.ProjectTest);
        Assert.Equal(extraction.ApexDiscoveryPreload.PageIndexMap["81|0"], roundTrip.ApexDiscoveryPreload.PageIndexMap["81|0"]);
        Assert.Equal(extraction.ApexDiscoveryPreload.DriverConfigMap[1].DeviceDisplayName, roundTrip.ApexDiscoveryPreload.DriverConfigMap[1].DeviceDisplayName);
        Assert.Single(roundTrip.ApexDiscoveryPreload.DriverTemplateVariables);
    }

    private static ProjectDataExtractionResult BuildLegacyResult()
    {
        var result = new ProjectDataExtractionResult();
        result.DiagnosticsMapping.Add(new DiagnosticsMappingEntry(1, "Device", 0, 0, 0, 0, "Page"));
        result.ProjectReport.Add(new ProjectReportEntry("Room", 1, 0, 0, "Room", ""));
        result.ProjectTest.Add(new ProjectTestEntry(1, "Device", 0, 1, 0, "Source", 1, 0, "Page", 1, 1, "Button"));

        var preload = new ApexDiscoveryPreloadResult();
        preload.PageIndexMap["81|0"] = "Room Select";
        preload.SysVarRefMap["SV1"] = new SysVarRefEntry(null, null, "Var", null);
        preload.DriverConfigMap[1] = new DriverConfigEntry("Driver 1", "Driver 1", new Dictionary<string, string>(StringComparer.Ordinal));
        preload.DriverTemplateVariables.Add(new DriverTemplateVariableEntry(
            1, "Driver 1", "Driver 1", "SV1", "SV1", "1", "Driver 1", "Category", "Var", "Type", "Format"));
        result.ApexDiscoveryPreload = preload;
        return result;
    }
}
