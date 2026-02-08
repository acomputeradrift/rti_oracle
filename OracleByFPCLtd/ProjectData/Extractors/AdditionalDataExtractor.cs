using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using OracleByFPCLtd.DriverProfiles.Catalog;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProjectData.Extractors;

public static class AdditionalDataExtractor
{
    private static readonly XNamespace WorkbookNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelationshipNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace DocumentRelationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static AdditionalData Extract(ProjectDataExtractionResult result)
    {
        return AdditionalData.FromExtractionResult(result);
    }

    public static AdditionalData Extract(string? filePath, IEnumerable<string> driverDeviceNames)
    {
        var data = new AdditionalData();
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return data;
        }

        var driverNames = new HashSet<string>(
            driverDeviceNames.Where(name => !string.IsNullOrWhiteSpace(name)),
            StringComparer.Ordinal);

        using var archive = ZipFile.OpenRead(filePath);
        var workbookSheets = ReadWorkbookSheets(archive, data.Errors);
        if (workbookSheets.Count == 0)
        {
            return data;
        }

        var sharedStrings = ReadSharedStrings(archive, data.Errors);
        var matched = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sheet in workbookSheets)
        {
            if (driverNames.Contains(sheet.Name))
            {
                if (matched.Add(sheet.Name))
                {
                    data.MatchedDriverNames.Add(sheet.Name);
                }
                continue;
            }

            data.Errors.Add($"Unmatched sheet: {sheet.Name}");
        }

        var profiles = DriverProfileCatalog.All()
            .Where(profile => profile.AdditionalInfoSchema is not null)
            .ToDictionary(profile => profile.DeviceName, StringComparer.Ordinal);

        foreach (var driverName in matched)
        {
            if (!profiles.TryGetValue(driverName, out var profile) || profile.AdditionalInfoSchema is null)
            {
                data.Errors.Add($"No driver profile for driver '{driverName}'.");
                continue;
            }

            var sheet = workbookSheets.FirstOrDefault(entry => string.Equals(entry.Name, driverName, StringComparison.Ordinal));
            if (sheet == null)
            {
                continue;
            }

            var rows = ReadSheetRows(archive, sheet.Path, sharedStrings, data.Errors, out var headers);
            if (rows.Count == 0 && headers.Count == 0)
            {
                continue;
            }

            ApplySchema(data, driverName, profile.AdditionalInfoSchema, headers, rows);
        }

        return data;
    }

    private static List<WorkbookSheet> ReadWorkbookSheets(ZipArchive archive, List<string> errors)
    {
        try
        {
            var workbookEntry = archive.GetEntry("xl/workbook.xml");
            if (workbookEntry == null)
            {
                errors.Add("Additional Info workbook is missing xl/workbook.xml.");
                return new List<WorkbookSheet>();
            }

            var relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
            if (relsEntry == null)
            {
                errors.Add("Additional Info workbook is missing xl/_rels/workbook.xml.rels.");
                return new List<WorkbookSheet>();
            }

            using var workbookStream = workbookEntry.Open();
            var workbook = XDocument.Load(workbookStream);
            using var relsStream = relsEntry.Open();
            var rels = XDocument.Load(relsStream);

            var targets = rels.Root?
                .Elements(RelationshipNamespace + "Relationship")
                .Select(rel => new
                {
                    Id = rel.Attribute("Id")?.Value,
                    Target = rel.Attribute("Target")?.Value
                })
                .Where(rel => !string.IsNullOrWhiteSpace(rel.Id) && !string.IsNullOrWhiteSpace(rel.Target))
                .ToDictionary(rel => rel.Id!, rel => rel.Target!, StringComparer.Ordinal)
                ?? new Dictionary<string, string>(StringComparer.Ordinal);

            var sheets = new List<WorkbookSheet>();
            foreach (var sheet in workbook.Descendants(WorkbookNamespace + "sheet"))
            {
                var name = sheet.Attribute("name")?.Value;
                var relId = sheet.Attribute(DocumentRelationshipNamespace + "id")?.Value;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(relId))
                {
                    continue;
                }

                if (!targets.TryGetValue(relId, out var target) || string.IsNullOrWhiteSpace(target))
                {
                    continue;
                }

                if (!target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
                {
                    target = $"xl/{target}";
                }

                sheets.Add(new WorkbookSheet(name, target));
            }

            return sheets;
        }
        catch (Exception ex)
        {
            errors.Add($"Additional Info workbook read failed: {ex.Message}");
            return new List<WorkbookSheet>();
        }
    }

    private static List<string> ReadSharedStrings(ZipArchive archive, List<string> errors)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
        {
            return new List<string>();
        }

        try
        {
            using var stream = entry.Open();
            var document = XDocument.Load(stream);
            var strings = new List<string>();
            foreach (var si in document.Descendants(WorkbookNamespace + "si"))
            {
                var text = string.Concat(si.Descendants(WorkbookNamespace + "t").Select(t => t.Value));
                strings.Add(text);
            }

            return strings;
        }
        catch (Exception ex)
        {
            errors.Add($"Additional Info shared strings read failed: {ex.Message}");
            return new List<string>();
        }
    }

    private static List<Dictionary<string, string>> ReadSheetRows(
        ZipArchive archive,
        string sheetPath,
        IReadOnlyList<string> sharedStrings,
        List<string> errors,
        out List<string> headers)
    {
        headers = new List<string>();
        var entry = archive.GetEntry(sheetPath);
        if (entry == null)
        {
            errors.Add($"Additional Info workbook is missing {sheetPath}.");
            return new List<Dictionary<string, string>>();
        }

        try
        {
            using var stream = entry.Open();
            var document = XDocument.Load(stream);
            var rows = document.Descendants(WorkbookNamespace + "row").ToList();
            if (rows.Count == 0)
            {
                return new List<Dictionary<string, string>>();
            }

            headers = ReadHeaderRow(rows[0], sharedStrings);
            if (headers.Count == 0)
            {
                return new List<Dictionary<string, string>>();
            }

            var dataRows = new List<Dictionary<string, string>>();
            foreach (var row in rows.Skip(1))
            {
                var rowData = new Dictionary<string, string>(StringComparer.Ordinal);
                var fallbackIndex = 1;
                foreach (var cell in row.Elements(WorkbookNamespace + "c"))
                {
                    var columnIndex = GetColumnIndex(cell, fallbackIndex);
                    fallbackIndex = columnIndex + 1;
                    if (columnIndex <= 0 || columnIndex > headers.Count)
                    {
                        continue;
                    }

                    var header = headers[columnIndex - 1];
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        continue;
                    }

                    var value = ReadCellValue(cell, sharedStrings);
                    rowData[header] = value;
                }

                if (rowData.Count > 0)
                {
                    dataRows.Add(rowData);
                }
            }

            return dataRows;
        }
        catch (Exception ex)
        {
            errors.Add($"Additional Info sheet read failed: {ex.Message}");
            return new List<Dictionary<string, string>>();
        }
    }

    private static List<string> ReadHeaderRow(XElement row, IReadOnlyList<string> sharedStrings)
    {
        var headers = new List<string>();
        var fallbackIndex = 1;
        foreach (var cell in row.Elements(WorkbookNamespace + "c"))
        {
            var columnIndex = GetColumnIndex(cell, fallbackIndex);
            fallbackIndex = columnIndex + 1;
            var header = ReadCellValue(cell, sharedStrings);
            if (columnIndex <= 0)
            {
                continue;
            }

            while (headers.Count < columnIndex)
            {
                headers.Add(string.Empty);
            }

            headers[columnIndex - 1] = header;
        }

        return headers;
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var cellType = cell.Attribute("t")?.Value;
        if (string.Equals(cellType, "inlineStr", StringComparison.OrdinalIgnoreCase))
        {
            var text = cell.Descendants(WorkbookNamespace + "t").Select(t => t.Value);
            return string.Concat(text);
        }

        var valueElement = cell.Element(WorkbookNamespace + "v");
        if (valueElement == null)
        {
            return string.Empty;
        }

        var value = valueElement.Value;
        if (string.Equals(cellType, "s", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(value, out var index)
            && index >= 0
            && index < sharedStrings.Count)
        {
            return sharedStrings[index];
        }

        return value;
    }

    private static int GetColumnIndex(XElement cell, int fallbackIndex)
    {
        var reference = cell.Attribute("r")?.Value;
        if (string.IsNullOrWhiteSpace(reference))
        {
            return fallbackIndex;
        }

        var columnLetters = new string(reference.TakeWhile(char.IsLetter).ToArray());
        if (string.IsNullOrWhiteSpace(columnLetters))
        {
            return fallbackIndex;
        }

        var index = 0;
        foreach (var letter in columnLetters.ToUpperInvariant())
        {
            index = (index * 26) + (letter - 'A' + 1);
        }

        return index <= 0 ? fallbackIndex : index;
    }

    private static void ApplySchema(
        AdditionalData data,
        string driverName,
        AdditionalInfoSchema schema,
        IReadOnlyList<string> headers,
        IReadOnlyList<Dictionary<string, string>> rows)
    {
        var requiredHeaders = schema.Columns.Select(column => column.Header).ToList();
        if (!requiredHeaders.All(header => headers.Contains(header)))
        {
            data.Errors.Add($"Missing required headers for driver '{driverName}'.");
            return;
        }

        if (!data.Drivers.TryGetValue(driverName, out var driverData))
        {
            driverData = new AdditionalDriverData();
            data.Drivers[driverName] = driverData;
        }

        var inputIndexHeader = schema.Columns.FirstOrDefault(c => c.Role == AdditionalInfoColumnRole.InputIndex)?.Header;
        var inputNameHeader = schema.Columns.FirstOrDefault(c => c.Role == AdditionalInfoColumnRole.InputName)?.Header;
        var outputIndexHeader = schema.Columns.FirstOrDefault(c => c.Role == AdditionalInfoColumnRole.OutputIndex)?.Header;
        var outputNameHeader = schema.Columns.FirstOrDefault(c => c.Role == AdditionalInfoColumnRole.OutputName)?.Header;

        foreach (var row in rows)
        {
            if (inputIndexHeader != null && inputNameHeader != null)
            {
                var inputIndexText = row.TryGetValue(inputIndexHeader, out var inputIndex) ? inputIndex : "";
                var inputName = row.TryGetValue(inputNameHeader, out var inputLabel) ? inputLabel : "";
                if (TryParseIndex(inputIndexText, out var index) && !string.IsNullOrWhiteSpace(inputName))
                {
                    AddMapping(driverData.InputNames, index, inputName, "input", driverName, data.Errors);
                }
            }

            if (outputIndexHeader != null && outputNameHeader != null)
            {
                var outputIndexText = row.TryGetValue(outputIndexHeader, out var outputIndex) ? outputIndex : "";
                var outputName = row.TryGetValue(outputNameHeader, out var outputLabel) ? outputLabel : "";
                if (TryParseIndex(outputIndexText, out var index) && !string.IsNullOrWhiteSpace(outputName))
                {
                    AddMapping(driverData.OutputNames, index, outputName, "output", driverName, data.Errors);
                }
            }
        }
    }

    private static bool TryParseIndex(string value, out int index)
    {
        index = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
        {
            return true;
        }

        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return false;
        }

        var rounded = Math.Round(number);
        if (Math.Abs(number - rounded) > 0.0001)
        {
            return false;
        }

        index = (int)rounded;
        return true;
    }

    private static void AddMapping(
        Dictionary<int, string> map,
        int index,
        string name,
        string kind,
        string driverName,
        List<string> errors)
    {
        if (map.TryGetValue(index, out var existing))
        {
            if (!string.Equals(existing, name, StringComparison.Ordinal))
            {
                errors.Add($"Conflicting {kind} name for index {index} in driver '{driverName}'.");
            }
            return;
        }

        map[index] = name;
    }

    private sealed record WorkbookSheet(string Name, string Path);
}
