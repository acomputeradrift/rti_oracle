using System;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
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

            var processed = GetTextBox(window, "ProcessedLogTextBox").Text;
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

            var processed = GetTextBox(window, "ProcessedLogTextBox").Text;
            Assert.DoesNotContain("No processed information available", processed);
            Assert.Contains("Change to page \"Room Select\"", processed);
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

    private static void InvokeAppendLog(MainWindow window, string line)
    {
        var method = typeof(MainWindow).GetMethod("AppendLog", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { line, false });
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
