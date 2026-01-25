using System.Collections.Generic;
using Engine = SHPDiagnosticsViewer.ProcessingEngine.ProcessingEngine;
using ProcessingContext = SHPDiagnosticsViewer.ProcessingEngine.ProcessingContext;
using Xunit;

namespace SHPDiagnosticsViewer.Tests;

public sealed class ProcessingEngineTests
{
    [Fact]
    public void MapsPageNumberToPageNameWithTimestamp()
    {
        // Requirement: mission.md - Core Capabilities #3/#4; invariants.md - Explicit Mapping.
        var context = new ProcessingContext(
            new Dictionary<string, int> { ["RTiPanel (iPhone X or newer)"] = 81 },
            new Dictionary<string, string> { ["81|0"] = "Room Select" });
        var engine = new Engine(context);

        var input = "[2026-01-24 10:00:00.000] Change to page 1 on device 'RTiPanel (iPhone X or newer)'";
        var output = engine.ProcessLine(input, 7);

        Assert.Equal("7 [2026-01-24 10:00:00.000] Change to page \"Room Select\" on device 'RTiPanel (iPhone X or newer)'", output.Text);
        Assert.False(output.IsUnresolved);
    }

    [Fact]
    public void UnresolvedPageMappingIsMarked()
    {
        // Requirement: mission.md - Core Capabilities #3/#4; invariants.md - Output Honesty.
        var context = new ProcessingContext(
            new Dictionary<string, int> { ["RTiPanel (iPhone X or newer)"] = 81 },
            new Dictionary<string, string>());
        var engine = new Engine(context);

        var input = "[2026-01-24 10:00:00.000] Change to page 1 on device 'RTiPanel (iPhone X or newer)'";
        var output = engine.ProcessLine(input, 7);

        Assert.Contains("[UNRESOLVED]", output.Text, StringComparison.Ordinal);
        Assert.True(output.IsUnresolved);
    }
}
