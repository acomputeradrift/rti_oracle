using System;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Documents;
using SHPDiagnosticsViewer.ProjectData;
using Xunit;

namespace SHPDiagnosticsViewer.Tests;

public sealed class MainWindowProcessedOutputTests
{
    [Fact]
    public void InitializeProcessingPopulatesProcessedLogTextBox()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var rawLog = GetTextBox(window, "RawLogTextBox");
            rawLog.Text = "1 [2026-01-24 10:00:00.000] Change to page 1 on device 'RTiPanel (iPhone X or newer)'";

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
            GetTextBox(window, "RawLogTextBox").Text = "raw";
            SetRichText(window, "ProcessedLogTextBox", "processed");

            InvokeClearDiagnostics(window);

            Assert.Equal(string.Empty, GetTextBox(window, "RawLogTextBox").Text);
            Assert.Equal(string.Empty, GetRichText(window, "ProcessedLogTextBox"));
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

    private static TextBox GetTextBox(MainWindow window, string fieldName)
    {
        var field = typeof(MainWindow).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(field);
        return (TextBox)field!.GetValue(window)!;
    }

    private static string GetRichText(MainWindow window, string fieldName)
    {
        var field = typeof(MainWindow).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(field);
        var richText = (RichTextBox)field!.GetValue(window)!;
        var range = new TextRange(richText.Document.ContentStart, richText.Document.ContentEnd);
        return range.Text.Trim();
    }

    private static void SetRichText(MainWindow window, string fieldName, string value)
    {
        var field = typeof(MainWindow).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(field);
        var richText = (RichTextBox)field!.GetValue(window)!;
        richText.Document.Blocks.Clear();
        richText.Document.Blocks.Add(new Paragraph(new Run(value)));
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
