using System.Collections.Generic;
using OracleByFPCLtd.ProjectData;

namespace OracleByFPCLtd.DriverProfiles.Models;

public sealed record DriverProfileDiscoveryRule(string Description, string Sql);
public sealed record DriverProfileAnalysisRule(string Description, string ExampleInput, string ExampleOutput);

public sealed record DriverProfileDefinition(
    string DeviceName,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string> DiscoveryKeys,
    IReadOnlyList<string> DiscoveryPrefixes,
    IReadOnlyList<DriverProfileDiscoveryRule> DiscoveryRules,
    IReadOnlyList<DriverProfileAnalysisRule> AnalysisRules,
    IReadOnlyList<string> Notes);

public sealed record DriverProfileMatch(int DriverDeviceId, DriverConfigEntry DriverConfig, DriverProfileDefinition Profile);

public sealed record DriverProfileBundle(IReadOnlyList<DriverProfileMatch> Matches);
