using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace OracleByFPCLtd.Logging;

public enum SeverityLevel
{
    Debug,
    Info,
    Success,
    Warn,
    Error
}

public sealed record LogEntry(
    SeverityLevel Severity,
    string CorrelationId,
    string Module,
    string Phase,
    string Message,
    IReadOnlyDictionary<string, string>? Details = null,
    Exception? Exception = null);

public sealed class CentralLoggerOptions
{
    public string LogFilePath { get; set; } = "";
    public string? LogDirectoryPath { get; set; }
    public string? SessionLogPath { get; set; }
    public int RetainedSessionFileCount { get; set; } = 5;
    public Func<DateTime>? TimestampProvider { get; set; }
    public Action<string, string>? StatusSink { get; set; }
}

public sealed class CentralLogger
{
    private const string LogDirectoryOverrideEnvironmentVariable = "ORACLE_EVENT_LOG_DIRECTORY_OVERRIDE";
    private const string SessionFileSuffix = "_oracle_event_logs.log";
    private static readonly HashSet<string> AllowedStatusLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "SUCCESS",
        "INFO",
        "WARN",
        "FAIL"
    };

    private static readonly object SessionLock = new();
    private static readonly Dictionary<string, string> SessionLogPathsByDirectory = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, object> PathLocks = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _eventLogPath;
    private readonly Func<DateTime> _timestampProvider;
    private readonly Action<string, string> _statusSink;

    public CentralLogger(CentralLoggerOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _timestampProvider = options.TimestampProvider ?? (() => LogTimestampSource.GetTimestamp(DateTime.Now));
        _statusSink = options.StatusSink ?? ((_, _) => { });
        _eventLogPath = ResolveLogPath(options, _timestampProvider());
        EnsureLogFileExists(_eventLogPath);
        ApplyRetention(_eventLogPath, options.RetainedSessionFileCount);
    }

    public void LogEvent(LogEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        var timestamp = _timestampProvider();
        try
        {
            AppendLine(BuildStructuredLine(entry, timestamp));
        }
        catch (Exception ex) when (IsNonFatalFileException(ex))
        {
            // Best-effort logging only; never fail caller workflows due to log file locks.
        }
    }

    public void EmitStatus(string level, string message, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(level))
        {
            throw new ArgumentException("Status level is required.", nameof(level));
        }

        if (!AllowedStatusLevels.Contains(level))
        {
            throw new ArgumentException($"Unsupported status level: {level}", nameof(level));
        }

        var trimmed = (message ?? "").Trim();
        var normalizedLevel = level.Trim().ToUpperInvariant();
        _statusSink(normalizedLevel, trimmed);
    }

    private void AppendLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        ExecuteFileIoWithRetry(() =>
        {
            lock (GetPathLock(_eventLogPath))
            {
                File.AppendAllText(_eventLogPath, line + Environment.NewLine);
            }
        });
    }

    private static string BuildStructuredLine(LogEntry entry, DateTime timestamp)
    {
        var source = BuildSource(entry.Module, entry.Phase);
        var detailPairs = NormalizeDetailPairs(entry.Details);
        var quotedMessage = BuildQuotedMessage(entry.Message, detailPairs);
        var line = string.Create(
            CultureInfo.InvariantCulture,
            $"{timestamp:yyyy-MM-dd HH:mm} [{entry.Severity.ToString().ToUpperInvariant()}] {source}: \"{quotedMessage}\"");

        foreach (var pair in detailPairs)
        {
            if (string.Equals(pair.Key, "line", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(pair.Key, "driver", StringComparison.OrdinalIgnoreCase)
                && detailPairs.Any(item => string.Equals(item.Key, "profile", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            line += string.Create(
                CultureInfo.InvariantCulture,
                $" {pair.Key}=\"{SanitizeInline(pair.Value)}\"");
        }

        if (entry.Exception is not null)
        {
            line += string.Create(
                CultureInfo.InvariantCulture,
                $" exception=\"{SanitizeInline(entry.Exception.ToString())}\"");
        }

        return line;
    }

    private static List<KeyValuePair<string, string>> NormalizeDetailPairs(IReadOnlyDictionary<string, string>? details)
    {
        var pairs = new List<KeyValuePair<string, string>>();
        if (details is null)
        {
            return pairs;
        }

        foreach (var pair in details)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }

            if (pairs.Any(existing => string.Equals(existing.Key, pair.Key, StringComparison.OrdinalIgnoreCase)
                && string.Equals(existing.Value, pair.Value, StringComparison.Ordinal)))
            {
                continue;
            }

            pairs.Add(new KeyValuePair<string, string>(pair.Key, pair.Value));
        }

        return pairs;
    }

    private static string BuildQuotedMessage(string message, IReadOnlyList<KeyValuePair<string, string>> detailPairs)
    {
        var safeMessage = SanitizeInline(message);
        var lineNumber = detailPairs
            .FirstOrDefault(pair => string.Equals(pair.Key, "line", StringComparison.OrdinalIgnoreCase))
            .Value;

        return string.IsNullOrWhiteSpace(lineNumber)
            ? safeMessage
            : $"Line {SanitizeInline(lineNumber)} - {safeMessage}";
    }

    private static string BuildSource(string module, string phase)
    {
        var safeModule = string.IsNullOrWhiteSpace(module) ? "Unknown" : module.Trim();
        var safePhase = string.IsNullOrWhiteSpace(phase) ? "" : phase.Trim();
        return string.IsNullOrWhiteSpace(safePhase)
            ? safeModule
            : $"{safeModule}/{safePhase}";
    }

    private static string SanitizeInline(string value)
    {
        return (value ?? "")
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\"", "'", StringComparison.Ordinal)
            .Trim();
    }

    private static void EnsureLogFileExists(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(path))
        {
            File.WriteAllText(path, string.Empty);
        }
    }

    private static string ResolveLogPath(CentralLoggerOptions options, DateTime timestamp)
    {
        if (!string.IsNullOrWhiteSpace(options.SessionLogPath))
        {
            return options.SessionLogPath!;
        }

        var directory = ResolveLogDirectory(options);
        lock (SessionLock)
        {
            if (!SessionLogPathsByDirectory.TryGetValue(directory, out var path))
            {
                var fileName = timestamp.ToString("yyyy-MM-dd_HH-mm", CultureInfo.InvariantCulture) + SessionFileSuffix;
                path = Path.Combine(directory, fileName);
                SessionLogPathsByDirectory[directory] = path;
            }

            return path;
        }
    }

    private static string ResolveLogDirectory(CentralLoggerOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.LogDirectoryPath))
        {
            return options.LogDirectoryPath!;
        }

        if (!string.IsNullOrWhiteSpace(options.LogFilePath))
        {
            var fromPath = Path.GetDirectoryName(options.LogFilePath);
            if (!string.IsNullOrWhiteSpace(fromPath))
            {
                return fromPath;
            }
        }

        var overrideDirectory = Environment.GetEnvironmentVariable(LogDirectoryOverrideEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
        {
            return overrideDirectory;
        }

        return BuildDefaultLogDirectory();
    }

    private static void ApplyRetention(string path, int retainedSessionFileCount)
    {
        if (retainedSessionFileCount < 1)
        {
            retainedSessionFileCount = 1;
        }

        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        try
        {
            var files = Directory.GetFiles(directory, $"*{SessionFileSuffix}")
                .OrderByDescending(file => Path.GetFileName(file), StringComparer.Ordinal)
                .ToList();

            foreach (var file in files.Skip(retainedSessionFileCount))
            {
                if (string.Equals(file, path, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                File.Delete(file);
            }
        }
        catch (Exception ex) when (IsNonFatalFileException(ex))
        {
            // Best-effort retention only; never fail caller workflows due to cleanup issues.
        }
    }

    private static object GetPathLock(string path)
    {
        lock (SessionLock)
        {
            if (!PathLocks.TryGetValue(path, out var syncRoot))
            {
                syncRoot = new object();
                PathLocks[path] = syncRoot;
            }

            return syncRoot;
        }
    }

    private static string BuildDefaultLogDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Oracle by FP&C",
            "Logs");
    }

    private static bool IsNonFatalFileException(Exception ex)
    {
        return ex is IOException || ex is UnauthorizedAccessException;
    }

    private static void ExecuteFileIoWithRetry(Action action)
    {
        var delayMs = 40;
        Exception? last = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                action();
                return;
            }
            catch (Exception ex) when (attempt < 2 && IsNonFatalFileException(ex))
            {
                last = ex;
                Thread.Sleep(delayMs);
                delayMs *= 2;
            }
            catch (Exception ex)
            {
                last = ex;
                break;
            }
        }

        if (last != null)
        {
            throw last;
        }
    }
}
