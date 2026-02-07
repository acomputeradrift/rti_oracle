using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace OracleByFPCLtd.UI.Panels;

public partial class DriverLogLevelsPanel : UserControl
{
    public DriverLogLevelsPanel()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? DriverToggleClick;
    public event RoutedEventHandler? DriverLevelButtonClick;

    public ToggleButton DriverLogLevelsToggleButton => DriverLogLevelsToggleButtonControl;

    private void DriverToggle_Click(object sender, RoutedEventArgs e)
    {
        DriverToggleClick?.Invoke(sender, e);
    }

    private void DriverLevelButton_Click(object sender, RoutedEventArgs e)
    {
        DriverLevelButtonClick?.Invoke(sender, e);
    }
}
