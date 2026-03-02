using OracleByFPCLtd;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class WebSocketMessageFormatterTests
{
    [Fact]
    public void MessageLogLinesAreMarkedForNumberingWithDatedTimestamp()
    {
        // Requirement: mission.md - Core Capabilities #4; invariants.md - Determinism Invariant.
        var formatter = new WebSocketMessageFormatter(new DateOnly(2018, 12, 1));
        var raw = "{\"messageType\":\"MessageLog\",\"time\":\"15:19:03.456\",\"text\":\"Driver event\"}";

        var formatted = formatter.Format(raw, out var isLogLine);

        Assert.True(isLogLine);
        Assert.Equal("[18-12-01 3:19:03.456 PM] Driver event", formatted);
    }

    [Fact]
    public void MessageLogDateRollsOverAtMidnight()
    {
        // Requirement: mission.md - Core Capabilities #4; invariants.md - Determinism Invariant.
        var formatter = new WebSocketMessageFormatter(new DateOnly(2018, 12, 1));
        var beforeMidnight = "{\"messageType\":\"MessageLog\",\"time\":\"23:59:59.900\",\"text\":\"Last\"}";
        var afterMidnight = "{\"messageType\":\"MessageLog\",\"time\":\"00:00:00.100\",\"text\":\"First\"}";

        var formattedBefore = formatter.Format(beforeMidnight, out var isLogLineBefore);
        var formattedAfter = formatter.Format(afterMidnight, out var isLogLineAfter);

        Assert.True(isLogLineBefore);
        Assert.True(isLogLineAfter);
        Assert.Equal("[18-12-01 11:59:59.900 PM] Last", formattedBefore);
        Assert.Equal("[18-12-02 12:00:00.100 AM] First", formattedAfter);
    }

    [Fact]
    public void MessageLogDateDoesNotAdvanceForOutOfOrderTimes()
    {
        // Requirement: mission.md - Core Capabilities #4; invariants.md - Determinism Invariant.
        var formatter = new WebSocketMessageFormatter(new DateOnly(2018, 12, 1));
        var first = "{\"messageType\":\"MessageLog\",\"time\":\"14:16:20.000\",\"text\":\"First\"}";
        var outOfOrder = "{\"messageType\":\"MessageLog\",\"time\":\"13:10:00.000\",\"text\":\"Second\"}";

        var formattedFirst = formatter.Format(first, out var isLogLineFirst);
        var formattedSecond = formatter.Format(outOfOrder, out var isLogLineSecond);

        Assert.True(isLogLineFirst);
        Assert.True(isLogLineSecond);
        Assert.Equal("[18-12-01 2:16:20.000 PM] First", formattedFirst);
        Assert.Equal("[18-12-01 1:10:00.000 PM] Second", formattedSecond);
    }

    [Fact]
    public void SysvarLinesAreNotMarkedForNumbering()
    {
        // Requirement: mission.md - Core Capabilities #4; invariants.md - Output Honesty Invariant.
        var formatter = new WebSocketMessageFormatter(new DateOnly(2018, 12, 1));
        var raw = "{\"messageType\":\"Sysvar\",\"sysvarid\":12,\"sysvarval\":34}";

        var formatted = formatter.Format(raw, out var isLogLine);

        Assert.False(isLogLine);
        Assert.Equal("Sysvar id=12 val=34", formatted);
    }

    [Fact]
    public void EchoLinesAreNotMarkedForNumbering()
    {
        // Requirement: mission.md - Core Capabilities #4; invariants.md - Output Honesty Invariant.
        var formatter = new WebSocketMessageFormatter(new DateOnly(2018, 12, 1));
        var raw = "{\"messageType\":\"echo\",\"message\":\"hi\"}";

        var formatted = formatter.Format(raw, out var isLogLine);

        Assert.False(isLogLine);
        Assert.Equal("Echo hi", formatted);
    }
}
