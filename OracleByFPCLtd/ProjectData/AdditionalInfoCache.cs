using System;
using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.Logging;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd.ProjectData;

public sealed record AdditionalInfoCacheKey(
    string ProjectPath,
    DateTime ProjectLastWriteUtc,
    string? AdditionalInfoPath,
    DateTime? AdditionalInfoLastWriteUtc);

public sealed class AdditionalInfoCache
{
    private AdditionalInfoCacheKey? _key;
    private AdditionalData? _data;
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public AdditionalData GetOrLoad(AdditionalInfoCacheKey key, Func<AdditionalData> loader)
    {
        if (loader is null)
        {
            LogStructuredEvent(
                SeverityLevel.Error,
                "GetOrLoad",
                "Additional info loader is null.",
                new Dictionary<string, string> { ["error"] = "ArgumentNullException" });
            throw new ArgumentNullException(nameof(loader));
        }

        if (_key is not null && _data is not null && _key.Equals(key))
        {
            LogStructuredEvent(
                SeverityLevel.Info,
                "GetOrLoad",
                "Additional info cache hit.",
                new Dictionary<string, string> { ["projectPath"] = key.ProjectPath });
            return _data;
        }

        var data = loader();
        _key = key;
        _data = data;
        LogStructuredEvent(
            SeverityLevel.Info,
            "GetOrLoad",
            "Additional info cache refreshed.",
            new Dictionary<string, string>
            {
                ["projectPath"] = key.ProjectPath,
                ["additionalInfoPath"] = key.AdditionalInfoPath ?? ""
            });
        return data;
    }

    private void LogStructuredEvent(
        SeverityLevel severity,
        string phase,
        string message,
        IReadOnlyDictionary<string, string>? details = null)
    {
        _centralLogger.LogEvent(new LogEntry(
            severity,
            CreateCorrelationId(),
            "AdditionalInfoCache",
            phase,
            message,
            details));
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
