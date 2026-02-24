using System.IO.Compression;
using System.Xml.Linq;
using OracleByFPCLtd.ProjectData;

var apexPath = @"\\Mac\Home\Desktop\Development\Oracle\ApexDiscovery\Assets\Verrier Home FEENY EDIT v55.1 (Debug Set CBUS Moved Test Page).apex";
var outPath = @"\\Mac\Home\Desktop\Development\Oracle\.tmp\Verrier Additional Info Template.generated.fromlive.xlsx";

var preload = ApexDiscoveryPreloadExtractor.Extract(apexPath);
var schemas = AdditionalInfoTemplatePlanner.DetermineSchemas(preload.DriverConfigMap.Values);
AdditionalInfoTemplateBuilder.Create(outPath, schemas);

Console.WriteLine($"SchemaCount={schemas.Count}");
foreach (var s in schemas)
{
    Console.WriteLine($"SCHEMA|{s.SheetName}");
}

using var zip = ZipFile.OpenRead(outPath);
var workbook = zip.GetEntry("xl/workbook.xml") ?? throw new InvalidOperationException("workbook missing");
using var stream = workbook.Open();
var doc = XDocument.Load(stream);
XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
foreach (var sheet in doc.Descendants(ns + "sheet"))
{
    Console.WriteLine($"WORKBOOK|{sheet.Attribute("name")?.Value}");
}

Console.WriteLine($"OUT|{outPath}");
