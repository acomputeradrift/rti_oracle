using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using SHPDiagnosticsViewer.DiagnosticsTransport;
using SHPDiagnosticsViewer.ProjectData;
using SHPDiagnosticsViewer.ProcessingEngine;

namespace SHPDiagnosticsViewer;

public partial class MainWindow : Window
{
    private const int MaxLogChars = 200_000;
    private const double DriverLogDefaultHeight = 160;
    private const string ProcessedPlaceholderText = "No processed information available";
    private IDiagnosticsTransport _transport;
    private bool _isConnecting;
    private bool _useTcpCapture;
    private int _rawLineNumber = 1;
    private bool _apexUploaded;
    private readonly WebSocketMessageFormatter _messageFormatter = new(DateOnly.FromDateTime(DateTime.Today));
    private readonly Dictionary<string, string> _friendlyNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _deviceNameToId = new(StringComparer.OrdinalIgnoreCase);
    private ProcessingEngine.ProcessingEngine? _processingEngine;
    private static readonly string[] AnchorNames =
    {
        "EVENTS_INPUT",
        "EVENTS_DRIVER",
        "EVENTS_SCHEDULED",
        "EVENTS_PERIODIC",
        "EVENTS_SENSE",
        "DEVICES_EXPANSION",
        "DEVICES_RTIPANEL",
        "USER_GENERAL"
    };

    public ObservableCollection<DriverEntry> Drivers { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        if (CollectionViewSource.GetDefaultView(Drivers) is ListCollectionView view)
        {
            view.CustomSort = new DriverEntryComparer();
        }

        _transport = new LegacyWebSocketDiagnosticsTransport();
        RegisterTransportHandlers(_transport);
    }

    private void Transport_RawMessageReceived(object? sender, string raw)
    {
        if (_useTcpCapture)
        {
            AppendLog($"{_rawLineNumber++}\t{raw}", true);
            return;
        }

        var formattedLine = FormatMessage(raw, out var isLogLine);
        if (isLogLine)
        {
            AppendLog($"{_rawLineNumber++} {formattedLine}");
            return;
        }

        AppendLog(formattedLine);
    }

    private void Transport_TransportInfo(object? sender, string message)
    {
        if (_useTcpCapture)
        {
            return;
        }

        AppendLog(message);
    }

    private void Transport_TransportError(object? sender, string message)
    {
        if (_useTcpCapture)
        {
            return;
        }

        AppendLog(message);
    }

    private void RegisterTransportHandlers(IDiagnosticsTransport transport)
    {
        transport.RawMessageReceived += Transport_RawMessageReceived;
        transport.TransportInfo += Transport_TransportInfo;
        transport.TransportError += Transport_TransportError;
    }

    private void UnregisterTransportHandlers(IDiagnosticsTransport transport)
    {
        transport.RawMessageReceived -= Transport_RawMessageReceived;
        transport.TransportInfo -= Transport_TransportInfo;
        transport.TransportError -= Transport_TransportError;
    }

    private void SetTransport(IDiagnosticsTransport transport, bool useTcpCapture)
    {
        UnregisterTransportHandlers(_transport);
        _transport = transport;
        _useTcpCapture = useTcpCapture;
        RegisterTransportHandlers(_transport);
    }

    private async void DiscoverButton_Click(object sender, RoutedEventArgs e)
    {
        DiscoverButton.IsEnabled = false;
        StatusText.Text = "Discovering...";

        try
        {
            var results = await _transport.DiscoverAsync(TimeSpan.FromSeconds(2));
            DiscoveredCombo.ItemsSource = results.OrderBy(ip => ip).ToList();
            if (results.Count == 1)
            {
                IpComboBox.Text = results[0];
            }
            StatusText.Text = results.Count == 0 ? "No devices found" : $"Found {results.Count}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Discovery failed";
            AppendLog($"[error] Discovery failed: {ex.Message}");
        }
        finally
        {
            DiscoverButton.IsEnabled = true;
        }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_apexUploaded)
        {
            StatusText.Text = "Upload project first";
            return;
        }

        var ip = IpComboBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(ip))
        {
            StatusText.Text = "Enter an IP";
            return;
        }

        if (_isConnecting)
        {
            return;
        }

        _isConnecting = true;
        ConnectButton.IsEnabled = false;
        DiscoverButton.IsEnabled = false;
        StatusText.Text = "Connecting...";

        try
        {
            _messageFormatter.Reset(DateOnly.FromDateTime(DateTime.Today));
            _friendlyNames.Clear();
            var useTcpCapture = TcpCaptureCheckBox.IsChecked == true;
            var sendProbe = SendProbeCheckBox.IsChecked == true;
            if (useTcpCapture)
            {
                SetTransport(new TcpCaptureDiagnosticsTransport(2113, sendProbe), true);
            }
            else if (_transport is not LegacyWebSocketDiagnosticsTransport)
            {
                SetTransport(new LegacyWebSocketDiagnosticsTransport(), false);
            }
            _useTcpCapture = useTcpCapture;

            await _transport.ConnectAsync(ip);
            if (!_useTcpCapture)
            {
                await LoadDriversAsync(ip);
            }
            StatusText.Text = "Connected";
            DisconnectButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusText.Text = "Connect failed";
            AppendLog($"[error] Connect failed: {ex.Message}");
            ConnectButton.IsEnabled = true;
            DiscoverButton.IsEnabled = true;
        }
        finally
        {
            _isConnecting = false;
        }
    }

    private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        await _transport.DisconnectAsync();
        _rawLineNumber = 1;
        _messageFormatter.Reset(DateOnly.FromDateTime(DateTime.Today));
        StatusText.Text = "Disconnected";
        DisconnectButton.IsEnabled = false;
        ConnectButton.IsEnabled = true;
        DiscoverButton.IsEnabled = true;
        Drivers.Clear();
    }

    private void DiscoveredCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DiscoveredCombo.SelectedItem is string selected)
        {
            IpComboBox.Text = selected;
        }
    }

    private void DriverLogLevelsToggleButton_Checked(object sender, RoutedEventArgs e)
    {
        DriverLogRow.Height = new GridLength(DriverLogDefaultHeight);
        DriverLogSplitter.Visibility = Visibility.Visible;
    }

    private void DriverLogLevelsToggleButton_Unchecked(object sender, RoutedEventArgs e)
    {
        DriverLogRow.Height = GridLength.Auto;
        DriverLogSplitter.Visibility = Visibility.Collapsed;
    }

    private void UploadProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "RTI Project (*.apex)|*.apex|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var preview = new ProjectDataPreviewWindow(dialog.FileName)
        {
            Owner = this
        };
        preview.ShowDialog();
        _apexUploaded = true;
        var fileName = Path.GetFileName(dialog.FileName);
        ProjectDataHeaderText.Text = $"Project Data: {fileName}";
        ProjectFileNameText.Text = fileName;
        if (!_isConnecting)
        {
            ConnectButton.IsEnabled = true;
        }
    }

    private void ClearDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        RawLogTextBox.Clear();
        ClearProcessedOutput();
        _rawLineNumber = 1;
        _messageFormatter.Reset(DateOnly.FromDateTime(DateTime.Today));
    }

    private string FormatMessage(string raw, out bool isLogLine)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.TryGetProperty("messageType", out var messageTypeElement)
                && string.Equals(messageTypeElement.GetString(), "LogLevels", StringComparison.OrdinalIgnoreCase))
            {
                isLogLine = false;
                return HandleLogLevels(root);
            }
        }
        catch
        {
        }

        return _messageFormatter.Format(raw, out isLogLine);
    }

    private string HandleLogLevels(JsonElement root)
    {
        var updates = new List<string>();
        var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var driverCount = 0;
        if (root.TryGetProperty("levels", out var levels) && levels.ValueKind == JsonValueKind.Array)
        {
            foreach (var level in levels.EnumerateArray())
            {
                var dName = level.TryGetProperty("dName", out var dn) ? dn.GetString() ?? "" : "";
                var logLevel = ParseLogLevel(level);
                if (string.IsNullOrWhiteSpace(dName))
                {
                    continue;
                }

                if (uniqueNames.Add(dName) && dName.StartsWith("DRIVER//", StringComparison.OrdinalIgnoreCase))
                {
                    driverCount++;
                }

                UpdateDriverFromLogLevel(dName, logLevel);
                updates.Add($"{dName}={logLevel}");
            }
        }

        if (updates.Count == 0)
        {
            return "LogLevels";
        }

        var summary = $"LogLevels ({uniqueNames.Count} total, {driverCount} drivers): ";
        return summary + string.Join(", ", updates);
    }

    private static int ParseLogLevel(JsonElement levelElement)
    {
        if (levelElement.TryGetProperty("logLevel", out var ll))
        {
            if (ll.ValueKind == JsonValueKind.Number && ll.TryGetInt32(out var intVal))
            {
                return intVal;
            }

            if (ll.ValueKind == JsonValueKind.String && int.TryParse(ll.GetString(), out var strVal))
            {
                return strVal;
            }
        }

        return 0;
    }

    private void UpdateDriverFromLogLevel(string dName, int level)
    {
        Dispatcher.Invoke(() =>
        {
            var existing = Drivers.FirstOrDefault(d => d.DName.Equals(dName, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                var displayName = IsAnchorName(dName) ? dName : _friendlyNames.TryGetValue(dName, out var friendly) ? friendly : dName;
                existing = new DriverEntry(ParseDriverId(dName), displayName, dName);
                Drivers.Add(existing);
            }
            else
            {
                if (!IsAnchorName(dName) && _friendlyNames.TryGetValue(dName, out var friendly) && !string.IsNullOrWhiteSpace(friendly))
                {
                    existing.UpdateName(friendly);
                }
            }

            existing.SelectedLevel = level;
            existing.IsEnabled = level > 0;
            RefreshDriverView();
        });
    }

    private static int ParseDriverId(string dName)
    {
        var suffix = dName.Replace("DRIVER//", "", StringComparison.OrdinalIgnoreCase);
        return int.TryParse(suffix, out var id) ? id : 0;
    }

    private void AppendLog(string line, bool allowEmpty = false)
    {
        Dispatcher.Invoke(() =>
        {
            if (!allowEmpty && string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            var newText = RawLogTextBox.Text + line + Environment.NewLine;
            if (newText.Length > MaxLogChars)
            {
                newText = newText.Substring(newText.Length - MaxLogChars);
            }

            RawLogTextBox.Text = newText;
            RawLogTextBox.CaretIndex = RawLogTextBox.Text.Length;
            RawLogTextBox.ScrollToEnd();

            if (_processingEngine != null)
            {
                AppendProcessedLineIfNumbered(line);
            }
        });
    }

    private async Task LoadDriversAsync(string ip)
    {
        try
        {
            var list = await _transport.LoadDriversAsync(ip);

            Dispatcher.Invoke(() =>
            {
                foreach (var entry in list)
                {
                    _friendlyNames[entry.DName] = entry.Name;
                    var existing = Drivers.FirstOrDefault(d => d.DName.Equals(entry.DName, StringComparison.OrdinalIgnoreCase));
                    if (existing == null)
                    {
                        Drivers.Add(new DriverEntry(entry.Id, entry.Name, entry.DName));
                    }
                    else
                    {
                        existing.UpdateName(entry.Name);
                    }
                }

                RefreshDriverView();
            });

            AppendLog($"[info] Loaded {list.Count} drivers");
        }
        catch (Exception ex)
        {
            AppendLog($"[error] Failed to load drivers: {ex.Message}");
        }
    }

    public void InitializeProcessing(ProjectDataExtractionResult result)
    {
        _deviceNameToId.Clear();
        foreach (var entry in result.DiagnosticsMapping)
        {
            if (!_deviceNameToId.ContainsKey(entry.DeviceName))
            {
                _deviceNameToId[entry.DeviceName] = entry.DeviceId;
            }
        }

        var context = new ProcessingContext(_deviceNameToId, result.ApexDiscoveryPreload.PageIndexMap);
        _processingEngine = new ProcessingEngine.ProcessingEngine(context);

        var processed = ProcessingEngineRunner.ProcessNumberedLines(
            RawLogTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
            _processingEngine);

        SetProcessedOutput(processed, showPlaceholderIfEmpty: true);
    }

    private void AppendProcessedLineIfNumbered(string line)
    {
        if (_processingEngine is null)
        {
            return;
        }

        var processed = ProcessingEngineRunner.ProcessNumberedLines(new[] { line }, _processingEngine);
        if (processed.Count == 0)
        {
            return;
        }

        AppendProcessedLine(processed[0]);
    }

    private void ClearProcessedOutput()
    {
        ProcessedLogTextBox.Document.Blocks.Clear();
    }

    private void SetProcessedOutput(IEnumerable<string> lines, bool showPlaceholderIfEmpty)
    {
        ProcessedLogTextBox.Document.Blocks.Clear();

        var paragraph = new Paragraph();
        var hasLines = false;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (hasLines)
            {
                paragraph.Inlines.Add(new LineBreak());
            }

            var category = ProcessedLineClassifier.DetermineCategory(line);
            var run = new Run(line)
            {
                Foreground = ProcessedLineClassifier.GetBrush(category)
            };
            paragraph.Inlines.Add(run);
            hasLines = true;
        }

        if (!hasLines && showPlaceholderIfEmpty)
        {
            paragraph.Inlines.Add(new Run(ProcessedPlaceholderText)
            {
                Foreground = ProcessedLineClassifier.GetBrush(ProcessedLineCategory.Default)
            });
            hasLines = true;
        }

        if (hasLines)
        {
            ProcessedLogTextBox.Document.Blocks.Add(paragraph);
        }
    }

    private bool IsProcessedPlaceholderVisible()
    {
        if (ProcessedLogTextBox.Document.Blocks.Count != 1)
        {
            return false;
        }

        if (ProcessedLogTextBox.Document.Blocks.FirstBlock is not Paragraph paragraph)
        {
            return false;
        }

        if (paragraph.Inlines.Count != 1)
        {
            return false;
        }

        if (paragraph.Inlines.FirstInline is not Run run)
        {
            return false;
        }

        return string.Equals(run.Text, ProcessedPlaceholderText, StringComparison.Ordinal);
    }

    private void AppendProcessedLine(string line)
    {
        if (IsProcessedPlaceholderVisible())
        {
            ProcessedLogTextBox.Document.Blocks.Clear();
        }

        var paragraph = ProcessedLogTextBox.Document.Blocks.FirstBlock as Paragraph;
        if (paragraph == null)
        {
            paragraph = new Paragraph();
            ProcessedLogTextBox.Document.Blocks.Add(paragraph);
        }

        if (paragraph.Inlines.Count > 0)
        {
            paragraph.Inlines.Add(new LineBreak());
        }

        var category = ProcessedLineClassifier.DetermineCategory(line);
        var run = new Run(line)
        {
            Foreground = ProcessedLineClassifier.GetBrush(category)
        };
        paragraph.Inlines.Add(run);
        ProcessedLogTextBox.ScrollToEnd();
    }

    private static bool IsAnchorName(string dName)
    {
        return AnchorNames.Any(anchor => string.Equals(anchor, dName, StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshDriverView()
    {
        if (CollectionViewSource.GetDefaultView(Drivers) is ListCollectionView view)
        {
            view.Refresh();
        }
    }

    private async void DriverToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton toggle || toggle.DataContext is not DriverEntry driver)
        {
            return;
        }

        if (!_transport.IsConnected)
        {
            driver.IsEnabled = false;
            return;
        }

        var isOn = toggle.IsChecked == true;
        driver.IsEnabled = isOn;
        var level = isOn ? driver.SelectedLevel.ToString() : "0";
        await _transport.SendLogLevelAsync(driver.DName, level);
        AppendLog($"[local] Set {driver.DName} to {(toggle.IsChecked == true ? level : "0")}");
    }

    private async void DriverLevelButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not DriverEntry driver)
        {
            return;
        }

        if (button.Tag is not string levelText || !int.TryParse(levelText, out var level))
        {
            return;
        }

        driver.SelectedLevel = level;
        driver.IsEnabled = true;
        if (!_transport.IsConnected)
        {
            return;
        }

        await _transport.SendLogLevelAsync(driver.DName, level.ToString());
        AppendLog($"[local] Set {driver.DName} to {level}");
    }

    public class DriverEntry : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private int _selectedLevel;
        private string _name;

        public DriverEntry(int id, string name, string dName)
        {
            Id = id;
            _name = name;
            DName = dName;
            SelectedLevel = 3;
        }

        public int Id { get; }
        public string Name => _name;
        public string DName { get; }

        public void UpdateName(string name)
        {
            if (string.Equals(_name, name, StringComparison.Ordinal))
            {
                return;
            }

            _name = name;
            OnPropertyChanged(nameof(Name));
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                {
                    return;
                }
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public int SelectedLevel
        {
            get => _selectedLevel;
            set
            {
                if (_selectedLevel == value)
                {
                    return;
                }
                _selectedLevel = value;
                OnPropertyChanged(nameof(SelectedLevel));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    private sealed class DriverEntryComparer : IComparer
    {
        public int Compare(object? x, object? y)
        {
            if (x is not DriverEntry a || y is not DriverEntry b)
            {
                return 0;
            }

            var aIndex = GetAnchorIndex(a.DName);
            var bIndex = GetAnchorIndex(b.DName);
            if (aIndex != bIndex)
            {
                return aIndex.CompareTo(bIndex);
            }

            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static int GetAnchorIndex(string dName)
        {
            for (var i = 0; i < AnchorNames.Length; i++)
            {
                if (string.Equals(AnchorNames[i], dName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return AnchorNames.Length + 1;
        }
    }
}
