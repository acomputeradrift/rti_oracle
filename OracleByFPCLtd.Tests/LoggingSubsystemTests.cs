using System;
using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.Logging;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class LoggingSubsystemTests
{
    [Fact]
    public void LogEventWritesPlainTextLineWithExpectedFormat()
    {
        var logPath = TestTempPaths.CreateFilePath(".log");
        var fixedTime = new DateTime(2026, 1, 18, 12, 34, 56, 789, DateTimeKind.Local);
        var logger = new CentralLogger(new CentralLoggerOptions
        {
            SessionLogPath = logPath,
            TimestampProvider = () => fixedTime
        });

        var entry = new LogEntry(
            SeverityLevel.Warn,
            CorrelationId: "c9f3a1",
            Module: "DiagnosticsTransport",
            Phase: "Connect",
            Message: "WebSocket closed by remote host",
            Details: new Dictionary<string, string>
            {
                ["ip"] = "192.168.1.10",
                ["attempt"] = "2"
            },
            Exception: new InvalidOperationException("socket closed"));

        logger.LogEvent(entry);

        var log = File.ReadAllText(logPath);
        Assert.Contains("2026-01-18 12:34", log, StringComparison.Ordinal);
        Assert.Contains("[WARN]", log, StringComparison.Ordinal);
        Assert.Contains("DiagnosticsTransport/Connect", log, StringComparison.Ordinal);
        Assert.Contains("\"WebSocket closed by remote host\"", log, StringComparison.Ordinal);
        Assert.Contains("ip=\"192.168.1.10\"", log, StringComparison.Ordinal);
        Assert.Contains("attempt=\"2\"", log, StringComparison.Ordinal);
        Assert.Contains("exception=\"System.InvalidOperationException: socket closed", log, StringComparison.Ordinal);

        if (File.Exists(logPath))
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void LogEventCreatesTimestampedSessionLogFileInConfiguredDirectory()
    {
        var logDirectory = TestTempPaths.CreateDirectoryPath();
        Directory.CreateDirectory(logDirectory);
        var fixedTime = new DateTime(2026, 2, 28, 12, 32, 44, DateTimeKind.Local);
        var logger = new CentralLogger(new CentralLoggerOptions
        {
            LogDirectoryPath = logDirectory,
            TimestampProvider = () => fixedTime
        });

        logger.LogEvent(new LogEntry(
            SeverityLevel.Info,
            "abc123",
            "MainWindow",
            "Connection",
            "Connected to Websocket"));

        var files = Directory.GetFiles(logDirectory, "*_oracle_event_logs.log");
        Assert.Single(files);
        Assert.Equal("2026-02-28_12-32_oracle_event_logs.log", Path.GetFileName(files[0]));
        Assert.Contains("Connected to Websocket", File.ReadAllText(files[0]), StringComparison.Ordinal);

        Directory.Delete(logDirectory, recursive: true);
    }

    [Fact]
    public void LogEventFormatsLineSpecificMessagesUsingLinePrefix()
    {
        var logPath = TestTempPaths.CreateFilePath(".log");
        var logger = new CentralLogger(new CentralLoggerOptions
        {
            SessionLogPath = logPath,
            TimestampProvider = () => new DateTime(2026, 2, 28, 12, 32, 44, DateTimeKind.Local)
        });

        logger.LogEvent(new LogEntry(
            SeverityLevel.Info,
            "abc123",
            "ProcessingEngineRunner",
            "Formatting",
            "formatted with DateTime, line number",
            new Dictionary<string, string>
            {
                ["line"] = "42"
            }));

        var log = File.ReadAllText(logPath);
        Assert.Contains("2026-02-28 12:32 [INFO] ProcessingEngineRunner/Formatting: \"Line 42 - formatted with DateTime, line number\"", log, StringComparison.Ordinal);
        Assert.DoesNotContain(" line=\"42\"", log, StringComparison.Ordinal);

        File.Delete(logPath);
    }

    [Fact]
    public void LogEventPrefersProfileOverDriverWhenBothArePresent()
    {
        var logPath = TestTempPaths.CreateFilePath(".log");
        var logger = new CentralLogger(new CentralLoggerOptions
        {
            SessionLogPath = logPath,
            TimestampProvider = () => new DateTime(2026, 2, 28, 12, 32, 44, DateTimeKind.Local)
        });

        logger.LogEvent(new LogEntry(
            SeverityLevel.Success,
            "abc123",
            "ProcessingEngineRunner",
            "Processing",
            "mapped index 81 for Room Select",
            new Dictionary<string, string>
            {
                ["line"] = "42",
                ["driver"] = "DRIVER//5",
                ["profile"] = "Clipsal C-Bus"
            }));

        var log = File.ReadAllText(logPath);
        Assert.Contains("2026-02-28 12:32 [SUCCESS] ProcessingEngineRunner/Processing: \"Line 42 - mapped index 81 for Room Select\" profile=\"Clipsal C-Bus\"", log, StringComparison.Ordinal);
        Assert.DoesNotContain("driver=\"DRIVER//5\"", log, StringComparison.Ordinal);

        File.Delete(logPath);
    }

    [Fact]
    public void EmitStatusRejectsUnknownLevels()
    {
        var logger = new CentralLogger(new CentralLoggerOptions
        {
            LogFilePath = TestTempPaths.CreateFilePath(".log")
        });

        Assert.Throws<ArgumentException>(() =>
            logger.EmitStatus("DEBUG", "This status should be rejected", "c9f3a1"));
    }

    [Fact]
    public void EmitStatusDoesNotAppendCorrelationToken()
    {
        string? capturedLevel = null;
        string? capturedMessage = null;
        var logger = new CentralLogger(new CentralLoggerOptions
        {
            LogFilePath = TestTempPaths.CreateFilePath(".log"),
            StatusSink = (level, message) =>
            {
                capturedLevel = level;
                capturedMessage = message;
            }
        });

        logger.EmitStatus("WARN", "Log level ack timed out", "c9f3a1");

        Assert.Equal("WARN", capturedLevel);
        Assert.NotNull(capturedMessage);
        Assert.DoesNotContain("c9f3a1", capturedMessage!, StringComparison.Ordinal);
    }

    [Fact]
    public void LogPlainLineWritesExactTextWithoutStructuredPrefix()
    {
        var logPath = TestTempPaths.CreateFilePath(".log");
        var logger = new CentralLogger(new CentralLoggerOptions
        {
            SessionLogPath = logPath,
            TimestampProvider = () => new DateTime(2026, 2, 19, 10, 44, 23, DateTimeKind.Local)
        });

        const string line = "----- MAPPING START 2026-02-19T10:44:23.8844280-06:00 -----";
        logger.LogEvent(new LogEntry(
            SeverityLevel.Info,
            "abc123",
            "MainWindow",
            "Connection",
            line));

        var log = File.ReadAllText(logPath);
        Assert.Contains(line, log, StringComparison.Ordinal);

        if (File.Exists(logPath))
        {
            File.Delete(logPath);
        }
    }

    [Fact]
    public void LogEventKeepsNewestFiveSessionFiles()
    {
        var logDirectory = TestTempPaths.CreateDirectoryPath();
        Directory.CreateDirectory(logDirectory);
        for (var i = 0; i < 6; i++)
        {
            var path = Path.Combine(logDirectory, $"2026-02-2{i}_12-3{i}_oracle_event_logs.log");
            File.WriteAllText(path, $"old-{i}");
        }

        var currentPath = Path.Combine(logDirectory, "2026-02-28_12-32_oracle_event_logs.log");
        var logger = new CentralLogger(new CentralLoggerOptions
        {
            SessionLogPath = currentPath,
            TimestampProvider = () => new DateTime(2026, 2, 28, 12, 32, 0, DateTimeKind.Local),
            RetainedSessionFileCount = 5
        });

        logger.LogEvent(new LogEntry(
            SeverityLevel.Info,
            "abc123",
            "MainWindow",
            "Connection",
            "current"));

        var files = Directory.GetFiles(logDirectory, "*_oracle_event_logs.log")
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(5, files.Count);
        Assert.Contains("2026-02-28_12-32_oracle_event_logs.log", files);
        Assert.DoesNotContain("2026-02-20_12-30_oracle_event_logs.log", files);
        Assert.DoesNotContain("2026-02-21_12-31_oracle_event_logs.log", files);

        Directory.Delete(logDirectory, recursive: true);
    }

    [Fact]
    public void LogEventDoesNotThrowWhenLogFileIsLocked()
    {
        var logPath = TestTempPaths.CreateFilePath(".log");
        var logger = new CentralLogger(new CentralLoggerOptions
        {
            SessionLogPath = logPath,
            TimestampProvider = () => new DateTime(2026, 2, 19, 11, 28, 0, DateTimeKind.Local)
        });

        File.WriteAllText(logPath, "existing");
        using var lockStream = new FileStream(logPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var ex = Record.Exception(() => logger.LogEvent(new LogEntry(
            SeverityLevel.Info,
            "abc123",
            "MainWindow",
            "Connection",
            "Connected to Websocket")));

        Assert.Null(ex);

        lockStream.Dispose();
        if (File.Exists(logPath))
        {
            File.Delete(logPath);
        }
    }
}
