using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace OracleByFPCLtd.Settings.Models;

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
