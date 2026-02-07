using System.Collections.Generic;
using System.Linq;
using OracleByFPCLtd.DriverProfiles.Models;

namespace OracleByFPCLtd.DriverProfiles.Matching;

public sealed class DriverProfileRegistry
{
    private readonly IReadOnlyList<DriverProfileDefinition> _profiles;

    public DriverProfileRegistry(IEnumerable<DriverProfileDefinition> profiles)
    {
        _profiles = profiles?.ToList() ?? new List<DriverProfileDefinition>();
    }

    public IReadOnlyList<DriverProfileDefinition> Profiles => _profiles;
}
