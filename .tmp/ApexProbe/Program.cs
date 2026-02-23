using System;
using System.Linq;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.ProjectData.Models;
using OracleByFPCLtd.ProcessingEngine.Mapping;
using OracleByFPCLtd.ProcessingEngine.Models;

var apexPath = @"\\mac\Home\Desktop\Development\Oracle\ApexDiscovery\Assets\Verrier Home FEENY EDIT v55.1 (Debug Set CBUS Moved Test Page).apex";
var preload = ApexDiscoveryPreloadExtractor.Extract(apexPath);
var sources = preload.SourceCatalog.OrderBy(s => s.DeviceId).ToList();

Console.WriteLine($"SourceCatalogCount={sources.Count}");
var zeroIdx = 149;
for (var i = Math.Max(0, zeroIdx - 3); i <= Math.Min(sources.Count - 1, zeroIdx + 3); i++)
{
    var s = sources[i];
    Console.WriteLine($"ordinal={i + 1} zeroIdx={i} deviceId={s.DeviceId} roomId={s.RoomId} controlType={s.ControlType} source={s.SourceDisplayName}");
}

Console.WriteLine("---- deviceId direct 149 and 150 in source catalog ----");
foreach (var s in sources.Where(s => s.DeviceId is 149 or 150))
{
    Console.WriteLine($"deviceId={s.DeviceId} roomId={s.RoomId} controlType={s.ControlType} source={s.SourceDisplayName}");
}

Console.WriteLine("---- names of interest ----");
foreach (var s in sources.Where(s =>
             (s.SourceDisplayName?.IndexOf("Heat Pumps Schedule Manager", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
             || (s.SourceDisplayName?.IndexOf("Climate Overview in Master Bedroom", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0))
{
    Console.WriteLine($"deviceId={s.DeviceId} source={s.SourceDisplayName}");
}

var bundle = new ProjectDataBundle
{
    System = new SystemData(),
    Drivers = new DriverData(),
    Additional = new AdditionalData()
};
bundle.System.SourceCatalog.AddRange(preload.SourceCatalog);

var svc = new DriverMappingService();
var mapped = svc.Map(new DiagnosticEvent(4681, "[2026-02-23 06:43:31.661] Driver - Command:'System Manager\\[Hide]\\Set Source(149)' Sustain:NO"), bundle);
Console.WriteLine("---- mapped line ----");
Console.WriteLine(mapped.Text);
