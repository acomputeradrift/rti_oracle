using SHPDiagnosticsViewer;
using Xunit;

namespace SHPDiagnosticsViewer.Tests;

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
        Assert.Equal("[2018-12-01 15:19:03.456] Driver event", formatted);
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
        Assert.Equal("[2018-12-01 23:59:59.900] Last", formattedBefore);
        Assert.Equal("[2018-12-02 00:00:00.100] First", formattedAfter);
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
