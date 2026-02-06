using System.Text.Json;

namespace VariableSubscribeProbe;

public sealed class SysvarCatalog
{
    public SysvarCatalog(List<SysvarDriver> drivers)
    {
        Drivers = drivers;
    }

    public List<SysvarDriver> Drivers { get; }

    public static SysvarCatalog ParseFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new SysvarCatalog(new List<SysvarDriver>());
        }

        var normalized = TrimToJson(json);
        using var doc = JsonDocument.Parse(normalized);
        var root = doc.RootElement;

        var drivers = new List<SysvarDriver>();
        if (root.TryGetProperty("Drivers", out var driversElement) && driversElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var driverElement in driversElement.EnumerateArray())
            {
                var driverName = GetString(driverElement, "Driver Name");
                var driverBaseName = GetString(driverElement, "Driver Base Name");
                var driverId = GetInt(driverElement, "Driver ID");

                var variables = new List<SysvarVariable>();
                if (driverElement.TryGetProperty("Driver Variables", out var varsElement)
                    && varsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var varElement in varsElement.EnumerateArray())
                    {
                        var name = GetString(varElement, "Name");
                        var id = GetInt(varElement, "ID");
                        var type = GetInt(varElement, "Type");
                        variables.Add(new SysvarVariable(name, id, type));
                    }
                }

                drivers.Add(new SysvarDriver(driverName, driverBaseName, driverId, variables));
            }
        }

        return new SysvarCatalog(drivers);
    }

    private static string TrimToJson(string input)
    {
        var index = input.IndexOf('{');
        return index >= 0 ? input[index..] : input;
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
            {
                return intValue;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var strValue))
            {
                return strValue;
            }
        }

        return 0;
    }
}

public sealed record SysvarDriver(string DriverName, string DriverBaseName, int DriverId, List<SysvarVariable> Variables);

public sealed record SysvarVariable(string Name, int Id, int Type);
