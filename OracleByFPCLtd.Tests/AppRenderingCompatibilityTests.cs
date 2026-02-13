using System;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using OracleByFPCLtd;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class AppRenderingCompatibilityTests
{
    [Fact]
    public void ConfigureRenderingCompatibilityForcesSoftwareRendering()
    {
        RunOnSta(() =>
        {
            App.ConfigureRenderingCompatibility();
            Assert.Equal(RenderMode.SoftwareOnly, RenderOptions.ProcessRenderMode);
        });
    }

    [Fact]
    public void ConfigureRenderingCompatibilitySetsDisableHardwareAccelerationSwitch()
    {
        RunOnSta(() =>
        {
            App.ConfigureRenderingCompatibility();
            Assert.True(AppContext.TryGetSwitch("Switch.System.Windows.Media.DisableHardwareAcceleration", out var enabled));
            Assert.True(enabled);
        });
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
