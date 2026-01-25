using System.Collections.Generic;
using SHPDiagnosticsViewer.ProcessingEngine;
using ProcessingContext = SHPDiagnosticsViewer.ProcessingEngine.ProcessingContext;
using Xunit;

namespace SHPDiagnosticsViewer.Tests;

public sealed class ProcessingEngineRunnerTests
{
    [Fact]
    public void ProcessesOnlyNumberedLines()
    {
        // Requirement: mission.md - Core Capabilities #3/#4; invariants.md - Output Honesty.
        var context = new ProcessingContext(
            new Dictionary<string, int> { ["RTiPanel (iPhone X or newer)"] = 81 },
            new Dictionary<string, string> { ["81|0"] = "Room Select" });
        var engine = new ProcessingEngine.ProcessingEngine(context);
        var lines = new[]
        {
            "1 [2026-01-24 10:00:00.000] Change to page 1 on device 'RTiPanel (iPhone X or newer)'",
            "not a numbered line"
        };

        var output = ProcessingEngineRunner.ProcessNumberedLines(lines, engine);

        Assert.Single(output);
        Assert.Equal("1 [2026-01-24 10:00:00.000] Change to page \"Room Select\" on device 'RTiPanel (iPhone X or newer)'", output[0]);
    }
}
