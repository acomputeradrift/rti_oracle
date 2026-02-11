using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using OracleByFPCLtd.DriverProfiles;
using OracleByFPCLtd.ProjectData;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class AdditionalInfoTemplateBuilderTests
{
    [Fact]
    public void BuilderCreatesSheetsWithHeaders()
    {
        var schemas = ClipsalCbusProfile.Definition.AdditionalInfoSchemas!;
        var tempPath = Path.Combine(Path.GetTempPath(), $"additional_info_{Guid.NewGuid():N}.xlsx");
        try
        {
            AdditionalInfoTemplateBuilder.Create(tempPath, schemas);

            using var archive = ZipFile.OpenRead(tempPath);
            var workbook = archive.GetEntry("xl/workbook.xml");
            Assert.NotNull(workbook);

            var workbookXml = XDocument.Load(workbook!.Open());
            var sheetNames = workbookXml.Descendants()
                .Where(node => node.Name.LocalName == "sheet")
                .Select(node => node.Attribute("name")?.Value)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            foreach (var schema in schemas)
            {
                Assert.Contains(schema.SheetName, sheetNames);
            }

            var firstSheet = archive.GetEntry("xl/worksheets/sheet1.xml");
            Assert.NotNull(firstSheet);
            var sheetXml = XDocument.Load(firstSheet!.Open());
            var headerRow = sheetXml.Descendants()
                .Where(node => node.Name.LocalName == "row" && node.Attribute("r")?.Value == "1")
                .FirstOrDefault();
            Assert.NotNull(headerRow);

            var headerText = headerRow!.Descendants()
                .Where(node => node.Name.LocalName == "t")
                .Select(node => node.Value)
                .ToList();
            var expectedHeaders = schemas[0].Columns.Select(c => c.Header).ToList();
            Assert.Equal(expectedHeaders, headerText);
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
