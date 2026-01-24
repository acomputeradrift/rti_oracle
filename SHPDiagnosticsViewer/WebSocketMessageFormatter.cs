using System;
using System.Text.Json;

namespace SHPDiagnosticsViewer;

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
                        catch
                        {
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
        catch
        {
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
                _currentDate = _currentDate.AddDays(1);
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
}
