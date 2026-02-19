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
    public Func<DateTime>? TimestampProvider { get; set; }
    public Action<string, string>? StatusSink { get; set; }
    public string? HtmlLogPath { get; set; }
}

public sealed class CentralLogger
{
    private static readonly HashSet<string> AllowedStatusLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "SUCCESS",
        "INFO",
        "WARN",
        "FAIL"
    };

    private static readonly object SessionLock = new();
    private static readonly HashSet<string> SessionHeadersWritten = new(StringComparer.OrdinalIgnoreCase);

    private readonly string _logFilePath;
    private readonly string? _htmlLogPath;
    private readonly Func<DateTime> _timestampProvider;
    private readonly Action<string, string> _statusSink;

    public CentralLogger(CentralLoggerOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _logFilePath = options.LogFilePath ?? "";
        _timestampProvider = options.TimestampProvider ?? (() => LogTimestampSource.GetTimestamp(DateTime.Now));
        _statusSink = options.StatusSink ?? ((_, _) => { });
        _htmlLogPath = string.IsNullOrWhiteSpace(options.HtmlLogPath)
            ? BuildDefaultHtmlPath()
            : options.HtmlLogPath;

        if (!string.IsNullOrWhiteSpace(_htmlLogPath))
        {
            EnsureHtmlLogFileExists(_htmlLogPath);
            AppendHtmlSessionSeparatorOnce(_htmlLogPath, _timestampProvider());
        }
    }

    public void LogEvent(LogEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        var timestamp = _timestampProvider();
        var payload = new Dictionary<string, object?>
        {
            ["timestampUtc"] = timestamp.ToString("O", CultureInfo.InvariantCulture),
            ["severity"] = entry.Severity.ToString().ToUpperInvariant(),
            ["correlationId"] = entry.CorrelationId,
            ["module"] = entry.Module,
            ["phase"] = entry.Phase,
            ["message"] = entry.Message
        };

        if (entry.Details is not null && entry.Details.Count > 0)
        {
            payload["details"] = entry.Details;
        }

        if (entry.Exception is not null)
        {
            payload["exception"] = entry.Exception.ToString();
        }

        try
        {
            WriteHtmlLine(payload);
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

    public void LogPlainLine(string line)
    {
        if (string.IsNullOrWhiteSpace(_htmlLogPath) || string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        try
        {
            EnsureHtmlLogFileExists(_htmlLogPath);
            ExecuteFileIoWithRetry(() =>
            {
                var html = File.ReadAllText(_htmlLogPath);
                var marker = "</pre>";
                var index = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    return;
                }

                html = html.Insert(index, EscapeHtml(line) + Environment.NewLine);
                File.WriteAllText(_htmlLogPath, html);
            });
        }
        catch (Exception ex) when (IsNonFatalFileException(ex))
        {
            // Best-effort logging only; never fail caller workflows due to log file locks.
        }
    }

    private void WriteHtmlLine(Dictionary<string, object?> payload)
    {
        if (string.IsNullOrWhiteSpace(_htmlLogPath))
        {
            return;
        }

        EnsureHtmlLogFileExists(_htmlLogPath);
        var severity = payload.TryGetValue("severity", out var severityValue)
            ? severityValue?.ToString() ?? "INFO"
            : "INFO";
        var timestamp = payload.TryGetValue("timestampUtc", out var timestampValue)
            ? timestampValue?.ToString() ?? ""
            : "";
        var module = payload.TryGetValue("module", out var moduleValue)
            ? moduleValue?.ToString() ?? ""
            : "";
        var phase = payload.TryGetValue("phase", out var phaseValue)
            ? phaseValue?.ToString() ?? ""
            : "";
        var message = payload.TryGetValue("message", out var messageValue)
            ? messageValue?.ToString() ?? ""
            : "";
        var details = payload.TryGetValue("details", out var detailValue) ? detailValue : null;
        var exception = payload.TryGetValue("exception", out var exceptionValue)
            ? exceptionValue?.ToString() ?? ""
            : "";

        var detailText = details is IReadOnlyDictionary<string, string> dict
            ? string.Join(";", dict.Select(pair => $"{pair.Key}={pair.Value}"))
            : "";

        var severityColor = severity switch
        {
            "SUCCESS" => "#2f7d32",
            "ERROR" => "#b00020",
            "WARN" => "#b26a00",
            "INFO" => "#000000",
            "DEBUG" => "#555555",
            _ => "#222222"
        };

        var safeTimestamp = EscapeHtml(timestamp);
        var safeSeverity = EscapeHtml(severity);
        var safeMessage = $"<strong>{EscapeHtml(message)}</strong>";
        var safeModulePhase = $"{EscapeHtml(module)}/{EscapeHtml(phase)}";
        var severityTag = $"<span style=\"color:{severityColor}\">[{safeSeverity}]</span>";
        var line = $"{safeTimestamp} {severityTag} {safeMessage} {safeModulePhase}";
        if (!string.IsNullOrWhiteSpace(detailText))
        {
            line += $" | details={EscapeHtml(detailText)}";
        }
        if (!string.IsNullOrWhiteSpace(exception))
        {
            line += $" | exception={EscapeHtml(exception)}";
        }

        ExecuteFileIoWithRetry(() =>
        {
            var html = File.ReadAllText(_htmlLogPath);
            var marker = "</pre>";
            var index = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return;
            }

            html = html.Insert(index, line + Environment.NewLine);
            File.WriteAllText(_htmlLogPath, html);
        });
    }

    private static void EnsureHtmlLogFileExists(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(path))
        {
            var html = "<!doctype html><html><head><meta charset=\"utf-8\"><title>Oracle Structured Log</title></head><body><pre>" +
                       "</pre></body></html>";
            File.WriteAllText(path, html);
        }
    }

    private static void AppendHtmlSessionSeparatorOnce(string path, DateTime timestamp)
    {
        lock (SessionLock)
        {
            if (!SessionHeadersWritten.Add(path))
            {
                return;
            }
        }

        var html = File.ReadAllText(path);
        var marker = "</pre>";
        var index = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return;
        }

        var stamp = EscapeHtml(timestamp.ToString("O", CultureInfo.InvariantCulture));
        var line = $"----- SESSION START {stamp} -----{Environment.NewLine}";
        html = html.Insert(index, line);
        File.WriteAllText(path, html);
    }

    private static string EscapeHtml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }

    private static string BuildDefaultHtmlPath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Oracle by FP&C",
            "Logs");
        return Path.Combine(folder, "oracle-log.html");
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
