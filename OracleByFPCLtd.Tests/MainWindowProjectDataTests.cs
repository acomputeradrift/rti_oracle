using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
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
            var apexPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "TEST - System Manager v10.apex");

            var task = InvokeLoadProjectDataForProcessing(window, apexPath);
            task.GetAwaiter().GetResult();

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

    private static System.Threading.Tasks.Task InvokeLoadProjectDataForProcessing(MainWindow window, string path)
    {
        var method = typeof(MainWindow).GetMethod("LoadProjectDataForProcessingAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (System.Threading.Tasks.Task)method!.Invoke(window, new object[] { path })!;
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
