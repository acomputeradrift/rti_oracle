using System;
using System.IO;
using System.Windows;

namespace OracleByFPCLtd.Reliability;

public sealed class MainWindowFailureNotifier : IUserFailureNotifier
{
    private readonly Window _owner;
    private readonly string _plainLogPath;
    private readonly string _htmlLogPath;
    private readonly Func<DateTime?>? _timestampProvider;

    public MainWindowFailureNotifier(Window owner, Func<DateTime?>? timestampProvider = null)
    {
        _owner = owner;
        _plainLogPath = BuildLogPath("oracle.log");
        _htmlLogPath = BuildLogPath("oracle-log.html");
        _timestampProvider = timestampProvider;
        EnsureLogFileExists();
    }

    public void ShowBlockingFailure(string feature, OperationFailure failure)
    {
        // Popups are disabled; failures are logged through the status area and operational logs.
    }

    public void AppendOperationalLog(OperationFailure failure)
    {
        AppendOperationalResult(failure.Code, "FAILED", failure.Message, failure.Context, failure.TimestampUtc);
    }

    public void AppendOperationalResult(string code, string status, string message, string context)
    {
        AppendOperationalResult(code, status, message, context, DateTime.UtcNow);
    }

    private void AppendOperationalResult(string code, string status, string message, string context, DateTime timestampUtc)
    {
        try
        {
            var normalizedStatus = NormalizeStatus(status);
            var effectiveTimestamp = _timestampProvider?.Invoke() ?? timestampUtc;
            var entry = $"{effectiveTimestamp:O} [result][{normalizedStatus}][{code}] {message} | context={context}{Environment.NewLine}";
            File.AppendAllText(_plainLogPath, entry);
            AppendToHtmlLog(effectiveTimestamp, code, normalizedStatus, message, context);
        }
        catch (Exception)
        {
            // File logging must never mutate raw diagnostics output or throw.
        }
    }

    private static string BuildLogPath(string fileName)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        return Path.Combine(desktop, fileName);
    }

    private void EnsureLogFileExists()
    {
        try
        {
            var directory = Path.GetDirectoryName(_plainLogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(_plainLogPath))
            {
                using var _ = File.Create(_plainLogPath);
            }

            if (!File.Exists(_htmlLogPath))
            {
                var html = "<!doctype html><html><head><meta charset=\"utf-8\"><title>Oracle Operational Log</title></head><body><pre>" +
                           "</pre></body></html>";
                File.WriteAllText(_htmlLogPath, html);
            }

            AppendSessionSeparator(DateTime.UtcNow);
            AppendOperationalResult("LOGGER_READY", "SUCCESS", "Operational result logging initialized.", _plainLogPath, DateTime.UtcNow);
        }
        catch (Exception)
        {
            // File logging must never mutate raw diagnostics output or throw.
        }
    }

    private void AppendSessionSeparator(DateTime timestampUtc)
    {
        var separator = $"{Environment.NewLine}----------- SESSION START {timestampUtc:O} -----------{Environment.NewLine}";
        File.AppendAllText(_plainLogPath, separator);

        var html = File.ReadAllText(_htmlLogPath);
        var marker = "</pre>";
        var line = $"{Environment.NewLine}----------- SESSION START {timestampUtc:O} -----------{Environment.NewLine}";
        var index = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return;
        }

        html = html.Insert(index, EscapeHtml(line));
        File.WriteAllText(_htmlLogPath, html);
    }

    private void AppendToHtmlLog(DateTime timestampUtc, string code, string status, string message, string context)
    {
        var html = File.ReadAllText(_htmlLogPath);
        var marker = "</pre>";
        var color = status.Equals("FAILED", StringComparison.OrdinalIgnoreCase)
            ? "#b00020"
            : status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase)
                ? "#2f7d32"
                : "#b26a00";
        var safeMessage = EscapeHtml(message);
        var safeContext = EscapeHtml(context);
        var safeCode = EscapeHtml(code);
        var safeStatus = EscapeHtml(status);
        var line = $"{timestampUtc:O} [result]<span style=\"color:{color}\">[{safeStatus}]</span>[{safeCode}] {safeMessage} | context={safeContext}";
        var index = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return;
        }

        html = html.Insert(index, line + Environment.NewLine);
        File.WriteAllText(_htmlLogPath, html);
    }

    private static string EscapeHtml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }

    private static string NormalizeStatus(string status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "FAILED"
            : status.Trim().ToUpperInvariant();
    }
}
