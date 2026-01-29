using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SHPDiagnosticsViewer.Settings;

public sealed class OracleSettings
{
    public List<string> RecentIps { get; set; } = new();
    public List<RecentProjectEntry> RecentProjects { get; set; } = new();
    public List<string> RecentAdditionalInfo { get; set; } = new();
}

public sealed class RecentProjectEntry
{
    public string FilePath { get; set; } = "";
    public string? LastSuccessfulIp { get; set; }
    public DateTime? LastConnectedAt { get; set; }

    [JsonIgnore]
    public string FileName => Path.GetFileName(FilePath);
}

public sealed class OracleSettingsStore
{
    private const int MaxRecentItems = 5;
    private readonly string _settingsPath;

    public OracleSettingsStore(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? GetDefaultPath();
    }

    public OracleSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new OracleSettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<OracleSettings>(json) ?? new OracleSettings();
        }
        catch
        {
            return new OracleSettings();
        }
    }

    public void Save(OracleSettings settings)
    {
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }

    public void RecordProjectSelection(OracleSettings settings, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var fileName = Path.GetFileName(filePath);
        var existing = settings.RecentProjects.FirstOrDefault(entry =>
            string.Equals(entry.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        var lastIp = existing?.LastSuccessfulIp;
        var lastConnected = existing?.LastConnectedAt;

        settings.RecentProjects.RemoveAll(entry =>
            string.Equals(entry.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        settings.RecentProjects.Insert(0, new RecentProjectEntry
        {
            FilePath = filePath,
            LastSuccessfulIp = lastIp,
            LastConnectedAt = lastConnected
        });

        Trim(settings.RecentProjects);
    }

    public void RecordSuccessfulConnection(OracleSettings settings, string filePath, string ip)
    {
        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(ip))
        {
            return;
        }

        RecordProjectSelection(settings, filePath);
        settings.RecentProjects[0].LastSuccessfulIp = ip;
        settings.RecentProjects[0].LastConnectedAt = DateTime.Now;

        RecordRecentIp(settings, ip);
    }

    public void RecordRecentIp(OracleSettings settings, string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return;
        }

        settings.RecentIps.Insert(0, ip);
        if (settings.RecentIps.Count > MaxRecentItems)
        {
            settings.RecentIps.RemoveRange(MaxRecentItems, settings.RecentIps.Count - MaxRecentItems);
        }
    }

    public void RecordAdditionalInfo(OracleSettings settings, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var fileName = Path.GetFileName(filePath);
        settings.RecentAdditionalInfo.RemoveAll(entry =>
            string.Equals(Path.GetFileName(entry), fileName, StringComparison.OrdinalIgnoreCase));
        settings.RecentAdditionalInfo.Insert(0, filePath);
        if (settings.RecentAdditionalInfo.Count > MaxRecentItems)
        {
            settings.RecentAdditionalInfo.RemoveRange(MaxRecentItems, settings.RecentAdditionalInfo.Count - MaxRecentItems);
        }
    }

    private static void Trim(List<RecentProjectEntry> items)
    {
        if (items.Count > MaxRecentItems)
        {
            items.RemoveRange(MaxRecentItems, items.Count - MaxRecentItems);
        }
    }

    private static string GetDefaultPath()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(folder, "RTI Oracle", "settings.json");
    }
}
