using OracleByFPCLtd.ProcessingEngine.Mapping;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class SystemVariableEventsProfileTests
{
    [Fact]
    public void DriverCommand_FormatsBaseInstance_AsPassthrough()
    {
        var service = new DriverMappingService();
        var bundle = new ProjectDataBundle();
        var evt = new DiagnosticEvent(
            20,
            "[2026-02-26 15:42:45.000] Driver - Command:'System Variable Events\\Integer 1 Event\\Disable' Sustain:NO");

        var result = service.Map(evt, bundle);

        Assert.False(result.IsUnresolved);
        Assert.Equal(
            "20 [2026-02-26 15:42:45.000] Driver Command (System Variable Events): 'Integer 1 Event\\Disable.'",
            result.Text);
    }

    [Fact]
    public void DriverCommand_FormatsNumberedInstance_AsPassthrough()
    {
        var service = new DriverMappingService();
        var bundle = new ProjectDataBundle();
        var evt = new DiagnosticEvent(
            21,
            "[2026-02-26 15:42:46.000] Driver - Command:'System Variable Events #2\\Integer 1 Event\\Disable' Sustain:NO");

        var result = service.Map(evt, bundle);

        Assert.False(result.IsUnresolved);
        Assert.Equal(
            "21 [2026-02-26 15:42:46.000] Driver Command (System Variable Events #2): 'Integer 1 Event\\Disable.'",
            result.Text);
    }

    [Fact]
    public void DriverCommand_FormatsThirdInstance_AsPassthrough()
    {
        var service = new DriverMappingService();
        var bundle = new ProjectDataBundle();
        var evt = new DiagnosticEvent(
            22,
            "[2026-02-26 15:42:47.000] Driver - Command:'System Variable Events #3\\Integer 4 Event\\Enable' Sustain:NO");

        var result = service.Map(evt, bundle);

        Assert.False(result.IsUnresolved);
        Assert.Equal(
            "22 [2026-02-26 15:42:47.000] Driver Command (System Variable Events #3): 'Integer 4 Event\\Enable.'",
            result.Text);
    }
}
