using System.Linq;
using OracleByFPCLtd.DriverProfiles.Catalog;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class DriverProfileResultMapperCompletenessTests
{
    [Fact]
    public void AllCatalogProfilesExposeResultMapper()
    {
        var missing = DriverProfileCatalog.All()
            .Where(profile => profile.ResultMapper is null)
            .Select(profile => profile.DeviceName)
            .OrderBy(name => name, System.StringComparer.Ordinal)
            .ToList();

        Assert.Empty(missing);
    }
}
