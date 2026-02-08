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
        data.Drivers["Driver A"] = driverData;

        var entries = AdditionalInfoDisplayBuilder.Build(data).ToList();

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, entry => entry.DriverName == "Driver A" && entry.MapType == "Input" && entry.Index == 1 && entry.Name == "Input 1");
        Assert.Contains(entries, entry => entry.DriverName == "Driver A" && entry.MapType == "Output" && entry.Index == 2 && entry.Name == "Output 2");
    }
}
