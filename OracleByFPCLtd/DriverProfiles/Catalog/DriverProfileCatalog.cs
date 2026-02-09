using System.Collections.Generic;
using OracleByFPCLtd.DriverProfiles.Models;

namespace OracleByFPCLtd.DriverProfiles.Catalog;

public static class DriverProfileCatalog
{
    public static IReadOnlyList<DriverProfileDefinition> All()
    {
        return new[]
        {
            RtiAd64Profile.Definition,
            RtiInternalProfile.Definition,
            RtiSystemVariableEventsProfile.Definition,
            VauxLattisMatrixProfile.Definition,
            ClipsalCbusProfile.Definition,
            SystemManagerProfile.Definition,
            RtiVirtualMultiroomAmpProfile.Definition,
            ActivitiesProfile.Definition
        };
    }

    public static IReadOnlyList<DriverProfileDefinition> Internal()
    {
        return new[] { RtiInternalProfile.Definition };
    }
}
