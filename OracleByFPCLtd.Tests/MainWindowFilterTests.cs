using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class MainWindowFilterTests
{
    [Fact]
    public void ParseKeywordFilterSupportsIncludeAndExcludeTerms()
    {
        var method = GetStaticMethod("TryParseKeywordFilter");
        var args = new object?[] { "Driver Command, IR, -Apple TV", null, null, null };

        var result = (bool)method.Invoke(null, args)!;

        Assert.True(result);
        var include = (List<string>)args[1]!;
        var exclude = (List<string>)args[2]!;
        Assert.Equal(new[] { "Driver Command", "IR" }, include);
        Assert.Equal(new[] { "Apple TV" }, exclude);
    }

    [Fact]
    public void ParseKeywordFilterRejectsEmptyTerms()
    {
        var method = GetStaticMethod("TryParseKeywordFilter");
        var args = new object?[] { "Driver Command, -, IR", null, null, null };

        var result = (bool)method.Invoke(null, args)!;

        Assert.False(result);
        var error = (string)args[3]!;
        Assert.Contains("Invalid keyword filter", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LineMatchesFilterHonorsIncludeExcludeAndDateRange()
    {
        var method = GetStaticMethod("LineMatchesFilter");
        var include = new List<string> { "Driver Command", "IR" };
        var exclude = new List<string> { "Apple TV" };
        var start = new DateTime(2026, 1, 24, 9, 0, 0);
        var end = new DateTime(2026, 1, 24, 11, 0, 0);

        var matchingLine = "12 [2026-01-24 10:00:00.000] Driver Command: IR on TV";
        var excludedLine = "13 [2026-01-24 10:10:00.000] Driver Command: IR Apple TV";
        var outOfRangeLine = "14 [2026-01-24 12:00:00.000] Driver Command: IR on TV";

        Assert.True((bool)method.Invoke(null, new object?[] { matchingLine, include, exclude, start, end })!);
        Assert.False((bool)method.Invoke(null, new object?[] { excludedLine, include, exclude, start, end })!);
        Assert.False((bool)method.Invoke(null, new object?[] { outOfRangeLine, include, exclude, start, end })!);
    }

    [Fact]
    public void LineMatchesFilterExcludesLinesWithoutTimestampWhenDateFilterSet()
    {
        var method = GetStaticMethod("LineMatchesFilter");
        var include = new List<string>();
        var exclude = new List<string>();
        var start = new DateTime(2026, 1, 24, 9, 0, 0);
        var end = new DateTime(2026, 1, 24, 11, 0, 0);

        var line = "Driver Command: IR on TV";

        Assert.False((bool)method.Invoke(null, new object?[] { line, include, exclude, start, end })!);
    }

    [Fact]
    public void TryParseDateRangeAcceptsCombinedDateTime()
    {
        var method = GetStaticMethod("TryParseDateRange");
        var args = new object?[] { "2026-01-24 10:00", "2026-01-24 11:00", null, null, null };

        var result = (bool)method.Invoke(null, args)!;

        Assert.True(result);
        Assert.NotNull(args[2]);
        Assert.NotNull(args[3]);
    }

    [Fact]
    public void TryParseDateRangeRejectsInvalidFormat()
    {
        var method = GetStaticMethod("TryParseDateRange");
        var args = new object?[] { "2026-01-24", "", null, null, null };

        var result = (bool)method.Invoke(null, args)!;

        Assert.False(result);
        var error = (string)args[4]!;
        Assert.Contains("Invalid date/time filter", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryParseDateRangeRejectsEndBeforeStart()
    {
        var method = GetStaticMethod("TryParseDateRange");
        var args = new object?[] { "2026-01-24 12:00", "2026-01-24 10:00", null, null, null };

        var result = (bool)method.Invoke(null, args)!;

        Assert.False(result);
        var error = (string)args[4]!;
        Assert.Contains("Invalid date/time range", error, StringComparison.OrdinalIgnoreCase);
    }

    private static MethodInfo GetStaticMethod(string name)
    {
        var method = typeof(MainWindow).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return method!;
    }
}
