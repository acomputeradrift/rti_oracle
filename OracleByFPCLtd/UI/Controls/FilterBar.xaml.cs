using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace OracleByFPCLtd.UI.Controls;

public partial class FilterBar : UserControl
{
    public FilterBar()
    {
        InitializeComponent();
    }

    public TextBox FilterKeywordTextBox => FilterKeywordTextBoxControl;
    public TextBox FilterStartTextBox => FilterStartTextBoxControl;
    public TextBox FilterEndTextBox => FilterEndTextBoxControl;
    public Button FilterStartPickerButton => FilterStartPickerButtonControl;
    public Button FilterEndPickerButton => FilterEndPickerButtonControl;
    public ComboBox FilterStartHourCombo => FilterStartHourComboControl;
    public ComboBox FilterStartMinuteCombo => FilterStartMinuteComboControl;
    public ComboBox FilterEndHourCombo => FilterEndHourComboControl;
    public ComboBox FilterEndMinuteCombo => FilterEndMinuteComboControl;
    public Button FilterApplyButton => FilterApplyButtonControl;
    public Button FilterClearButton => FilterClearButtonControl;
    public TextBlock FilterCountText => FilterCountTextControl;
    public Popup FilterStartDatePopup => FilterStartDatePopupControl;
    public Popup FilterEndDatePopup => FilterEndDatePopupControl;
    public Calendar FilterStartCalendar => FilterStartCalendarControl;
    public Calendar FilterEndCalendar => FilterEndCalendarControl;
    public Button ClearDiagnosticsButton => ClearDiagnosticsButtonControl;
}
