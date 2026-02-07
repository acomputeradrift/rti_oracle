using OracleByFPCLtd.UI.Controls;
using System.Windows.Controls;

namespace OracleByFPCLtd.UI.Panels;

public partial class DiagnosticsPanel : UserControl
{
    public DiagnosticsPanel()
    {
        InitializeComponent();
    }

    public FilterBar FilterBar => FilterBarControl;
    public RawOutputPanel RawOutputPanel => RawOutputPanelControl;
    public ProcessedOutputPanel ProcessedOutputPanel => ProcessedOutputPanelControl;
}
