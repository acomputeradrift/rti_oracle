using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.ProjectData.Models;

namespace OracleByFPCLtd;

public partial class ProjectDataPreviewWindow : Window
{
    private readonly string _apexPath;
    private readonly IProjectDataExtractor _extractor;
    private readonly AdditionalData? _additionalData;
    private ApexDiscoveryPreloadResult? _preload;

    public ObservableCollection<DiagnosticsMappingEntry> DiagnosticsMapping { get; } = new();
    public ObservableCollection<DriverConfigMapEntry> DriverConfigEntries { get; } = new();
    public ObservableCollection<SysVarRefMapEntry> SysVarRefMapEntries { get; } = new();
    public ObservableCollection<PageMappingEntry> PageMappings { get; } = new();
    public ObservableCollection<RelayPortEntry> RelayPorts { get; } = new();
    public ObservableCollection<MpioIrPortEntry> MpioIrPorts { get; } = new();
    public ObservableCollection<SensePortEntry> SensePorts { get; } = new();
    public ObservableCollection<TriggerPortEntry> TriggerPorts { get; } = new();
    public ObservableCollection<Rs232PortEntry> Rs232Ports { get; } = new();
    public ObservableCollection<RoomMappingEntry> RoomMappings { get; } = new();
    public ObservableCollection<DriverTemplateVariableEntry> DriverTemplateVariables { get; } = new();
    public ObservableCollection<AdditionalInfoDisplayEntry> AdditionalInfoEntries { get; } = new();
    public ObservableCollection<string> AdditionalInfoErrors { get; } = new();

    public ProjectDataPreviewWindow(string apexPath, AdditionalData? additionalData = null)
    {
        InitializeComponent();
        DataContext = this;
        _apexPath = apexPath;
        _extractor = new ProjectDataExtractor();
        _additionalData = additionalData;
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

            PageMappings.Clear();
            foreach (var entry in result.ApexDiscoveryPreload.PageMappings)
            {
                PageMappings.Add(entry);
            }

            RelayPorts.Clear();
            foreach (var entry in result.ApexDiscoveryPreload.RelayPorts)
            {
                RelayPorts.Add(entry);
            }

            MpioIrPorts.Clear();
            foreach (var entry in result.ApexDiscoveryPreload.MpioIrPorts)
            {
                MpioIrPorts.Add(entry);
            }

            SensePorts.Clear();
            foreach (var entry in result.ApexDiscoveryPreload.SensePorts)
            {
                SensePorts.Add(entry);
            }

            TriggerPorts.Clear();
            foreach (var entry in result.ApexDiscoveryPreload.TriggerPorts)
            {
                TriggerPorts.Add(entry);
            }

            Rs232Ports.Clear();
            foreach (var entry in result.ApexDiscoveryPreload.Rs232Ports)
            {
                Rs232Ports.Add(entry);
            }

            RoomMappings.Clear();
            foreach (var entry in result.ApexDiscoveryPreload.RoomMappings)
            {
                RoomMappings.Add(entry);
            }

            DriverTemplateVariables.Clear();
            foreach (var entry in result.ApexDiscoveryPreload.DriverTemplateVariables)
            {
                DriverTemplateVariables.Add(entry);
            }

            LoadAdditionalInfoEntries();
            _preload = result.ApexDiscoveryPreload;
            if (Owner is MainWindow mainWindow)
            {
                mainWindow.InitializeProcessing(result);
            }
            UpdateProgress(new ProjectDataExtractionProgress("Complete", 100));
        }
        catch (Exception ex)
        {
            ReportStatus("FAIL", ex.Message);
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
            ReportStatus("INFO", "No diagnostics mapping rows available.");
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
                ReportStatus("FAIL", "Preload data not available.");
                return;
            }

            DiagnosticsMappingExporter.Export(dialog.FileName, DiagnosticsMapping, _preload);
            ReportStatus("INFO", $"Saved to {Path.GetFileName(dialog.FileName)}");
        }
        catch (Exception ex)
        {
            ReportStatus("FAIL", ex.Message);
        }
    }

    private void ReportStatus(string level, string message)
    {
        if (Owner is MainWindow main)
        {
            main.AppendStatusFromChild(level, $"Project Data Preview: {message}");
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

    private void LoadAdditionalInfoEntries()
    {
        AdditionalInfoEntries.Clear();
        AdditionalInfoErrors.Clear();
        if (_additionalData is null)
        {
            return;
        }

        foreach (var entry in AdditionalInfoDisplayBuilder.Build(_additionalData))
        {
            AdditionalInfoEntries.Add(entry);
        }

        foreach (var error in _additionalData.Errors)
        {
            AdditionalInfoErrors.Add(error);
        }
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
