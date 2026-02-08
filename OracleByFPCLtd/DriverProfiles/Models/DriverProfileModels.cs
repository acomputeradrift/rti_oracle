using System.Collections.Generic;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.DriverProfiles.Models;

public sealed record DriverProfileDiscoveryRule(string Description, string Sql);
public sealed record DriverProfileAnalysisRule(string Description, string ExampleInput, string ExampleOutput);
public sealed record AdditionalInfoSchema(IReadOnlyList<AdditionalInfoColumn> Columns);
public sealed record AdditionalInfoColumn(string Header, AdditionalInfoColumnRole Role);
public enum AdditionalInfoColumnRole
{
    InputIndex,
    InputName,
    OutputIndex,
    OutputName
}

public interface IDriverProfileMapper
{
    bool TryMap(string rawText, ProjectDataBundle bundle, out string mappedText, out bool unresolved);
}

public sealed record DriverProfileDefinition(
    string DeviceName,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> DiscoveryKeys,
    IReadOnlyList<string> DiscoveryPrefixes,
    IReadOnlyList<DriverProfileDiscoveryRule> DiscoveryRules,
    IReadOnlyList<DriverProfileAnalysisRule> AnalysisRules,
    IReadOnlyList<string> Notes,
    AdditionalInfoSchema? AdditionalInfoSchema = null,
    IDriverProfileMapper? Mapper = null);

public sealed record DriverProfileMatch(int DriverDeviceId, DriverConfigEntry DriverConfig, DriverProfileDefinition Profile);

public sealed record DriverProfileBundle(IReadOnlyList<DriverProfileMatch> Matches);
