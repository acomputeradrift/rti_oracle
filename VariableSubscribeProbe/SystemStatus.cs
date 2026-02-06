using System.Text.Json;

namespace VariableSubscribeProbe;

public sealed class SystemStatus
{
    public SystemStatus(string memoryFree, int memoryLoad, string sysvarLoad, long uptime, List<SystemStatusEntry> history)
    {
        MemoryFree = memoryFree;
        MemoryLoad = memoryLoad;
        SysvarLoad = sysvarLoad;
        Uptime = uptime;
        MemoryHistory = history;
    }

    public string MemoryFree { get; }
    public int MemoryLoad { get; }
    public string SysvarLoad { get; }
    public long Uptime { get; }
    public List<SystemStatusEntry> MemoryHistory { get; }

    public static SystemStatus Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new SystemStatus(string.Empty, 0, string.Empty, 0, new List<SystemStatusEntry>());
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var memoryFree = GetString(root, "memory_free");
        var memoryLoad = GetInt(root, "memory_load");
        var sysvarLoad = GetString(root, "sysvar_load");
        var uptime = GetLong(root, "uptime");

        var history = new List<SystemStatusEntry>();
        if (root.TryGetProperty("memory_history", out var historyElement)
            && historyElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var entryElement in historyElement.EnumerateArray())
            {
                var entryFree = GetString(entryElement, "memory_free");
                var entryLoad = GetInt(entryElement, "memory_load");
                var timestamp = GetLong(entryElement, "timestamp");
                history.Add(new SystemStatusEntry(entryFree, entryLoad, timestamp));
            }
        }

        return new SystemStatus(memoryFree, memoryLoad, sysvarLoad, uptime, history);
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        if (element.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Number)
        {
            return value.ToString();
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

    private static long GetLong(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var longValue))
            {
                return longValue;
            }

            if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out var strValue))
            {
                return strValue;
            }
        }

        return 0;
    }
}

public sealed record SystemStatusEntry(string MemoryFree, int MemoryLoad, long Timestamp);
