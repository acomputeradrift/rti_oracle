using System;
using System.Linq;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.ProjectData.Extractors;

var apexPath = @"\\mac\Home\Desktop\Development\Oracle\ApexDiscovery\Assets\Sung Residence v207.2.apex";
var additionalPath = @"\\mac\Home\Desktop\Development\Oracle\AdditionalInfo\Assets\Additional Info - Sung.xlsx";

var result = new ProjectDataExtractor().Extract(apexPath);
var schemas = AdditionalInfoTemplatePlanner.DetermineSchemas(
    result.ApexDiscoveryPreload.DriverConfigMap.Values,
    result.ApexDiscoveryPreload.ExpansionDeviceTypes,
    result.ApexDiscoveryPreload.RelayPorts);
Console.WriteLine("Template has RCM-12 sheet: " + schemas.Any(s => s.SheetName == "RTI RCM-12 Relay Module"));

var driverNames = result.ApexDiscoveryPreload.DriverConfigMap.Values.Select(v => v.DeviceName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.Ordinal);
var additional = AdditionalDataExtractor.Extract(additionalPath, driverNames);
if (additional.Drivers.TryGetValue("RTI Internal", out var internalData))
{
    Console.WriteLine("RTI Internal relay names parsed: " + internalData.RelayNames.Count);
}
else
{
    Console.WriteLine("RTI Internal relay names parsed: 0");
}
