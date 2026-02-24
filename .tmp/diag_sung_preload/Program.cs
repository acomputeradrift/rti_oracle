using System;
using System.Linq;
using OracleByFPCLtd.ProjectData;

var apexPath = @"\\mac\Home\Desktop\Development\Oracle\ApexDiscovery\Assets\Sung Residence v207.2.apex";
var extractor = new ProjectDataExtractor();
var result = extractor.Extract(apexPath);
Console.WriteLine("RelayPorts count: " + result.ApexDiscoveryPreload.RelayPorts.Count);
foreach (var p in result.ApexDiscoveryPreload.RelayPorts.Take(40))
{
    Console.WriteLine($"CTRL={p.ControllerDeviceName} | TYPE={p.ExpanderDeviceType} | EXP={p.ExpanderName} | REL={p.RelayName}");
}
