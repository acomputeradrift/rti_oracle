using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;

namespace SHPDiagnosticsViewer.ProjectData;

public static class DiagnosticsMappingExporter
{
    public static void Export(string outputPath, IEnumerable<DiagnosticsMappingEntry> rows, ApexDiscoveryPreloadResult preload)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }
        if (preload is null)
        {
            throw new ArgumentNullException(nameof(preload));
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var file = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);

        WriteEntry(archive, "[Content_Types].xml", BuildContentTypes());
        WriteEntry(archive, "_rels/.rels", BuildRootRels());
        WriteEntry(archive, "xl/workbook.xml", BuildWorkbook());
        WriteEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRels());
        WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildSheet(rows));
        WriteEntry(archive, "xl/worksheets/sheet2.xml", BuildDriverConfigSheet(preload));
        WriteEntry(archive, "xl/worksheets/sheet3.xml", BuildSysVarSheet(preload));
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private static string BuildContentTypes()
    {
        return """
<?xml version="1.0" encoding="UTF-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml" />
  <Default Extension="xml" ContentType="application/xml" />
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml" />
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml" />
  <Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml" />
  <Override PartName="/xl/worksheets/sheet3.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml" />
</Types>
""";
    }

    private static string BuildRootRels()
    {
        return """
<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml" />
</Relationships>
""";
    }

    private static string BuildWorkbook()
    {
        return """
<?xml version="1.0" encoding="UTF-8"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="Diagnostics Mapping" sheetId="1" r:id="rId1" />
    <sheet name="Driver Config Map" sheetId="2" r:id="rId2" />
    <sheet name="System Variable Map" sheetId="3" r:id="rId3" />
  </sheets>
</workbook>
""";
    }

    private static string BuildWorkbookRels()
    {
        return """
<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml" />
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml" />
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet3.xml" />
</Relationships>
""";
    }

    private static string BuildSheet(IEnumerable<DiagnosticsMappingEntry> rows)
    {
        var ordered = rows.ToList();
        var allRows = new List<string[]>
        {
            new[] { "DeviceId", "DeviceName", "PageNumber", "PageName" }
        };
        allRows.AddRange(ordered.Select(entry => new[]
        {
            entry.DeviceId.ToString(),
            entry.DeviceName,
            entry.PageNumber.ToString(),
            entry.PageName
        }));
        return BuildWorksheet(allRows);
    }

    private static string BuildDriverConfigSheet(ApexDiscoveryPreloadResult preload)
    {
        var allRows = new List<string[]>
        {
            new[] { "DriverDeviceId", "DeviceName", "DisplayName", "Key", "Value", "ResolvedVariableName" }
        };
        foreach (var entry in preload.DriverConfigMap.OrderBy(entry => entry.Key))
        {
            foreach (var config in entry.Value.Config.OrderBy(kvp => kvp.Key))
            {
                var resolved = ResolveSysVarName(preload, config.Value);
                allRows.Add(new[]
                {
                    entry.Key.ToString(),
                    entry.Value.DeviceName,
                    entry.Value.DeviceDisplayName,
                    config.Key,
                    config.Value,
                    resolved
                });
            }
        }
        return BuildWorksheet(allRows);
    }

    private static string BuildSysVarSheet(ApexDiscoveryPreloadResult preload)
    {
        var allRows = new List<string[]>
        {
            new[] { "SysVarRef", "DriverDeviceId", "DriverName", "DeviceId", "VariableName" }
        };
        foreach (var entry in preload.SysVarRefMap.OrderBy(entry => entry.Key))
        {
            allRows.Add(new[]
            {
                entry.Key,
                entry.Value.DriverDeviceId?.ToString() ?? "",
                entry.Value.DriverName ?? "",
                entry.Value.DeviceId?.ToString() ?? "",
                entry.Value.VariableName ?? ""
            });
        }
        return BuildWorksheet(allRows);
    }

    private static string BuildWorksheet(List<string[]> rows)
    {
        var builder = new StringBuilder();
        builder.Append("""
<?xml version="1.0" encoding="UTF-8"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <sheetData>
""");

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            builder.Append($"    <row r=\"{rowIndex + 1}\">");
            var cells = rows[rowIndex];
            for (var colIndex = 0; colIndex < cells.Length; colIndex++)
            {
                var value = EscapeXml(cells[colIndex] ?? "");
                var cellRef = $"{ColumnName(colIndex + 1)}{rowIndex + 1}";
                builder.Append($"<c r=\"{cellRef}\" t=\"inlineStr\"><is><t>{value}</t></is></c>");
            }
            builder.AppendLine("</row>");
        }

        builder.Append("""
  </sheetData>
</worksheet>
""");
        return builder.ToString();
    }

    private static string ColumnName(int index)
    {
        var name = "";
        var dividend = index;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            name = Convert.ToChar('A' + modulo) + name;
            dividend = (dividend - modulo) / 26;
        }
        return name;
    }

    private static string EscapeXml(string value)
    {
        return SecurityElement.Escape(value) ?? "";
    }

    private static string ResolveSysVarName(ApexDiscoveryPreloadResult preload, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var normalized = value.StartsWith("SYSVARREF:", StringComparison.OrdinalIgnoreCase)
            ? value
            : $"SYSVARREF:{value}";
        if (preload.SysVarRefMap.TryGetValue(normalized, out var entry) && !string.IsNullOrWhiteSpace(entry.VariableName))
        {
            var driverName = entry.DriverName ?? "";
            return string.IsNullOrWhiteSpace(driverName) ? entry.VariableName : $"{driverName}: {entry.VariableName}";
        }

        return "";
    }
}
