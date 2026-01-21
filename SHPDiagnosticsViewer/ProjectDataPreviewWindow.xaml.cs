using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using SHPDiagnosticsViewer.ProjectData;

namespace SHPDiagnosticsViewer;

public partial class ProjectDataPreviewWindow : Window
{
    private readonly string _apexPath;
    private readonly IProjectDataExtractor _extractor;

    public ObservableCollection<DiagnosticsMappingEntry> DiagnosticsMapping { get; } = new();
    public ObservableCollection<ProjectReportEntry> ProjectReport { get; } = new();
    public ObservableCollection<ProjectTestEntry> ProjectTest { get; } = new();

    public ProjectDataPreviewWindow(string apexPath)
    {
        InitializeComponent();
        DataContext = this;
        _apexPath = apexPath;
        _extractor = new ProjectDataExtractor();
        Loaded += ProjectDataPreviewWindow_Loaded;
    }

    private async void ProjectDataPreviewWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var progress = new Progress<ProjectDataExtractionProgress>(UpdateProgress);
            var result = await Task.Run(() => _extractor.Extract(_apexPath, progress));

            DiagnosticsMapping.Clear();
            foreach (var entry in result.DiagnosticsMapping)
            {
                DiagnosticsMapping.Add(entry);
            }

            ProjectReport.Clear();
            foreach (var entry in result.ProjectReport)
            {
                ProjectReport.Add(entry);
            }

            ProjectTest.Clear();
            foreach (var entry in result.ProjectTest)
            {
                ProjectTest.Add(entry);
            }

            UpdateProgress(new ProjectDataExtractionProgress("Complete", 100));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Project Data Preview", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private void UpdateProgress(ProjectDataExtractionProgress progress)
    {
        StageText.Text = progress.Stage;
        ExtractionProgressBar.Value = progress.Percent;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
