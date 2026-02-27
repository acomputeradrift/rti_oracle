using OracleByFPCLtd.ProcessingEngine.Mapping;
using OracleByFPCLtd.ProcessingEngine.Models;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class PrintToLogProfileTests
{
    [Fact]
    public void DriverCommand_FormatsSetpointMessage_AsPassthroughSentence()
    {
        var service = new DriverMappingService();
        var bundle = new ProjectDataBundle();
        var evt = new DiagnosticEvent(
            7,
            "[2026-02-26 15:42:45.000] Driver - Command:'Print To Log\\Print To Log(Setpoint 22 pressed in Garage, current temp is <I1>, Connections)' Sustain:NO");

        var result = service.Map(evt, bundle);

        Assert.False(result.IsUnresolved);
        Assert.Equal(
            "7 [2026-02-26 15:42:45.000] Driver Command (Print To Log): 'Setpoint 22 pressed in Garage, current temp is <I1>, Connections.'",
            result.Text);
    }

    [Fact]
    public void DriverCommand_FormatsOtherCommands_UsingSamePassthroughRule()
    {
        var service = new DriverMappingService();
        var bundle = new ProjectDataBundle();
        var evt = new DiagnosticEvent(
            8,
            "[2026-02-26 15:42:46.000] Driver - Command:'Print To Log\\Another Command(Alpha, Beta, Gamma)' Sustain:NO");

        var result = service.Map(evt, bundle);

        Assert.False(result.IsUnresolved);
        Assert.Equal(
            "8 [2026-02-26 15:42:46.000] Driver Command (Print To Log): 'Alpha, Beta, Gamma.'",
            result.Text);
    }
}
