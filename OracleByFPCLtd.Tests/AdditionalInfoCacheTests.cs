using System;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.ProjectData.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class AdditionalInfoCacheTests
{
    [Fact]
    public void CacheReusesDataForSameKey()
    {
        var cache = new AdditionalInfoCache();
        var key = new AdditionalInfoCacheKey("project.apex", DateTime.UnixEpoch, "info.xlsx", DateTime.UnixEpoch);
        var calls = 0;

        AdditionalData FirstLoad()
        {
            calls++;
            return new AdditionalData();
        }

        var first = cache.GetOrLoad(key, FirstLoad);
        var second = cache.GetOrLoad(key, FirstLoad);

        Assert.Same(first, second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void CacheReloadsWhenKeyChanges()
    {
        var cache = new AdditionalInfoCache();
        var keyA = new AdditionalInfoCacheKey("project.apex", DateTime.UnixEpoch, "info.xlsx", DateTime.UnixEpoch);
        var keyB = new AdditionalInfoCacheKey("project.apex", DateTime.UnixEpoch, "info.xlsx", DateTime.UnixEpoch.AddMinutes(1));
        var calls = 0;

        AdditionalData Load()
        {
            calls++;
            return new AdditionalData();
        }

        _ = cache.GetOrLoad(keyA, Load);
        _ = cache.GetOrLoad(keyB, Load);

        Assert.Equal(2, calls);
    }
}
