using OracleByFPCLtd.UI.Controls;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace OracleByFPCLtd.UI.Panels;

public partial class ProcessedOutputPanel : UserControl
{
    public ProcessedOutputPanel()
    {
        InitializeComponent();

        LogOutputView.LogTextBox.Background = Brushes.Black;
        LogOutputView.LogTextBox.Foreground = Brushes.White;
        LogOutputView.LogTextBox.Document.Blocks.Clear();
        LogOutputView.LogTextBox.Document.Blocks.Add(new Paragraph(new Run("No processed information available")));
    }

    public FindBar FindBar => FindBarControl;
    public LogOutputView LogOutputView => LogOutputViewControl;
}
