using System;
using System.Collections.Generic;
using System.IO;
using OracleByFPCLtd.Logging;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class LoggingSubsystemTests
{
    [Fact]
    public void LogEventWritesHtmlLineWithExpectedFormat()
    {
        var htmlPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.html");
        var fixedTime = new DateTime(2026, 1, 18, 12, 34, 56, 789, DateTimeKind.Local);
        var logger = new CentralLogger(new CentralLoggerOptions
        {
            HtmlLogPath = htmlPath,
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

        var html = File.ReadAllText(htmlPath);
        Assert.Contains(fixedTime.ToString("O"), html, StringComparison.Ordinal);
        Assert.Contains("[WARN]", html, StringComparison.Ordinal);
        Assert.Contains("<strong>WebSocket closed by remote host</strong>", html, StringComparison.Ordinal);
        Assert.Contains("DiagnosticsTransport/Connect", html, StringComparison.Ordinal);
        Assert.Contains("details=ip=192.168.1.10;attempt=2", html, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", html, StringComparison.Ordinal);
        Assert.Contains("socket closed", html, StringComparison.Ordinal);

        if (File.Exists(htmlPath))
        {
            File.Delete(htmlPath);
        }
    }

    [Fact]
    public void EmitStatusRejectsUnknownLevels()
    {
        var logger = new CentralLogger(new CentralLoggerOptions
        {
            LogFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.log")
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
            LogFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.log"),
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
}
