using System;
using System.Collections.Generic;

namespace OracleByFPCLtd.ProjectData.Models;

public sealed class ProjectDataBundle
{
    public SystemData System { get; init; } = new();
    public DriverData Drivers { get; init; } = new();
    public AdditionalData Additional { get; init; } = new();

    public static ProjectDataBundle FromExtractionResult(ProjectDataExtractionResult result)
    {
        return new ProjectDataBundle
        {
            System = SystemData.FromExtractionResult(result),
            Drivers = DriverData.FromExtractionResult(result),
            Additional = AdditionalData.FromExtractionResult(result)
        };
    }

    public ProjectDataExtractionResult ToExtractionResult()
    {
        var result = new ProjectDataExtractionResult();
        System.ApplyTo(result);
        Drivers.ApplyTo(result);
        Additional.ApplyTo(result);
        return result;
    }
}

public sealed class SystemData
{
    public List<DiagnosticsMappingEntry> DiagnosticsMapping { get; } = new();
    public List<ProjectReportEntry> ProjectReport { get; } = new();
    public List<ProjectTestEntry> ProjectTest { get; } = new();
    public Dictionary<string, string> PageIndexMap { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, SysVarRefEntry> SysVarRefMap { get; } = new(StringComparer.Ordinal);
    public List<PageMappingEntry> PageMappings { get; } = new();
    public List<RelayPortEntry> RelayPorts { get; } = new();
    public List<MpioIrPortEntry> MpioIrPorts { get; } = new();
    public List<SensePortEntry> SensePorts { get; } = new();
    public List<TriggerPortEntry> TriggerPorts { get; } = new();
    public List<Rs232PortEntry> Rs232Ports { get; } = new();
    public List<RoomMappingEntry> RoomMappings { get; } = new();

    public static SystemData FromExtractionResult(ProjectDataExtractionResult result)
    {
        var data = new SystemData();
        data.DiagnosticsMapping.AddRange(result.DiagnosticsMapping);
        data.ProjectReport.AddRange(result.ProjectReport);
        data.ProjectTest.AddRange(result.ProjectTest);
        foreach (var entry in result.ApexDiscoveryPreload.PageIndexMap)
        {
            data.PageIndexMap[entry.Key] = entry.Value;
        }
        foreach (var entry in result.ApexDiscoveryPreload.SysVarRefMap)
        {
            data.SysVarRefMap[entry.Key] = entry.Value;
        }
        data.PageMappings.AddRange(result.ApexDiscoveryPreload.PageMappings);
        data.RelayPorts.AddRange(result.ApexDiscoveryPreload.RelayPorts);
        data.MpioIrPorts.AddRange(result.ApexDiscoveryPreload.MpioIrPorts);
        data.SensePorts.AddRange(result.ApexDiscoveryPreload.SensePorts);
        data.TriggerPorts.AddRange(result.ApexDiscoveryPreload.TriggerPorts);
        data.Rs232Ports.AddRange(result.ApexDiscoveryPreload.Rs232Ports);
        data.RoomMappings.AddRange(result.ApexDiscoveryPreload.RoomMappings);
        return data;
    }

    public void ApplyTo(ProjectDataExtractionResult result)
    {
        result.DiagnosticsMapping.AddRange(DiagnosticsMapping);
        result.ProjectReport.AddRange(ProjectReport);
        result.ProjectTest.AddRange(ProjectTest);
        foreach (var entry in PageIndexMap)
        {
            result.ApexDiscoveryPreload.PageIndexMap[entry.Key] = entry.Value;
        }
        foreach (var entry in SysVarRefMap)
        {
            result.ApexDiscoveryPreload.SysVarRefMap[entry.Key] = entry.Value;
        }
        result.ApexDiscoveryPreload.PageMappings.AddRange(PageMappings);
        result.ApexDiscoveryPreload.RelayPorts.AddRange(RelayPorts);
        result.ApexDiscoveryPreload.MpioIrPorts.AddRange(MpioIrPorts);
        result.ApexDiscoveryPreload.SensePorts.AddRange(SensePorts);
        result.ApexDiscoveryPreload.TriggerPorts.AddRange(TriggerPorts);
        result.ApexDiscoveryPreload.Rs232Ports.AddRange(Rs232Ports);
        result.ApexDiscoveryPreload.RoomMappings.AddRange(RoomMappings);
    }
}

public sealed class DriverData
{
    public Dictionary<int, DriverConfigEntry> DriverConfigMap { get; } = new();
    public List<DriverTemplateVariableEntry> DriverTemplateVariables { get; } = new();

    public static DriverData FromExtractionResult(ProjectDataExtractionResult result)
    {
        var data = new DriverData();
        foreach (var entry in result.ApexDiscoveryPreload.DriverConfigMap)
        {
            data.DriverConfigMap[entry.Key] = entry.Value;
        }
        data.DriverTemplateVariables.AddRange(result.ApexDiscoveryPreload.DriverTemplateVariables);
        return data;
    }

    public void ApplyTo(ProjectDataExtractionResult result)
    {
        foreach (var entry in DriverConfigMap)
        {
            result.ApexDiscoveryPreload.DriverConfigMap[entry.Key] = entry.Value;
        }
        result.ApexDiscoveryPreload.DriverTemplateVariables.AddRange(DriverTemplateVariables);
    }
}

public sealed class AdditionalData
{
    public List<string> Errors { get; } = new();
    public List<string> MatchedDriverNames { get; } = new();
    public Dictionary<string, AdditionalDriverData> Drivers { get; } = new(StringComparer.Ordinal);

    public static AdditionalData FromExtractionResult(ProjectDataExtractionResult result)
    {
        return new AdditionalData();
    }

    public void ApplyTo(ProjectDataExtractionResult result)
    {
    }
}

public sealed class AdditionalDriverData
{
    public Dictionary<int, string> InputNames { get; } = new();
    public Dictionary<int, string> OutputNames { get; } = new();
    public Dictionary<(int AppId, int GroupId), CbusGroupEntry> CbusGroups { get; } = new();
    public Dictionary<(int GroupId, int ZoneId), CbusHvacEntry> CbusHvacZones { get; } = new();
    public Dictionary<(int AppId, int GroupId, int ActionSelector), CbusSceneEntry> CbusScenes { get; } = new();
}

public sealed record CbusGroupEntry(string GroupRoom, string GroupName);
public sealed record CbusHvacEntry(string GroupName, string ZoneName);
public sealed record CbusSceneEntry(string SceneName);
