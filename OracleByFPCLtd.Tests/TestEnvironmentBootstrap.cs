using System;
using System.Runtime.CompilerServices;

namespace OracleByFPCLtd.Tests;

internal static class TestEnvironmentBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        Environment.SetEnvironmentVariable(
            "ORACLE_EVENT_LOG_DIRECTORY_OVERRIDE",
            TestTempPaths.DefaultEventLogOverrideDirectory);
    }
}
