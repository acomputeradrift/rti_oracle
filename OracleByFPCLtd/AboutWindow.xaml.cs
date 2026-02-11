using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace OracleByFPCLtd;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var label = $"Oracle by FP&C {AppVersion.CurrentLabel()}";
        VersionTextBlock.Text = label;
        Title = $"About {AppVersion.CurrentLabel()}";
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
        {
            UseShellExecute = true
        });
        e.Handled = true;
    }
}
