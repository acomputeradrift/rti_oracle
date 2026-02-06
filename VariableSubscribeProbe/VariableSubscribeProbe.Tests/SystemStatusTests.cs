using VariableSubscribeProbe;
using Xunit;

namespace VariableSubscribeProbe.Tests;

public sealed class SystemStatusTests
{
    [Fact]
    public void ParseExtractsKeyMetrics()
    {
        var json = "{" +
                   "\"name\":\"XP-8v (Primary Processor)\"," +
                   "\"firmware_version\":\"24.3.29\"," +
                   "\"ip_address\":\"192.168.1.143\"," +
                   "\"sysvar_load\":\"0\"," +
                   "\"uptime\":335686," +
                   "\"memory_free\":\"19.6 MB\"," +
                   "\"memory_load\":57," +
                   "\"memory_history\":[{" +
                   "\"memory_free\":\"19.6 MB\"," +
                   "\"memory_load\":57," +
                   "\"timestamp\":1770029287000" +
                   "}]" +
                   "}";

        var status = SystemStatus.Parse(json);

        Assert.Equal("19.6 MB", status.MemoryFree);
        Assert.Equal(57, status.MemoryLoad);
        Assert.Equal("0", status.SysvarLoad);
        Assert.Equal(335686, status.Uptime);
        Assert.Single(status.MemoryHistory);
    }
}
