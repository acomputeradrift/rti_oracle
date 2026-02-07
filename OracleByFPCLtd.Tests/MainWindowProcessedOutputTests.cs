using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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

    private static ProjectDataExtractionResult BuildResult()
    {
        var result = new ProjectDataExtractionResult();
        result.DiagnosticsMapping.Add(new DiagnosticsMappingEntry(
            81,
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

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        using var done = new ManualResetEvent(false);
        var thread = new Thread(() =>
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
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        done.WaitOne();
        if (failure != null)
        {
            throw new InvalidOperationException("STA test failed.", failure);
        }
    }
}
