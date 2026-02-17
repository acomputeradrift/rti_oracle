using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using OracleByFPCLtd.Logging;

namespace OracleByFPCLtd;

public sealed class WebSocketMessageFormatter
{
    private static readonly string[] TimeFormats =
    {
        "hh\\:mm\\:ss",
        "h\\:mm\\:ss",
        "hh\\:mm\\:ss\\.f",
        "h\\:mm\\:ss\\.f",
        "hh\\:mm\\:ss\\.ff",
        "h\\:mm\\:ss\\.ff",
        "hh\\:mm\\:ss\\.fff",
        "h\\:mm\\:ss\\.fff",
        "hh\\:mm\\:ss\\.ffff",
        "h\\:mm\\:ss\\.ffff",
        "hh\\:mm\\:ss\\.fffff",
        "h\\:mm\\:ss\\.fffff",
        "hh\\:mm\\:ss\\.ffffff",
        "h\\:mm\\:ss\\.ffffff",
        "hh\\:mm\\:ss\\.fffffff",
        "h\\:mm\\:ss\\.fffffff"
    };

    private DateOnly _currentDate;
    private TimeSpan? _lastMessageLogTime;
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public WebSocketMessageFormatter(DateOnly? startDate = null)
    {
        _currentDate = startDate ?? DateOnly.FromDateTime(DateTime.Today);
    }

    public void Reset(DateOnly? startDate = null)
    {
        _currentDate = startDate ?? DateOnly.FromDateTime(DateTime.Today);
        _lastMessageLogTime = null;
    }

    public string Format(string raw, out bool isLogLine)
    {
        isLogLine = false;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (root.TryGetProperty("messageType", out var messageTypeElement))
            {
                var messageType = messageTypeElement.GetString() ?? "Unknown";
                if (string.Equals(messageType, "echo", StringComparison.OrdinalIgnoreCase))
                {
                    var msg = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() ?? "" : "";
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        try
                        {
                            using var inner = JsonDocument.Parse(msg);
                            var innerRoot = inner.RootElement;
                            if (innerRoot.TryGetProperty("type", out var t) && innerRoot.TryGetProperty("resource", out var r))
                            {
                                var type = t.GetString();
                                var res = r.GetString();
                                return $"Echo {type}/{res}";
                            }
                        }
                        catch (JsonException)
                        {
                            LogStructuredEvent(
                                SeverityLevel.Warn,
                                "Format",
                                "WebSocket echo payload parse failed.",
                                new Dictionary<string, string> { ["payload"] = msg });
                        }
                        catch (FormatException)
                        {
                            LogStructuredEvent(
                                SeverityLevel.Warn,
                                "Format",
                                "WebSocket echo payload format failed.",
                                new Dictionary<string, string> { ["payload"] = msg });
                        }
                        return $"Echo {msg}";
                    }
                    return "Echo";
                }

                if (string.Equals(messageType, "MessageLog", StringComparison.OrdinalIgnoreCase))
                {
                    var time = root.TryGetProperty("time", out var timeElement) ? timeElement.GetString() : "";
                    var text = root.TryGetProperty("text", out var textElement) ? textElement.GetString() : "";
                    isLogLine = true;
                    return FormatMessageLog(time, text);
                }

                if (string.Equals(messageType, "Sysvar", StringComparison.OrdinalIgnoreCase))
                {
                    var id = root.TryGetProperty("sysvarid", out var idElement) ? idElement.ToString() : "?";
                    var val = root.TryGetProperty("sysvarval", out var valElement) ? valElement.ToString() : "?";
                    return $"Sysvar id={id} val={val}";
                }

                return $"{messageType} {raw}";
            }

            if (root.TryGetProperty("type", out var typeElement) && root.TryGetProperty("resource", out var resElement))
            {
                var type = typeElement.GetString();
                var resource = resElement.GetString();
                return $"{type}/{resource} {raw}";
            }
        }
        catch (JsonException)
        {
            LogStructuredEvent(
                SeverityLevel.Warn,
                "Format",
                "WebSocket JSON parse failed.",
                new Dictionary<string, string> { ["payload"] = raw });
        }
        catch (FormatException)
        {
            LogStructuredEvent(
                SeverityLevel.Warn,
                "Format",
                "WebSocket payload format failed.",
                new Dictionary<string, string> { ["payload"] = raw });
        }

        return raw;
    }

    private string FormatMessageLog(string? timeText, string? text)
    {
        var timeValue = timeText ?? "";
        if (TryParseTime(timeValue, out var timeOfDay))
        {
            if (_lastMessageLogTime.HasValue && timeOfDay < _lastMessageLogTime.Value)
            {
                var delta = _lastMessageLogTime.Value - timeOfDay;
                if (delta > TimeSpan.FromHours(12))
                {
                    _currentDate = _currentDate.AddDays(1);
                }
            }

            _lastMessageLogTime = timeOfDay;
            var timestamp = $"[{_currentDate:yyyy-MM-dd} {timeOfDay:hh\\:mm\\:ss\\.fff}]";
            return string.IsNullOrWhiteSpace(text) ? timestamp : $"{timestamp} {text}";
        }

        var fallback = $"[{_currentDate:yyyy-MM-dd} {timeValue}]".Trim();
        return string.IsNullOrWhiteSpace(text) ? fallback : $"{fallback} {text}";
    }

    private static bool TryParseTime(string timeText, out TimeSpan timeOfDay)
    {
        return TimeSpan.TryParseExact(
            timeText,
            TimeFormats,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.TimeSpanStyles.None,
            out timeOfDay);
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
            "WebSocketMessageFormatter",
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
