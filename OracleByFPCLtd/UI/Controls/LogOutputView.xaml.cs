using System.Windows.Controls;

namespace OracleByFPCLtd.UI.Controls;

public partial class LogOutputView : UserControl
{
    public LogOutputView()
    {
        InitializeComponent();
    }

    public RichTextBox LogTextBox => LogTextBoxControl;
}
