using OracleByFPCLtd.DriverProfiles.Matching;

namespace OracleByFPCLtd.DriverProfiles.Catalog;

public static class DriverProfileRegistryFactory
{
    public static DriverProfileRegistry CreateDefault()
    {
        return new DriverProfileRegistry(DriverProfileCatalog.All());
    }
}
