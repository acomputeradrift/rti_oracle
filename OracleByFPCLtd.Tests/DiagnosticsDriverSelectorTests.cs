using System.Collections.Generic;
using OracleByFPCLtd;
using OracleByFPCLtd.DiagnosticsTransport;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class DiagnosticsDriverSelectorTests
{
    [Fact]
    public void FindsDiagnosticsDriverByNamePrefix()
    {
        var drivers = new List<DriverInfo>
        {
            new DriverInfo(1, "Clock", "DRIVER//1"),
            new DriverInfo(47, "Diagnostics: Primary Processor", "DRIVER//47"),
            new DriverInfo(3, "Audio Matrix", "DRIVER//3")
        };

        var found = DiagnosticsDriverSelector.TryGetDiagnosticsDriverDName(drivers, out var dName);

        Assert.True(found);
        Assert.Equal("DRIVER//47", dName);
    }

    [Fact]
    public void ReturnsFalseWhenDiagnosticsDriverMissing()
    {
        var drivers = new List<DriverInfo>
        {
            new DriverInfo(1, "Clock", "DRIVER//1"),
            new DriverInfo(3, "Audio Matrix", "DRIVER//3")
        };

        var found = DiagnosticsDriverSelector.TryGetDiagnosticsDriverDName(drivers, out var dName);

        Assert.False(found);
        Assert.Equal(string.Empty, dName);
    }
}
