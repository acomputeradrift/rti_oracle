using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
using System.Windows.Threading;
using Microsoft.Win32;
using OracleByFPCLtd.DiagnosticsTransport;
using OracleByFPCLtd.DiagnosticsTransport.Connection;
using OracleByFPCLtd.DiagnosticsTransport.Controls;
using OracleByFPCLtd.DiagnosticsTransport.Messaging;
using OracleByFPCLtd.ExportProcessedLogs.IO;
using OracleByFPCLtd.ExportProcessedLogs.Models;
using OracleByFPCLtd.ExportProcessedLogs.Rendering;
using OracleByFPCLtd.ExportProcessedLogs.Services;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.ProjectData.Extractors;
using OracleByFPCLtd.ProjectData.Models;
using OracleByFPCLtd.ProcessingEngine;
using OracleByFPCLtd.Settings.Models;
using OracleByFPCLtd.Settings.Services;
using OracleByFPCLtd.Settings.Storage;

namespace OracleByFPCLtd;

public partial class MainWindow : Window
{
    private const int MaxLogChars = 200_000;
    private const double DriverLogDefaultHeight = 160;
    private const string ProcessedPlaceholderText = "No processed information available";
    private const string FilterInvalidKeywordMessage = "Invalid keyword filter. Use comma-separated terms, with optional +include / -exclude.";
    private const string FilterInvalidDateMessage = "Invalid date/time filter. Use yyyy-MM-dd HH:mm.";
    private const string FilterInvalidRangeMessage = "Invalid date/time range. Start must be before End.";
    private const double ProcessedWidthPadding = 12;
    private const double RawWidthPadding = 12;
    private static readonly TimeSpan FindDebounceInterval = TimeSpan.FromMilliseconds(200);
    private static readonly string[] DateTimeFormats =
    {
        "yyyy-MM-dd HH:mm"
    };
    private IDiagnosticsTransport _transport;
    private bool _isConnecting;
    private bool _useTcpCapture;
    private int _rawLineNumber = 1;
    private bool _apexUploaded;
    private string? _projectFilePath;
    private string? _additionalInfoPath;
    private bool _isUpdatingRecentProjects;
    private int _processedVisibleLineCount;
    private bool _pendingProcessedLayoutUpdate;
    private int _rawVisibleLineCount;
    private bool _pendingRawLayoutUpdate;
    private bool _filterActive;
    private int _filteredRawCount;
    private DateTime? _filterStart;
    private DateTime? _filterEnd;
    private bool _isUpdatingStartPicker;
    private bool _isUpdatingEndPicker;
    private DateTime? _minRawLogTimestamp;
    private DateTime? _maxRawLogTimestamp;
    private bool _autoScrollEnabled = true;
    private readonly FindState _rawFindState = new();
    private readonly FindState _processedFindState = new();
    private static readonly Color RawMatchColor = Color.FromRgb(255, 236, 153);
    private static readonly Color RawFocusMatchColor = Color.FromRgb(255, 165, 0);
    private static readonly Color ProcessedMatchColor = Color.FromRgb(255, 236, 153);
    private static readonly Color ProcessedFocusMatchColor = Color.FromRgb(255, 165, 0);
    private readonly DispatcherTimer _rawFindTimer = new();
    private readonly DispatcherTimer _processedFindTimer = new();
    private readonly OracleSettingsStore _settingsStore = new();
    private readonly RecentProjectService _recentProjectService = new();
    private readonly RecentIpService _recentIpService = new();
    private readonly AdditionalInfoService _additionalInfoService = new();
    private readonly AdditionalInfoCache _additionalInfoCache = new();
    private readonly ProcessedLogsExportService _exportService = new(
        new PdfSharpRenderer(),
        new ExportFileWriter());
    private OracleSettings _settings = new();
    private List<string> _filterIncludeTerms = new();
    private List<string> _filterExcludeTerms = new();
    private readonly List<string> _rawLogLines = new();
    private readonly List<string> _processedLogLines = new();
    private readonly WebSocketMessageFormatter _messageFormatter = new(DateOnly.FromDateTime(DateTime.Today));
    private readonly Dictionary<string, string> _friendlyNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _deviceNameToId = new(StringComparer.OrdinalIgnoreCase);
    private ProcessingEngine.ProcessingEngine? _processingEngine;
    private AdditionalData? _lastAdditionalData;
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

    private TextBox IpTextBox => ConnectionPanel.IpTextBox;
    private Button ConnectButton => ConnectionPanel.ConnectButton;
    private Button DisconnectButton => ConnectionPanel.DisconnectButton;
    private Button DiscoverButton => ConnectionPanel.DiscoverButton;
    private ComboBox DiscoveredCombo => ConnectionPanel.DiscoveredCombo;
    private TextBlock StatusText => ConnectionPanel.StatusText;
    private Button UploadProjectButton => ProjectDataPanel.UploadProjectButton;
    private TextBlock ProjectDataHeaderText => ProjectDataPanel.ProjectDataHeaderText;
    private ComboBox RecentProjectComboBox => ProjectDataPanel.RecentProjectComboBox;
    private Button ProjectPreviewButton => ProjectDataPanel.ProjectPreviewButton;
    private Button UploadAdditionalInfoButton => ProjectDataPanel.UploadAdditionalInfoButton;
    private TextBlock AdditionalInfoFileNameText => ProjectDataPanel.AdditionalInfoFileNameText;
    private ToggleButton DriverLogLevelsToggleButton => DriverLogLevelsPanel.DriverLogLevelsToggleButton;
    private TextBox FilterKeywordTextBox => DiagnosticsPanel.FilterBar.FilterKeywordTextBox;
    private TextBox FilterStartTextBox => DiagnosticsPanel.FilterBar.FilterStartTextBox;
    private TextBox FilterEndTextBox => DiagnosticsPanel.FilterBar.FilterEndTextBox;
    private Button FilterStartPickerButton => DiagnosticsPanel.FilterBar.FilterStartPickerButton;
    private Button FilterEndPickerButton => DiagnosticsPanel.FilterBar.FilterEndPickerButton;
    private ComboBox FilterStartHourCombo => DiagnosticsPanel.FilterBar.FilterStartHourCombo;
    private ComboBox FilterStartMinuteCombo => DiagnosticsPanel.FilterBar.FilterStartMinuteCombo;
    private ComboBox FilterEndHourCombo => DiagnosticsPanel.FilterBar.FilterEndHourCombo;
    private ComboBox FilterEndMinuteCombo => DiagnosticsPanel.FilterBar.FilterEndMinuteCombo;
    private Button FilterApplyButton => DiagnosticsPanel.FilterBar.FilterApplyButton;
    private Button FilterClearButton => DiagnosticsPanel.FilterBar.FilterClearButton;
    private TextBlock FilterCountText => DiagnosticsPanel.FilterBar.FilterCountText;
    private Popup FilterStartDatePopup => DiagnosticsPanel.FilterBar.FilterStartDatePopup;
    private Popup FilterEndDatePopup => DiagnosticsPanel.FilterBar.FilterEndDatePopup;
    private System.Windows.Controls.Calendar FilterStartCalendar => DiagnosticsPanel.FilterBar.FilterStartCalendar;
    private System.Windows.Controls.Calendar FilterEndCalendar => DiagnosticsPanel.FilterBar.FilterEndCalendar;
    private Button ClearDiagnosticsButton => DiagnosticsPanel.FilterBar.ClearDiagnosticsButton;
    private TextBox RawFindTextBox => DiagnosticsPanel.RawOutputPanel.FindBar.FindTextBox;
    private Button RawFindPrevButton => DiagnosticsPanel.RawOutputPanel.FindBar.FindPrevButton;
    private Button RawFindNextButton => DiagnosticsPanel.RawOutputPanel.FindBar.FindNextButton;
    private Button RawFindClearButton => DiagnosticsPanel.RawOutputPanel.FindBar.FindClearButton;
    private TextBlock RawFindCountText => DiagnosticsPanel.RawOutputPanel.FindBar.FindCountText;
    private TextBox ProcessedFindTextBox => DiagnosticsPanel.ProcessedOutputPanel.FindBar.FindTextBox;
    private Button ProcessedFindPrevButton => DiagnosticsPanel.ProcessedOutputPanel.FindBar.FindPrevButton;
    private Button ProcessedFindNextButton => DiagnosticsPanel.ProcessedOutputPanel.FindBar.FindNextButton;
    private Button ProcessedFindClearButton => DiagnosticsPanel.ProcessedOutputPanel.FindBar.FindClearButton;
    private TextBlock ProcessedFindCountText => DiagnosticsPanel.ProcessedOutputPanel.FindBar.FindCountText;
    private RichTextBox RawLogTextBox => DiagnosticsPanel.RawOutputPanel.LogOutputView.LogTextBox;
    private RichTextBox ProcessedLogTextBox => DiagnosticsPanel.ProcessedOutputPanel.LogOutputView.LogTextBox;
    private MenuItem DownloadProcessedLogsMenuItem => DownloadProcessedLogsMenuItemControl;
    private MenuItem DownloadAdditionalInfoTemplateMenuItem => DownloadAdditionalInfoTemplateMenuItemControl;
    private MenuItem AutoscrollMenuItem => AutoscrollMenuItemControl;
    private MenuItem AboutMenuItem => AboutMenuItemControl;

    public MainWindow()
    {
        InitializeComponent();
        WirePanelHandlers();
        ConfigureLogOutputBoxes();
        ConfigureFilterControls();
        UpdateDownloadLogsState();
        ConfigureFindTimers();
        LoadSettings();
        DataContext = this;
        if (CollectionViewSource.GetDefaultView(Drivers) is ListCollectionView view)
        {
            view.CustomSort = new DriverEntryComparer();
        }

        _transport = CreateWebSocketTransport();
        RegisterTransportHandlers(_transport);
        UpdateAllLogLevelsVisibility();
        _autoScrollEnabled = AutoscrollMenuItem.IsChecked;
    }

    private void WirePanelHandlers()
    {
        ConnectButton.Click += ConnectButton_Click;
        DisconnectButton.Click += DisconnectButton_Click;
        DiscoverButton.Click += DiscoverButton_Click;
        DiscoveredCombo.SelectionChanged += DiscoveredCombo_SelectionChanged;

        UploadProjectButton.Click += UploadProject_Click;
        RecentProjectComboBox.SelectionChanged += RecentProjectComboBox_SelectionChanged;
        ProjectPreviewButton.Click += ProjectPreviewButton_Click;
        UploadAdditionalInfoButton.Click += UploadAdditionalInfo_Click;

        DriverLogLevelsToggleButton.Checked += DriverLogLevelsToggleButton_Checked;
        DriverLogLevelsToggleButton.Unchecked += DriverLogLevelsToggleButton_Unchecked;
        DriverLogLevelsPanel.DriverToggleClick += DriverToggle_Click;
        DriverLogLevelsPanel.DriverLevelButtonClick += DriverLevelButton_Click;
        DriverLogLevelsPanel.AllLogLevelsClick += DriverAllLogLevels_Click;
        DriverLogLevelsPanel.SystemOnlyLogLevelsClick += DriverSystemOnlyLogLevels_Click;
        DriverLogLevelsPanel.NoneLogLevelsClick += DriverNoneLogLevels_Click;

        FilterKeywordTextBox.TextChanged += FilterKeywordTextBox_TextChanged;
        FilterStartTextBox.TextChanged += FilterStartTextBox_TextChanged;
        FilterEndTextBox.TextChanged += FilterEndTextBox_TextChanged;
        FilterStartPickerButton.Click += FilterStartPickerButton_Click;
        FilterEndPickerButton.Click += FilterEndPickerButton_Click;
        FilterApplyButton.Click += FilterApplyButton_Click;
        FilterClearButton.Click += FilterClearButton_Click;
        FilterStartCalendar.SelectedDatesChanged += FilterStartCalendar_SelectedDatesChanged;
        FilterEndCalendar.SelectedDatesChanged += FilterEndCalendar_SelectedDatesChanged;
        FilterStartHourCombo.SelectionChanged += FilterStartTimeCombo_SelectionChanged;
        FilterStartMinuteCombo.SelectionChanged += FilterStartTimeCombo_SelectionChanged;
        FilterEndHourCombo.SelectionChanged += FilterEndTimeCombo_SelectionChanged;
        FilterEndMinuteCombo.SelectionChanged += FilterEndTimeCombo_SelectionChanged;
        ClearDiagnosticsButton.Click += ClearDiagnostics_Click;
        DownloadProcessedLogsMenuItem.Click += DownloadLogsButton_Click;
        DownloadAdditionalInfoTemplateMenuItem.Click += DownloadAdditionalInfoTemplateMenuItem_Click;
        AutoscrollMenuItem.Click += AutoscrollMenuItem_Click;
        AboutMenuItem.Click += AboutMenuItem_Click;

        RawFindTextBox.TextChanged += RawFindTextBox_TextChanged;
        RawFindPrevButton.Click += RawFindPrevButton_Click;
        RawFindNextButton.Click += RawFindNextButton_Click;
        RawFindClearButton.Click += RawFindClearButton_Click;

        ProcessedFindTextBox.TextChanged += ProcessedFindTextBox_TextChanged;
        ProcessedFindPrevButton.Click += ProcessedFindPrevButton_Click;
        ProcessedFindNextButton.Click += ProcessedFindNextButton_Click;
        ProcessedFindClearButton.Click += ProcessedFindClearButton_Click;
    }

    private static IDiagnosticsTransport CreateWebSocketTransport()
    {
        var legacy = new LegacyWebSocketDiagnosticsTransport();
        return new DiagnosticsTransportFacade(
            new WebSocketConnectionManager(legacy),
            new WebSocketMessageReceiver(legacy),
            new LogLevelController(legacy),
            new SysvarSubscriptionController(legacy));
    }

    private static IDiagnosticsTransport CreateTcpCaptureTransport(int port, bool sendProbe)
    {
        var tcp = new TcpCaptureDiagnosticsTransport(port, sendProbe);
        return new DiagnosticsTransportFacade(
            new TcpCaptureConnectionManager(tcp),
            new TcpCaptureMessageReceiver(tcp),
            new TcpCaptureLogLevelController(tcp),
            new NoOpSysvarSubscriptionController());
    }

    private void ConfigureLogOutputBoxes()
    {
        RawLogTextBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        RawLogTextBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        RawLogTextBox.Padding = new Thickness(0);
        RawLogTextBox.Document.PagePadding = new Thickness(0);
        RawLogTextBox.Loaded += (_, _) => QueueRawLayoutUpdate();
        RawLogTextBox.SizeChanged += (_, _) => QueueRawLayoutUpdate();

        ProcessedLogTextBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        ProcessedLogTextBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        ProcessedLogTextBox.Padding = new Thickness(0);
        ProcessedLogTextBox.Document.PagePadding = new Thickness(0);
        ProcessedLogTextBox.Loaded += (_, _) => QueueProcessedLayoutUpdate();
        ProcessedLogTextBox.SizeChanged += (_, _) => QueueProcessedLayoutUpdate();
    }

    private void ConfigureFilterControls()
    {
        InitializeTimePickers();
        UpdateFilterApplyState();
        UpdateDateTimePickerBounds();
    }

    private void ConfigureFindTimers()
    {
        _rawFindTimer.Interval = FindDebounceInterval;
        _rawFindTimer.Tick += (_, _) =>
        {
            _rawFindTimer.Stop();
            ExecuteRawFind();
        };

        _processedFindTimer.Interval = FindDebounceInterval;
        _processedFindTimer.Tick += (_, _) =>
        {
            _processedFindTimer.Stop();
            ExecuteProcessedFind();
        };
    }

    private void LoadSettings()
    {
        _settings = _settingsStore.Load();
        UpdateRecentProjectList();
    }

    private void UpdateRecentProjectList(string? selectFilePath = null)
    {
        _isUpdatingRecentProjects = true;
        RecentProjectComboBox.ItemsSource = _settings.RecentProjects;
        RecentProjectComboBox.SelectedItem = null;
        if (!string.IsNullOrWhiteSpace(selectFilePath))
        {
            var selected = _settings.RecentProjects.FirstOrDefault(entry =>
                string.Equals(entry.FilePath, selectFilePath, StringComparison.OrdinalIgnoreCase));
            if (selected != null)
            {
                RecentProjectComboBox.SelectedItem = selected;
            }
        }
        _isUpdatingRecentProjects = false;
    }

    private void InitializeTimePickers()
    {
        for (var hour = 0; hour < 24; hour++)
        {
            var value = hour.ToString("00", CultureInfo.InvariantCulture);
            FilterStartHourCombo.Items.Add(value);
            FilterEndHourCombo.Items.Add(value);
        }

        for (var minute = 0; minute < 60; minute++)
        {
            var value = minute.ToString("00", CultureInfo.InvariantCulture);
            FilterStartMinuteCombo.Items.Add(value);
            FilterEndMinuteCombo.Items.Add(value);
        }
    }

    private void FilterKeywordTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateFilterApplyState();
    }

    private void FilterStartTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateFilterApplyState();
    }

    private void FilterEndTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateFilterApplyState();
    }

    private void FilterStartPickerButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateDateTimePickerBounds();
        SyncPickerFromText(FilterStartTextBox.Text, FilterStartCalendar, FilterStartHourCombo, FilterStartMinuteCombo, isStart: true);
        FilterStartDatePopup.IsOpen = true;
    }

    private void FilterEndPickerButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateDateTimePickerBounds();
        SyncPickerFromText(FilterEndTextBox.Text, FilterEndCalendar, FilterEndHourCombo, FilterEndMinuteCombo, isStart: false);
        FilterEndDatePopup.IsOpen = true;
    }

    private void FilterStartCalendar_SelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FilterStartCalendar.SelectedDate is null)
        {
            return;
        }

        UpdateDateTimeTextFromPicker(FilterStartTextBox, FilterStartCalendar, FilterStartHourCombo, FilterStartMinuteCombo, isStart: true);
    }

    private void FilterEndCalendar_SelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FilterEndCalendar.SelectedDate is null)
        {
            return;
        }

        UpdateDateTimeTextFromPicker(FilterEndTextBox, FilterEndCalendar, FilterEndHourCombo, FilterEndMinuteCombo, isStart: false);
    }

    private void FilterStartTimeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingStartPicker)
        {
            return;
        }

        UpdateDateTimeTextFromPicker(FilterStartTextBox, FilterStartCalendar, FilterStartHourCombo, FilterStartMinuteCombo, isStart: true);
    }

    private void FilterEndTimeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingEndPicker)
        {
            return;
        }

        UpdateDateTimeTextFromPicker(FilterEndTextBox, FilterEndCalendar, FilterEndHourCombo, FilterEndMinuteCombo, isStart: false);
    }

    private void FilterApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseKeywordFilter(FilterKeywordTextBox.Text, out var include, out var exclude, out _)
            || !TryParseDateRange(FilterStartTextBox.Text, FilterEndTextBox.Text, out var start, out var end, out _))
        {
            UpdateFilterApplyState();
            return;
        }

        _filterIncludeTerms = include;
        _filterExcludeTerms = exclude;
        _filterStart = start;
        _filterEnd = end;
        _filterActive = _filterIncludeTerms.Count > 0 || _filterExcludeTerms.Count > 0 || _filterStart.HasValue || _filterEnd.HasValue;

        ApplyCurrentFilter();
    }

    private void FilterClearButton_Click(object sender, RoutedEventArgs e)
    {
        FilterKeywordTextBox.Text = "";
        FilterStartTextBox.Text = "";
        FilterEndTextBox.Text = "";
        FilterStartCalendar.SelectedDate = null;
        FilterEndCalendar.SelectedDate = null;
        _filterIncludeTerms = new List<string>();
        _filterExcludeTerms = new List<string>();
        _filterStart = null;
        _filterEnd = null;
        _filterActive = false;
        ApplyCurrentFilter();
        UpdateFilterApplyState();
    }

    private void RawFindTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _rawFindTimer.Stop();
        _rawFindTimer.Start();
    }

    private void ProcessedFindTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _processedFindTimer.Stop();
        _processedFindTimer.Start();
    }

    private void RawFindPrevButton_Click(object sender, RoutedEventArgs e)
    {
        MoveFindSelection(_rawFindState, RawLogTextBox, moveNext: false, isProcessed: false);
        UpdateMatchLabel(_rawFindState, RawFindCountText, isProcessed: false);
        ApplyRawFocusHighlight();
    }

    private void RawFindNextButton_Click(object sender, RoutedEventArgs e)
    {
        MoveFindSelection(_rawFindState, RawLogTextBox, moveNext: true, isProcessed: false);
        UpdateMatchLabel(_rawFindState, RawFindCountText, isProcessed: false);
        ApplyRawFocusHighlight();
    }

    private void RawFindClearButton_Click(object sender, RoutedEventArgs e)
    {
        RawFindTextBox.Text = "";
        ResetFindState(_rawFindState, RawFindCountText, RawFindPrevButton, RawFindNextButton);
        RawLogTextBox.Selection.Select(RawLogTextBox.Document.ContentStart, RawLogTextBox.Document.ContentStart);
    }

    private void ProcessedFindPrevButton_Click(object sender, RoutedEventArgs e)
    {
        MoveFindSelection(_processedFindState, ProcessedLogTextBox, moveNext: false, isProcessed: true);
        UpdateMatchLabel(_processedFindState, ProcessedFindCountText, isProcessed: true);
        ApplyProcessedFocusHighlight();
    }

    private void ProcessedFindNextButton_Click(object sender, RoutedEventArgs e)
    {
        MoveFindSelection(_processedFindState, ProcessedLogTextBox, moveNext: true, isProcessed: true);
        UpdateMatchLabel(_processedFindState, ProcessedFindCountText, isProcessed: true);
        ApplyProcessedFocusHighlight();
    }

    private void ProcessedFindClearButton_Click(object sender, RoutedEventArgs e)
    {
        ProcessedFindTextBox.Text = "";
        ResetFindState(_processedFindState, ProcessedFindCountText, ProcessedFindPrevButton, ProcessedFindNextButton);
        ProcessedLogTextBox.Selection.Select(ProcessedLogTextBox.Document.ContentStart, ProcessedLogTextBox.Document.ContentStart);
    }

    private void UpdateFilterApplyState()
    {
        var keywordValid = TryParseKeywordFilter(FilterKeywordTextBox.Text, out _, out _, out var keywordError);
        var dateValid = TryParseDateRange(FilterStartTextBox.Text, FilterEndTextBox.Text, out _, out _, out var dateError);

        string? error = null;
        if (!keywordValid)
        {
            error = keywordError;
        }
        else if (!dateValid)
        {
            error = dateError;
        }

        FilterApplyButton.IsEnabled = error == null;
        FilterApplyButton.ToolTip = error;
    }

    private static void UpdateFindState(FindState state, string query, string text, TextBlock countText, Button prevButton, Button nextButton, bool resetIndex)
    {
        state.Matches.Clear();
        state.ProcessedMatches.Clear();
        var previousIndex = state.CurrentIndex;
        var previousQuery = state.Query;
        state.Query = query ?? "";

        if (!string.IsNullOrWhiteSpace(query))
        {
            var index = 0;
            while (index <= text.Length - query.Length)
            {
                index = text.IndexOf(query, index, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    break;
                }

                state.Matches.Add(index);
                index += query.Length;
            }
        }

        var hasMatches = state.Matches.Count > 0;
        prevButton.IsEnabled = hasMatches;
        nextButton.IsEnabled = hasMatches;
        if (!hasMatches)
        {
            state.CurrentIndex = -1;
            UpdateMatchLabel(state, countText, isProcessed: false);
            return;
        }

        if (resetIndex || !string.Equals(previousQuery, state.Query, StringComparison.Ordinal))
        {
            state.CurrentIndex = 0;
            UpdateMatchLabel(state, countText, isProcessed: false);
            return;
        }

        state.CurrentIndex = previousIndex;
        if (state.CurrentIndex < 0)
        {
            state.CurrentIndex = 0;
        }
        else if (state.CurrentIndex >= state.Matches.Count)
        {
            state.CurrentIndex = state.Matches.Count - 1;
        }
        UpdateMatchLabel(state, countText, isProcessed: false);
    }

    private void UpdateProcessedFindState(FindState state, string query, TextBlock countText, Button prevButton, Button nextButton, bool resetIndex)
    {
        state.Matches.Clear();
        state.ProcessedMatches.Clear();
        var previousIndex = state.CurrentIndex;
        var previousQuery = state.Query;
        state.Query = query ?? "";

        if (!string.IsNullOrWhiteSpace(query))
        {
            var paragraph = ProcessedLogTextBox.Document.Blocks.FirstBlock as Paragraph;
            if (paragraph != null)
            {
                var lineIndex = 0;
                var lineStart = paragraph.ContentStart;
                foreach (var inline in paragraph.Inlines)
                {
                    if (inline is Run run)
                    {
                        var text = run.Text ?? "";
                        if (text.Length > 0)
                        {
                            var startIndex = 0;
                            while (startIndex <= text.Length - query.Length)
                            {
                                var matchIndex = text.IndexOf(query, startIndex, StringComparison.OrdinalIgnoreCase);
                                if (matchIndex < 0)
                                {
                                    break;
                                }

                                var pointer = run.ContentStart.GetPositionAtOffset(matchIndex, LogicalDirection.Forward);
                                if (pointer != null)
                                {
                                    state.ProcessedMatches.Add(new ProcessedMatch(pointer, lineIndex, lineStart));
                                }

                                startIndex = matchIndex + query.Length;
                            }
                        }
                    }
                    else if (inline is LineBreak)
                    {
                        lineIndex++;
                        lineStart = inline.ContentEnd;
                    }
                }
            }
        }

        var hasMatches = state.ProcessedMatches.Count > 0;
        prevButton.IsEnabled = hasMatches;
        nextButton.IsEnabled = hasMatches;
        if (!hasMatches)
        {
            state.CurrentIndex = -1;
            UpdateMatchLabel(state, countText, isProcessed: true);
            return;
        }

        if (resetIndex || !string.Equals(previousQuery, state.Query, StringComparison.Ordinal))
        {
            state.CurrentIndex = 0;
            UpdateMatchLabel(state, countText, isProcessed: true);
            return;
        }

        state.CurrentIndex = previousIndex;
        if (state.CurrentIndex < 0)
        {
            state.CurrentIndex = 0;
        }
        else if (state.CurrentIndex >= state.ProcessedMatches.Count)
        {
            state.CurrentIndex = state.ProcessedMatches.Count - 1;
        }
        UpdateMatchLabel(state, countText, isProcessed: true);
    }

    private static void ResetFindState(FindState state, TextBlock countText, Button prevButton, Button nextButton)
    {
        state.Matches.Clear();
        state.ProcessedMatches.Clear();
        state.CurrentIndex = -1;
        state.Query = "";
        countText.Text = "Match: None";
        prevButton.IsEnabled = false;
        nextButton.IsEnabled = false;
    }

    private static void UpdateMatchLabel(FindState state, TextBlock countText, bool isProcessed)
    {
        var total = isProcessed ? state.ProcessedMatches.Count : state.Matches.Count;
        if (total == 0)
        {
            countText.Text = "Match: None";
            return;
        }

        var current = Math.Clamp(state.CurrentIndex + 1, 1, total);
        countText.Text = $"Match: {current}/{total}";
    }

    private void ExecuteRawFind()
    {
        _rawFindState.Query = RawFindTextBox.Text ?? "";
        var lines = _filterActive
            ? _rawLogLines.Where(line => LineMatchesFilter(line, _filterIncludeTerms, _filterExcludeTerms, _filterStart, _filterEnd)).ToList()
            : _rawLogLines.ToList();
        if (lines.Count == 0)
        {
            lines = SplitLines(GetRawText());
        }
        SetRawOutput(lines);
        UpdateFindState(_rawFindState, _rawFindState.Query, GetRawText(), RawFindCountText, RawFindPrevButton, RawFindNextButton, resetIndex: true);
        SelectFindMatch(_rawFindState, RawLogTextBox, isProcessed: false);
        ApplyRawFocusHighlight();
    }

    private void ExecuteProcessedFind()
    {
        _processedFindState.Query = ProcessedFindTextBox.Text ?? "";
        var lines = _filterActive
            ? _processedLogLines.Where(line => LineMatchesFilter(line, _filterIncludeTerms, _filterExcludeTerms, _filterStart, _filterEnd)).ToList()
            : _processedLogLines.ToList();
        if (lines.Count == 0)
        {
            lines = SplitLines(GetProcessedText());
        }
        if (lines.Count > 0)
        {
            SetProcessedOutput(lines, showPlaceholderIfEmpty: true);
        }
        UpdateProcessedFindState(_processedFindState, _processedFindState.Query, ProcessedFindCountText, ProcessedFindPrevButton, ProcessedFindNextButton, resetIndex: true);
        SelectFindMatch(_processedFindState, ProcessedLogTextBox, isProcessed: true);
        ApplyProcessedFocusHighlight();
    }

    private void ApplyRawFocusHighlight()
    {
        ApplyFocusHighlight(RawLogTextBox, _rawFindState.Query, RawMatchColor, RawFocusMatchColor, _rawFindState.CurrentIndex);
    }

    private void ApplyProcessedFocusHighlight()
    {
        ApplyFocusHighlight(ProcessedLogTextBox, _processedFindState.Query, ProcessedMatchColor, ProcessedFocusMatchColor, _processedFindState.CurrentIndex);
    }

    private static void ApplyFocusHighlight(RichTextBox logTextBox, string query, Color matchColor, Color focusColor, int focusIndex)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        var paragraph = logTextBox.Document.Blocks.FirstBlock as Paragraph;
        if (paragraph == null)
        {
            return;
        }

        var matchRunIndex = 0;
        foreach (var inline in paragraph.Inlines)
        {
            if (inline is not Run run)
            {
                continue;
            }

            if (run.Background is not SolidColorBrush brush)
            {
                continue;
            }

            if (brush.Color == matchColor || brush.Color == focusColor)
            {
                run.Background = new SolidColorBrush(matchRunIndex == focusIndex ? focusColor : matchColor);
                matchRunIndex++;
            }
        }
    }

    private void SelectFindMatch(FindState state, Control logControl, bool isProcessed)
    {
        if (isProcessed)
        {
            if (state.ProcessedMatches.Count == 0 || state.CurrentIndex < 0 || state.CurrentIndex >= state.ProcessedMatches.Count)
            {
                return;
            }

            SelectProcessedMatch(state, (RichTextBox)logControl);
            return;
        }

        if (state.Matches.Count == 0 || state.CurrentIndex < 0 || state.CurrentIndex >= state.Matches.Count)
        {
            return;
        }

        SelectRawMatch(state, (RichTextBox)logControl);
    }

    private void MoveFindSelection(FindState state, Control logControl, bool moveNext, bool isProcessed)
    {
        var matchCount = isProcessed ? state.ProcessedMatches.Count : state.Matches.Count;
        if (matchCount == 0)
        {
            return;
        }

        if (moveNext)
        {
            state.CurrentIndex = (state.CurrentIndex + 1) % matchCount;
        }
        else
        {
            state.CurrentIndex = state.CurrentIndex <= 0 ? matchCount - 1 : state.CurrentIndex - 1;
        }

        SelectFindMatch(state, logControl, isProcessed);
    }

    private void SelectRawMatch(FindState state, RichTextBox logTextBox)
    {
        var start = state.Matches[state.CurrentIndex];
        var startPointer = GetTextPointerAtOffset(logTextBox.Document.ContentStart, start);
        var endPointer = GetTextPointerAtOffset(logTextBox.Document.ContentStart, start + state.Query.Length);
        if (startPointer == null || endPointer == null)
        {
            return;
        }

        logTextBox.Selection.Select(startPointer, endPointer);

        EnsureTextBoxSelectionVisible(logTextBox, start);
    }

    private void SelectProcessedMatch(FindState state, RichTextBox logTextBox)
    {
        var match = state.ProcessedMatches[state.CurrentIndex];
        var startPointer = match.Start;
        var startOffset = new TextRange(logTextBox.Document.ContentStart, startPointer).Text.Length;
        var endPointer = GetTextPointerAtOffset(logTextBox.Document.ContentStart, startOffset + state.Query.Length);
        if (startPointer == null || endPointer == null)
        {
            return;
        }

        logTextBox.Selection.Select(startPointer, endPointer);
        EnsureRichTextSelectionVisible(logTextBox, startPointer);
    }

    private void EnsureTextBoxSelectionVisible(RichTextBox logTextBox, int selectionStart)
    {
        logTextBox.UpdateLayout();
        var scrollViewer = FindVisualChild<ScrollViewer>(logTextBox);
        if (scrollViewer == null)
        {
            return;
        }

        var pointer = GetTextPointerAtOffset(logTextBox.Document.ContentStart, selectionStart);
        if (pointer == null)
        {
            return;
        }

        var rect = pointer.GetCharacterRect(LogicalDirection.Forward);
        if (rect.IsEmpty || scrollViewer.ViewportHeight <= 0)
        {
            return;
        }

        var yInViewport = rect.Top - scrollViewer.VerticalOffset;
        if (yInViewport < 0)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + yInViewport);
            logTextBox.UpdateLayout();
            return;
        }

        var bottom = yInViewport + rect.Height;
        if (bottom > scrollViewer.ViewportHeight)
        {
            var delta = bottom - scrollViewer.ViewportHeight;
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + delta);
            logTextBox.UpdateLayout();
        }
    }

    private void EnsureRichTextSelectionVisible(RichTextBox logTextBox, TextPointer selectionStart)
    {
        logTextBox.UpdateLayout();
        var scrollViewer = FindVisualChild<ScrollViewer>(logTextBox);
        if (scrollViewer == null)
        {
            return;
        }

        var rect = selectionStart.GetCharacterRect(LogicalDirection.Forward);
        if (rect.IsEmpty || scrollViewer.ViewportHeight <= 0)
        {
            return;
        }

        var yInViewport = rect.Top - scrollViewer.VerticalOffset;
        if (yInViewport < 0)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + yInViewport);
            logTextBox.UpdateLayout();
            return;
        }

        var bottom = yInViewport + rect.Height;
        if (bottom > scrollViewer.ViewportHeight)
        {
            var delta = bottom - scrollViewer.ViewportHeight;
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + delta);
            logTextBox.UpdateLayout();
        }
    }

    private static TextPointer? GetTextPointerAtOffset(TextPointer start, int offset)
    {
        var remaining = offset;
        var pointer = start;
        while (pointer != null)
        {
            if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                var text = pointer.GetTextInRun(LogicalDirection.Forward);
                if (text.Length >= remaining)
                {
                    return pointer.GetPositionAtOffset(remaining, LogicalDirection.Forward);
                }

                remaining -= text.Length;
                pointer = pointer.GetPositionAtOffset(text.Length, LogicalDirection.Forward);
            }
            else if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart
                     && pointer.GetAdjacentElement(LogicalDirection.Forward) is LineBreak)
            {
                if (remaining <= 2)
                {
                    return pointer.GetNextContextPosition(LogicalDirection.Forward);
                }

                remaining -= 2;
                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
            }
            else
            {
                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        return null;
    }

    private string GetRawText()
    {
        var range = new TextRange(RawLogTextBox.Document.ContentStart, RawLogTextBox.Document.ContentEnd);
        return range.Text ?? "";
    }

    private string GetProcessedText()
    {
        var range = new TextRange(ProcessedLogTextBox.Document.ContentStart, ProcessedLogTextBox.Document.ContentEnd);
        return range.Text ?? "";
    }

    private static List<string> SplitLines(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        return text
            .Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }

    private static T? FindVisualChild<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            var nested = FindVisualChild<T>(child);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private void UpdateDateTimePickerBounds()
    {
        var hasLogs = _minRawLogTimestamp.HasValue && _maxRawLogTimestamp.HasValue;
        FilterStartPickerButton.IsEnabled = hasLogs;
        FilterEndPickerButton.IsEnabled = hasLogs;

        if (!hasLogs)
        {
            FilterStartCalendar.DisplayDateStart = null;
            FilterStartCalendar.DisplayDateEnd = null;
            FilterEndCalendar.DisplayDateStart = null;
            FilterEndCalendar.DisplayDateEnd = null;
            return;
        }

        var minDate = _minRawLogTimestamp!.Value.Date;
        var maxDate = _maxRawLogTimestamp!.Value.Date;
        FilterStartCalendar.DisplayDateStart = minDate;
        FilterStartCalendar.DisplayDateEnd = maxDate;
        FilterEndCalendar.DisplayDateStart = minDate;
        FilterEndCalendar.DisplayDateEnd = maxDate;
    }

    private void ApplyCurrentFilter()
    {
        if (!_filterActive)
        {
            _filteredRawCount = _rawLogLines.Count;
            FilterCountText.Text = $"Count: {_filteredRawCount}";
            _rawFindState.Query = RawFindTextBox.Text ?? "";
            SetRawOutput(_rawLogLines);
            UpdateFindState(_rawFindState, _rawFindState.Query, GetRawText(), RawFindCountText, RawFindPrevButton, RawFindNextButton, resetIndex: true);
            SelectFindMatch(_rawFindState, RawLogTextBox, isProcessed: false);

            SetProcessedOutput(_processedLogLines, showPlaceholderIfEmpty: true);
            _processedFindState.Query = ProcessedFindTextBox.Text ?? "";
            UpdateProcessedFindState(_processedFindState, _processedFindState.Query, ProcessedFindCountText, ProcessedFindPrevButton, ProcessedFindNextButton, resetIndex: true);
            SelectFindMatch(_processedFindState, ProcessedLogTextBox, isProcessed: true);
            UpdateDownloadLogsState();
            return;
        }

        var filteredRaw = _rawLogLines.Where(line => LineMatchesFilter(line, _filterIncludeTerms, _filterExcludeTerms, _filterStart, _filterEnd)).ToList();
        _filteredRawCount = filteredRaw.Count;
        FilterCountText.Text = $"Count: {_filteredRawCount}";
        _rawFindState.Query = RawFindTextBox.Text ?? "";
        SetRawOutput(filteredRaw);
        UpdateFindState(_rawFindState, _rawFindState.Query, GetRawText(), RawFindCountText, RawFindPrevButton, RawFindNextButton, resetIndex: true);
        SelectFindMatch(_rawFindState, RawLogTextBox, isProcessed: false);

        var filteredProcessed = _processedLogLines.Where(line => LineMatchesFilter(line, _filterIncludeTerms, _filterExcludeTerms, _filterStart, _filterEnd)).ToList();
        SetProcessedOutput(filteredProcessed, showPlaceholderIfEmpty: true);
        _processedFindState.Query = ProcessedFindTextBox.Text ?? "";
        UpdateProcessedFindState(_processedFindState, _processedFindState.Query, ProcessedFindCountText, ProcessedFindPrevButton, ProcessedFindNextButton, resetIndex: true);
        SelectFindMatch(_processedFindState, ProcessedLogTextBox, isProcessed: true);
        UpdateDownloadLogsState();
    }

    private void QueueProcessedLayoutUpdate()
    {
        if (_pendingProcessedLayoutUpdate)
        {
            return;
        }

        _pendingProcessedLayoutUpdate = true;
        Dispatcher.BeginInvoke(() =>
        {
            _pendingProcessedLayoutUpdate = false;
            AdjustProcessedDocumentWidth();
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void QueueRawLayoutUpdate()
    {
        if (_pendingRawLayoutUpdate)
        {
            return;
        }

        _pendingRawLayoutUpdate = true;
        Dispatcher.BeginInvoke(() =>
        {
            _pendingRawLayoutUpdate = false;
            AdjustRawDocumentWidth();
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void AdjustProcessedDocumentWidth()
    {
        var viewportWidth = ProcessedLogTextBox.ViewportWidth;
        if (viewportWidth <= 0)
        {
            viewportWidth = ProcessedLogTextBox.ActualWidth;
        }

        if (viewportWidth <= 0)
        {
            return;
        }

        if (_processedVisibleLineCount <= 0)
        {
            SetProcessedDocumentWidth(viewportWidth);
            return;
        }

        var maxWidth = MeasureProcessedTextWidth();
        if (maxWidth <= 0)
        {
            SetProcessedDocumentWidth(viewportWidth);
            return;
        }

        var desiredWidth = Math.Max(viewportWidth, maxWidth + ProcessedWidthPadding);
        SetProcessedDocumentWidth(desiredWidth);
    }

    private void AdjustRawDocumentWidth()
    {
        var viewportWidth = RawLogTextBox.ViewportWidth;
        if (viewportWidth <= 0)
        {
            viewportWidth = RawLogTextBox.ActualWidth;
        }

        if (viewportWidth <= 0)
        {
            return;
        }

        if (_rawVisibleLineCount <= 0)
        {
            SetRawDocumentWidth(viewportWidth);
            return;
        }

        var maxWidth = MeasureRawTextWidth();
        if (maxWidth <= 0)
        {
            SetRawDocumentWidth(viewportWidth);
            return;
        }

        var desiredWidth = Math.Max(viewportWidth, maxWidth + RawWidthPadding);
        SetRawDocumentWidth(desiredWidth);
    }

    private void SetProcessedDocumentWidth(double width)
    {
        ProcessedLogTextBox.Document.PageWidth = width;
        ProcessedLogTextBox.Document.ColumnWidth = width;
    }

    private void SetRawDocumentWidth(double width)
    {
        RawLogTextBox.Document.PageWidth = width;
        RawLogTextBox.Document.ColumnWidth = width;
    }

    private double MeasureProcessedTextWidth()
    {
        var range = new TextRange(ProcessedLogTextBox.Document.ContentStart, ProcessedLogTextBox.Document.ContentEnd);
        var text = range.Text?.TrimEnd('\r', '\n') ?? "";
        if (text.Length == 0)
        {
            return 0;
        }

        var dpi = VisualTreeHelper.GetDpi(ProcessedLogTextBox);
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(
                ProcessedLogTextBox.FontFamily,
                ProcessedLogTextBox.FontStyle,
                ProcessedLogTextBox.FontWeight,
                ProcessedLogTextBox.FontStretch),
            ProcessedLogTextBox.FontSize,
            Brushes.Black,
            dpi.PixelsPerDip);

        return formatted.WidthIncludingTrailingWhitespace;
    }

    private double MeasureRawTextWidth()
    {
        var range = new TextRange(RawLogTextBox.Document.ContentStart, RawLogTextBox.Document.ContentEnd);
        var text = range.Text?.TrimEnd('\r', '\n') ?? "";
        if (text.Length == 0)
        {
            return 0;
        }

        var dpi = VisualTreeHelper.GetDpi(RawLogTextBox);
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(
                RawLogTextBox.FontFamily,
                RawLogTextBox.FontStyle,
                RawLogTextBox.FontWeight,
                RawLogTextBox.FontStretch),
            RawLogTextBox.FontSize,
            Brushes.Black,
            dpi.PixelsPerDip);

        return formatted.WidthIncludingTrailingWhitespace;
    }

    private static bool TryParseKeywordFilter(string input, out List<string> includeTerms, out List<string> excludeTerms, out string? error)
    {
        includeTerms = new List<string>();
        excludeTerms = new List<string>();
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        var terms = input.Split(',');
        foreach (var raw in terms)
        {
            var term = raw.Trim();
            if (term.Length == 0)
            {
                error = FilterInvalidKeywordMessage;
                return false;
            }

            var sign = term[0];
            if (sign == '+' || sign == '-')
            {
                term = term.Substring(1).Trim();
                if (term.Length == 0)
                {
                    error = FilterInvalidKeywordMessage;
                    return false;
                }

                if (sign == '-')
                {
                    excludeTerms.Add(term);
                }
                else
                {
                    includeTerms.Add(term);
                }

                continue;
            }

            includeTerms.Add(term);
        }

        return true;
    }

    private static bool TryParseDateRange(string startText, string endText, out DateTime? start, out DateTime? end, out string? error)
    {
        start = null;
        end = null;
        error = null;

        if (!string.IsNullOrWhiteSpace(startText))
        {
            if (!DateTime.TryParseExact(startText, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var startValue))
            {
                error = FilterInvalidDateMessage;
                return false;
            }

            start = startValue;
        }

        if (!string.IsNullOrWhiteSpace(endText))
        {
            if (!DateTime.TryParseExact(endText, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var endValue))
            {
                error = FilterInvalidDateMessage;
                return false;
            }

            end = endValue;
        }

        if (start.HasValue && end.HasValue && start.Value > end.Value)
        {
            error = FilterInvalidRangeMessage;
            return false;
        }

        return true;
    }

    private void SyncPickerFromText(string text, System.Windows.Controls.Calendar calendar, ComboBox hourCombo, ComboBox minuteCombo, bool isStart)
    {
        if (TryParseDateTime(text, out var value))
        {
            value = ClampToLogRange(value);
            SetPickerValues(calendar, hourCombo, minuteCombo, value, isStart);
            return;
        }

        var fallbackDate = calendar.SelectedDate ?? DateTime.Today;
        var fallback = new DateTime(fallbackDate.Year, fallbackDate.Month, fallbackDate.Day, 0, 0, 0);
        SetPickerValues(calendar, hourCombo, minuteCombo, fallback, isStart);
    }

    private void SetPickerValues(System.Windows.Controls.Calendar calendar, ComboBox hourCombo, ComboBox minuteCombo, DateTime value, bool isStart)
    {
        if (isStart)
        {
            _isUpdatingStartPicker = true;
        }
        else
        {
            _isUpdatingEndPicker = true;
        }

        calendar.SelectedDate = value.Date;
        hourCombo.SelectedItem = value.Hour.ToString("00", CultureInfo.InvariantCulture);
        minuteCombo.SelectedItem = value.Minute.ToString("00", CultureInfo.InvariantCulture);

        if (isStart)
        {
            _isUpdatingStartPicker = false;
        }
        else
        {
            _isUpdatingEndPicker = false;
        }
    }

    private void UpdateDateTimeTextFromPicker(TextBox target, System.Windows.Controls.Calendar calendar, ComboBox hourCombo, ComboBox minuteCombo, bool isStart)
    {
        if (isStart)
        {
            _isUpdatingStartPicker = true;
        }
        else
        {
            _isUpdatingEndPicker = true;
        }

        var date = calendar.SelectedDate ?? DateTime.Today;
        var hour = ParseComboValue(hourCombo, 0);
        var minute = ParseComboValue(minuteCombo, 0);
        var value = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);
        value = ClampToLogRange(value);
        target.Text = value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        if (isStart)
        {
            _isUpdatingStartPicker = false;
        }
        else
        {
            _isUpdatingEndPicker = false;
        }
    }

    private static int ParseComboValue(ComboBox combo, int fallback)
    {
        if (combo.SelectedItem is string text && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return fallback;
    }

    private static bool TryParseDateTime(string text, out DateTime value)
    {
        return DateTime.TryParseExact(text, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out value);
    }

    private DateTime ClampToLogRange(DateTime value)
    {
        if (_minRawLogTimestamp.HasValue && value < _minRawLogTimestamp.Value)
        {
            return _minRawLogTimestamp.Value;
        }

        if (_maxRawLogTimestamp.HasValue && value > _maxRawLogTimestamp.Value)
        {
            return _maxRawLogTimestamp.Value;
        }

        return value;
    }

    private static bool LineMatchesFilter(string line, IReadOnlyList<string> includeTerms, IReadOnlyList<string> excludeTerms, DateTime? start, DateTime? end)
    {
        if (!LineMatchesKeywordFilter(line, includeTerms, excludeTerms))
        {
            return false;
        }

        if (!start.HasValue && !end.HasValue)
        {
            return true;
        }

        if (!TryExtractTimestamp(line, out var timestamp))
        {
            return false;
        }

        if (start.HasValue && timestamp < start.Value)
        {
            return false;
        }

        if (end.HasValue && timestamp > end.Value)
        {
            return false;
        }

        return true;
    }

    private static bool LineMatchesKeywordFilter(string line, IReadOnlyList<string> includeTerms, IReadOnlyList<string> excludeTerms)
    {
        foreach (var term in includeTerms)
        {
            if (line.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }
        }

        foreach (var term in excludeTerms)
        {
            if (line.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryExtractTimestamp(string line, out DateTime timestamp)
    {
        timestamp = default;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var openIndex = line.IndexOf('[');
        if (openIndex < 0)
        {
            return false;
        }

        var closeIndex = line.IndexOf(']', openIndex + 1);
        if (closeIndex < 0)
        {
            return false;
        }

        var rawTimestamp = line.Substring(openIndex + 1, closeIndex - openIndex - 1);
        return DateTime.TryParse(rawTimestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out timestamp);
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
                if (_apexUploaded)
                {
                    IpTextBox.Text = results[0];
                }
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

        var ip = IpTextBox.Text.Trim();
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
                SetTransport(CreateTcpCaptureTransport(2113, sendProbe), true);
            }
            else if (_useTcpCapture)
            {
                SetTransport(CreateWebSocketTransport(), false);
            }
            _useTcpCapture = useTcpCapture;

            await _transport.ConnectAsync(ip);
            if (!_useTcpCapture)
            {
                await LoadDriversAsync(ip);
            }
            StatusText.Text = "Connected";
            DisconnectButton.IsEnabled = true;
            UpdateAllLogLevelsVisibility();
            if (!string.IsNullOrWhiteSpace(_projectFilePath))
            {
                _recentProjectService.RecordSuccessfulConnection(_settings, _projectFilePath, ip);
                _recentIpService.RecordRecentIp(_settings, ip);
                _settingsStore.Save(_settings);
                UpdateRecentProjectList(_projectFilePath);
            }
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
        UpdateAllLogLevelsVisibility();
    }

    private void DiscoveredCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DiscoveredCombo.SelectedItem is string selected)
        {
            if (_apexUploaded)
            {
                IpTextBox.Text = selected;
            }
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
        HandleProjectSelected(dialog.FileName);
    }

    private void RecentProjectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingRecentProjects)
        {
            return;
        }

        if (RecentProjectComboBox.SelectedItem is not RecentProjectEntry entry)
        {
            return;
        }

        HandleProjectSelected(entry.FilePath);
    }

    private void ProjectPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_projectFilePath) || !File.Exists(_projectFilePath))
        {
            return;
        }

        var preview = new ProjectDataPreviewWindow(_projectFilePath, _lastAdditionalData)
        {
            Owner = this
        };
        preview.ShowDialog();
    }

    private void HandleProjectSelected(string filePath)
    {
        LoadProjectFromPath(filePath, openPreview: false);
        _ = LoadProjectDataForProcessingAsync(filePath);
    }

    private async Task LoadProjectDataForProcessingAsync(string filePath)
    {
        try
        {
            var extractor = new ProjectDataExtractor();
            var result = await Task.Run(() => extractor.Extract(filePath));
            Dispatcher.Invoke(() => InitializeProcessing(result));
        }
        catch (Exception ex)
        {
            ShowMessageOnUiThread(ex.Message, "Project Data", MessageBoxImage.Error);
        }
    }

    private void UploadAdditionalInfo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Additional Info (*.xlsx)|*.xlsx|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _additionalInfoService.RecordAdditionalInfo(_settings, dialog.FileName);
        _settingsStore.Save(_settings);
        _additionalInfoPath = dialog.FileName;
        AdditionalInfoFileNameText.Text = Path.GetFileName(dialog.FileName);
        if (!string.IsNullOrWhiteSpace(_projectFilePath))
        {
            _ = LoadProjectDataForProcessingAsync(_projectFilePath);
        }
    }

    private void LoadProjectFromPath(string filePath, bool openPreview)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        _apexUploaded = true;
        _projectFilePath = filePath;
        ProjectDataHeaderText.Text = "Project Data";

        _recentProjectService.RecordProjectSelection(_settings, filePath);
        _settingsStore.Save(_settings);
        UpdateRecentProjectList(filePath);

        var selected = _settings.RecentProjects.FirstOrDefault(entry =>
            string.Equals(entry.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (selected != null && !string.IsNullOrWhiteSpace(selected.LastSuccessfulIp))
        {
            IpTextBox.Text = selected.LastSuccessfulIp;
        }
        else
        {
            IpTextBox.Text = "";
        }

        ProjectPreviewButton.IsEnabled = true;

        if (openPreview)
        {
            var preview = new ProjectDataPreviewWindow(filePath, _lastAdditionalData)
            {
                Owner = this
            };
            preview.ShowDialog();
        }

        if (!_isConnecting)
        {
            ConnectButton.IsEnabled = true;
        }
    }

    private void ClearDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        ClearRawOutput();
        ClearProcessedOutput();
        _rawLogLines.Clear();
        _processedLogLines.Clear();
        UpdateDownloadLogsState();
        _filteredRawCount = 0;
        FilterCountText.Text = "Count: 0";
        _minRawLogTimestamp = null;
        _maxRawLogTimestamp = null;
        UpdateDateTimePickerBounds();
        _rawLineNumber = 1;
        _messageFormatter.Reset(DateOnly.FromDateTime(DateTime.Today));
        ResetFindState(_rawFindState, RawFindCountText, RawFindPrevButton, RawFindNextButton);
        ResetFindState(_processedFindState, ProcessedFindCountText, ProcessedFindPrevButton, ProcessedFindNextButton);
        RawFindTextBox.Text = "";
        ProcessedFindTextBox.Text = "";
    }

    private void DownloadLogsButton_Click(object sender, RoutedEventArgs e)
    {
        var exportBaseName = string.IsNullOrWhiteSpace(_projectFilePath)
            ? "Unknown"
            : Path.GetFileNameWithoutExtension(_projectFilePath);
        var exportDate = DateTime.Now.ToString("yyyy_MM_dd_HHmm");
        var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf|All files (*.*)|*.*",
            FileName = $"{exportDate}_{exportBaseName}_Oracle_Export.pdf"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var request = BuildExportRequest();
        _exportService.Export(request, dialog.FileName);
    }

    private void DownloadAdditionalInfoTemplateMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var templatePath = ResolveAdditionalInfoTemplatePath();
        if (templatePath == null)
        {
            MessageBox.Show(this, "Additional info template not found.", "Download Additional Info Template",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx|All files (*.*)|*.*",
            FileName = Path.GetFileName(templatePath)
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        File.Copy(templatePath, dialog.FileName, overwrite: true);
    }

    private void AutoscrollMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _autoScrollEnabled = AutoscrollMenuItem.IsChecked;
        if (_autoScrollEnabled)
        {
            RawLogTextBox.ScrollToEnd();
            ProcessedLogTextBox.ScrollToEnd();
        }
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow
        {
            Owner = this
        };
        about.ShowDialog();
    }

    private ExportRequest BuildExportRequest()
    {
        var apexFile = string.IsNullOrWhiteSpace(_projectFilePath)
            ? "Unknown"
            : Path.GetFileName(_projectFilePath);
        var additionalName = AdditionalInfoFileNameText.Text;
        if (string.IsNullOrWhiteSpace(additionalName) || string.Equals(additionalName, "No additional info", StringComparison.OrdinalIgnoreCase))
        {
            additionalName = null;
        }

        var metadata = new ExportMetadata(DateTime.Now, apexFile, additionalName);
        var filterSummary = new FilterSummary(
            FilterKeywordTextBox.Text.Trim(),
            FilterStartTextBox.Text.Trim(),
            FilterEndTextBox.Text.Trim());
        var exportLines = _filterActive
            ? _processedLogLines.Where(line => LineMatchesFilter(line, _filterIncludeTerms, _filterExcludeTerms, _filterStart, _filterEnd)).ToList()
            : new List<string>(_processedLogLines);
        return new ExportRequest(exportLines, metadata, filterSummary);
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

            UpdateRawLogTimestampBounds(line);
            _rawLogLines.Add(line);

            if (!_filterActive || LineMatchesFilter(line, _filterIncludeTerms, _filterExcludeTerms, _filterStart, _filterEnd))
            {
                _filteredRawCount = _filterActive ? _filteredRawCount + 1 : _rawLogLines.Count;
                FilterCountText.Text = $"Count: {_filteredRawCount}";
                AppendRawLine(line);
            }

            if (_processingEngine != null)
            {
                AppendProcessedLineIfNumbered(line);
            }
        });
    }

    private void UpdateRawLogTimestampBounds(string line)
    {
        if (!TryExtractTimestamp(line, out var timestamp))
        {
            return;
        }

        if (!_minRawLogTimestamp.HasValue || timestamp < _minRawLogTimestamp.Value)
        {
            _minRawLogTimestamp = timestamp;
        }

        if (!_maxRawLogTimestamp.HasValue || timestamp > _maxRawLogTimestamp.Value)
        {
            _maxRawLogTimestamp = timestamp;
        }

        UpdateDateTimePickerBounds();
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
        var additionalData = LoadAdditionalData(result);
        _lastAdditionalData = additionalData;
        InitializeProcessing(result, additionalData);
    }

    private void InitializeProcessing(ProjectDataExtractionResult result, AdditionalData additionalData)
    {
        _deviceNameToId.Clear();
        foreach (var entry in result.DiagnosticsMapping)
        {
            if (!_deviceNameToId.ContainsKey(entry.DeviceName))
            {
                _deviceNameToId[entry.DeviceName] = entry.DeviceId;
            }
        }

        var baseBundle = ProjectDataBundle.FromExtractionResult(result);
        var bundle = new ProjectDataBundle
        {
            System = baseBundle.System,
            Drivers = baseBundle.Drivers,
            Additional = additionalData
        };
        _processingEngine = new ProcessingEngine.ProcessingEngine(bundle);

        var processed = ProcessingEngineRunner.ProcessNumberedLines(_rawLogLines, _processingEngine);

        _processedLogLines.Clear();
        _processedLogLines.AddRange(processed);
        if (_filterActive)
        {
            ApplyCurrentFilter();
        }
        else
        {
            SetProcessedOutput(_processedLogLines, showPlaceholderIfEmpty: true);
            UpdateDownloadLogsState();
        }
    }

    private AdditionalData LoadAdditionalData(ProjectDataExtractionResult result)
    {
        if (string.IsNullOrWhiteSpace(_projectFilePath) || !File.Exists(_projectFilePath))
        {
            return new AdditionalData();
        }

        var projectLastWrite = File.GetLastWriteTimeUtc(_projectFilePath);
        DateTime? additionalLastWrite = null;
        if (!string.IsNullOrWhiteSpace(_additionalInfoPath) && File.Exists(_additionalInfoPath))
        {
            additionalLastWrite = File.GetLastWriteTimeUtc(_additionalInfoPath);
        }

        var key = new AdditionalInfoCacheKey(_projectFilePath, projectLastWrite, _additionalInfoPath, additionalLastWrite);
        return _additionalInfoCache.GetOrLoad(key, () =>
        {
            var driverNames = result.ApexDiscoveryPreload.DriverConfigMap.Values
                .Select(entry => entry.DeviceName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal);

            var data = AdditionalDataExtractor.Extract(_additionalInfoPath, driverNames);
            if (data.Errors.Count > 0)
            {
                ShowMessageOnUiThread(string.Join(Environment.NewLine, data.Errors), "Additional Info", MessageBoxImage.Warning);
            }

            return data;
        });
    }

    private void ShowMessageOnUiThread(string message, string title, MessageBoxImage image)
    {
        if (Dispatcher.CheckAccess())
        {
            MessageBox.Show(this, message, title, MessageBoxButton.OK, image);
            return;
        }

        Dispatcher.Invoke(() => MessageBox.Show(this, message, title, MessageBoxButton.OK, image));
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
        _processedVisibleLineCount = 0;
        QueueProcessedLayoutUpdate();
    }

    private void ClearRawOutput()
    {
        RawLogTextBox.Document.Blocks.Clear();
        _rawVisibleLineCount = 0;
        QueueRawLayoutUpdate();
    }

    private void SetRawOutput(IEnumerable<string> lines)
    {
        RawLogTextBox.Document.Blocks.Clear();

        var paragraph = new Paragraph();
        var hasLines = false;
        var lineCount = 0;
        var applyHighlights = !string.IsNullOrWhiteSpace(_rawFindState.Query);
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

            AppendRunsWithHighlights(paragraph, line, _rawFindState.Query, RawMatchColor, applyHighlights, Brushes.Black);
            hasLines = true;
            lineCount++;
        }

        if (hasLines)
        {
            RawLogTextBox.Document.Blocks.Add(paragraph);
        }

        _rawVisibleLineCount = lineCount;
        QueueRawLayoutUpdate();
    }

    private void AppendRawLine(string line)
    {
        var paragraph = RawLogTextBox.Document.Blocks.FirstBlock as Paragraph;
        if (paragraph == null)
        {
            paragraph = new Paragraph();
            RawLogTextBox.Document.Blocks.Add(paragraph);
        }

        if (paragraph.Inlines.Count > 0)
        {
            paragraph.Inlines.Add(new LineBreak());
        }

        AppendRunsWithHighlights(paragraph, line, _rawFindState.Query, RawMatchColor, applyHighlights: !string.IsNullOrWhiteSpace(_rawFindState.Query), Brushes.Black);
        _rawVisibleLineCount++;
        QueueRawLayoutUpdate();
        if (_autoScrollEnabled)
        {
            RawLogTextBox.ScrollToEnd();
        }
        if (!string.IsNullOrWhiteSpace(_rawFindState.Query))
        {
            UpdateFindState(_rawFindState, _rawFindState.Query, GetRawText(), RawFindCountText, RawFindPrevButton, RawFindNextButton, resetIndex: false);
        }
    }

    private void SetProcessedOutput(IEnumerable<string> lines, bool showPlaceholderIfEmpty)
    {
        ProcessedLogTextBox.Document.Blocks.Clear();

        var paragraph = new Paragraph();
        var hasLines = false;
        var lineCount = 0;
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

            AppendProcessedRunsWithHighlights(paragraph, line);
            hasLines = true;
            lineCount++;
        }

        if (!hasLines && showPlaceholderIfEmpty)
        {
            paragraph.Inlines.Add(new Run(ProcessedPlaceholderText)
            {
                Foreground = ProcessedLineClassifier.GetBrush(ProcessedLineCategory.Default)
            });
            hasLines = true;
            lineCount = 1;
        }

        if (hasLines)
        {
            ProcessedLogTextBox.Document.Blocks.Add(paragraph);
        }

        _processedVisibleLineCount = lineCount;
        QueueProcessedLayoutUpdate();
        UpdateDownloadLogsState();
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

    private void UpdateDownloadLogsState()
    {
        DownloadProcessedLogsMenuItem.IsEnabled = GetFilteredProcessedCount() > 0;
    }

    private int GetFilteredProcessedCount()
    {
        if (!_filterActive)
        {
            return _processedLogLines.Count;
        }

        return _processedLogLines.Count(line => LineMatchesFilter(line, _filterIncludeTerms, _filterExcludeTerms, _filterStart, _filterEnd));
    }

    private void AppendProcessedLine(string line)
    {
        if (IsProcessedPlaceholderVisible())
        {
            ProcessedLogTextBox.Document.Blocks.Clear();
            _processedVisibleLineCount = 0;
        }

        _processedLogLines.Add(line);
        UpdateDownloadLogsState();

        if (_filterActive && !LineMatchesFilter(line, _filterIncludeTerms, _filterExcludeTerms, _filterStart, _filterEnd))
        {
            return;
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

        AppendProcessedRunsWithHighlights(paragraph, line);
        _processedVisibleLineCount++;
        QueueProcessedLayoutUpdate();
        if (_autoScrollEnabled)
        {
            ProcessedLogTextBox.ScrollToEnd();
        }
        if (!string.IsNullOrWhiteSpace(_processedFindState.Query))
        {
            UpdateProcessedFindState(_processedFindState, _processedFindState.Query, ProcessedFindCountText, ProcessedFindPrevButton, ProcessedFindNextButton, resetIndex: false);
        }
    }

    private static string? ResolveAdditionalInfoTemplatePath()
    {
        var templateName = "Additional Info - Sung v4.xlsx";
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, templateName),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "ProjectSpreadsheet", templateName)),
            Path.Combine(Environment.CurrentDirectory, "ProjectSpreadsheet", templateName)
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private void AppendProcessedRunsWithHighlights(Paragraph paragraph, string line)
    {
        var category = ProcessedLineClassifier.DetermineCategory(line);
        var foreground = ProcessedLineClassifier.GetBrush(category);
        AppendRunsWithHighlights(paragraph, line, _processedFindState.Query, ProcessedMatchColor, applyHighlights: true, foreground);
    }

    private static void AppendRunsWithHighlights(Paragraph paragraph, string line, string query, Color highlightColor, bool applyHighlights, Brush? foreground = null)
    {
        if (!applyHighlights || string.IsNullOrWhiteSpace(query))
        {
            paragraph.Inlines.Add(new Run(line) { Foreground = foreground });
            return;
        }

        var index = 0;
        while (index < line.Length)
        {
            var matchIndex = line.IndexOf(query, index, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0)
            {
                var tail = line.Substring(index);
                if (tail.Length > 0)
                {
                    paragraph.Inlines.Add(new Run(tail) { Foreground = foreground });
                }
                break;
            }

            if (matchIndex > index)
            {
                var segment = line.Substring(index, matchIndex - index);
                paragraph.Inlines.Add(new Run(segment) { Foreground = foreground });
            }

            var matchText = line.Substring(matchIndex, query.Length);
            paragraph.Inlines.Add(new Run(matchText)
            {
                Foreground = foreground,
                Background = new SolidColorBrush(highlightColor)
            });
            index = matchIndex + query.Length;
        }
    }

    private sealed class FindState
    {
        public List<int> Matches { get; } = new();
        public List<ProcessedMatch> ProcessedMatches { get; } = new();
        public int CurrentIndex { get; set; } = -1;
        public string Query { get; set; } = "";
    }

    private sealed class ProcessedMatch
    {
        public ProcessedMatch(TextPointer start, int lineIndex, TextPointer lineStart)
        {
            Start = start;
            LineIndex = lineIndex;
            LineStart = lineStart;
        }

        public TextPointer Start { get; }
        public int LineIndex { get; }
        public TextPointer LineStart { get; }
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

    private async void DriverAllLogLevels_Click(object sender, RoutedEventArgs e)
    {
        foreach (var driver in Drivers)
        {
            driver.SelectedLevel = 3;
            driver.IsEnabled = true;
        }

        if (!_transport.IsConnected)
        {
            return;
        }

        foreach (var driver in Drivers)
        {
            await _transport.SendLogLevelAsync(driver.DName, "3");
        }

        AppendLog("[local] Set all drivers to 3");
    }

    private async void DriverSystemOnlyLogLevels_Click(object sender, RoutedEventArgs e)
    {
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EVENTS_INPUT",
            "EVENTS_SENSE",
            "EVENTS_DRIVER",
            "DEVICES_EXPANSION",
            "EVENTS_SCHEDULED",
            "DEVICES_RTIPANEL",
            "EVENTS_PERIODIC",
            "USER_GENERAL"
        };

        foreach (var driver in Drivers)
        {
            if (targets.Contains(driver.DName))
            {
                driver.SelectedLevel = 3;
                driver.IsEnabled = true;
            }
            else
            {
                driver.IsEnabled = false;
            }
        }

        if (!_transport.IsConnected)
        {
            return;
        }

        foreach (var driver in Drivers)
        {
            var level = targets.Contains(driver.DName) ? "3" : "0";
            await _transport.SendLogLevelAsync(driver.DName, level);
        }

        AppendLog("[local] Set system drivers to 3");
    }

    private async void DriverNoneLogLevels_Click(object sender, RoutedEventArgs e)
    {
        foreach (var driver in Drivers)
        {
            driver.IsEnabled = false;
        }

        if (!_transport.IsConnected)
        {
            return;
        }

        foreach (var driver in Drivers)
        {
            await _transport.SendLogLevelAsync(driver.DName, "0");
        }

        AppendLog("[local] Set all drivers to 0");
    }

    private void UpdateAllLogLevelsVisibility()
    {
        var visibility = _transport.IsConnected ? Visibility.Visible : Visibility.Collapsed;
        DriverLogLevelsPanel.AllLogLevelsButton.Visibility = visibility;
        DriverLogLevelsPanel.SystemOnlyLogLevelsButton.Visibility = visibility;
        DriverLogLevelsPanel.NoneLogLevelsButton.Visibility = visibility;
        if (visibility == Visibility.Visible)
        {
            DriverLogLevelsPanel.UpdatePresetButtonSizing();
        }
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
