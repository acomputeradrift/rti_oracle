using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using SHPDiagnosticsViewer.ProjectData;
using Xunit;

namespace SHPDiagnosticsViewer.Tests;

public sealed class DiagnosticsMappingExportTests
{
    [Fact]
    public void ExportCreatesXlsxWithHeadersAndRows()
    {
        // Requirement: mission.md - Core Capabilities #3/#4; invariants.md - Output Honesty.
        var rows = new List<DiagnosticsMappingEntry>
        {
            new(81, "Panel", 3, 0, 10, 100, "Room Select"),
            new(81, "Panel", 3, 1, 11, 101, "Overview")
        };
        var preload = new ApexDiscoveryPreloadResult();
        preload.DriverConfigMap[1] = new DriverConfigEntry(
            "Clipsal C-Bus",
            "Clipsal C-Bus",
            new Dictionary<string, string> { ["Input1"] = "SYSVARREF:{ABC}#1@Temp" });
        preload.SysVarRefMap["SYSVARREF:{ABC}#1@Temp"] = new SysVarRefEntry(1, "Clipsal C-Bus", "Group 1 Temp", 0);

        var tempPath = Path.Combine(Path.GetTempPath(), $"diagnostics_mapping_{Guid.NewGuid():N}.xlsx");
        try
        {
            DiagnosticsMappingExporter.Export(tempPath, rows, preload);

            Assert.True(File.Exists(tempPath));
            using var archive = ZipFile.OpenRead(tempPath);
            var sheet = archive.GetEntry("xl/worksheets/sheet1.xml");
            var driverSheet = archive.GetEntry("xl/worksheets/sheet2.xml");
            var sysvarSheet = archive.GetEntry("xl/worksheets/sheet3.xml");
            Assert.NotNull(sheet);
            Assert.NotNull(driverSheet);
            Assert.NotNull(sysvarSheet);
            using var reader = new StreamReader(sheet!.Open(), Encoding.UTF8);
            var xml = reader.ReadToEnd();

            Assert.Contains(">DeviceId<", xml, StringComparison.Ordinal);
            Assert.Contains(">DeviceName<", xml, StringComparison.Ordinal);
            Assert.Contains(">PageNumber<", xml, StringComparison.Ordinal);
            Assert.Contains(">PageName<", xml, StringComparison.Ordinal);
            Assert.Contains(">81<", xml, StringComparison.Ordinal);
            Assert.Contains(">Panel<", xml, StringComparison.Ordinal);
            Assert.Contains(">1<", xml, StringComparison.Ordinal);
            Assert.Contains(">Room Select<", xml, StringComparison.Ordinal);

            using var driverReader = new StreamReader(driverSheet!.Open(), Encoding.UTF8);
            var driverXml = driverReader.ReadToEnd();
            Assert.Contains(">DriverDeviceId<", driverXml, StringComparison.Ordinal);
            Assert.Contains(">DisplayName<", driverXml, StringComparison.Ordinal);
            Assert.Contains(">ResolvedVariableName<", driverXml, StringComparison.Ordinal);

            using var sysvarReader = new StreamReader(sysvarSheet!.Open(), Encoding.UTF8);
            var sysvarXml = sysvarReader.ReadToEnd();
            Assert.Contains(">SysVarRef<", sysvarXml, StringComparison.Ordinal);
            Assert.Contains(">VariableName<", sysvarXml, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
