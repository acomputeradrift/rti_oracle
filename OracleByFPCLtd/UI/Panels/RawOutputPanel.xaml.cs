using OracleByFPCLtd.UI.Controls;
using System.Windows.Controls;

namespace OracleByFPCLtd.UI.Panels;

public partial class RawOutputPanel : UserControl
{
    public RawOutputPanel()
    {
        InitializeComponent();
    }

    public FindBar FindBar => FindBarControl;
    public LogOutputView LogOutputView => LogOutputViewControl;
}
