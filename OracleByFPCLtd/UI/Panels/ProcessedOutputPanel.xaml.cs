using System;
using OracleByFPCLtd.UI.Controls;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace OracleByFPCLtd.UI.Panels;

public partial class ProcessedOutputPanel : UserControl
{
    public ProcessedOutputPanel()
    {
        InitializeComponent();

        LogOutputView.LogTextBox.Background = Brushes.Black;
        LogOutputView.LogTextBox.Foreground = Brushes.White;
        LogOutputView.LogTextBox.Cursor = TryLoadCursor("pack://application:,,,/OracleByFPCLtd;component/Resources/Cursors/white-arrow.cur")
            ?? Cursors.IBeam;
        LogOutputView.LogTextBox.Document.Blocks.Clear();
        LogOutputView.LogTextBox.Document.Blocks.Add(new Paragraph(new Run("No processed information available")));
    }

    public FindBar FindBar => FindBarControl;
    public LogOutputView LogOutputView => LogOutputViewControl;

    private static Cursor? TryLoadCursor(string resourceUri)
    {
        try
        {
            var streamInfo = Application.GetResourceStream(new Uri(resourceUri, UriKind.Absolute));
            return streamInfo == null ? null : new Cursor(streamInfo.Stream);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
