using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace OracleByFPCLtd;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ConfigureRenderingCompatibility();
        base.OnStartup(e);
    }

    public static void ConfigureRenderingCompatibility()
    {
        AppContext.SetSwitch("Switch.System.Windows.Media.DisableHardwareAcceleration", true);
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
    }
}
