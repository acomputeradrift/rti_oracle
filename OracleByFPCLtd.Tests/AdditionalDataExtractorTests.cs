using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using OracleByFPCLtd.ProjectData.Extractors;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class AdditionalDataExtractorTests
{
    [Fact]
    public void ExtractReturnsEmptyWhenPathMissing()
    {
        var data = AdditionalDataExtractor.Extract(null, new[] { "Driver One" });

        Assert.Empty(data.Errors);
        Assert.Empty(data.MatchedDriverNames);
    }

    [Fact]
    public void ExtractReportsUnmatchedSheetsAndMissingProfiles()
    {
        var path = CreateWorkbook("Driver One", "Other");
        try
        {
            var data = AdditionalDataExtractor.Extract(path, new[] { "Driver One", "Driver Two" });

            Assert.Contains("Unmatched sheet: Other", data.Errors);
            Assert.Contains("No driver profile for driver 'Driver One'.", data.Errors);
            Assert.Single(data.MatchedDriverNames);
            Assert.Contains("Driver One", data.MatchedDriverNames);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void ExtractParsesVauxLattisMatrixSheet()
    {
        var headers = new[]
        {
            "Audio Zone Input Index",
            "Audio Zone Input Name",
            "Audio Zone Output Index",
            "Audio Zone Output Name"
        };
        var rows = new List<object[]>
        {
            new object[] { 1.0, "Shaw 1", 13.0, "Gym" }
        };

        var path = CreateWorkbookWithSheetData("Vaux Lattis Matrix", headers, rows);
        try
        {
            var data = AdditionalDataExtractor.Extract(path, new[] { "Vaux Lattis Matrix" });

            Assert.DoesNotContain(data.Errors, error => error.Contains("No driver profile", StringComparison.Ordinal));
            Assert.True(data.Drivers.ContainsKey("Vaux Lattis Matrix"));
            var driverData = data.Drivers["Vaux Lattis Matrix"];
            Assert.Equal("Shaw 1", driverData.InputNames[1]);
            Assert.Equal("Gym", driverData.OutputNames[13]);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void ExtractParsesClipsalCbusSheets()
    {
        var cbusHeaders = new[] { "AppID", "GroupID", "GroupRoom", "GroupName" };
        var cbusRows = new List<object[]>
        {
            new object[] { 56.0, 25.0, "Living Room", "Pendant" }
        };
        var sceneHeaders = new[] { "AppID", "GroupID", "ActionSelector", "SceneName" };
        var sceneRows = new List<object[]>
        {
            new object[] { 202.0, 33.0, 0.0, "Lower Floor On" }
        };
        var hvacHeaders = new[] { "Groupld", "GroupName", "ZoneID", "ZoneName" };
        var hvacRows = new List<object[]>
        {
            new object[] { 1.0, "HVAC Group", 0.0, "Zone A" }
        };

        var path = CreateWorkbookWithSheets(new List<(string Name, IReadOnlyList<string> Headers, IReadOnlyList<object[]> Rows)>
        {
            ("Clipsal C-Bus", cbusHeaders, cbusRows),
            ("Clipsal C-Bus Scenes", sceneHeaders, sceneRows),
            ("Clipsal C-Bus HVAC", hvacHeaders, hvacRows)
        });
        try
        {
            var data = AdditionalDataExtractor.Extract(path, new[] { "Clipsal C-Bus" });

            Assert.Contains(data.Drivers, entry => entry.Key == "Clipsal C-Bus");
            var driverData = data.Drivers["Clipsal C-Bus"];
            Assert.Equal("Living Room", driverData.CbusGroups[(56, 25)].GroupRoom);
            Assert.Equal("Pendant", driverData.CbusGroups[(56, 25)].GroupName);
            Assert.Equal("Lower Floor On", driverData.CbusScenes[(202, 33, 0)].SceneName);
            Assert.Equal("HVAC Group", driverData.CbusHvacZones[(1, 0)].GroupName);
            Assert.Equal("Zone A", driverData.CbusHvacZones[(1, 0)].ZoneName);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void ExtractParsesSystemVariablesIntegerSheet()
    {
        var headers = new[] { "IntegerIndex", "IntegerName" };
        var rows = new List<object[]>
        {
            new object[] { 1.0, "Room Count" }
        };

        var path = CreateWorkbookWithSheetData("System Variables", headers, rows);
        try
        {
            var data = AdditionalDataExtractor.Extract(path, new[] { "System Variables" });

            Assert.DoesNotContain(data.Errors, error => error.Contains("No driver profile", StringComparison.Ordinal));
            Assert.True(data.Drivers.ContainsKey("System Variables"));
            var driverData = data.Drivers["System Variables"];
            Assert.Equal("Room Count", driverData.IntegerNames[1]);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void ExtractParsesRtiInternalRelaySheetEvenWhenInternalDriverIsNotInDriverNameInput()
    {
        var headers = new[] { "RelayIndex", "RelayName" };
        var rows = new List<object[]>
        {
            new object[] { 2.0, "Boiler Pump" }
        };

        var path = CreateWorkbookWithSheetData("RTI RCM-12 Relay Module", headers, rows);
        try
        {
            var data = AdditionalDataExtractor.Extract(path, new[] { "Clipsal C-Bus" });

            Assert.DoesNotContain(data.Errors, error => error.Contains("Unmatched sheet: RTI RCM-12 Relay Module", StringComparison.Ordinal));
            Assert.Contains("RTI Internal", data.Drivers.Keys);
            Assert.Equal("Boiler Pump", data.Drivers["RTI Internal"].RelayNames[2]);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static string CreateWorkbook(params string[] sheetNames)
    {
        var path = Path.Combine(Path.GetTempPath(), $"additional_{Guid.NewGuid():N}.xlsx");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);

        AddEntry(archive, "[Content_Types].xml", BuildContentTypes(sheetNames));
        AddEntry(archive, "_rels/.rels", BuildRootRelationships());
        AddEntry(archive, "xl/workbook.xml", BuildWorkbook(sheetNames));
        AddEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationships(sheetNames));

        for (var i = 0; i < sheetNames.Length; i++)
        {
            var entryName = $"xl/worksheets/sheet{i + 1}.xml";
            AddEntry(archive, entryName, BuildWorksheet());
        }

        return path;
    }

    private static string CreateWorkbookWithSheetData(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<object[]> rows)
    {
        var path = Path.Combine(Path.GetTempPath(), $"additional_{Guid.NewGuid():N}.xlsx");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);

        AddEntry(archive, "[Content_Types].xml", BuildContentTypes(new[] { sheetName }));
        AddEntry(archive, "_rels/.rels", BuildRootRelationships());
        AddEntry(archive, "xl/workbook.xml", BuildWorkbook(new[] { sheetName }));
        AddEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationships(new[] { sheetName }));
        AddEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheet(headers, rows));

        return path;
    }

    private static string CreateWorkbookWithSheets(
        IReadOnlyList<(string Name, IReadOnlyList<string> Headers, IReadOnlyList<object[]> Rows)> sheets)
    {
        var path = Path.Combine(Path.GetTempPath(), $"additional_{Guid.NewGuid():N}.xlsx");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);

        AddEntry(archive, "[Content_Types].xml", BuildContentTypes(sheets.Select(sheet => sheet.Name)));
        AddEntry(archive, "_rels/.rels", BuildRootRelationships());
        AddEntry(archive, "xl/workbook.xml", BuildWorkbook(sheets.Select(sheet => sheet.Name)));
        AddEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationships(sheets.Select(sheet => sheet.Name)));

        for (var i = 0; i < sheets.Count; i++)
        {
            var sheet = sheets[i];
            AddEntry(archive, $"xl/worksheets/sheet{i + 1}.xml", BuildWorksheet(sheet.Headers, sheet.Rows));
        }

        return path;
    }

    private static void AddEntry(ZipArchive archive, string entryName, string contents)
    {
        var entry = archive.CreateEntry(entryName);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(contents);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string BuildContentTypes(IEnumerable<string> sheetNames)
    {
        var overrides = sheetNames.Select((_, index) =>
            $"  <Override PartName=\"/xl/worksheets/sheet{index + 1}.xml\" " +
            "ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        return
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" " +
            "ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            string.Join("", overrides) +
            "</Types>";
    }

    private static string BuildRootRelationships()
    {
        return
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" " +
            "Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" " +
            "Target=\"xl/workbook.xml\"/>" +
            "</Relationships>";
    }

    private static string BuildWorkbook(IEnumerable<string> sheetNames)
    {
        var sheets = sheetNames.Select((name, index) =>
            $"<sheet name=\"{name}\" sheetId=\"{index + 1}\" r:id=\"rId{index + 1}\"/>");
        return
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
            "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets>" +
            string.Join("", sheets) +
            "</sheets>" +
            "</workbook>";
    }

    private static string BuildWorkbookRelationships(IEnumerable<string> sheetNames)
    {
        var relationships = sheetNames.Select((_, index) =>
            $"<Relationship Id=\"rId{index + 1}\" " +
            "Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" " +
            $"Target=\"worksheets/sheet{index + 1}.xml\"/>");
        return
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            string.Join("", relationships) +
            "</Relationships>";
    }

    private static string BuildWorksheet()
    {
        return
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<sheetData />" +
            "</worksheet>";
    }

    private static string BuildWorksheet(IReadOnlyList<string> headers, IReadOnlyList<object[]> rows)
    {
        var builder = new StringBuilder();
        builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        builder.Append("<sheetData>");
        builder.Append(BuildRow(1, headers.Cast<object>().ToArray()));
        for (var i = 0; i < rows.Count; i++)
        {
            builder.Append(BuildRow(i + 2, rows[i]));
        }
        builder.Append("</sheetData>");
        builder.Append("</worksheet>");
        return builder.ToString();
    }

    private static string BuildRow(int rowIndex, IReadOnlyList<object> values)
    {
        var builder = new StringBuilder();
        builder.Append($"<row r=\"{rowIndex}\">");
        for (var i = 0; i < values.Count; i++)
        {
            var column = ColumnName(i + 1);
            var reference = $"{column}{rowIndex}";
            builder.Append(BuildCell(reference, values[i]));
        }
        builder.Append("</row>");
        return builder.ToString();
    }

    private static string BuildCell(string reference, object value)
    {
        if (value is int or long or double)
        {
            return $"<c r=\"{reference}\"><v>{value}</v></c>";
        }

        var text = value?.ToString() ?? "";
        return $"<c r=\"{reference}\" t=\"inlineStr\"><is><t>{EscapeXml(text)}</t></is></c>";
    }

    private static string ColumnName(int index)
    {
        var value = index;
        var letters = new StringBuilder();
        while (value > 0)
        {
            value--;
            letters.Insert(0, (char)('A' + (value % 26)));
            value /= 26;
        }
        return letters.ToString();
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);
    }
}
