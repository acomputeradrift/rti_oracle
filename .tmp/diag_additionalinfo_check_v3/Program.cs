using System;
using System.Linq;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.ProjectData.Extractors;

var apexPath = @"\\mac\Home\Desktop\Development\Oracle\ApexDiscovery\Assets\Sung Residence v207.2.apex";
var additionalPath = @"\\mac\Home\Desktop\Development\Oracle\AdditionalInfo\Assets\26 02 24 Additional Info - Sung v3.xlsx";

var extractor = new ProjectDataExtractor();
var result = extractor.Extract(apexPath);
var driverNames = result.ApexDiscoveryPreload.DriverConfigMap.Values
    .Select(v => v.DeviceName)
    .Where(n => !string.IsNullOrWhiteSpace(n))
    .Distinct(StringComparer.Ordinal)
    .ToList();

var additional = AdditionalDataExtractor.Extract(additionalPath, driverNames);
Console.WriteLine($"ErrorCount: {additional.Errors.Count}");
foreach (var e in additional.Errors)
{
    Console.WriteLine("ERROR: " + e);
}
Console.WriteLine($"MatchedDriverNames: {string.Join(", ", additional.MatchedDriverNames)}");
