using System;
using System.IO;
using System.Linq;
using OracleByFPCLtd.ProjectData;

var assetsDir = @"\\mac\Home\Desktop\Development\Oracle\ApexDiscovery\Assets";
var targets = new[] { "VHDx", "RTI VIP-UHD-CTRL" };
var apexFiles = Directory.GetFiles(assetsDir, "*.apex").OrderBy(path => path).ToList();

foreach (var apexPath in apexFiles)
{
    try
    {
        var preload = ApexDiscoveryPreloadExtractor.Extract(apexPath);
        var drivers = preload.DriverConfigMap.Values
            .Select(entry => entry.DeviceName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matched = drivers
            .Where(name => targets.Any(target => string.Equals(name, target, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (matched.Count == 0)
        {
            continue;
        }

        Console.WriteLine(Path.GetFileName(apexPath));
        foreach (var name in matched)
        {
            Console.WriteLine($"  - {name}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{Path.GetFileName(apexPath)} -> ERROR: {ex.Message}");
    }
}
