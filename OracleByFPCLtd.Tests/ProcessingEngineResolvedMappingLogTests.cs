using System;
using System.IO;
using System.Reflection;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.ProcessingEngine;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class ProcessingEngineResolvedMappingLogTests
{
    [Fact]
    public void ProcessEventLogsResolvedCbusMappingsWhenUsingResultMapperPath()
    {
        var logPath = TestTempPaths.CreateFilePath(".log");
        var logger = new CentralLogger(new CentralLoggerOptions
        {
            SessionLogPath = logPath,
            TimestampProvider = () => new DateTime(2026, 2, 28, 12, 32, 0, DateTimeKind.Local)
        });

        var bundle = BuildBundleWithCbus();
        var engine = new OracleByFPCLtd.ProcessingEngine.ProcessingEngine(bundle);
        OverrideProcessingEngineLogger(engine, logger);

        _ = engine.ProcessEvent(new OracleByFPCLtd.ProcessingEngine.Models.DiagnosticEvent(
            42,
            "Driver - Command:'Clipsal C-Bus\\General\\Immediate Switch(121, 78, 56)' Sustain:NO"));

        var log = File.ReadAllText(logPath);
        Assert.Contains("[SUCCESS] ProcessingEngine/Mapping:", log, StringComparison.Ordinal);
        Assert.Contains("profile=\"Clipsal C-Bus\"", log, StringComparison.Ordinal);
        Assert.Contains("source=\"Additional Info\"", log, StringComparison.Ordinal);

        File.Delete(logPath);
    }

    private static ProjectDataBundle BuildBundleWithCbus()
    {
        var bundle = new ProjectDataBundle();
        var driverData = new AdditionalDriverData();
        driverData.CbusGroups[(56, 78)] = new CbusGroupEntry("Living Room", "Pendant");
        bundle.Additional.Drivers["Clipsal C-Bus"] = driverData;
        return bundle;
    }

    private static void OverrideProcessingEngineLogger(OracleByFPCLtd.ProcessingEngine.ProcessingEngine engine, CentralLogger logger)
    {
        var method = typeof(OracleByFPCLtd.ProcessingEngine.ProcessingEngine).GetMethod(
            "OverrideCentralLoggerForTesting",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(engine, new object[] { logger });
    }
}
