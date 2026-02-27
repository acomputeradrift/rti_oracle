using System;
using System.Collections.Generic;
using OracleByFPCLtd.Reliability;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class UnhandledTaggedReportBuilderTests
{
    [Fact]
    public void Build_IncludesFullRawSampleText_ForTaggedProcessedMessage()
    {
        var tagged = new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Clipsal C-Bus"] = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["[Unknown State!]"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "Driver Command (Clipsal C-Bus): 'Garage Door 1 turned 121.'"
                }
            }
        };

        var processed = new[]
        {
            "42 [2026-02-26 15:41:22.123] Driver Command (Clipsal C-Bus): 'Garage Door 1 turned 121.' [Unknown State!]"
        };

        var raw = new[]
        {
            "42 [2026-02-26 15:41:22.123] Driver - Command:'Clipsal C-Bus\\General\\Immediate Switch(121, 1, 48)' Sustain:NO"
        };

        var report = UnhandledTaggedReportBuilder.Build(
            tagged,
            processed,
            raw,
            "1.2.0.0",
            createdUtc: new DateTime(2026, 2, 27, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(2, report.SchemaVersion);
        var entry = Assert.Single(Assert.Single(Assert.Single(report.Drivers).Tags).Entries);
        Assert.Equal("Driver Command (Clipsal C-Bus): 'Garage Door 1 turned 121.'", entry.ProcessedMessage);
        var sample = Assert.Single(entry.RawSamples);
        Assert.Equal(42, sample.RawLineNumber);
        Assert.Equal("[2026-02-26 15:41:22.123] Driver - Command:'Clipsal C-Bus\\General\\Immediate Switch(121, 1, 48)' Sustain:NO", sample.RawText);
    }

    [Fact]
    public void Build_DeduplicatesRawSamplesByLineNumberAndText()
    {
        var tagged = new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Clipsal C-Bus"] = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["[Unknown State!]"] = new HashSet<string>(StringComparer.Ordinal)
                {
                    "Driver Command (Clipsal C-Bus): 'Garage Door 1 turned 121.'"
                }
            }
        };

        var processed = new[]
        {
            "42 Driver Command (Clipsal C-Bus): 'Garage Door 1 turned 121.' [Unknown State!]",
            "42 Driver Command (Clipsal C-Bus): 'Garage Door 1 turned 121.' [Unknown State!]"
        };

        var raw = new[]
        {
            "42 Driver - Command:'Clipsal C-Bus\\General\\Immediate Switch(121, 1, 48)' Sustain:NO"
        };

        var report = UnhandledTaggedReportBuilder.Build(tagged, processed, raw, "1.2.0.0");
        var entry = Assert.Single(Assert.Single(Assert.Single(report.Drivers).Tags).Entries);
        Assert.Single(entry.RawSamples);
    }
}
