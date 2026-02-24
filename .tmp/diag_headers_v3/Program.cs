using System;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

var xlsx = @"\\mac\Home\Desktop\Development\Oracle\AdditionalInfo\Assets\26 02 24 Additional Info - Sung v3.xlsx";
XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
XNamespace relNs = "http://schemas.openxmlformats.org/package/2006/relationships";
XNamespace docRelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
using var zip = ZipFile.OpenRead(xlsx);
var wb = XDocument.Load(zip.GetEntry("xl/workbook.xml")!.Open());
var rels = XDocument.Load(zip.GetEntry("xl/_rels/workbook.xml.rels")!.Open());
var relMap = rels.Root!.Elements(relNs + "Relationship").ToDictionary(x => (string)x.Attribute("Id")!, x => (string)x.Attribute("Target")!);
var shared = zip.GetEntry("xl/sharedStrings.xml");
var sharedStrings = shared == null ? new System.Collections.Generic.List<string>() : XDocument.Load(shared.Open()).Descendants(ns+"si").Select(si => string.Concat(si.Descendants(ns+"t").Select(t => t.Value))).ToList();

foreach (var s in wb.Descendants(ns+"sheet"))
{
  var name=(string?)s.Attribute("name") ?? "";
  if (name != "Clipsal C-Bus HVAC") continue;
  var rid=(string?)s.Attribute(docRelNs + "id") ?? "";
  if (!relMap.TryGetValue(rid, out var target)) continue;
  if (!target.StartsWith("xl/")) target = "xl/" + target;
  var sh = XDocument.Load(zip.GetEntry(target)!.Open());
  var row = sh.Descendants(ns+"row").FirstOrDefault();
  var vals = row!.Elements(ns+"c").Select(c => {
    var t=(string?)c.Attribute("t") ?? "";
    var v = c.Element(ns+"v")?.Value ?? "";
    if (t=="s" && int.TryParse(v, out var i) && i>=0 && i<sharedStrings.Count) return sharedStrings[i];
    return v;
  });
  Console.WriteLine(string.Join(", ", vals));
}
