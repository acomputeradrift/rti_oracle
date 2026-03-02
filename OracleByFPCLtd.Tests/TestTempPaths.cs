using System;
using System.IO;

namespace OracleByFPCLtd.Tests;

internal static class TestTempPaths
{
    private static readonly string RootPath = Path.Combine(Path.GetTempPath(), "OracleByFPCLtd.Tests");
    private static readonly string DefaultEventLogOverridePath = Path.Combine(RootPath, "default-event-logs");

    internal static string RootDirectory => RootPath;
    internal static string DefaultEventLogOverrideDirectory => DefaultEventLogOverridePath;

    internal static string CreateFilePath(string extension)
    {
        Directory.CreateDirectory(RootPath);
        var normalizedExtension = extension.StartsWith(".", StringComparison.Ordinal)
            ? extension
            : "." + extension;
        return Path.Combine(RootPath, $"{Guid.NewGuid():N}{normalizedExtension}");
    }

    internal static string CreateDirectoryPath()
    {
        Directory.CreateDirectory(RootPath);
        return Path.Combine(RootPath, Guid.NewGuid().ToString("N"));
    }
}
