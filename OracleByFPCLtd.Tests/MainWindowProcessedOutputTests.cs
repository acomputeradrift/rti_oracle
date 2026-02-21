using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using OracleByFPCLtd.DiagnosticsTransport;
using OracleByFPCLtd.UI.Panels;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.Reliability;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class MainWindowProcessedOutputTests
{
    [Fact]
    public void LogLevelsSnapshotUsedOnlyForBaseline()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var first = "LogLevels {\"messageType\":\"LogLevels\",\"levels\":[{\"dName\":\"DRIVER//4\",\"logLevel\":2}]}";
            var second = "LogLevels {\"messageType\":\"LogLevels\",\"levels\":[{\"dName\":\"DRIVER//4\",\"logLevel\":1}]}";

            InvokeRawMessageReceived(window, first);
            var driver = window.Drivers.First(entry => entry.DName == "DRIVER//4");
            Assert.Equal(2, driver.SelectedLevel);

            InvokeRawMessageReceived(window, second);
            driver = window.Drivers.First(entry => entry.DName == "DRIVER//4");
            Assert.Equal(2, driver.SelectedLevel);
        });
    }

    [Fact]
    public void LogLevelsBaselineEmitsStatusCounts()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            SetProjectDriverNames(window, new[] { "DRIVER//12" });
            var baseline = "LogLevels {\"messageType\":\"LogLevels\",\"levels\":[{\"dName\":\"EVENTS_INPUT\",\"logLevel\":3},{\"dName\":\"DRIVER//12\",\"logLevel\":2}]}";

            InvokeRawMessageReceived(window, baseline);

            var statusText = GetStatusText(window);
            Assert.Contains("Log levels baseline received", statusText);
        });
    }

    [Fact]
    public void InitializeProcessingPopulatesProcessedLogTextBox()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            InvokeAppendLog(window, "1 [2026-01-24 10:00:00.000] Change to page 1 on device 'RTiPanel (iPhone X or newer)'");

            var result = BuildResult();
            window.InitializeProcessing(result);

            var processed = GetRichText(window, "ProcessedLogTextBox");
            Assert.Contains("Change to page \"Room Select\"", processed);
        });
    }

    [Fact]
    public void AppendLogAppendsProcessedLineForNumberedEntries()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var result = BuildResult();
            window.InitializeProcessing(result);

            InvokeAppendLog(window, "2 [2026-01-24 10:00:01.000] Change to page 1 on device 'RTiPanel (iPhone X or newer)'");

            var processed = GetRichText(window, "ProcessedLogTextBox");
            Assert.DoesNotContain("No processed information available", processed);
            Assert.Contains("Change to page \"Room Select\"", processed);
        });
    }

    [Fact]
    public void ClearDiagnosticsClearsRawAndProcessedOutput()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            SetRichText(window, "RawLogTextBox", "raw");
            SetRichText(window, "ProcessedLogTextBox", "processed");

            InvokeClearDiagnostics(window);

            Assert.Equal(string.Empty, GetRichText(window, "RawLogTextBox"));
            Assert.Equal(string.Empty, GetRichText(window, "ProcessedLogTextBox"));
        });
    }

    [Fact]
    public void ProcessedLogScrollsHorizontallyOnlyWhenNeeded()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow
            {
                Width = 900,
                Height = 700
            };
            window.Show();
            window.UpdateLayout();

            InvokeSetProcessedOutput(window, new[] { "Short line." });
            window.UpdateLayout();

            var processed = GetRichTextBox(window, "ProcessedLogTextBox");
            FlushLayout(processed);
            var viewport = processed.ViewportWidth;
            Assert.True(viewport > 0);
            Assert.InRange(processed.Document.PageWidth, viewport - 1, viewport + 1);
            Assert.Equal(processed.Document.PageWidth, processed.Document.ColumnWidth);
            Assert.Equal(1, GetVisualLineCount(processed));

            InvokeSetProcessedOutput(window, new[] { new string('X', 500) });
            window.UpdateLayout();
            FlushLayout(processed);

            Assert.True(processed.Document.PageWidth > processed.ViewportWidth);
            Assert.Equal(1, GetVisualLineCount(processed));
            window.Hide();
        });
    }

    [Fact]
    public void ProcessedLogUsesMeasuredWidthWhenViewportUnavailable()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow
            {
                Width = 900,
                Height = 700
            };
            window.Show();
            window.UpdateLayout();
            InvokeSetProcessedOutput(window, new[] { new string('X', 300) });
            window.UpdateLayout();

            var processed = GetRichTextBox(window, "ProcessedLogTextBox");
            FlushLayout(processed);
            Assert.False(double.IsNaN(processed.Document.PageWidth));
            Assert.Equal(processed.Document.PageWidth, processed.Document.ColumnWidth);
            Assert.True(processed.Document.PageWidth > 0);
            window.Hide();
        });
    }

    [Fact]
    public void DiscoverShowsPlaceholderWhenResultsFound()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            window.Show();
            window.UpdateLayout();
            var transport = new FakeDiagnosticsTransport
            {
                DiscoverResults = new List<string> { "10.0.0.10" }
            };

            InvokeSetTransport(window, transport, false);
            InvokeDiscover(window);
            DoEvents();

            var combo = GetConnectionPanel(window).DiscoveredCombo;
            Assert.NotNull(combo.ItemsSource);
            var items = (IList<string>)combo.ItemsSource!;
            Assert.True(items.Count >= 2);
            Assert.Equal("Select a device...", items[0]);
            window.Hide();
        });
    }

    [Fact]
    public void TransportErrorStartsReconnectStatus()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            window.Show();
            window.UpdateLayout();
            var transport = new FakeDiagnosticsTransport
            {
                ConnectShouldFail = true
            };
            InvokeSetTransport(window, transport, false);
            SetPrivateField(window, "_apexUploaded", true);
            SetIpAddress(window, "10.0.0.10");

            transport.RaiseTransportError("[error] WebSocket closed by remote host.");
            DoEvents();

            var status = GetConnectionPanel(window).StatusText;
            Assert.Equal("Attempting Reconnect...", status.Text);
            Assert.Equal("Stop", GetConnectionPanel(window).ConnectButton.Content);
            window.Hide();
        });
    }

    [Fact]
    public void DiagnosticsDriverAckResolvesPendingPrimaryProcessorAlias()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            SetPrivateField(window, "_diagnosticsDriverDName", "DRIVER//6");

            var timeoutSource = new CancellationTokenSource();
            SetPendingAck(window, "Diagnostics: Primary Processor", 0, timeoutSource);

            InvokeTryResolvePendingLogLevel(window, "DRIVER//6", 0);

            Assert.True(timeoutSource.IsCancellationRequested);
            Assert.False(HasPendingAck(window, "Diagnostics: Primary Processor"));
        });
    }

    [Fact]
    public void PrimaryProcessorAckResolvesPendingDiagnosticsDriverAlias()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            SetPrivateField(window, "_diagnosticsDriverDName", "DRIVER//6");

            var timeoutSource = new CancellationTokenSource();
            SetPendingAck(window, "DRIVER//6", 1, timeoutSource);

            InvokeTryResolvePendingLogLevel(window, "Diagnostics: Primary Processor", 1);

            Assert.True(timeoutSource.IsCancellationRequested);
            Assert.False(HasPendingAck(window, "DRIVER//6"));
        });
    }

    [Fact]
    public void EmitPhaseStatusFromBackgroundThreadDoesNotThrow()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            Exception? backgroundFailure = null;
            using var done = new ManualResetEvent(false);

            _ = Task.Run(() =>
            {
                try
                {
                    InvokeEmitPhaseStatus(
                        window,
                        "Connection",
                        "INFO",
                        "Reconnecting...",
                        "reconnect_start",
                        new Dictionary<string, string> { ["ip"] = "192.168.1.143" },
                        null);
                }
                catch (Exception ex)
                {
                    backgroundFailure = ex;
                }
                finally
                {
                    done.Set();
                }
            });

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (!done.WaitOne(25))
            {
                DoEvents();
                if (DateTime.UtcNow >= deadline)
                {
                    break;
                }
            }

            Assert.True(done.WaitOne(0));
            DoEvents();
            Assert.Null(backgroundFailure);
            Assert.Contains("Reconnecting", GetStatusText(window));
        });
    }

    [Fact]
    public void VisibleLogLevelDriversSnapshotIsStableAfterCollectionMutation()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            window.Drivers.Add(new MainWindow.DriverEntry(1, "A", "DRIVER//1"));
            window.Drivers.Add(new MainWindow.DriverEntry(2, "B", "DRIVER//2"));

            var snapshot = InvokeVisibleLogLevelDriversSnapshot(window);
            window.Drivers.Add(new MainWindow.DriverEntry(3, "C", "DRIVER//3"));

            Assert.Equal(2, snapshot.Count);
        });
    }

    [Fact]
    public void DriverInventoryValueIncludesIdNameAndDName()
    {
        var value = InvokeBuildDriverInventoryValue(new List<DriverInfo>
        {
            new(47, "Diagnostics: Primary Processor", "DRIVER//47"),
            new(12, "Jandy iAquaLink", "DRIVER//12")
        });

        Assert.Contains("47:Diagnostics: Primary Processor:DRIVER//47", value, StringComparison.Ordinal);
        Assert.Contains("12:Jandy iAquaLink:DRIVER//12", value, StringComparison.Ordinal);
    }

    [Fact]
    public void DiagnosticsDriverCandidateValueIncludesDiagnosticsOnly()
    {
        var value = InvokeBuildDiagnosticsDriverCandidateValue(new List<DriverInfo>
        {
            new(47, "Diagnostics: Primary Processor", "DRIVER//47"),
            new(12, "Jandy iAquaLink", "DRIVER//12"),
            new(49, "Diagnostics: Secondary Processor", "DRIVER//49")
        });

        Assert.Contains("47:Diagnostics: Primary Processor:DRIVER//47", value, StringComparison.Ordinal);
        Assert.Contains("49:Diagnostics: Secondary Processor:DRIVER//49", value, StringComparison.Ordinal);
        Assert.DoesNotContain("12:Jandy iAquaLink:DRIVER//12", value, StringComparison.Ordinal);
    }

    [Fact]
    public void WaitForLogLevelAckHonorsProvidedTimeout()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var started = DateTime.UtcNow;
            var acknowledged = Task.Run(() => InvokeWaitForLogLevelAck(window, "DRIVER//99", 1, 0, 50)).GetAwaiter().GetResult();
            var elapsed = DateTime.UtcNow - started;

            Assert.False(acknowledged);
            Assert.InRange(elapsed.TotalMilliseconds, 25, 800);
        });
    }

    [Fact]
    public void ForceProtectedLogLevelsSendsProjectPrimeConfirmThenSystem()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var transport = new FakeDiagnosticsTransport
            {
                IsConnected = true,
                AutoAckCommands = true
            };
            InvokeSetTransport(window, transport, false);
            SetForceProtectedPreconditions(window, "192.168.1.143", "DRIVER//6");

            var drivers = new List<DriverInfo>
            {
                new(6, "Diagnostics: Primary Processor", "DRIVER//6"),
                new(1, "Weather", "DRIVER//1")
            };

            WaitForTaskWithDoEvents(InvokeForceProtectedLogLevels(window, drivers));

            var sent = transport.SentLogLevelCommands.Select(x => $"{x.Type}:{x.Level}").ToList();
            Assert.True(sent.Count >= 3);
            Assert.Equal("DRIVER//6:1", sent[0]);
            Assert.Equal("DRIVER//6:1", sent[1]);
            Assert.Contains("Diagnostics: Primary Processor:0", sent);
        });
    }

    [Fact]
    public void ForceProtectedLogLevelsSkipsSystemWhenProjectConfirmFails()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var transport = new FakeDiagnosticsTransport
            {
                IsConnected = true,
                AutoAckCommands = true,
                ShouldDispatch = (index, _, _) => index == 1
            };
            InvokeSetTransport(window, transport, false);
            SetForceProtectedPreconditions(window, "192.168.1.143", "DRIVER//6");

            var drivers = new List<DriverInfo>
            {
                new(6, "Diagnostics: Primary Processor", "DRIVER//6"),
                new(1, "Weather", "DRIVER//1")
            };

            WaitForTaskWithDoEvents(InvokeForceProtectedLogLevels(window, drivers));

            var sent = transport.SentLogLevelCommands.Select(x => $"{x.Type}:{x.Level}").ToList();
            Assert.DoesNotContain("Diagnostics: Primary Processor:0", sent);
            Assert.Contains("Log level status failed", GetStatusText(window));
        });
    }

    [Fact]
    public void ProcessedLogKeepsWidthWhenResizingPane()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow
            {
                Width = 900,
                Height = 700
            };
            window.Show();
            window.UpdateLayout();

            var longLine = new string('X', 400);
            InvokeSetProcessedOutput(window, new[] { longLine });
            window.UpdateLayout();

            var processed = GetRichTextBox(window, "ProcessedLogTextBox");
            FlushLayout(processed);
            var initialWidth = processed.Document.PageWidth;
            Assert.True(initialWidth > 0);

            window.Width = 700;
            window.UpdateLayout();
            FlushLayout(processed);

            Assert.True(processed.Document.PageWidth >= processed.ViewportWidth);
            window.Hide();
        });
    }

    [Fact]
    public void RawLogAutoScrollsToEndOnAppend()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow
            {
                Width = 900,
                Height = 700
            };
            window.Show();
            window.UpdateLayout();

            var raw = GetRichTextBox(window, "RawLogTextBox");
            for (var i = 0; i < 60; i++)
            {
                InvokeAppendLog(window, $"[{i:00}] line {i}");
            }

            FlushLayout(raw);
            var scrollViewer = GetScrollViewer(raw);
            Assert.NotNull(scrollViewer);
            Assert.True(scrollViewer!.ScrollableHeight > 0);
            Assert.InRange(scrollViewer.VerticalOffset, scrollViewer.ScrollableHeight - 1, scrollViewer.ScrollableHeight + 1);
            window.Hide();
        });
    }

    [Fact]
    public void DownloadProcessedLogsMenuItemDisabledWhenNoProcessedLines()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();

            InvokeApplyCurrentFilter(window);

            var downloadMenuItem = GetDownloadProcessedLogsMenuItem(window);
            Assert.False(downloadMenuItem.IsEnabled);
        });
    }

    [Fact]
    public void DownloadProcessedLogsMenuItemDisabledWhenFilterMatchesNothing()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var processedLines = GetProcessedLines(window);
            processedLines.Add("[2026-01-24 09:00] Macro - Start");

            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
            diagnostics.FilterBar.FilterKeywordTextBox.Text = "Driver";

            InvokeFilterApply(window);

            var downloadMenuItem = GetDownloadProcessedLogsMenuItem(window);
            Assert.False(downloadMenuItem.IsEnabled);
        });
    }

    [Fact]
    public void DownloadProcessedLogsMenuItemEnabledWhenFilteredProcessedHasMatches()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var processedLines = GetProcessedLines(window);
            processedLines.Add("[2026-01-24 10:15] Driver event: Match");

            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
            diagnostics.FilterBar.FilterKeywordTextBox.Text = "Driver";

            InvokeFilterApply(window);

            var downloadMenuItem = GetDownloadProcessedLogsMenuItem(window);
            Assert.True(downloadMenuItem.IsEnabled);
        });
    }

    private static ProjectDataExtractionResult BuildResult()
    {
        var result = new ProjectDataExtractionResult();
        result.DiagnosticsMapping.Add(new DiagnosticsMappingEntry(
            81,
            "RTiPanel (iPhone X or newer)",
            "RTiPanel (iPhone X or newer)",
            0,
            0,
            0,
            0,
            "Room Select"));
        result.ApexDiscoveryPreload.PageIndexMap["81|0"] = "Room Select";
        return result;
    }

    private static string GetRichText(MainWindow window, string fieldName)
    {
        var richText = GetRichTextBox(window, fieldName);
        var range = new TextRange(richText.Document.ContentStart, richText.Document.ContentEnd);
        return range.Text.Trim();
    }

    private static string GetStatusText(MainWindow window)
    {
        var statusPanel = (OracleByFPCLtd.UI.Panels.StatusPanel)window.FindName("StatusPanel")!;
        var range = new TextRange(statusPanel.StatusOutputTextBox.Document.ContentStart, statusPanel.StatusOutputTextBox.Document.ContentEnd);
        return range.Text.Trim();
    }

    private static void SetRichText(MainWindow window, string fieldName, string value)
    {
        var richText = GetRichTextBox(window, fieldName);
        richText.Document.Blocks.Clear();
        richText.Document.Blocks.Add(new Paragraph(new Run(value)));
    }

    private static void SetProjectDriverNames(MainWindow window, IEnumerable<string> dNames)
    {
        var field = typeof(MainWindow).GetField("_projectDriverDNames", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var set = (HashSet<string>)field!.GetValue(window)!;
        set.Clear();
        foreach (var dName in dNames)
        {
            set.Add(dName);
        }
    }

    private static RichTextBox GetRichTextBox(MainWindow window, string fieldName)
    {
        var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
        return fieldName == "RawLogTextBox"
            ? diagnostics.RawOutputPanel.LogOutputView.LogTextBox
            : diagnostics.ProcessedOutputPanel.LogOutputView.LogTextBox;
    }

    private static int GetVisualLineCount(RichTextBox richTextBox)
    {
        var pointer = richTextBox.Document.ContentStart;
        var count = 1;
        while (true)
        {
            var next = pointer.GetLineStartPosition(1);
            if (next == null)
            {
                break;
            }

            count++;
            pointer = next;
        }

        return count;
    }

    private static void FlushLayout(FrameworkElement element)
    {
        element.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
        element.UpdateLayout();
    }

    private static ScrollViewer? GetScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer viewer)
        {
            return viewer;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var result = GetScrollViewer(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static void InvokeSetProcessedOutput(MainWindow window, IEnumerable<string> lines)
    {
        var method = typeof(MainWindow).GetMethod("SetProcessedOutput", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { lines, false });
    }

    private static void InvokeRawMessageReceived(MainWindow window, string raw)
    {
        var method = typeof(MainWindow).GetMethod("Transport_RawMessageReceived", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object?[] { null, raw });
    }

    private static string InvokeFormatMessage(MainWindow window, string raw, out bool isLogLine)
    {
        var method = typeof(MainWindow).GetMethod("FormatMessage", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        object?[] args = { raw, false };
        var result = (string)method!.Invoke(window, args)!;
        isLogLine = (bool)args[1]!;
        return result;
    }

    private static void InvokeAppendLog(MainWindow window, string line)
    {
        var method = typeof(MainWindow).GetMethod("AppendLog", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { line, false });
    }

    private static void InvokeClearDiagnostics(MainWindow window)
    {
        var method = typeof(MainWindow).GetMethod("ClearDiagnostics_Click", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { window, null! });
    }

    private static void InvokeApplyCurrentFilter(MainWindow window)
    {
        var method = typeof(MainWindow).GetMethod("ApplyCurrentFilter", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, null);
    }

    private static void InvokeFilterApply(MainWindow window)
    {
        var method = typeof(MainWindow).GetMethod("FilterApplyButton_Click", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { window, new RoutedEventArgs() });
    }

    private static List<string> GetProcessedLines(MainWindow window)
    {
        var field = typeof(MainWindow).GetField("_processedLogLines", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (List<string>)field!.GetValue(window)!;
    }

    private static MenuItem GetDownloadProcessedLogsMenuItem(MainWindow window)
    {
        var menuItem = window.FindName("DownloadProcessedLogsMenuItemControl");
        Assert.NotNull(menuItem);
        return (MenuItem)menuItem!;
    }

    private static void InvokeDiscover(MainWindow window)
    {
        var method = typeof(MainWindow).GetMethod("DiscoverButton_Click", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { window, new RoutedEventArgs() });
    }

    private static void InvokeSetTransport(MainWindow window, IDiagnosticsTransport transport, bool useTcpCapture)
    {
        var method = typeof(MainWindow).GetMethod("SetTransport", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { transport, useTcpCapture });
    }

    private static void SetPrivateField(MainWindow window, string fieldName, object value)
    {
        var field = typeof(MainWindow).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(window, value);
    }

    private static void SetIpAddress(MainWindow window, string ip)
    {
        GetConnectionPanel(window).IpTextBox.Text = ip;
    }

    private static void SetPendingAck(MainWindow window, string dName, int level, CancellationTokenSource timeoutSource)
    {
        var pendingField = typeof(MainWindow).GetField("_pendingLogLevelCommands", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(pendingField);
        var pendingMap = pendingField!.GetValue(window)!;

        var nestedType = typeof(MainWindow).GetNestedType("PendingLogLevelCommand", BindingFlags.NonPublic);
        Assert.NotNull(nestedType);
        var ctor = nestedType!.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            binder: null,
            new[] { typeof(int), typeof(int), typeof(CancellationTokenSource) },
            modifiers: null);
        Assert.NotNull(ctor);

        var pending = ctor!.Invoke(new object[] { level, 0, timeoutSource });
        var key = BuildLogLevelAckKey(dName);
        pendingMap.GetType().GetMethod("Add")!.Invoke(pendingMap, new[] { (object)key, pending });
    }

    private static bool HasPendingAck(MainWindow window, string dName)
    {
        var pendingField = typeof(MainWindow).GetField("_pendingLogLevelCommands", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(pendingField);
        var pendingMap = pendingField!.GetValue(window)!;
        var key = BuildLogLevelAckKey(dName);
        return (bool)pendingMap.GetType().GetMethod("ContainsKey")!.Invoke(pendingMap, new object[] { key })!;
    }

    private static void InvokeTryResolvePendingLogLevel(MainWindow window, string dName, int level)
    {
        var method = typeof(MainWindow).GetMethod("TryResolvePendingLogLevelCommand", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { dName, level });
    }

    private static void InvokeEmitPhaseStatus(
        MainWindow window,
        string phase,
        string level,
        string message,
        string op,
        IReadOnlyDictionary<string, string>? details,
        Exception? exception)
    {
        var method = typeof(MainWindow).GetMethod("EmitPhaseStatus", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object?[] { phase, level, message, op, details, exception });
    }

    private static IReadOnlyList<MainWindow.DriverEntry> InvokeVisibleLogLevelDriversSnapshot(MainWindow window)
    {
        var method = typeof(MainWindow).GetMethod("GetVisibleLogLevelDriversSnapshot", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (IReadOnlyList<MainWindow.DriverEntry>)method!.Invoke(window, Array.Empty<object>())!;
    }

    private static string InvokeBuildDriverInventoryValue(IReadOnlyList<DriverInfo> drivers)
    {
        var method = typeof(MainWindow).GetMethod("BuildDriverInventoryValue", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (string)method!.Invoke(null, new object[] { drivers })!;
    }

    private static string InvokeBuildDiagnosticsDriverCandidateValue(IReadOnlyList<DriverInfo> drivers)
    {
        var method = typeof(MainWindow).GetMethod("BuildDiagnosticsDriverCandidateValue", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (string)method!.Invoke(null, new object[] { drivers })!;
    }

    private static string BuildLogLevelAckKey(string dName)
    {
        var method = typeof(MainWindow).GetMethod("BuildLogLevelAckKey", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (string)method!.Invoke(null, new object[] { dName })!;
    }

    private static Task<bool> InvokeWaitForLogLevelAck(MainWindow window, string dName, int level, int retryCount, int timeoutMs)
    {
        var method = typeof(MainWindow).GetMethod("WaitForLogLevelAckAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (Task<bool>)method!.Invoke(window, new object[] { dName, level, retryCount, timeoutMs })!;
    }

    private static ConnectionPanel GetConnectionPanel(MainWindow window)
    {
        var field = typeof(MainWindow).GetField("ConnectionPanel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(field);
        return (ConnectionPanel)field!.GetValue(window)!;
    }

    private static Task InvokeForceProtectedLogLevels(MainWindow window, IReadOnlyList<DriverInfo> drivers)
    {
        var method = typeof(MainWindow).GetMethod("ForceProtectedLogLevelsAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (Task)method!.Invoke(window, new object[] { drivers })!;
    }

    private static void SetForceProtectedPreconditions(MainWindow window, string ip, string diagnosticsDName)
    {
        SetPrivateField(window, "_lastConnectedIp", ip);
        SetPrivateField(window, "_logLevelsBaselineCaptured", true);
        SetPrivateField(window, "_diagnosticsDriverDName", diagnosticsDName);

        var tcs = new TaskCompletionSource<bool>();
        tcs.TrySetResult(true);
        SetPrivateField(window, "_logLevelsBaselineTcs", tcs);

        var phaseField = typeof(MainWindow).GetField("_connectionPhase", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(phaseField);
        var phaseValue = Enum.Parse(phaseField!.FieldType, "BaselineAwait");
        phaseField.SetValue(window, phaseValue);
    }

    private static void DoEvents()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(_ =>
        {
            frame.Continue = false;
            return null;
        }), null);
        Dispatcher.PushFrame(frame);
    }

    private static void WaitForTaskWithDoEvents(Task task, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!task.IsCompleted && DateTime.UtcNow < deadline)
        {
            DoEvents();
            Thread.Sleep(10);
        }

        if (!task.IsCompleted)
        {
            throw new TimeoutException("Timed out waiting for task completion.");
        }

        task.GetAwaiter().GetResult();
    }

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        using var done = new ManualResetEvent(false);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    done.Set();
                    dispatcher.InvokeShutdown();
                }
            }));
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!done.WaitOne(TimeSpan.FromSeconds(15)))
        {
            thread.IsBackground = true;
            throw new TimeoutException("STA test timed out.");
        }
        if (failure != null)
        {
            throw new InvalidOperationException("STA test failed.", failure);
        }
    }

    private sealed class FakeDiagnosticsTransport : IDiagnosticsTransport
    {
        public event EventHandler<string>? RawMessageReceived;
        public event EventHandler<string>? TransportInfo
        {
            add { }
            remove { }
        }
        public event EventHandler<string>? TransportError;
        public event EventHandler<FeatureOperation>? OperationStateChanged
        {
            add { }
            remove { }
        }

        public bool IsConnected { get; set; }
        public List<string> DiscoverResults { get; set; } = new();
        public bool ConnectShouldFail { get; set; }
        public bool AutoAckCommands { get; set; }
        public int AckDelayMs { get; set; } = 25;
        public Func<int, string, string, bool>? ShouldDispatch { get; set; }
        public Func<int, string, string, bool>? ShouldAck { get; set; }
        public List<(string Type, string Level)> SentLogLevelCommands { get; } = new();

        public Task<List<string>> DiscoverAsync(TimeSpan timeout) => Task.FromResult(DiscoverResults);
        public Task ConnectAsync(string ip)
        {
            if (ConnectShouldFail)
            {
                return Task.FromException(new InvalidOperationException("Connect failed."));
            }
            return Task.CompletedTask;
        }
        public Task DisconnectAsync() => Task.CompletedTask;
        public Task<CommandDispatchResult> SendLogLevelCommandAsync(string type, string level, CancellationToken token = default)
        {
            SentLogLevelCommands.Add((type, level));
            var sendIndex = SentLogLevelCommands.Count;
            var shouldDispatch = ShouldDispatch?.Invoke(sendIndex, type, level) ?? true;
            if (!shouldDispatch)
            {
                var failure = new OperationFailure(
                    FailureCodes.LogLevelDispatchFailed,
                    "Dispatch blocked by fake transport.",
                    $"type={type};level={level};index={sendIndex}",
                    DateTime.UtcNow);
                return Task.FromResult(CommandDispatchResult.Fail(failure));
            }

            if (AutoAckCommands)
            {
                var shouldAck = ShouldAck?.Invoke(sendIndex, type, level) ?? true;
                if (shouldAck)
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(AckDelayMs);
                        var websocketPayload = JsonSerializer.Serialize(new
                        {
                            type = "Subscribe",
                            resource = "LogLevel",
                            value = new
                            {
                                type,
                                level
                            }
                        });
                        var text = $"Diagnostics: Primary Processor - OnHTTPServerData() data.websocket = {websocketPayload}";
                        var raw = JsonSerializer.Serialize(new
                        {
                            messageType = "MessageLog",
                            text
                        });
                        RawMessageReceived?.Invoke(this, raw);
                    });
                }
            }

            return Task.FromResult(CommandDispatchResult.Success());
        }
        public Task SendLogLevelAsync(string type, string level) => Task.CompletedTask;
        public Task<List<DriverInfo>> LoadDriversAsync(string ip) => Task.FromResult(new List<DriverInfo>());

        public void RaiseTransportError(string message) => TransportError?.Invoke(this, message);
    }
}
