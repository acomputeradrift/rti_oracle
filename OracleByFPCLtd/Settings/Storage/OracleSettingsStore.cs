using System;
using System.IO;
using System.Text.Json;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.Reliability;
using OracleByFPCLtd.Settings.Models;

namespace OracleByFPCLtd.Settings.Storage;

public sealed class OracleSettingsStore
{
    private readonly string _settingsPath;
    private static readonly CentralLogger CentralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public OracleSettingsStore(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? GetDefaultPath();
    }

    public OracleSettings Load(Action<OperationFailure>? onFailure = null)
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
        catch (Exception ex)
        {
            LogStructuredEvent(
                SeverityLevel.Warn,
                "OracleSettingsStore",
                "Load",
                "Settings load failed; defaults used.",
                new Dictionary<string, string> { ["path"] = _settingsPath },
                ex);
            onFailure?.Invoke(new OperationFailure(
                FailureCodes.SettingsLoadFallback,
                $"Failed to load settings, using defaults: {ex.Message}",
                _settingsPath,
                DateTime.UtcNow));
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

    private static string GetDefaultPath()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(folder, "Oracle by FP&C", "settings.json");
    }

    private static void LogStructuredEvent(
        SeverityLevel severity,
        string module,
        string phase,
        string message,
        IReadOnlyDictionary<string, string>? details = null,
        Exception? exception = null)
    {
        var correlationId = CreateCorrelationId();
        CentralLogger.LogEvent(new LogEntry(
            severity,
            correlationId,
            module,
            phase,
            message,
            details,
            exception));
    }

    private static string CreateCorrelationId()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 6);
    }

    private static string BuildStructuredLogPath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Oracle by FP&C",
            "Logs");
        return Path.Combine(folder, "oracle-structured.log");
    }
}
