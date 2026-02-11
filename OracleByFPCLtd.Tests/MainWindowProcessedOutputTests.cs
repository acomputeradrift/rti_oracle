using System;
using System.Collections.Generic;
using System.Reflection;
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
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class MainWindowProcessedOutputTests
{
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

    private static void SetRichText(MainWindow window, string fieldName, string value)
    {
        var richText = GetRichTextBox(window, fieldName);
        richText.Document.Blocks.Clear();
        richText.Document.Blocks.Add(new Paragraph(new Run(value)));
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

    private static ConnectionPanel GetConnectionPanel(MainWindow window)
    {
        var field = typeof(MainWindow).GetField("ConnectionPanel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(field);
        return (ConnectionPanel)field!.GetValue(window)!;
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
        public event EventHandler<string>? TransportInfo;
        public event EventHandler<string>? TransportError;

        public bool IsConnected { get; set; }
        public List<string> DiscoverResults { get; set; } = new();
        public bool ConnectShouldFail { get; set; }

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
        public Task SendLogLevelAsync(string type, string level) => Task.CompletedTask;
        public Task<List<DriverInfo>> LoadDriversAsync(string ip) => Task.FromResult(new List<DriverInfo>());

        public void RaiseTransportError(string message) => TransportError?.Invoke(this, message);
    }
}
