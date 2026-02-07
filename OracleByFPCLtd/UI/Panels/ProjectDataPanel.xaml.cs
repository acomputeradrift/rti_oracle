using System.Windows.Controls;

namespace OracleByFPCLtd.UI.Panels;

public partial class ProjectDataPanel : UserControl
{
    public ProjectDataPanel()
    {
        InitializeComponent();
    }

    public TextBlock ProjectDataHeaderText => ProjectDataHeaderTextControl;
    public Button UploadProjectButton => UploadProjectButtonControl;
    public ComboBox RecentProjectComboBox => RecentProjectComboBoxControl;
    public Button ProjectPreviewButton => ProjectPreviewButtonControl;
    public Button UploadAdditionalInfoButton => UploadAdditionalInfoButtonControl;
    public TextBlock AdditionalInfoFileNameText => AdditionalInfoFileNameTextControl;
}
