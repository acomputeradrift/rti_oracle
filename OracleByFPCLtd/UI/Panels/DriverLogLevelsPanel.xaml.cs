using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace OracleByFPCLtd.UI.Panels;

public partial class DriverLogLevelsPanel : UserControl
{
    private int _visibleDriverCount;

    public DriverLogLevelsPanel()
    {
        InitializeComponent();
        DriverLogLevelsToggleButtonControl.Checked += (_, _) => UpdateExpandedHostSpacing();
        DriverLogLevelsToggleButtonControl.Unchecked += (_, _) => UpdateExpandedHostSpacing();
        UpdateExpandedHostSpacing();
    }

    public event RoutedEventHandler? DriverToggleClick;
    public event RoutedEventHandler? DriverLevelButtonClick;
    public event RoutedEventHandler? AllLogLevelsClick;
    public event RoutedEventHandler? SystemOnlyLogLevelsClick;
    public event RoutedEventHandler? NoneLogLevelsClick;

    public ToggleButton DriverLogLevelsToggleButton => DriverLogLevelsToggleButtonControl;
    public Button AllLogLevelsButton => AllLogLevelsButtonControl;
    public Button SystemOnlyLogLevelsButton => SystemOnlyLogLevelsButtonControl;
    public Button NoneLogLevelsButton => NoneLogLevelsButtonControl;
    public TextBlock DriverCountText => DriverCountTextBlockControl;

    private void DriverToggle_Click(object sender, RoutedEventArgs e)
    {
        DriverToggleClick?.Invoke(sender, e);
    }

    private void DriverLevelButton_Click(object sender, RoutedEventArgs e)
    {
        DriverLevelButtonClick?.Invoke(sender, e);
    }

    private void AllLogLevelsButton_Click(object sender, RoutedEventArgs e)
    {
        AllLogLevelsClick?.Invoke(sender, e);
    }

    private void SystemOnlyLogLevelsButton_Click(object sender, RoutedEventArgs e)
    {
        SystemOnlyLogLevelsClick?.Invoke(sender, e);
    }

    private void NoneLogLevelsButton_Click(object sender, RoutedEventArgs e)
    {
        NoneLogLevelsClick?.Invoke(sender, e);
    }

    public void UpdatePresetButtonSizing()
    {
        var buttons = new[] { AllLogLevelsButtonControl, SystemOnlyLogLevelsButtonControl, NoneLogLevelsButtonControl };
        foreach (var button in buttons)
        {
            button.Width = double.NaN;
            button.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        }

        var maxWidth = 0.0;
        foreach (var button in buttons)
        {
            if (button.DesiredSize.Width > maxWidth)
            {
                maxWidth = button.DesiredSize.Width;
            }
        }

        if (maxWidth <= 0)
        {
            return;
        }

        var rounded = Math.Ceiling(maxWidth);
        foreach (var button in buttons)
        {
            button.Width = rounded;
        }
    }

    public void SetDriverCount(int count)
    {
        _visibleDriverCount = Math.Max(0, count);
        DriverCountTextBlockControl.Text = _visibleDriverCount.ToString();
        UpdateExpandedHostSpacing();
    }

    private void UpdateExpandedHostSpacing()
    {
        DriverLogLevelsExpandedHost.Margin = _visibleDriverCount > 0
            ? new Thickness(0, 6, 0, 0)
            : new Thickness(0);
    }
}
