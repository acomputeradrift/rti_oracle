using System.Linq;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class AdditionalInfoDisplayBuilderTests
{
    [Fact]
    public void BuildsEntriesFromDriverMaps()
    {
        var data = new AdditionalData();
        var driverData = new AdditionalDriverData();
        driverData.InputNames[1] = "Input 1";
        driverData.OutputNames[2] = "Output 2";
        driverData.IntegerNames[3] = "Integer 3";
        driverData.RelayNames[4] = "Relay 4";
        driverData.CbusGroups[(56, 25)] = new CbusGroupEntry("Living Room", "Pendant");
        driverData.CbusHvacZones[(1, 0)] = new CbusHvacEntry("HVAC Group", "Zone A");
        driverData.CbusScenes[(202, 33, 0)] = new CbusSceneEntry("Lower Floor On");
        data.Drivers["Driver A"] = driverData;

        var entries = AdditionalInfoDisplayBuilder.Build(data).ToList();

        Assert.Equal(7, entries.Count);
        Assert.Contains(entries, entry => entry.DriverName == "Driver A" && entry.MapType == "Input" && entry.Index == 1 && entry.Name == "Input 1");
        Assert.Contains(entries, entry => entry.DriverName == "Driver A" && entry.MapType == "Output" && entry.Index == 2 && entry.Name == "Output 2");
        Assert.Contains(entries, entry => entry.DriverName == "Driver A" && entry.MapType == "Integer" && entry.Index == 3 && entry.Name == "Integer 3");
        Assert.Contains(entries, entry => entry.DriverName == "Driver A" && entry.MapType == "Relay" && entry.Index == 4 && entry.Name == "Relay 4");
        Assert.Contains(entries, entry => entry.DriverName == "Driver A" && entry.MapType == "C-Bus Group" && entry.Index == 25 && entry.Name == "Living Room Pendant");
        Assert.Contains(entries, entry => entry.DriverName == "Driver A" && entry.MapType == "C-Bus HVAC" && entry.Index == 0 && entry.Name == "HVAC Group Zone A");
        Assert.Contains(entries, entry => entry.DriverName == "Driver A" && entry.MapType == "C-Bus Scene" && entry.Index == 33 && entry.Name == "Lower Floor On");
    }

    [Fact]
    public void UsesSchemaPreviewEntriesWhenAvailable()
    {
        var data = new AdditionalData();
        var driverData = new AdditionalDriverData();
        driverData.PreviewEntries.Add(new AdditionalInfoPreviewEntry("System Variables", 1, "Room Count"));
        driverData.PreviewEntries.Add(new AdditionalInfoPreviewEntry("System Variables", 2, "Guests"));
        driverData.InputNames[1] = "Legacy Input";
        data.Drivers["System Variables"] = driverData;

        var entries = AdditionalInfoDisplayBuilder.Build(data).ToList();

        Assert.Equal(2, entries.Count);
        Assert.All(entries, entry => Assert.Equal("System Variables", entry.MapType));
        Assert.DoesNotContain(entries, entry => entry.Name == "Legacy Input");
    }
}
