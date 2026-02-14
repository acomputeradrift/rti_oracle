using System.Windows.Controls;

namespace OracleByFPCLtd.UI.Panels;

public partial class StatusPanel : UserControl
{
    public StatusPanel()
    {
        InitializeComponent();
    }

    public TextBlock StatusHeaderText => StatusHeaderTextControl;
    public RichTextBox StatusOutputTextBox => StatusOutputTextBoxControl;
}
