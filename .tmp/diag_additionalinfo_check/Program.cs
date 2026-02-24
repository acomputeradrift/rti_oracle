using System;
using System.Linq;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.ProjectData.Extractors;

var apexPath = @"\\mac\Home\Desktop\Development\Oracle\ApexDiscovery\Assets\Sung Residence v207.2.apex";
var additionalPath = @"\\mac\Home\Desktop\Development\Oracle\AdditionalInfo\Assets\26 02 24 Additional Info - Sung.xlsx";

var extractor = new ProjectDataExtractor();
var result = extractor.Extract(apexPath);
var driverNames = result.ApexDiscoveryPreload.DriverConfigMap.Values
    .Select(v => v.DeviceName)
    .Where(n => !string.IsNullOrWhiteSpace(n))
    .Distinct(StringComparer.Ordinal)
    .ToList();

Console.WriteLine($"Drivers in preload: {driverNames.Count}");
var additional = AdditionalDataExtractor.Extract(additionalPath, driverNames);
Console.WriteLine($"Additional drivers parsed: {additional.Drivers.Count}");
Console.WriteLine($"MatchedDriverNames: {string.Join(", ", additional.MatchedDriverNames)}");
Console.WriteLine($"ErrorCount: {additional.Errors.Count}");
foreach (var e in additional.Errors)
{
    Console.WriteLine("ERROR: " + e);
}
