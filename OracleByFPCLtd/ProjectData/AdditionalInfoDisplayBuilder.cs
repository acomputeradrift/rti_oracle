using System.Collections.Generic;
using System.Linq;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProjectData;

public static class AdditionalInfoDisplayBuilder
{
    public static IEnumerable<AdditionalInfoDisplayEntry> Build(AdditionalData data)
    {
        if (data is null)
        {
            yield break;
        }

        foreach (var driver in data.Drivers.OrderBy(entry => entry.Key))
        {
            foreach (var entry in driver.Value.InputNames.OrderBy(entry => entry.Key))
            {
                yield return new AdditionalInfoDisplayEntry(driver.Key, "Input", entry.Key, entry.Value);
            }

            foreach (var entry in driver.Value.OutputNames.OrderBy(entry => entry.Key))
            {
                yield return new AdditionalInfoDisplayEntry(driver.Key, "Output", entry.Key, entry.Value);
            }
        }
    }
}

public sealed record AdditionalInfoDisplayEntry(string DriverName, string MapType, int Index, string Name);
