using OracleByFPCLtd;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class LogLevelAckParserTests
{
    [Theory]
    [InlineData("Diagnostics: Primary Processor - Setting LogLevel on DRIVER (36) to 0", "DRIVER//36", 0)]
    [InlineData("Diagnostics: Primary Processor - Setting LogLevel on EVENTS_INPUT to 3", "EVENTS_INPUT", 3)]
    [InlineData(
        "Diagnostics: Primary Processor - OnHTTPServerData() data.websocket = {\"type\":\"Subscribe\",\"resource\":\"LogLevel\",\"value\":{\"type\":\"DRIVER//1\",\"level\":\"3\"}}",
        "DRIVER//1",
        3)]
    public void ParsesLogLevelAckLines(string text, string expectedDName, int expectedLevel)
    {
        var parsed = LogLevelAckParser.TryParse(text, out var dName, out var level);

        Assert.True(parsed);
        Assert.Equal(expectedDName, dName);
        Assert.Equal(expectedLevel, level);
    }

    [Fact]
    public void IgnoresUnrelatedMessageLogLines()
    {
        var parsed = LogLevelAckParser.TryParse("Diagnostics: Primary Processor - Driver started", out var dName, out var level);

        Assert.False(parsed);
        Assert.Equal(string.Empty, dName);
        Assert.Equal(0, level);
    }
}
