using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class MainWindowProjectDataTests
{
    [Fact]
    public void ProjectUploadDoesNotAutoOpenPreview()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var apexPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "TEST - System Manager v10.apex");

            InvokeHandleProjectSelected(window, apexPath);

            var windows = Application.Current?.Windows.Cast<Window>() ?? Enumerable.Empty<Window>();
            Assert.DoesNotContain(windows, w => w.GetType().Name == "ProjectDataPreviewWindow");
        });
    }

    [Fact]
    public void ProjectUploadInitializesProcessing()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            window.InitializeProcessing(new OracleByFPCLtd.ProjectData.ProjectDataExtractionResult());

            var field = typeof(MainWindow).GetField("_processingEngine", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            Assert.NotNull(field!.GetValue(window));
        });
    }

    private static void InvokeHandleProjectSelected(MainWindow window, string path)
    {
        var method = typeof(MainWindow).GetMethod("HandleProjectSelected", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { path });
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
