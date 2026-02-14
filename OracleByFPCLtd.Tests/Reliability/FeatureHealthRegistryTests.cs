using OracleByFPCLtd.Reliability;
using Xunit;

namespace OracleByFPCLtd.Tests.Reliability;

public sealed class FeatureHealthRegistryTests
{
    [Fact]
    public void UpdateStoresLatestOperationPerFeatureAndTarget()
    {
        var registry = new FeatureHealthRegistry();
        var first = new FeatureOperation("LogLevel", "DRIVER//1", "3", OperationStatus.Pending, 0, null);
        var second = new FeatureOperation("LogLevel", "DRIVER//1", "3", OperationStatus.Confirmed, 0, null);

        registry.Update(first);
        registry.Update(second);

        var found = registry.TryGet("LogLevel", "DRIVER//1", out var operation);
        Assert.True(found);
        Assert.NotNull(operation);
        Assert.Equal(OperationStatus.Confirmed, operation!.Status);
    }

    [Fact]
    public void TryGetReturnsFalseWhenNoStateExists()
    {
        var registry = new FeatureHealthRegistry();
        var found = registry.TryGet("LogLevel", "DRIVER//404", out var operation);

        Assert.False(found);
        Assert.Null(operation);
    }
}
