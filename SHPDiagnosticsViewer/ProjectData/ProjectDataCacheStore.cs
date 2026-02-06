using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SHPDiagnosticsViewer.ProjectData;

public static class ProjectDataCacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    public static string GetCachePath(string apexPath)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RTI Oracle",
            "ProjectCache");
        var hash = ComputePathHash(apexPath);
        return Path.Combine(folder, $"{hash}.json");
    }

    public static bool TryLoad(string apexPath, out ProjectDataExtractionResult result)
    {
        result = new ProjectDataExtractionResult();
        if (string.IsNullOrWhiteSpace(apexPath))
        {
            return false;
        }

        var cachePath = GetCachePath(apexPath);
        if (!File.Exists(cachePath))
        {
            return false;
        }

        ProjectDataCacheFile? cache;
        try
        {
            var json = File.ReadAllText(cachePath);
            cache = JsonSerializer.Deserialize<ProjectDataCacheFile>(json, JsonOptions);
        }
        catch
        {
            return false;
        }

        if (cache is null || string.IsNullOrWhiteSpace(cache.ApexPath))
        {
            return false;
        }

        if (!File.Exists(apexPath))
        {
            return false;
        }

        var lastWrite = File.GetLastWriteTimeUtc(apexPath);
        if (cache.ApexLastWriteUtc != lastWrite)
        {
            return false;
        }

        result = cache.ToResult();
        return true;
    }

    public static void Save(string apexPath, ProjectDataExtractionResult result)
    {
        if (string.IsNullOrWhiteSpace(apexPath))
        {
            return;
        }

        var cachePath = GetCachePath(apexPath);
        var directory = Path.GetDirectoryName(cachePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var cache = ProjectDataCacheFile.FromResult(apexPath, result);
        var json = JsonSerializer.Serialize(cache, JsonOptions);
        File.WriteAllText(cachePath, json);
    }

    private static string ComputePathHash(string apexPath)
    {
        var normalized = Path.GetFullPath(apexPath).ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }

    private sealed class ProjectDataCacheFile
    {
        public string ApexPath { get; set; } = "";
        public DateTime ApexLastWriteUtc { get; set; }
        public List<DiagnosticsMappingEntry> DiagnosticsMapping { get; set; } = new();
        public List<ProjectReportEntry> ProjectReport { get; set; } = new();
        public List<ProjectTestEntry> ProjectTest { get; set; } = new();
        public ApexDiscoveryPreloadCache ApexDiscoveryPreload { get; set; } = new();

        public static ProjectDataCacheFile FromResult(string apexPath, ProjectDataExtractionResult result)
        {
            return new ProjectDataCacheFile
            {
                ApexPath = apexPath,
                ApexLastWriteUtc = File.GetLastWriteTimeUtc(apexPath),
                DiagnosticsMapping = new List<DiagnosticsMappingEntry>(result.DiagnosticsMapping),
                ProjectReport = new List<ProjectReportEntry>(result.ProjectReport),
                ProjectTest = new List<ProjectTestEntry>(result.ProjectTest),
                ApexDiscoveryPreload = ApexDiscoveryPreloadCache.FromResult(result.ApexDiscoveryPreload)
            };
        }

        public ProjectDataExtractionResult ToResult()
        {
            var result = new ProjectDataExtractionResult();
            result.DiagnosticsMapping.AddRange(DiagnosticsMapping);
            result.ProjectReport.AddRange(ProjectReport);
            result.ProjectTest.AddRange(ProjectTest);
            result.ApexDiscoveryPreload = ApexDiscoveryPreload.ToResult();
            return result;
        }
    }

    private sealed class ApexDiscoveryPreloadCache
    {
        public Dictionary<string, string> PageIndexMap { get; set; } = new();
        public Dictionary<string, SysVarRefEntry> SysVarRefMap { get; set; } = new();
        public Dictionary<int, DriverConfigEntry> DriverConfigMap { get; set; } = new();
        public List<PageMappingEntry> PageMappings { get; set; } = new();
        public List<RelayPortEntry> RelayPorts { get; set; } = new();
        public List<MpioIrPortEntry> MpioIrPorts { get; set; } = new();
        public List<SensePortEntry> SensePorts { get; set; } = new();
        public List<TriggerPortEntry> TriggerPorts { get; set; } = new();
        public List<Rs232PortEntry> Rs232Ports { get; set; } = new();
        public List<RoomMappingEntry> RoomMappings { get; set; } = new();
        public List<DriverTemplateVariableEntry> DriverTemplateVariables { get; set; } = new();

        public static ApexDiscoveryPreloadCache FromResult(ApexDiscoveryPreloadResult result)
        {
            return new ApexDiscoveryPreloadCache
            {
                PageIndexMap = new Dictionary<string, string>(result.PageIndexMap, StringComparer.Ordinal),
                SysVarRefMap = new Dictionary<string, SysVarRefEntry>(result.SysVarRefMap, StringComparer.Ordinal),
                DriverConfigMap = new Dictionary<int, DriverConfigEntry>(result.DriverConfigMap),
                PageMappings = new List<PageMappingEntry>(result.PageMappings),
                RelayPorts = new List<RelayPortEntry>(result.RelayPorts),
                MpioIrPorts = new List<MpioIrPortEntry>(result.MpioIrPorts),
                SensePorts = new List<SensePortEntry>(result.SensePorts),
                TriggerPorts = new List<TriggerPortEntry>(result.TriggerPorts),
                Rs232Ports = new List<Rs232PortEntry>(result.Rs232Ports),
                RoomMappings = new List<RoomMappingEntry>(result.RoomMappings),
                DriverTemplateVariables = new List<DriverTemplateVariableEntry>(result.DriverTemplateVariables)
            };
        }

        public ApexDiscoveryPreloadResult ToResult()
        {
            var result = new ApexDiscoveryPreloadResult();
            foreach (var entry in PageIndexMap)
            {
                result.PageIndexMap[entry.Key] = entry.Value;
            }

            foreach (var entry in SysVarRefMap)
            {
                result.SysVarRefMap[entry.Key] = entry.Value;
            }

            foreach (var entry in DriverConfigMap)
            {
                result.DriverConfigMap[entry.Key] = entry.Value;
            }

            result.PageMappings.AddRange(PageMappings);
            result.RelayPorts.AddRange(RelayPorts);
            result.MpioIrPorts.AddRange(MpioIrPorts);
            result.SensePorts.AddRange(SensePorts);
            result.TriggerPorts.AddRange(TriggerPorts);
            result.Rs232Ports.AddRange(Rs232Ports);
            result.RoomMappings.AddRange(RoomMappings);
            result.DriverTemplateVariables.AddRange(DriverTemplateVariables);
            return result;
        }
    }
}
