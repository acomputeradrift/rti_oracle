using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProjectData.Extractors;

public static class AdditionalDataExtractor
{
    private static readonly XNamespace WorkbookNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

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

        var sheetNames = ReadSheetNames(filePath, data.Errors);
        if (sheetNames.Count == 0)
        {
            return data;
        }

        var matched = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sheetName in sheetNames)
        {
            if (driverNames.Contains(sheetName))
            {
                if (matched.Add(sheetName))
                {
                    data.MatchedDriverNames.Add(sheetName);
                }
                continue;
            }

            data.Errors.Add($"Unmatched sheet: {sheetName}");
        }

        foreach (var driverName in matched)
        {
            data.Errors.Add($"No driver profile for driver '{driverName}'.");
        }

        return data;
    }

    private static List<string> ReadSheetNames(string filePath, List<string> errors)
    {
        try
        {
            using var archive = ZipFile.OpenRead(filePath);
            var entry = archive.GetEntry("xl/workbook.xml");
            if (entry == null)
            {
                errors.Add("Additional Info workbook is missing xl/workbook.xml.");
                return new List<string>();
            }

            using var stream = entry.Open();
            var document = XDocument.Load(stream);
            return document.Descendants(WorkbookNamespace + "sheet")
                .Select(sheet => sheet.Attribute("name")?.Value)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList()!;
        }
        catch (Exception ex)
        {
            errors.Add($"Additional Info workbook read failed: {ex.Message}");
            return new List<string>();
        }
    }
}
