using System.Windows.Controls;

namespace OracleByFPCLtd.UI.Panels;

public partial class ConnectionPanel : UserControl
{
    public ConnectionPanel()
    {
        InitializeComponent();
    }

    public TextBlock ConnectHeaderText => ConnectHeaderTextControl;
    public TextBox IpTextBox => IpTextBoxControl;
    public Button ConnectButton => ConnectButtonControl;
    public Button DisconnectButton => DisconnectButtonControl;
    public Button DiscoverButton => DiscoverButtonControl;
    public ComboBox DiscoveredCombo => DiscoveredComboControl;
    public TextBlock StatusText => StatusTextControl;
}
