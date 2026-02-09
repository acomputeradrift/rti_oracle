using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using OracleByFPCLtd.ExportProcessedLogs.Models;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class ExportProcessedLogsUiTests
{
    [Fact]
    public void BuildExportRequestUsesUiAndState()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();

            var processedLines = GetProcessedLines(window);
            processedLines.Add("1 [2026-01-24 10:15] Driver - Command: Line one");
            processedLines.Add("2 [2026-01-24 10:45] Driver event: Line two");

            SetProjectFilePath(window, @"C:\Projects\Project.apex");
            var projectPanel = (OracleByFPCLtd.UI.Panels.ProjectDataPanel)window.FindName("ProjectDataPanel")!;
            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;

            projectPanel.AdditionalInfoFileNameText.Text = "Additional.xlsx";
            diagnostics.FilterBar.FilterKeywordTextBox.Text = "Driver";
            diagnostics.FilterBar.FilterStartTextBox.Text = "2026-01-24 10:00";
            diagnostics.FilterBar.FilterEndTextBox.Text = "2026-01-24 11:00";
            InvokeFilterApply(window);

            var request = InvokeBuildExportRequest(window);

            Assert.Equal("Project.apex", request.Metadata.ApexFileName);
            Assert.Equal("Additional.xlsx", request.Metadata.AdditionalDataName);
            Assert.Equal("Driver", request.FilterSummary.Keywords);
            Assert.Equal("2026-01-24 10:00", request.FilterSummary.Start);
            Assert.Equal("2026-01-24 11:00", request.FilterSummary.End);
            Assert.Equal(2, request.Lines.Count);
        });
    }

    [Fact]
    public void BuildExportRequestHonorsFilters()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();

            var processedLines = GetProcessedLines(window);
            processedLines.Add("1 [2026-01-24 09:00] Driver - Command: Early");
            processedLines.Add("2 [2026-01-24 10:30] Driver event: Match");
            processedLines.Add("3 [2026-01-24 10:45] Macro - Start");

            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
            diagnostics.FilterBar.FilterKeywordTextBox.Text = "Driver";
            diagnostics.FilterBar.FilterStartTextBox.Text = "2026-01-24 10:00";
            diagnostics.FilterBar.FilterEndTextBox.Text = "2026-01-24 11:00";
            InvokeFilterApply(window);

            var request = InvokeBuildExportRequest(window);

            Assert.Single(request.Lines);
            Assert.Equal("2 [2026-01-24 10:30] Driver event: Match", request.Lines[0]);
        });
    }

    private static List<string> GetProcessedLines(MainWindow window)
    {
        var field = typeof(MainWindow).GetField("_processedLogLines", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (List<string>)field!.GetValue(window)!;
    }

    private static void SetProjectFilePath(MainWindow window, string path)
    {
        var field = typeof(MainWindow).GetField("_projectFilePath", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(window, path);
    }

    private static ExportRequest InvokeBuildExportRequest(MainWindow window)
    {
        var method = typeof(MainWindow).GetMethod("BuildExportRequest", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (ExportRequest)method!.Invoke(window, null)!;
    }

    private static void InvokeFilterApply(MainWindow window)
    {
        var method = typeof(MainWindow).GetMethod("FilterApplyButton_Click", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { window, new RoutedEventArgs() });
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
}
