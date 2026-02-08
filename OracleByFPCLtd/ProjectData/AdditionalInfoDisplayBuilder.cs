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

            foreach (var entry in driver.Value.CbusGroups.OrderBy(entry => entry.Key.GroupId))
            {
                var name = FormatCbusGroup(entry.Value);
                yield return new AdditionalInfoDisplayEntry(driver.Key, "C-Bus Group", entry.Key.GroupId, name);
            }

            foreach (var entry in driver.Value.CbusHvacZones.OrderBy(entry => entry.Key.ZoneId))
            {
                var name = FormatCbusHvac(entry.Value);
                yield return new AdditionalInfoDisplayEntry(driver.Key, "C-Bus HVAC", entry.Key.ZoneId, name);
            }

            foreach (var entry in driver.Value.CbusScenes.OrderBy(entry => entry.Key.GroupId))
            {
                var name = entry.Value.SceneName;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    yield return new AdditionalInfoDisplayEntry(driver.Key, "C-Bus Scene", entry.Key.GroupId, name);
                }
            }
        }
    }

    private static string FormatCbusGroup(CbusGroupEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.GroupRoom))
        {
            return entry.GroupName;
        }

        if (string.IsNullOrWhiteSpace(entry.GroupName))
        {
            return entry.GroupRoom;
        }

        return $"{entry.GroupRoom} {entry.GroupName}";
    }

    private static string FormatCbusHvac(CbusHvacEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.GroupName))
        {
            return entry.ZoneName;
        }

        if (string.IsNullOrWhiteSpace(entry.ZoneName))
        {
            return entry.GroupName;
        }

        return $"{entry.GroupName} {entry.ZoneName}";
    }
}

public sealed record AdditionalInfoDisplayEntry(string DriverName, string MapType, int Index, string Name);
