using System.Collections.Generic;
using OracleByFPCLtd.ProjectData;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class AdditionalInfoTemplatePlannerTests
{
    [Fact]
    public void PlannerFindsSchemasForDrivers()
    {
        var drivers = new List<DriverConfigEntry>
        {
            new("Clipsal C-Bus", "Clipsal C-Bus", new Dictionary<string, string>()),
            new("Vaux Lattis Matrix", "Vaux Lattis Matrix", new Dictionary<string, string>())
        };

        var schemas = AdditionalInfoTemplatePlanner.DetermineSchemas(drivers);

        Assert.Contains(schemas, schema => schema.SheetName == "Clipsal C-Bus");
        Assert.Contains(schemas, schema => schema.SheetName == "Clipsal C-Bus Scenes");
        Assert.Contains(schemas, schema => schema.SheetName == "Clipsal C-Bus HVAC");
        Assert.Contains(schemas, schema => schema.SheetName == "Vaux Lattis Matrix");
    }
}
