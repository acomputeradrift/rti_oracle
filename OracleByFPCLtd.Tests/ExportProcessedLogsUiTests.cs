using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows;
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
            processedLines.Add("1 Line one");
            processedLines.Add("2 Line two");

            SetProjectFilePath(window, @"C:\Projects\Project.apex");
            var projectPanel = (OracleByFPCLtd.UI.Panels.ProjectDataPanel)window.FindName("ProjectDataPanel")!;
            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;

            projectPanel.AdditionalInfoFileNameText.Text = "Additional.xlsx";
            diagnostics.FilterBar.FilterKeywordTextBox.Text = "Driver";
            diagnostics.FilterBar.FilterStartTextBox.Text = "2026-01-24 10:00";
            diagnostics.FilterBar.FilterEndTextBox.Text = "2026-01-24 11:00";

            var request = InvokeBuildExportRequest(window);

            Assert.Equal("Project.apex", request.Metadata.ApexFileName);
            Assert.Equal("Additional.xlsx", request.Metadata.AdditionalDataName);
            Assert.Equal("Driver", request.FilterSummary.Keywords);
            Assert.Equal("2026-01-24 10:00", request.FilterSummary.Start);
            Assert.Equal("2026-01-24 11:00", request.FilterSummary.End);
            Assert.Equal(2, request.Lines.Count);
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
