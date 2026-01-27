using System;
using System.Threading;
using System.Windows;
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
            Assert.NotNull(window.FindName("IpComboBox"));
            Assert.NotNull(window.FindName("ProjectDataHeaderText"));
            Assert.NotNull(window.FindName("UploadProjectButton"));
            Assert.NotNull(window.FindName("UploadAdditionalInfoButton"));
            Assert.NotNull(window.FindName("ProjectFileNameText"));
            Assert.NotNull(window.FindName("AdditionalInfoFileNameText"));
            Assert.NotNull(window.FindName("DriverLogLevelsToggleButton"));
            Assert.NotNull(window.FindName("FilterKeywordTextBox"));
            Assert.NotNull(window.FindName("FilterStartTextBox"));
            Assert.NotNull(window.FindName("FilterEndTextBox"));
            Assert.NotNull(window.FindName("FilterApplyButton"));
            Assert.NotNull(window.FindName("FilterClearButton"));
            Assert.NotNull(window.FindName("FilterCountText"));
            Assert.NotNull(window.FindName("DownloadLogsButton"));
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
