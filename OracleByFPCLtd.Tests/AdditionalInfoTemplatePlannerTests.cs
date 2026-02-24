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

    [Fact]
    public void PlannerDoesNotIncludeRcm12SchemaWhenRcm12ExpansionTypeIsMissing()
    {
        var drivers = new List<DriverConfigEntry>
        {
            new("Clipsal C-Bus", "Clipsal C-Bus", new Dictionary<string, string>())
        };

        var schemas = AdditionalInfoTemplatePlanner.DetermineSchemas(drivers, new[] { 3, 5, 6 });

        Assert.DoesNotContain(schemas, schema => schema.SheetName == "RTI RCM-12 Relay Module");
    }

    [Fact]
    public void PlannerIncludesRcm12SchemaWhenRcm12ExpansionTypeIsPresent()
    {
        var drivers = new List<DriverConfigEntry>
        {
            new("Clipsal C-Bus", "Clipsal C-Bus", new Dictionary<string, string>())
        };

        var schemas = AdditionalInfoTemplatePlanner.DetermineSchemas(drivers, new[] { 7 });

        Assert.Contains(schemas, schema => schema.SheetName == "RTI RCM-12 Relay Module");
    }
}
