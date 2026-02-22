using System;
using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.Reliability;
using Xunit;

namespace OracleByFPCLtd.Tests.Reliability;

public sealed class NoProfileReportFileServiceTests
{
    [Fact]
    public void WriteCreatesJsonInPreferredFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"oracle_report_{Guid.NewGuid():N}");
        try
        {
            var report = new { CreatedUtc = DateTime.UtcNow, Drivers = new List<string> { "Driver A" } };
            var result = NoProfileReportFileService.Write(
                report,
                preferredFolders: new[] { folder },
                localNow: () => new DateTime(2026, 2, 22, 10, 0, 0));

            Assert.True(result.Success);
            Assert.NotNull(result.Path);
            Assert.True(File.Exists(result.Path!));
            Assert.Contains("oracle_no_profile_messages_20260222_100000.json", result.Path!, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }

    [Fact]
    public void WriteFallsBackWhenFirstFolderFails()
    {
        var root = Path.Combine(Path.GetTempPath(), $"oracle_report_{Guid.NewGuid():N}");
        var invalidFirst = Path.Combine(root, "blocked");
        var fallback = Path.Combine(root, "fallback");
        Directory.CreateDirectory(root);
        try
        {
            // Make first candidate invalid by creating a file where a directory is expected.
            File.WriteAllText(invalidFirst, "not a folder");

            var report = new { CreatedUtc = DateTime.UtcNow, Drivers = new List<string> { "Driver A" } };
            var result = NoProfileReportFileService.Write(
                report,
                preferredFolders: new[] { invalidFirst, fallback },
                localNow: () => new DateTime(2026, 2, 22, 10, 0, 1));

            Assert.True(result.Success);
            Assert.NotNull(result.Path);
            Assert.StartsWith(fallback, result.Path!, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(result.Path!));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public void WriteReturnsFailureWhenNoFoldersProvided()
    {
        var report = new { CreatedUtc = DateTime.UtcNow, Drivers = new List<string>() };

        var result = NoProfileReportFileService.Write(report, preferredFolders: Array.Empty<string>());

        Assert.False(result.Success);
        Assert.Null(result.Path);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }
}
