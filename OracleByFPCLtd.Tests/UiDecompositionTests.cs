using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using OracleByFPCLtd.UI.Controls;
using OracleByFPCLtd.UI.Panels;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class UiDecompositionTests
{
    [Fact]
    public void MainWindowHostsPanelControls()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();

            Assert.IsType<ConnectionPanel>(window.FindName("ConnectionPanel"));
            Assert.IsType<ProjectDataPanel>(window.FindName("ProjectDataPanel"));
            Assert.IsType<DriverLogLevelsPanel>(window.FindName("DriverLogLevelsPanel"));
            Assert.IsType<DiagnosticsPanel>(window.FindName("DiagnosticsPanel"));
        });
    }

    [Fact]
    public void ConnectionPanelExposesConnectionControls()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var panel = (ConnectionPanel)window.FindName("ConnectionPanel")!;

            Assert.NotNull(panel.ConnectButton);
            Assert.NotNull(panel.DisconnectButton);
            Assert.NotNull(panel.DiscoverButton);
            Assert.NotNull(panel.IpTextBox);
            Assert.NotNull(panel.DiscoveredCombo);
            Assert.NotNull(panel.StatusText);
        });
    }

    [Fact]
    public void ProjectDataPanelExposesProjectControls()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var panel = (ProjectDataPanel)window.FindName("ProjectDataPanel")!;

            Assert.NotNull(panel.UploadProjectButton);
            Assert.NotNull(panel.RecentProjectComboBox);
            Assert.NotNull(panel.ProjectPreviewButton);
            Assert.NotNull(panel.UploadAdditionalInfoButton);
            Assert.NotNull(panel.AdditionalInfoFileNameText);
        });
    }

    [Fact]
    public void DiagnosticsPanelContainsFilterAndOutputs()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var panel = (DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;

            Assert.NotNull(panel.FilterBar);
            Assert.NotNull(panel.RawOutputPanel);
            Assert.NotNull(panel.ProcessedOutputPanel);
        });
    }

    [Fact]
    public void DriverLogLevelsPanelExposesPresetButtons()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var panel = (DriverLogLevelsPanel)window.FindName("DriverLogLevelsPanel")!;

            Assert.NotNull(panel.AllLogLevelsButton);
            Assert.NotNull(panel.SystemOnlyLogLevelsButton);
            Assert.NotNull(panel.NoneLogLevelsButton);
        });
    }

    [Fact]
    public void FilterBarExposesFilterControls()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var panel = (DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
            var bar = panel.FilterBar;

            Assert.NotNull(bar.FilterKeywordTextBox);
            Assert.NotNull(bar.FilterStartTextBox);
            Assert.NotNull(bar.FilterEndTextBox);
            Assert.NotNull(bar.FilterStartPickerButton);
            Assert.NotNull(bar.FilterEndPickerButton);
            Assert.NotNull(bar.FilterStartHourCombo);
            Assert.NotNull(bar.FilterStartMinuteCombo);
            Assert.NotNull(bar.FilterEndHourCombo);
            Assert.NotNull(bar.FilterEndMinuteCombo);
            Assert.NotNull(bar.FilterApplyButton);
            Assert.NotNull(bar.FilterClearButton);
            Assert.NotNull(bar.FilterCountText);
            Assert.NotNull(bar.ClearDiagnosticsButton);
        });
    }

    [Fact]
    public void OutputPanelsExposeFindBarAndLogView()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var panel = (DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;

            AssertFindBar(panel.RawOutputPanel.FindBar);
            AssertFindBar(panel.ProcessedOutputPanel.FindBar);

            Assert.NotNull(panel.RawOutputPanel.LogOutputView.LogTextBox);
            Assert.NotNull(panel.ProcessedOutputPanel.LogOutputView.LogTextBox);
        });
    }

    private static void AssertFindBar(FindBar bar)
    {
        Assert.NotNull(bar.FindTextBox);
        Assert.NotNull(bar.FindPrevButton);
        Assert.NotNull(bar.FindNextButton);
        Assert.NotNull(bar.FindClearButton);
        Assert.NotNull(bar.FindCountText);
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
