using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace SHPDiagnosticsViewer.Tests;

public sealed class MainWindowUiLayoutTests
{
    [Fact]
    public void MainWindowContainsPlannedUiElements()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();

            Assert.NotNull(window.FindName("AppLogoImage"));
            Assert.NotNull(window.FindName("IpTextBox"));
            Assert.NotNull(window.FindName("ConnectHeaderText"));
            Assert.NotNull(window.FindName("ProjectDataHeaderText"));
            Assert.NotNull(window.FindName("UploadProjectButton"));
            Assert.NotNull(window.FindName("UploadAdditionalInfoButton"));
            Assert.NotNull(window.FindName("RecentProjectComboBox"));
            Assert.NotNull(window.FindName("ProjectPreviewButton"));
            Assert.NotNull(window.FindName("AdditionalInfoFileNameText"));
            Assert.NotNull(window.FindName("DriverLogLevelsToggleButton"));
            Assert.NotNull(window.FindName("FilterKeywordTextBox"));
            Assert.NotNull(window.FindName("FilterStartTextBox"));
            Assert.NotNull(window.FindName("FilterEndTextBox"));
            Assert.NotNull(window.FindName("FilterStartPickerButton"));
            Assert.NotNull(window.FindName("FilterEndPickerButton"));
            Assert.NotNull(window.FindName("FilterStartHourCombo"));
            Assert.NotNull(window.FindName("FilterStartMinuteCombo"));
            Assert.NotNull(window.FindName("FilterEndHourCombo"));
            Assert.NotNull(window.FindName("FilterEndMinuteCombo"));
            Assert.NotNull(window.FindName("FilterApplyButton"));
            Assert.NotNull(window.FindName("FilterClearButton"));
            Assert.NotNull(window.FindName("FilterCountText"));
            Assert.NotNull(window.FindName("DownloadLogsButton"));
            Assert.NotNull(window.FindName("ClearDiagnosticsButton"));
            Assert.NotNull(window.FindName("RawFindTextBox"));
            Assert.NotNull(window.FindName("RawFindPrevButton"));
            Assert.NotNull(window.FindName("RawFindNextButton"));
            Assert.NotNull(window.FindName("RawFindClearButton"));
            Assert.NotNull(window.FindName("RawFindCountText"));
            Assert.NotNull(window.FindName("ProcessedFindTextBox"));
            Assert.NotNull(window.FindName("ProcessedFindPrevButton"));
            Assert.NotNull(window.FindName("ProcessedFindNextButton"));
            Assert.NotNull(window.FindName("ProcessedFindClearButton"));
            Assert.NotNull(window.FindName("ProcessedFindCountText"));
        });
    }

    [Fact]
    public void LogOutputsShareNoWrapHorizontalScrollBehavior()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();

            var rawLog = (RichTextBox)window.FindName("RawLogTextBox")!;
            var processedLog = (RichTextBox)window.FindName("ProcessedLogTextBox")!;

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

            var keyword = (TextBox)window.FindName("FilterKeywordTextBox")!;
            var start = (TextBox)window.FindName("FilterStartTextBox")!;
            var end = (TextBox)window.FindName("FilterEndTextBox")!;

            Assert.True(keyword.ActualWidth >= 180);
            Assert.True(start.ActualWidth >= 160);
            Assert.True(end.ActualWidth >= 160);

            window.Hide();
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

            var startButton = (Button)window.FindName("FilterStartPickerButton")!;
            var endButton = (Button)window.FindName("FilterEndPickerButton")!;
            Assert.False(startButton.IsEnabled);
            Assert.False(endButton.IsEnabled);

            InvokeAppendLog(window, "1 [2026-01-24 10:00:00.000] test");
            InvokeAppendLog(window, "2 [2026-01-24 11:00:00.000] test");
            FlushLayout(window);

            Assert.True(startButton.IsEnabled);
            Assert.True(endButton.IsEnabled);

            var startCalendar = (System.Windows.Controls.Calendar)window.FindName("FilterStartCalendar")!;
            var startHour = (ComboBox)window.FindName("FilterStartHourCombo")!;
            var startMinute = (ComboBox)window.FindName("FilterStartMinuteCombo")!;
            startCalendar.SelectedDate = new DateTime(2026, 1, 23);
            startHour.SelectedItem = "00";
            startMinute.SelectedItem = "00";

            InvokeUpdateDateTimeTextFromPicker(window, "FilterStartTextBox", startCalendar, startHour, startMinute);

            var startText = (TextBox)window.FindName("FilterStartTextBox")!;
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
        var textBox = (TextBox)window.FindName(textBoxName)!;
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
