using System;
using System.Collections.Generic;
using System.Linq;
using OracleByFPCLtd.DriverProfiles.Catalog;
using OracleByFPCLtd.DriverProfiles.Matching;
using OracleByFPCLtd.DriverProfiles.Models;

namespace OracleByFPCLtd.ProjectData;

public static class AdditionalInfoTemplatePlanner
{
    public static IReadOnlyList<AdditionalInfoSheetSchema> DetermineSchemas(IEnumerable<DriverConfigEntry> drivers)
    {
        if (drivers is null)
        {
            return Array.Empty<AdditionalInfoSheetSchema>();
        }

        var registry = new DriverProfileRegistry(DriverProfileCatalog.All());
        var matcher = new DriverProfileMatcher();
        var schemas = new List<AdditionalInfoSheetSchema>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var driver in drivers)
        {
            if (driver == null)
            {
                continue;
            }

            var profile = matcher.Find(driver.DeviceName, registry)
                ?? matcher.Find(driver.DeviceDisplayName, registry);
            if (profile?.AdditionalInfoSchemas == null || profile.AdditionalInfoSchemas.Count == 0)
            {
                continue;
            }

            foreach (var schema in profile.AdditionalInfoSchemas)
            {
                if (schema == null)
                {
                    continue;
                }

                if (seen.Add(schema.SheetName))
                {
                    schemas.Add(schema);
                }
            }
        }

        return schemas;
    }
}
