using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.DriverProfiles.Models;

namespace OracleByFPCLtd.ProjectData;

public static class AdditionalInfoTemplateBuilder
{
    private static readonly CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildEventLogFilePathHint()
    });

    public static void Create(string outputPath, IReadOnlyList<AdditionalInfoSheetSchema> schemas)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            WriteEventLogEntry(
                SeverityLevel.Error,
                "Create",
                "Additional info template output path missing.",
                new Dictionary<string, string> { ["error"] = "ArgumentException" });
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }

        if (schemas == null || schemas.Count == 0)
        {
            WriteEventLogEntry(
                SeverityLevel.Error,
                "Create",
                "Additional info template schemas missing.",
                new Dictionary<string, string> { ["error"] = "ArgumentException" });
            throw new ArgumentException("At least one schema is required.", nameof(schemas));
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var file = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);

        WriteEntry(archive, "[Content_Types].xml", BuildContentTypes(schemas.Count));
        WriteEntry(archive, "_rels/.rels", BuildRootRels());
        WriteEntry(archive, "xl/workbook.xml", BuildWorkbook(schemas));
        WriteEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRels(schemas.Count));

        for (var i = 0; i < schemas.Count; i++)
        {
            var sheetName = $"xl/worksheets/sheet{i + 1}.xml";
            var headers = schemas[i].Columns.Select(column => column.Header).ToList();
            var rows = new List<string[]> { headers.ToArray() };
            WriteEntry(archive, sheetName, BuildWorksheet(rows));
        }

        WriteEventLogEntry(
            SeverityLevel.Info,
            "Create",
            "Additional info template created.",
            new Dictionary<string, string>
            {
                ["path"] = outputPath,
                ["sheets"] = schemas.Count.ToString()
            });
    }

    private static void WriteEventLogEntry(
        SeverityLevel severity,
        string phase,
        string message,
        IReadOnlyDictionary<string, string>? details = null)
    {
        CentralLogger.LogEvent(new LogEntry(
            severity,
            CreateCorrelationId(),
            "AdditionalInfoTemplateBuilder",
            phase,
            message,
            details));
    }

    private static string CreateCorrelationId()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    private static string BuildEventLogFilePathHint()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Oracle by FP&C",
            "Logs");
        return Path.Combine(folder, "oracle-structured.log");
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private static string BuildContentTypes(int sheetCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        builder.AppendLine("""<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">""");
        builder.AppendLine("  <Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\" />");
        builder.AppendLine("  <Default Extension=\"xml\" ContentType=\"application/xml\" />");
        builder.AppendLine("  <Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\" />");
        for (var i = 1; i <= sheetCount; i++)
        {
            builder.AppendLine($"  <Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\" />");
        }
        builder.AppendLine("</Types>");
        return builder.ToString();
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

    private static string BuildWorkbook(IReadOnlyList<AdditionalInfoSheetSchema> schemas)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""
<?xml version="1.0" encoding="UTF-8"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
          xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
""");
        for (var i = 0; i < schemas.Count; i++)
        {
            var sheetName = EscapeXml(schemas[i].SheetName);
            builder.AppendLine($"    <sheet name=\"{sheetName}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\" />");
        }
        builder.AppendLine("""
  </sheets>
</workbook>
""");
        return builder.ToString();
    }

    private static string BuildWorkbookRels(int sheetCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine("""
<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
""");
        for (var i = 1; i <= sheetCount; i++)
        {
            builder.AppendLine($"  <Relationship Id=\"rId{i}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i}.xml\" />");
        }
        builder.AppendLine("</Relationships>");
        return builder.ToString();
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
}

