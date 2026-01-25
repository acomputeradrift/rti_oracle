using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using SHPDiagnosticsViewer.ProjectData;

namespace SHPDiagnosticsViewer;

public partial class ProjectDataPreviewWindow : Window
{
    private readonly string _apexPath;
    private readonly IProjectDataExtractor _extractor;
    private ApexDiscoveryPreloadResult? _preload;

    public ObservableCollection<DiagnosticsMappingEntry> DiagnosticsMapping { get; } = new();
    public ObservableCollection<DriverConfigMapEntry> DriverConfigEntries { get; } = new();
    public ObservableCollection<SysVarRefMapEntry> SysVarRefMapEntries { get; } = new();

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

            DriverConfigEntries.Clear();
            foreach (var driverEntry in result.ApexDiscoveryPreload.DriverConfigMap)
            {
                foreach (var configEntry in driverEntry.Value.Config)
                {
                    var resolvedVariableName = ResolveSysVarName(result.ApexDiscoveryPreload, driverEntry.Value.DeviceName, configEntry.Value);
                    DriverConfigEntries.Add(new DriverConfigMapEntry(
                        driverEntry.Key,
                        driverEntry.Value.DeviceName,
                        driverEntry.Value.DeviceDisplayName,
                        configEntry.Key,
                        configEntry.Value,
                        resolvedVariableName));
                }
            }

            SysVarRefMapEntries.Clear();
            foreach (var entry in result.ApexDiscoveryPreload.SysVarRefMap)
            {
                SysVarRefMapEntries.Add(new SysVarRefMapEntry(
                    entry.Key,
                    entry.Value.DriverDeviceId,
                    entry.Value.DriverName ?? "",
                    entry.Value.DeviceId,
                    entry.Value.VariableName ?? ""));
            }

            _preload = result.ApexDiscoveryPreload;
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

    private void DownloadDiagnosticsMapping_Click(object sender, RoutedEventArgs e)
    {
        if (DiagnosticsMapping.Count == 0)
        {
            MessageBox.Show(this, "No diagnostics mapping rows available.", "Project Data Preview", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx|All files (*.*)|*.*",
            FileName = "diagnostics_mapping.xlsx",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            if (_preload is null)
            {
                MessageBox.Show(this, "Preload data not available.", "Project Data Preview", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DiagnosticsMappingExporter.Export(dialog.FileName, DiagnosticsMapping, _preload);
            MessageBox.Show(this, $"Saved to {Path.GetFileName(dialog.FileName)}", "Project Data Preview", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Project Data Preview", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string ResolveSysVarName(ApexDiscoveryPreloadResult preload, string driverName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var normalized = value.StartsWith("SYSVARREF:", StringComparison.OrdinalIgnoreCase)
            ? value
            : $"SYSVARREF:{value}";
        if (preload.SysVarRefMap.TryGetValue(normalized, out var entry) && !string.IsNullOrWhiteSpace(entry.VariableName))
        {
            var sysvarDriverName = entry.DriverName ?? "";
            return string.IsNullOrWhiteSpace(sysvarDriverName) ? entry.VariableName : $"{sysvarDriverName}: {entry.VariableName}";
        }

        return "";
    }

    public sealed record DriverConfigMapEntry(
        int DriverDeviceId,
        string DeviceName,
        string DeviceDisplayName,
        string Key,
        string Value,
        string ResolvedVariableName);
    public sealed record SysVarRefMapEntry(string SysVarRef, int? DriverDeviceId, string DriverName, int? DeviceId, string VariableName);
}
