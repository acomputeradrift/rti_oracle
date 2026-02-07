using System.Windows.Controls;

namespace OracleByFPCLtd.UI.Controls;

public partial class FindBar : UserControl
{
    public FindBar()
    {
        InitializeComponent();
    }

    public TextBox FindTextBox => FindTextBoxControl;
    public Button FindPrevButton => FindPrevButtonControl;
    public Button FindNextButton => FindNextButtonControl;
    public Button FindClearButton => FindClearButtonControl;
    public TextBlock FindCountText => FindCountTextControl;
}
