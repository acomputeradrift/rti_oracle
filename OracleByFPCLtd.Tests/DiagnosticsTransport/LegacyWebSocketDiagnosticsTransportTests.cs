using System.Threading.Tasks;
using System.Text.Json;
using OracleByFPCLtd.DiagnosticsTransport;
using OracleByFPCLtd.Reliability;
using Xunit;

namespace OracleByFPCLtd.Tests.DiagnosticsTransport;

public sealed class LegacyWebSocketDiagnosticsTransportTests
{
    [Fact]
    public void BuildLogLevelPayloadUsesCanonicalDriverShapeForDriverDName()
    {
        var payload = BuildPayload("DRIVER//2", "1");
        var json = JsonSerializer.Serialize(payload);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("Subscribe", root.GetProperty("type").GetString());
        Assert.Equal("LogLevel", root.GetProperty("resource").GetString());

        var value = root.GetProperty("value");
        Assert.Equal("DRIVER", value.GetProperty("type").GetString());
        Assert.Equal("2", value.GetProperty("driverId").GetString());
        Assert.Equal("1", value.GetProperty("level").GetString());
    }

    [Fact]
    public void BuildLogLevelPayloadKeepsChannelShapeForNonDriverTarget()
    {
        var payload = BuildPayload("EVENTS_INPUT", "3");
        var json = JsonSerializer.Serialize(payload);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var value = root.GetProperty("value");
        Assert.Equal("EVENTS_INPUT", value.GetProperty("type").GetString());
        Assert.Equal("3", value.GetProperty("level").GetString());
        Assert.False(value.TryGetProperty("driverId", out _));
    }

    [Fact]
    public async Task SendLogLevelCommandAsyncReturnsFailureWhenDisconnected()
    {
        var transport = new LegacyWebSocketDiagnosticsTransport();

        var result = await transport.SendLogLevelCommandAsync("DRIVER//1", "3");

        Assert.False(result.Dispatched);
        Assert.NotNull(result.Failure);
        Assert.Equal(FailureCodes.LogLevelDispatchFailed, result.Failure!.Code);
    }

    private static object BuildPayload(string type, string level)
    {
        var method = typeof(LegacyWebSocketDiagnosticsTransport).GetMethod(
            "BuildLogLevelPayload",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method!.Invoke(null, new object[] { type, level })!;
    }
}
