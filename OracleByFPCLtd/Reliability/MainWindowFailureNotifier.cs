using System;
using System.IO;
using System.Windows;
using OracleByFPCLtd.Logging;

namespace OracleByFPCLtd.Reliability;

public sealed class MainWindowFailureNotifier : IUserFailureNotifier
{
    private readonly Window _owner;
    private readonly Func<DateTime?>? _timestampProvider;
    private readonly CentralLogger _centralLogger = new(new CentralLoggerOptions
    {
        LogFilePath = BuildStructuredLogPath()
    });

    public MainWindowFailureNotifier(Window owner, Func<DateTime?>? timestampProvider = null)
    {
        _owner = owner;
        _timestampProvider = timestampProvider;
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
            LogStructuredEvent(normalizedStatus, code, message, context, effectiveTimestamp);
        }
        catch (Exception)
        {
            // File logging must never mutate raw diagnostics output or throw.
        }
    }

    private static string BuildStructuredLogPath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Oracle by FP&C",
            "Logs");
        return Path.Combine(folder, "oracle-structured.log");
    }


    private static string NormalizeStatus(string status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "FAILED"
            : status.Trim().ToUpperInvariant();
    }

    private void LogStructuredEvent(string status, string code, string message, string context, DateTime timestampUtc)
    {
        var severity = status switch
        {
            "WARN" => SeverityLevel.Warn,
            "FAIL" => SeverityLevel.Error,
            "FAILED" => SeverityLevel.Error,
            "SUCCESS" => SeverityLevel.Success,
            _ => SeverityLevel.Info
        };

        _centralLogger.LogEvent(new LogEntry(
            severity,
            CreateCorrelationId(),
            "MainWindowFailureNotifier",
            "OperationalResult",
            message,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["code"] = code,
                ["status"] = status,
                ["context"] = context,
                ["timestampUtc"] = timestampUtc.ToString("O")
            }));
    }

    private static string CreateCorrelationId()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 6);
    }
}
