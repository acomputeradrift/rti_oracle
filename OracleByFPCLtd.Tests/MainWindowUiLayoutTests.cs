using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.IO;
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
            Assert.NotNull(window.FindName("DownloadProcessedLogsMenuItemControl"));
            Assert.NotNull(window.FindName("DownloadAdditionalInfoTemplateMenuItemControl"));
            Assert.NotNull(window.FindName("AutoscrollMenuItemControl"));
            Assert.NotNull(window.FindName("AboutMenuItemControl"));
            Assert.NotNull(window.FindName("ReprocessingOverlay"));
            Assert.NotNull(window.FindName("ReprocessingStatusText"));
            Assert.NotNull(window.FindName("ReprocessingProgressBar"));

            var connection = (OracleByFPCLtd.UI.Panels.ConnectionPanel)window.FindName("ConnectionPanel")!;
            Assert.NotNull(connection.ConnectHeaderText);
            Assert.NotNull(connection.IpTextBox);
            Assert.NotNull(connection.ConnectButton);
            Assert.NotNull(connection.DisconnectButton);
            Assert.NotNull(connection.DiscoverButton);
            Assert.NotNull(connection.DiscoveredCombo);
            Assert.NotNull(connection.StatusText);

            var statusPanel = (OracleByFPCLtd.UI.Panels.StatusPanel)window.FindName("StatusPanel")!;
            Assert.NotNull(statusPanel.StatusHeaderText);
            Assert.NotNull(statusPanel.StatusOutputTextBox);

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
            Assert.NotNull(diagnostics.FilterBar.FilterStartPeriodCombo);
            Assert.NotNull(diagnostics.FilterBar.FilterEndHourCombo);
            Assert.NotNull(diagnostics.FilterBar.FilterEndMinuteCombo);
            Assert.NotNull(diagnostics.FilterBar.FilterEndPeriodCombo);
            Assert.NotNull(diagnostics.FilterBar.FilterApplyButton);
            Assert.NotNull(diagnostics.FilterBar.FilterClearButton);
            Assert.NotNull(diagnostics.FilterBar.FilterCountText);
            Assert.NotNull(diagnostics.FilterBar.ClearDiagnosticsButton);
            Assert.NotNull(diagnostics.FilterBar.DiagnosticsHeaderText);
            Assert.NotNull(diagnostics.FilterBar.DiagnosticsZoomOutButton);
            Assert.NotNull(diagnostics.FilterBar.DiagnosticsZoomResetButton);
            Assert.NotNull(diagnostics.FilterBar.DiagnosticsZoomInButton);

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

            Assert.Equal(180, Math.Round(keyword.ActualWidth));
            Assert.Equal(156, Math.Round(start.ActualWidth));
            Assert.Equal(156, Math.Round(end.ActualWidth));

            window.Hide();
        });
    }

    [Fact]
    public void DiagnosticsZoomDefaultsToOneHundredPercent()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();

            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
            Assert.Equal("100%", diagnostics.FilterBar.DiagnosticsZoomResetButton.Content);
            Assert.Equal(12, diagnostics.RawOutputPanel.LogOutputView.LogTextBox.FontSize);
            Assert.Equal(12, diagnostics.ProcessedOutputPanel.LogOutputView.LogTextBox.FontSize);
        });
    }

    [Fact]
    public void DiagnosticsZoomButtonsAdjustAndClampLogFontSize()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
            var zoomOut = diagnostics.FilterBar.DiagnosticsZoomOutButton;
            var zoomIn = diagnostics.FilterBar.DiagnosticsZoomInButton;
            var zoomReset = diagnostics.FilterBar.DiagnosticsZoomResetButton;
            var rawLog = diagnostics.RawOutputPanel.LogOutputView.LogTextBox;
            var processedLog = diagnostics.ProcessedOutputPanel.LogOutputView.LogTextBox;

            zoomIn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal("101%", zoomReset.Content);
            Assert.Equal(12.12, rawLog.FontSize, 2);
            Assert.Equal(12.12, processedLog.FontSize, 2);

            for (var i = 0; i < 50; i++)
            {
                zoomOut.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }

            Assert.Equal("75%", zoomReset.Content);
            Assert.Equal(9.0, rawLog.FontSize, 2);
            Assert.Equal(9.0, processedLog.FontSize, 2);

            for (var i = 0; i < 100; i++)
            {
                zoomIn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }

            Assert.Equal("125%", zoomReset.Content);
            Assert.Equal(15.0, rawLog.FontSize, 2);
            Assert.Equal(15.0, processedLog.FontSize, 2);

            zoomReset.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal("100%", zoomReset.Content);
            Assert.Equal(12.0, rawLog.FontSize, 2);
            Assert.Equal(12.0, processedLog.FontSize, 2);
        });
    }

    [Fact]
    public void HeaderLayoutOrdersLogoStatusConnectionProjectData()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();

            var logo = (FrameworkElement)window.FindName("AppLogoImage")!;
            var status = (FrameworkElement)window.FindName("StatusBorder")!;
            var connection = (FrameworkElement)window.FindName("ConnectBoxBorder")!;
            var projectData = (FrameworkElement)window.FindName("ProjectDataBorder")!;

            Assert.Equal(0, Grid.GetColumn(logo));
            Assert.Equal(2, Grid.GetColumn(status));
            Assert.Equal(4, Grid.GetColumn(connection));
            Assert.Equal(6, Grid.GetColumn(projectData));
        });
    }

    [Fact]
    public void DriverLogLevelsDoesNotAddExpandedGapWhenNoDriversExist()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var panel = (OracleByFPCLtd.UI.Panels.DriverLogLevelsPanel)window.FindName("DriverLogLevelsPanel")!;
            var expandedHost = (Grid)panel.FindName("DriverLogLevelsExpandedHost")!;

            panel.DriverLogLevelsToggleButton.IsChecked = true;
            window.UpdateLayout();

            Assert.Equal(new Thickness(0), expandedHost.Margin);

            panel.SetDriverCount(1);
            window.UpdateLayout();

            Assert.Equal(new Thickness(0, 6, 0, 0), expandedHost.Margin);
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
            var startPeriod = diagnostics.FilterBar.FilterStartPeriodCombo;
            startCalendar.SelectedDate = new DateTime(2026, 1, 23);
            startHour.SelectedItem = "12";
            startMinute.SelectedItem = "00";
            startPeriod.SelectedItem = "AM";

            InvokeUpdateDateTimeTextFromPicker(window, "FilterStartTextBox", startCalendar, startHour, startMinute, startPeriod);

            var startText = diagnostics.FilterBar.FilterStartTextBox;
            Assert.Equal("26-01-24 10:00 AM", startText.Text);

            window.Hide();
        });
    }

    [Fact]
    public void AdditionalInfoGuidanceTooltipAppearsOnlyAfterApexUpload()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var projectData = (OracleByFPCLtd.UI.Panels.ProjectDataPanel)window.FindName("ProjectDataPanel")!;

            Assert.Null(projectData.AdditionalInfoFileNameText.ToolTip);

            InvokeSetPrivateField(window, "_apexUploaded", true);
            InvokeUpdateAdditionalInfoGuidanceTooltip(window);

            Assert.Equal(
                "See generated project template for Additional Info under File menu",
                projectData.AdditionalInfoFileNameText.ToolTip);

            projectData.AdditionalInfoFileNameText.Text = "info.xlsx";
            InvokeUpdateAdditionalInfoGuidanceTooltip(window);
            Assert.Null(projectData.AdditionalInfoFileNameText.ToolTip);
        });
    }

    [Fact]
    public void ProjectDataActionsRequireApexBeforeAdditionalInfoUpload()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var connection = (OracleByFPCLtd.UI.Panels.ConnectionPanel)window.FindName("ConnectionPanel")!;
            var projectData = (OracleByFPCLtd.UI.Panels.ProjectDataPanel)window.FindName("ProjectDataPanel")!;

            Assert.Equal("Upload Apex file first", connection.ConnectButton.ToolTip);
            Assert.False(projectData.UploadAdditionalInfoButton.IsEnabled);
            Assert.Equal("Upload Apex file first", projectData.UploadAdditionalInfoButton.ToolTip);

            InvokeSetPrivateField(window, "_apexUploaded", true);
            InvokeUpdateProjectDataActionStates(window);

            Assert.Null(connection.ConnectButton.ToolTip);
            Assert.True(projectData.UploadAdditionalInfoButton.IsEnabled);
            Assert.Null(projectData.UploadAdditionalInfoButton.ToolTip);
        });
    }

    [Fact]
    public void DateTimePickerWritesTwelveHourAmPmFormat()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;

            var startCalendar = diagnostics.FilterBar.FilterStartCalendar;
            var startHour = diagnostics.FilterBar.FilterStartHourCombo;
            var startMinute = diagnostics.FilterBar.FilterStartMinuteCombo;
            var startPeriod = diagnostics.FilterBar.FilterStartPeriodCombo;
            startCalendar.SelectedDate = new DateTime(2026, 1, 24);
            startHour.SelectedItem = "1";
            startMinute.SelectedItem = "15";
            startPeriod.SelectedItem = "PM";

            InvokeUpdateDateTimeTextFromPicker(window, "FilterStartTextBox", startCalendar, startHour, startMinute, startPeriod);

            Assert.Equal("26-01-24 1:15 PM", diagnostics.FilterBar.FilterStartTextBox.Text);
        });
    }

    [Fact]
    public void SyncPickerFromTextUsesTwelveHourSelections()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;

            var startCalendar = diagnostics.FilterBar.FilterStartCalendar;
            var startHour = diagnostics.FilterBar.FilterStartHourCombo;
            var startMinute = diagnostics.FilterBar.FilterStartMinuteCombo;
            var startPeriod = diagnostics.FilterBar.FilterStartPeriodCombo;

            InvokeSyncPickerFromText(window, "26-01-24 1:15 PM", startCalendar, startHour, startMinute, startPeriod);

            Assert.Equal(new DateTime(2026, 1, 24), startCalendar.SelectedDate);
            Assert.Equal("1", startHour.SelectedItem);
            Assert.Equal("15", startMinute.SelectedItem);
            Assert.Equal("PM", startPeriod.SelectedItem);
        });
    }

    [Fact]
    public void StartFilterAutoPopulatesFromFirstLogTimestamp()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;

            Assert.Equal(string.Empty, diagnostics.FilterBar.FilterStartTextBox.Text);

            InvokeAppendLog(window, "1 [2026-01-24 10:15:00.000] test");

            Assert.Equal("26-01-24 10:15 AM", diagnostics.FilterBar.FilterStartTextBox.Text);
        });
    }

    [Fact]
    public void StartPickerDoesNotOfferChoicesBeforeFirstLogTime()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;

            InvokeAppendLog(window, "1 [2026-01-24 10:15:00.000] test");
            InvokeAppendLog(window, "2 [2026-01-24 11:45:00.000] test");
            FlushLayout(window);

            diagnostics.FilterBar.FilterStartPickerButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            var periods = diagnostics.FilterBar.FilterStartPeriodCombo.Items.Cast<string>().ToList();
            var hours = diagnostics.FilterBar.FilterStartHourCombo.Items.Cast<string>().ToList();
            var minutes = diagnostics.FilterBar.FilterStartMinuteCombo.Items.Cast<string>().ToList();

            Assert.Single(periods);
            Assert.Equal("AM", periods[0]);
            Assert.DoesNotContain("9", hours);
            Assert.Contains("10", hours);
            Assert.Contains("11", hours);
            Assert.DoesNotContain("14", minutes);
            Assert.Contains("15", minutes);
        });
    }

    [Fact]
    public void StartPickerButtonTogglesPopupOpen()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
            window.Show();
            window.UpdateLayout();

            InvokeAppendLog(window, "1 [2026-01-24 10:15:00.000] test");
            FlushLayout(window);

            diagnostics.FilterBar.FilterStartPickerButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            FlushLayout(window);
            Assert.True(diagnostics.FilterBar.FilterStartDatePopup.IsOpen);

            diagnostics.FilterBar.FilterStartPickerButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            FlushLayout(window);
            Assert.False(diagnostics.FilterBar.FilterStartDatePopup.IsOpen);

            window.Hide();
        });
    }

    [Fact]
    public void EndPickerDefaultsToLatestLogTimestampAndAllowsAvailableRange()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
            window.Show();
            window.UpdateLayout();

            InvokeAppendLog(window, "1 [2026-01-24 10:15:00.000] test");
            InvokeAppendLog(window, "2 [2026-01-24 11:45:00.000] test");
            FlushLayout(window);

            diagnostics.FilterBar.FilterEndPickerButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.True(diagnostics.FilterBar.FilterEndDatePopup.IsOpen);
            Assert.Equal(new DateTime(2026, 1, 24), diagnostics.FilterBar.FilterEndCalendar.SelectedDate);
            Assert.Equal("11", diagnostics.FilterBar.FilterEndHourCombo.SelectedItem);
            Assert.Equal("45", diagnostics.FilterBar.FilterEndMinuteCombo.SelectedItem);
            Assert.Equal("AM", diagnostics.FilterBar.FilterEndPeriodCombo.SelectedItem);

            var hours = diagnostics.FilterBar.FilterEndHourCombo.Items.Cast<string>().ToList();
            Assert.Contains("10", hours);
            Assert.Contains("11", hours);

            diagnostics.FilterBar.FilterEndHourCombo.SelectedItem = "10";
            var minutes = diagnostics.FilterBar.FilterEndMinuteCombo.Items.Cast<string>().ToList();
            Assert.Contains("15", minutes);
            Assert.Contains("59", minutes);

            window.Hide();
        });
    }

    [Fact]
    public void FilterApplyAndClearCloseAndResetDatePopups()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
            window.Show();
            window.UpdateLayout();

            InvokeAppendLog(window, "1 [2026-01-24 10:15:00.000] test");
            InvokeAppendLog(window, "2 [2026-01-24 11:45:00.000] test");
            FlushLayout(window);

            diagnostics.FilterBar.FilterStartPickerButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            diagnostics.FilterBar.FilterEndPickerButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.True(diagnostics.FilterBar.FilterEndDatePopup.IsOpen);

            diagnostics.FilterBar.FilterKeywordTextBox.Text = "test";
            diagnostics.FilterBar.FilterStartTextBox.Text = "26-01-24 10:15 AM";
            diagnostics.FilterBar.FilterEndTextBox.Text = "26-01-24 11:45 AM";

            diagnostics.FilterBar.FilterApplyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.Equal("26-01-24 10:15 AM", diagnostics.FilterBar.FilterStartTextBox.Text);
            Assert.Equal("26-01-24 11:45 AM", diagnostics.FilterBar.FilterEndTextBox.Text);
            Assert.False(diagnostics.FilterBar.FilterStartDatePopup.IsOpen);
            Assert.False(diagnostics.FilterBar.FilterEndDatePopup.IsOpen);
            Assert.Null(diagnostics.FilterBar.FilterStartCalendar.SelectedDate);
            Assert.Null(diagnostics.FilterBar.FilterEndCalendar.SelectedDate);

            diagnostics.FilterBar.FilterStartPickerButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            diagnostics.FilterBar.FilterEndPickerButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.True(diagnostics.FilterBar.FilterEndDatePopup.IsOpen);

            diagnostics.FilterBar.FilterClearButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.False(diagnostics.FilterBar.FilterStartDatePopup.IsOpen);
            Assert.False(diagnostics.FilterBar.FilterEndDatePopup.IsOpen);
            Assert.Null(diagnostics.FilterBar.FilterStartCalendar.SelectedDate);
            Assert.Null(diagnostics.FilterBar.FilterEndCalendar.SelectedDate);

            window.Hide();
        });
    }

    [Fact]
    public void MainWindowDefaultEventLogFlushesAfterFirstProcessorLine()
    {
        var original = Environment.GetEnvironmentVariable("ORACLE_EVENT_LOG_DIRECTORY_OVERRIDE");
        var overrideDirectory = TestTempPaths.CreateDirectoryPath();

        try
        {
            Environment.SetEnvironmentVariable("ORACLE_EVENT_LOG_DIRECTORY_OVERRIDE", overrideDirectory);

            RunOnSta(() =>
            {
                OracleByFPCLtd.Logging.LogTimestampSource.Reset();
                var window = new MainWindow();

                var files = Directory.GetFiles(overrideDirectory, "*_oracle_event_logs.log");
                Assert.Single(files);
                var startupLog = File.ReadAllText(files[0]);
                Assert.Contains("------Local Time", startupLog, StringComparison.Ordinal);
                Assert.Contains("Settings loaded", startupLog, StringComparison.Ordinal);

                InvokeAppendLog(window, "1 [26-01-24 10:15:00.000 AM] test");

                var log = File.ReadAllText(files[0]);
                Assert.Contains("------Processor Time", log, StringComparison.Ordinal);
                Assert.Contains("Settings loaded", log, StringComparison.Ordinal);
            });
        }
        finally
        {
            OracleByFPCLtd.Logging.LogTimestampSource.Reset();
            Environment.SetEnvironmentVariable("ORACLE_EVENT_LOG_DIRECTORY_OVERRIDE", original);
            if (Directory.Exists(overrideDirectory))
            {
                Directory.Delete(overrideDirectory, recursive: true);
            }
        }
    }

    private static void InvokeAppendLog(MainWindow window, string line)
    {
        var method = typeof(MainWindow).GetMethod("AppendLog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { line, false });
    }

    private static void InvokeUpdateDateTimeTextFromPicker(MainWindow window, string textBoxName, System.Windows.Controls.Calendar calendar, ComboBox hourCombo, ComboBox minuteCombo, ComboBox periodCombo)
    {
        var diagnostics = (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
        var isStart = textBoxName == "FilterStartTextBox";
        var textBox = isStart
            ? diagnostics.FilterBar.FilterStartTextBox
            : diagnostics.FilterBar.FilterEndTextBox;
        var method = typeof(MainWindow).GetMethod("UpdateDateTimeTextFromPicker", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { textBox, calendar, hourCombo, minuteCombo, periodCombo, isStart });
    }

    private static void InvokeSyncPickerFromText(MainWindow window, string text, System.Windows.Controls.Calendar calendar, ComboBox hourCombo, ComboBox minuteCombo, ComboBox periodCombo, bool isStart = true)
    {
        var method = typeof(MainWindow).GetMethod("SyncPickerFromText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { text, calendar, hourCombo, minuteCombo, periodCombo, isStart });
    }

    private static void InvokeUpdateAdditionalInfoGuidanceTooltip(MainWindow window)
    {
        var method = typeof(MainWindow).GetMethod("UpdateAdditionalInfoGuidanceTooltip", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, Array.Empty<object>());
    }

    private static void InvokeUpdateProjectDataActionStates(MainWindow window)
    {
        var method = typeof(MainWindow).GetMethod("UpdateProjectDataActionStates", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, Array.Empty<object>());
    }

    private static void InvokeSetPrivateField(MainWindow window, string fieldName, object value)
    {
        var field = typeof(MainWindow).GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(window, value);
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
