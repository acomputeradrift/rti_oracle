using System;
using OracleByFPCLtd.Formatting;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class DateTimeDisplayFormatterTests
{
    [Fact]
    public void FormatsHighPrecisionDisplayInTwelveHourClock()
    {
        var value = new DateTime(2026, 2, 28, 13, 56, 58, 942);

        var formatted = DateTimeDisplayFormatter.FormatHighPrecisionDisplay(value);

        Assert.Equal("26-02-28 1:56:58.942 PM", formatted);
    }

    [Fact]
    public void FormatsFilterDisplayInMinutePrecisionTwelveHourClock()
    {
        var value = new DateTime(2026, 2, 28, 13, 56, 58, 942);

        var formatted = DateTimeDisplayFormatter.FormatFilterDisplay(value);

        Assert.Equal("26-02-28 1:56 PM", formatted);
    }

    [Fact]
    public void ParsesNewFilterDisplayFormat()
    {
        var success = DateTimeDisplayFormatter.TryParseFilterInput("26-02-28 1:56 PM", out var parsed);

        Assert.True(success);
        Assert.Equal(new DateTime(2026, 2, 28, 13, 56, 0), parsed);
    }

    [Fact]
    public void ParsesLegacyTwentyFourHourFilterFormat()
    {
        var success = DateTimeDisplayFormatter.TryParseFilterInput("2026-02-28 13:56", out var parsed);

        Assert.True(success);
        Assert.Equal(new DateTime(2026, 2, 28, 13, 56, 0), parsed);
    }

    [Fact]
    public void ParsesNewHighPrecisionDisplayFormat()
    {
        var success = DateTimeDisplayFormatter.TryParseHighPrecisionInput("26-02-28 1:56:58.942 PM", out var parsed);

        Assert.True(success);
        Assert.Equal(new DateTime(2026, 2, 28, 13, 56, 58, 942), parsed);
    }

    [Fact]
    public void ParsesLegacyHighPrecisionTwentyFourHourFormat()
    {
        var success = DateTimeDisplayFormatter.TryParseHighPrecisionInput("2026-02-28 13:56:58.942", out var parsed);

        Assert.True(success);
        Assert.Equal(new DateTime(2026, 2, 28, 13, 56, 58, 942), parsed);
    }
}
