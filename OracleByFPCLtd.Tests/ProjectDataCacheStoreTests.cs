using System;
using System.IO;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.Reliability;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class ProjectDataCacheStoreTests
{
    [Fact]
    public void TryLoadReportsFailureWhenCacheIsCorrupt()
    {
        var apexPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.apex");
        File.WriteAllText(apexPath, "stub");
        var cachePath = ProjectDataCacheStore.GetCachePath(apexPath);
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
        File.WriteAllText(cachePath, "{not-json");

        OperationFailure? failure = null;
        try
        {
            var loaded = ProjectDataCacheStore.TryLoad(apexPath, out _, candidate => failure = candidate);

            Assert.False(loaded);
            Assert.NotNull(failure);
            Assert.Equal(FailureCodes.ProjectParseFailed, failure!.Code);
        }
        finally
        {
            if (File.Exists(apexPath))
            {
                File.Delete(apexPath);
            }

            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }
        }
    }
}
