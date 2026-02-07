using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class MainWindowUiLayoutTests
{
    [Fact]
    public void MainWindowContainsPlannedUiElements()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();

            Assert.NotNull(window.FindName("AppLogoImage"));

            var connection = (OracleByFPCLtd.UI.Panels.ConnectionPanel)window.FindName("ConnectionPanel")!;
            Assert.NotNull(connection.ConnectHeaderText);
            Assert.NotNull(connection.IpTextBox);
            Assert.NotNull(connection.ConnectButton);
            Assert.NotNull(connection.DisconnectButton);
            Assert.NotNull(connection.DiscoverButton);
            Assert.NotNull(connection.DiscoveredCombo);
            Assert.NotNull(connection.StatusText);

            var projectData = (OracleByFPCLtd.UI.Panels.ProjectDataPanel)window.FindName("ProjectDataPanel")!;
            Assert.NotNull(projectData.ProjectDataHeaderText);
            Assert.NotNull(projectData.UploadProjectButton);
            Assert.NotNull(projectData.UploadAdditionalInfoButton);
            Assert.NotNull(projectData.RecentProjectComboBox);
            Assert.NotNull(projectData.ProjectPreviewButton);
            Assert.NotNull(projectData.AdditionalInfoFileNameText);

            var driverLogLevels = (OracleByFPCLtd.UI.Panels.DriverLogLevelsPanel)window.FindName("DriverLogLevelsPanel")!;
            Assert.NotNull(driverLogLevels.DriverLogLevelsToggleButton);

            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
            Assert.NotNull(diagnostics.FilterBar.FilterKeywordTextBox);
            Assert.NotNull(diagnostics.FilterBar.FilterStartTextBox);
            Assert.NotNull(diagnostics.FilterBar.FilterEndTextBox);
            Assert.NotNull(diagnostics.FilterBar.FilterStartPickerButton);
            Assert.NotNull(diagnostics.FilterBar.FilterEndPickerButton);
            Assert.NotNull(diagnostics.FilterBar.FilterStartHourCombo);
            Assert.NotNull(diagnostics.FilterBar.FilterStartMinuteCombo);
            Assert.NotNull(diagnostics.FilterBar.FilterEndHourCombo);
            Assert.NotNull(diagnostics.FilterBar.FilterEndMinuteCombo);
            Assert.NotNull(diagnostics.FilterBar.FilterApplyButton);
            Assert.NotNull(diagnostics.FilterBar.FilterClearButton);
            Assert.NotNull(diagnostics.FilterBar.FilterCountText);
            Assert.NotNull(diagnostics.FilterBar.DownloadLogsButton);
            Assert.NotNull(diagnostics.FilterBar.ClearDiagnosticsButton);

            Assert.NotNull(diagnostics.RawOutputPanel.FindBar.FindTextBox);
            Assert.NotNull(diagnostics.RawOutputPanel.FindBar.FindPrevButton);
            Assert.NotNull(diagnostics.RawOutputPanel.FindBar.FindNextButton);
            Assert.NotNull(diagnostics.RawOutputPanel.FindBar.FindClearButton);
            Assert.NotNull(diagnostics.RawOutputPanel.FindBar.FindCountText);
            Assert.NotNull(diagnostics.ProcessedOutputPanel.FindBar.FindTextBox);
            Assert.NotNull(diagnostics.ProcessedOutputPanel.FindBar.FindPrevButton);
            Assert.NotNull(diagnostics.ProcessedOutputPanel.FindBar.FindNextButton);
            Assert.NotNull(diagnostics.ProcessedOutputPanel.FindBar.FindClearButton);
            Assert.NotNull(diagnostics.ProcessedOutputPanel.FindBar.FindCountText);
        });
    }

    [Fact]
    public void LogOutputsShareNoWrapHorizontalScrollBehavior()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();

            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
            var rawLog = diagnostics.RawOutputPanel.LogOutputView.LogTextBox;
            var processedLog = diagnostics.ProcessedOutputPanel.LogOutputView.LogTextBox;

            Assert.Equal(ScrollBarVisibility.Auto, rawLog.HorizontalScrollBarVisibility);
            Assert.Equal(ScrollBarVisibility.Auto, processedLog.HorizontalScrollBarVisibility);
        });
    }

    [Fact]
    public void FilterFieldsHaveMinimumWidths()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow
            {
                Width = 1000,
                Height = 700
            };
            window.Show();
            window.UpdateLayout();

            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
            var keyword = diagnostics.FilterBar.FilterKeywordTextBox;
            var start = diagnostics.FilterBar.FilterStartTextBox;
            var end = diagnostics.FilterBar.FilterEndTextBox;

            Assert.True(keyword.ActualWidth >= 180);
            Assert.True(start.ActualWidth >= 160);
            Assert.True(end.ActualWidth >= 160);

            window.Hide();
        });
    }

    [Fact]
    public void HeaderLayoutOrdersLogoConnectionProjectData()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();

            var logo = (FrameworkElement)window.FindName("AppLogoImage")!;
            var connection = (FrameworkElement)window.FindName("ConnectBoxBorder")!;
            var projectData = (FrameworkElement)window.FindName("ProjectDataBorder")!;

            Assert.Equal(0, Grid.GetColumn(logo));
            Assert.Equal(1, Grid.GetColumn(connection));
            Assert.Equal(3, Grid.GetColumn(projectData));
        });
    }

    [Fact]
    public void DateTimePickersDisableWithoutLogsAndClampToLogRange()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow
            {
                Width = 1000,
                Height = 700
            };
            window.Show();
            window.UpdateLayout();

            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
            var startButton = diagnostics.FilterBar.FilterStartPickerButton;
            var endButton = diagnostics.FilterBar.FilterEndPickerButton;
            Assert.False(startButton.IsEnabled);
            Assert.False(endButton.IsEnabled);

            InvokeAppendLog(window, "1 [2026-01-24 10:00:00.000] test");
            InvokeAppendLog(window, "2 [2026-01-24 11:00:00.000] test");
            FlushLayout(window);

            Assert.True(startButton.IsEnabled);
            Assert.True(endButton.IsEnabled);

            var startCalendar = diagnostics.FilterBar.FilterStartCalendar;
            var startHour = diagnostics.FilterBar.FilterStartHourCombo;
            var startMinute = diagnostics.FilterBar.FilterStartMinuteCombo;
            startCalendar.SelectedDate = new DateTime(2026, 1, 23);
            startHour.SelectedItem = "00";
            startMinute.SelectedItem = "00";

            InvokeUpdateDateTimeTextFromPicker(window, "FilterStartTextBox", startCalendar, startHour, startMinute);

            var startText = diagnostics.FilterBar.FilterStartTextBox;
            Assert.Equal("2026-01-24 10:00", startText.Text);

            window.Hide();
        });
    }

    private static void InvokeAppendLog(MainWindow window, string line)
    {
        var method = typeof(MainWindow).GetMethod("AppendLog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { line, false });
    }

    private static void InvokeUpdateDateTimeTextFromPicker(MainWindow window, string textBoxName, System.Windows.Controls.Calendar calendar, ComboBox hourCombo, ComboBox minuteCombo)
    {
        var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
        var textBox = textBoxName == "FilterStartTextBox"
            ? diagnostics.FilterBar.FilterStartTextBox
            : diagnostics.FilterBar.FilterEndTextBox;
        var method = typeof(MainWindow).GetMethod("UpdateDateTimeTextFromPicker", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { textBox, calendar, hourCombo, minuteCombo, true });
    }

    private static void FlushLayout(FrameworkElement element)
    {
        element.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
        element.UpdateLayout();
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
