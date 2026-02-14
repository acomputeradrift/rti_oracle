using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using OracleByFPCLtd.DriverProfiles.Catalog;
using OracleByFPCLtd.DiagnosticsTransport;
using OracleByFPCLtd.DiagnosticsTransport.Connection;
using OracleByFPCLtd.DiagnosticsTransport.Controls;
using OracleByFPCLtd.DiagnosticsTransport.Messaging;
using OracleByFPCLtd.ExportProcessedLogs.IO;
using OracleByFPCLtd.ExportProcessedLogs.Models;
using OracleByFPCLtd.ExportProcessedLogs.Rendering;
using OracleByFPCLtd.ExportProcessedLogs.Services;
using OracleByFPCLtd.DriverProfiles.Models;
using OracleByFPCLtd.ProjectData;
using OracleByFPCLtd.ProjectData.Extractors;
using OracleByFPCLtd.ProjectData.Models;
using OracleByFPCLtd.ProcessingEngine;
using OracleByFPCLtd.Reliability;
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
    private const int ReconnectDelaySeconds = 3;
    private const int ReconnectInitialDelaySeconds = 3;
    private const int LogLevelAckTimeoutMilliseconds = 3000;
    private const int LogLevelAckMaxRetryCount = 1;
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
    private static readonly Regex NoProfileDriverCommandPattern = new Regex("Driver - Command:\\s*'(?<driver>[^\\\\']+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NoProfileDriverEventPattern = new Regex("happens on\\s*'(?<driver>[^\\\\']+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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
    private readonly Dictionary<string, HashSet<string>> _noProfileMessages = new(StringComparer.OrdinalIgnoreCase);
    private ProcessingEngine.ProcessingEngine? _processingEngine;
    private AdditionalData? _lastAdditionalData;
    private IReadOnlyList<AdditionalInfoSheetSchema> _requiredAdditionalInfoSchemas = Array.Empty<AdditionalInfoSheetSchema>();
    private CancellationTokenSource? _reconnectCts;
    private readonly Dictionary<string, PendingLogLevelCommand> _pendingLogLevelCommands = new(StringComparer.OrdinalIgnoreCase);
    private readonly FeatureHealthRegistry _featureHealthRegistry = new();
    private readonly IUserFailureNotifier _failureNotifier;
    private bool _isReconnecting;
    private int _reconnectAttempt;
    private bool _suppressReconnect;
    private string? _lastConnectedIp;
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
    private RichTextBox AppStatusTextBox => StatusPanel.StatusOutputTextBox;
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
    private MenuItem DriverProfilesMenuItem => DriverProfilesMenuItemControl;
    private MenuItem AboutMenuItem => AboutMenuItemControl;

    public MainWindow()
    {
        InitializeComponent();
        WindowIconLoader.TryApply(this);
        _failureNotifier = new MainWindowFailureNotifier(this);
        Title = $"Oracle by FP&C {AppVersion.CurrentLabel()}";
        WirePanelHandlers();
        ConfigureLogOutputBoxes();
        ConfigureFilterControls();
        UpdateDownloadLogsState();
        DownloadAdditionalInfoTemplateMenuItem.IsEnabled = false;
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
        Closing += MainWindow_Closing;
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
        DriverProfilesMenuItem.Click += DriverProfilesMenuItem_Click;
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
        var loadFailed = false;
        _settings = _settingsStore.Load(failure =>
        {
            loadFailed = true;
            _failureNotifier.AppendOperationalLog(failure);
        });
        if (!loadFailed)
        {
            ReportSuccess("SETTINGS_LOAD_SUCCESS", "Settings loaded successfully.", "settings");
        }

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

        AppendAppStatus(message);
    }

    private void Transport_TransportError(object? sender, string message)
    {
        if (_useTcpCapture)
        {
            return;
        }

        AppendAppStatus(message);
        HandleTransportFailure(message);
    }

    private void Transport_OperationStateChanged(object? sender, FeatureOperation operation)
    {
        _featureHealthRegistry.Update(operation);
        if (operation.Status != OperationStatus.Failed || operation.LastError == null)
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            _failureNotifier.AppendOperationalLog(operation.LastError);
            _failureNotifier.ShowBlockingFailure(operation.Feature, operation.LastError);
        });
    }

    private void HandleTransportFailure(string message)
    {
        if (_suppressReconnect || _useTcpCapture || _isConnecting)
        {
            return;
        }

        var shouldReconnect = !_transport.IsConnected || IsRemoteCloseMessage(message);

        var ip = _lastConnectedIp ?? IpTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(ip))
        {
            Dispatcher.Invoke(() =>
            {
                SetConnectionStatus("Disconnected");
                DisconnectButton.IsEnabled = false;
                ConnectButton.IsEnabled = true;
                DiscoverButton.IsEnabled = true;
                UpdateAllLogLevelsVisibility();
            });
            return;
        }

        if (!shouldReconnect)
        {
            return;
        }

        Dispatcher.Invoke(() => StartReconnectLoop(ip));
    }

    private void RegisterTransportHandlers(IDiagnosticsTransport transport)
    {
        transport.RawMessageReceived += Transport_RawMessageReceived;
        transport.TransportInfo += Transport_TransportInfo;
        transport.TransportError += Transport_TransportError;
        transport.OperationStateChanged += Transport_OperationStateChanged;
    }

    private void UnregisterTransportHandlers(IDiagnosticsTransport transport)
    {
        transport.RawMessageReceived -= Transport_RawMessageReceived;
        transport.TransportInfo -= Transport_TransportInfo;
        transport.TransportError -= Transport_TransportError;
        transport.OperationStateChanged -= Transport_OperationStateChanged;
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
        SetConnectionStatus("Discovering...");

        try
        {
            var results = await _transport.DiscoverAsync(TimeSpan.FromSeconds(2));
            var sorted = results.OrderBy(ip => ip).ToList();
            if (sorted.Count > 0)
            {
                var items = new List<string> { "Select a device..." };
                items.AddRange(sorted);
                DiscoveredCombo.ItemsSource = items;
                DiscoveredCombo.SelectedIndex = 0;
            }
            else
            {
                DiscoveredCombo.ItemsSource = sorted;
            }

            if (sorted.Count == 1)
            {
                if (_apexUploaded)
                {
                    IpTextBox.Text = sorted[0];
                }
            }
            SetConnectionStatus(sorted.Count == 0 ? "No Devices Found" : $"Found {sorted.Count}");
            ReportSuccess("DISCOVERY_SUCCESS", $"Discovery completed with {sorted.Count} result(s).", "discover");
        }
        catch (Exception ex)
        {
            SetConnectionStatus("Discovery Failed");
            AppendAppStatus($"[error] Discovery failed: {ex.Message}");
            ReportFailure("Discover", FailureCodes.DiscoveryFailed, $"Discovery failed: {ex.Message}", "discover");
        }
        finally
        {
            DiscoverButton.IsEnabled = true;
        }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isReconnecting)
        {
            StopReconnectLoop();
            SetConnectionStatus("Reconnect Stopped");
            ConnectButton.Content = "Connect";
            ConnectButton.IsEnabled = true;
            DiscoverButton.IsEnabled = true;
            return;
        }

        StopReconnectLoop();
        _suppressReconnect = false;
        if (!_apexUploaded)
        {
            SetConnectionStatus("Upload Project First");
            return;
        }

        var ip = IpTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(ip))
        {
            SetConnectionStatus("Enter an IP");
            return;
        }

        if (_isConnecting)
        {
            return;
        }

        _isConnecting = true;
        ConnectButton.IsEnabled = false;
        DiscoverButton.IsEnabled = false;
        SetConnectionStatus("Connecting...");

        try
        {
            _featureHealthRegistry.Update(new FeatureOperation("Connect", ip, "", OperationStatus.Pending, 0, null));
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
            SetConnectionStatus("Ready");
            DisconnectButton.IsEnabled = true;
            ConnectButton.Content = "Connect";
            _lastConnectedIp = ip;
            UpdateAllLogLevelsVisibility();
            if (!string.IsNullOrWhiteSpace(_projectFilePath))
            {
                _recentProjectService.RecordSuccessfulConnection(_settings, _projectFilePath, ip);
                _recentIpService.RecordRecentIp(_settings, ip);
                _settingsStore.Save(_settings);
                UpdateRecentProjectList(_projectFilePath);
            }
            _featureHealthRegistry.Update(new FeatureOperation("Connect", ip, "", OperationStatus.Confirmed, 0, null));
            ReportSuccess("CONNECT_SUCCESS", "Connection established.", ip);
        }
        catch (Exception ex)
        {
            SetConnectionStatus("Connect Failed");
            AppendAppStatus($"[error] Connect failed: {ex.Message}");
            ReportFailure("Connect", FailureCodes.TransportNotConnected, $"Connect failed: {ex.Message}", ip);
            ConnectButton.IsEnabled = true;
            DiscoverButton.IsEnabled = true;
            ConnectButton.Content = "Connect";
        }
        finally
        {
            _isConnecting = false;
        }
    }

    private async void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
        _suppressReconnect = true;
        StopReconnectLoop();
        await _transport.DisconnectAsync();
        _suppressReconnect = false;
        _rawLineNumber = 1;
        _messageFormatter.Reset(DateOnly.FromDateTime(DateTime.Today));
        _pendingLogLevelCommands.Clear();
        SetConnectionStatus("Disconnected");
        DisconnectButton.IsEnabled = false;
        ConnectButton.IsEnabled = true;
        DiscoverButton.IsEnabled = true;
        Drivers.Clear();
        UpdateAllLogLevelsVisibility();
    }

    private void StartReconnectLoop(string ip)
    {
        if (_isReconnecting)
        {
            return;
        }

        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();
        _isReconnecting = true;
        _reconnectAttempt = 0;
        SetConnectionStatus("Attempting Reconnect...");
        DisconnectButton.IsEnabled = false;
        DiscoverButton.IsEnabled = false;
        ConnectButton.IsEnabled = true;
        ConnectButton.Content = "Stop";
        UpdateAllLogLevelsVisibility();

        _ = Task.Run(() => ReconnectLoopAsync(ip, _reconnectCts.Token));
    }

    private async Task ReconnectLoopAsync(string ip, CancellationToken token)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(ReconnectInitialDelaySeconds), token);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    _isConnecting = true;
                    _reconnectAttempt++;
                    Dispatcher.Invoke(() =>
                    {
                    SetConnectionStatus($"Attempting Reconnect... {_reconnectAttempt}");
                    });
                    await _transport.ConnectAsync(ip);
                    if (!_useTcpCapture)
                    {
                        await LoadDriversAsync(ip);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        SetConnectionStatus("Ready");
                        DisconnectButton.IsEnabled = true;
                        DiscoverButton.IsEnabled = true;
                        ConnectButton.IsEnabled = false;
                        ConnectButton.Content = "Connect";
                        UpdateAllLogLevelsVisibility();
                    });

                    _lastConnectedIp = ip;
                    _isReconnecting = false;
                    _isConnecting = false;
                    return;
                }
                catch (Exception ex)
                {
                    AppendAppStatus($"[error] Reconnect failed: {ex.Message}");
                    _isConnecting = false;
                }

                await Task.Delay(TimeSpan.FromSeconds(ReconnectDelaySeconds), token);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void StopReconnectLoop()
    {
        _reconnectCts?.Cancel();
        _reconnectCts = null;
        _isReconnecting = false;
        _reconnectAttempt = 0;
        ConnectButton.Content = "Connect";
    }

    private static bool IsRemoteCloseMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.Contains("WebSocket error", StringComparison.OrdinalIgnoreCase)
            || message.Contains("closed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("close handshake", StringComparison.OrdinalIgnoreCase);
    }

    private void ReportFailure(string feature, string code, string message, string context)
    {
        var failure = new OperationFailure(code, message, context, DateTime.UtcNow);
        _featureHealthRegistry.Update(new FeatureOperation(feature, context, "", OperationStatus.Failed, 0, failure));
        _failureNotifier.AppendOperationalLog(failure);
        AppendAppStatus("FAIL", BuildStatusMessage(code, message, context));
        _failureNotifier.ShowBlockingFailure(feature, failure);
    }

    private void ReportFailure(string feature, OperationFailure failure, int retryCount = 0)
    {
        _featureHealthRegistry.Update(new FeatureOperation(feature, failure.Context, "", OperationStatus.Failed, retryCount, failure));
        _failureNotifier.AppendOperationalLog(failure);
        AppendAppStatus("FAIL", BuildStatusMessage(failure.Code, failure.Message, failure.Context));
        _failureNotifier.ShowBlockingFailure(feature, failure);
    }

    private void ReportSuccess(string code, string message, string context)
    {
        var statusMessage = BuildStatusMessage(code, message, context);
        if (!string.IsNullOrWhiteSpace(statusMessage))
        {
            AppendAppStatus("SUCCESS", statusMessage);
        }

        _failureNotifier.AppendOperationalResult(code, "SUCCESS", message, context);
    }

    private void DiscoveredCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DiscoveredCombo.SelectedItem is string selected)
        {
            if (_apexUploaded)
            {
                if (!string.Equals(selected, "Select a device...", StringComparison.Ordinal))
                {
                    IpTextBox.Text = selected;
                }
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
            Dispatcher.Invoke(() =>
            {
                InitializeProcessing(result);
                UpdateAdditionalInfoTemplateAvailability(result);
            });
            ReportSuccess("PROJECT_PARSE_SUCCESS", "Project data parsed successfully.", filePath);
        }
        catch (Exception ex)
        {
            ShowMessageOnUiThread(ex.Message, "Project Data", MessageBoxImage.Error);
            ReportFailure("ProjectData", FailureCodes.ProjectParseFailed, $"Project data parse failed: {ex.Message}", filePath);
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

        try
        {
            _featureHealthRegistry.Update(new FeatureOperation("Export", dialog.FileName, "", OperationStatus.Pending, 0, null));
            var request = BuildExportRequest();
            _exportService.Export(request, dialog.FileName);
            _featureHealthRegistry.Update(new FeatureOperation("Export", dialog.FileName, "", OperationStatus.Confirmed, 0, null));
            ReportSuccess("EXPORT_SUCCESS", "Export completed successfully.", dialog.FileName);
        }
        catch (Exception ex)
        {
            ReportFailure("Export", FailureCodes.ExportFailed, $"Export failed: {ex.Message}", dialog.FileName);
        }
    }

    private void DownloadAdditionalInfoTemplateMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (_requiredAdditionalInfoSchemas.Count == 0)
        {
            MessageBox.Show(this, "No additional info template is required for this project.", "Download Additional Info Template",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx|All files (*.*)|*.*",
            FileName = BuildAdditionalInfoTemplateFileName()
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        AdditionalInfoTemplateBuilder.Create(dialog.FileName, _requiredAdditionalInfoSchemas);
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

    private void DriverProfilesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var profiles = DriverProfileCatalog.All()
            .Select(profile => profile.DeviceName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name => !string.Equals(name, "RTI Internal", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (profiles.Count == 0)
        {
            MessageBox.Show(this, "No driver profiles are currently registered.", "Driver Profiles",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var timestampBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120));
        var rows = new StackPanel();
        foreach (var profileName in profiles)
        {
            var row = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 6),
                TextWrapping = TextWrapping.Wrap
            };

            row.Inlines.Add(new Run(profileName) { FontWeight = FontWeights.Bold });
            row.Inlines.Add(new Run($" ({GetProfileTimestampText(profileName)})") { Foreground = timestampBrush });
            rows.Children.Add(row);
        }

        var header = new TextBlock
        {
            Text = $"Current driver profiles ({profiles.Count})",
            Margin = new Thickness(0, 0, 0, 10),
            FontWeight = FontWeights.SemiBold
        };

        var content = new DockPanel
        {
            Margin = new Thickness(14)
        };
        DockPanel.SetDock(header, Dock.Top);
        content.Children.Add(header);
        content.Children.Add(new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = rows
        });

        var dialog = new Window
        {
            Owner = this,
            Title = "Driver Profiles",
            Width = 540,
            Height = 620,
            MinWidth = 420,
            MinHeight = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brushes.White,
            Content = content
        };
        dialog.ShowDialog();
    }

    private static string GetProfileTimestampText(string profileName)
    {
        if (!DriverProfileVersionCatalog.TryGetLastUpdatedUtc(profileName, out var lastUpdatedUtc))
        {
            return "last updated unknown";
        }

        var localTimestamp = lastUpdatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        return $"last updated {localTimestamp}";
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
            if (root.TryGetProperty("messageType", out var messageTypeElement))
            {
                var messageType = messageTypeElement.GetString();
                if (string.Equals(messageType, "LogLevels", StringComparison.OrdinalIgnoreCase))
                {
                    isLogLine = false;
                    return HandleLogLevels(root);
                }

                if (string.Equals(messageType, "MessageLog", StringComparison.OrdinalIgnoreCase)
                    && root.TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString();
                    if (LogLevelAckParser.TryParse(text, out var dName, out var level))
                    {
                        UpdateDriverFromLogLevel(dName, level);
                    }
                }
            }
        }
        catch (Exception)
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
            existing.OperationStatus = OperationStatus.Confirmed;
            TryResolvePendingLogLevelCommand(dName, level);
            RefreshDriverView();
        });
    }

    private async Task<bool> ApplyLogLevelCommandWithAckAsync(DriverEntry driver, int level)
    {
        driver.OperationStatus = OperationStatus.Pending;
        var retryCount = 0;

        while (true)
        {
            _featureHealthRegistry.Update(new FeatureOperation("LogLevel", driver.DName, level.ToString(CultureInfo.InvariantCulture), OperationStatus.Pending, retryCount, null));
            var dispatchResult = await _transport.SendLogLevelCommandAsync(driver.DName, level.ToString(CultureInfo.InvariantCulture));
            if (!dispatchResult.Dispatched)
            {
                driver.OperationStatus = OperationStatus.Failed;
                if (dispatchResult.Failure != null)
                {
                    _featureHealthRegistry.Update(new FeatureOperation(
                        "LogLevel",
                        driver.DName,
                        level.ToString(CultureInfo.InvariantCulture),
                        OperationStatus.Failed,
                        retryCount,
                        dispatchResult.Failure));
                }

                return false;
            }

            var acknowledged = await WaitForLogLevelAckAsync(driver.DName, level, retryCount);
            if (acknowledged)
            {
                driver.OperationStatus = OperationStatus.Confirmed;
                _featureHealthRegistry.Update(new FeatureOperation("LogLevel", driver.DName, level.ToString(CultureInfo.InvariantCulture), OperationStatus.Confirmed, retryCount, null));
                var driverName = string.IsNullOrWhiteSpace(driver.Name) ? driver.DName : driver.Name;
                _failureNotifier.AppendOperationalResult(
                    "LOGLEVEL_ACK_INFO",
                    "INFO",
                    $"{driverName} Log level acknowledged at {level}.",
                    BuildLogLevelSuccessContext(driver, level, retryCount));
                return true;
            }

            if (retryCount >= LogLevelAckMaxRetryCount)
            {
                driver.OperationStatus = OperationStatus.Failed;
                var failure = new OperationFailure(
                    FailureCodes.LogLevelAckTimeout,
                    $"Log level for {driver.DName} did not acknowledge level {level} after retry.",
                    $"dName={driver.DName};level={level}",
                    DateTime.UtcNow);
                ReportFailure("LogLevel", failure, retryCount);
                return false;
            }

            retryCount++;
        }
    }

    private async Task<bool> WaitForLogLevelAckAsync(string dName, int level, int retryCount)
    {
        var key = BuildLogLevelAckKey(dName);
        var timeoutSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(LogLevelAckTimeoutMilliseconds));
        _pendingLogLevelCommands[key] = new PendingLogLevelCommand(level, retryCount, timeoutSource);
        try
        {
            while (!timeoutSource.IsCancellationRequested)
            {
                await Task.Delay(50, timeoutSource.Token);
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            return !_pendingLogLevelCommands.ContainsKey(key);
        }
        finally
        {
            if (_pendingLogLevelCommands.TryGetValue(key, out var pending)
                && ReferenceEquals(pending.TimeoutSource, timeoutSource))
            {
                _pendingLogLevelCommands.Remove(key);
            }
            timeoutSource.Dispose();
        }
    }

    private static string BuildLogLevelSuccessContext(DriverEntry driver, int level, int retryCount)
    {
        if (driver == null)
        {
            return $"level={level};retry={retryCount}";
        }

        var baseContext = $"dName={driver.DName};level={level};retry={retryCount}";
        return string.IsNullOrWhiteSpace(driver.Name)
            ? baseContext
            : $"{baseContext};name={driver.Name}";
    }

    private void TryResolvePendingLogLevelCommand(string dName, int level)
    {
        var key = BuildLogLevelAckKey(dName);
        if (!_pendingLogLevelCommands.TryGetValue(key, out var pending))
        {
            return;
        }

        if (pending.RequestedLevel != level)
        {
            return;
        }

        _pendingLogLevelCommands.Remove(key);
        pending.TimeoutSource.Cancel();
    }

    private static string BuildLogLevelAckKey(string dName)
    {
        return dName.Trim().ToUpperInvariant();
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

    private void AppendAppStatus(string line, bool allowEmpty = false)
    {
        Dispatcher.Invoke(() =>
        {
            if (!allowEmpty && string.IsNullOrWhiteSpace(line))
            {
                return;
            }
            var trimmed = line.Trim();
            if (TryParseStatusLine(trimmed, out var level, out var message))
            {
                AppendAppStatus(level, message);
                return;
            }

            AppendAppStatus("INFO", trimmed);
        });
    }

    private void AppendAppStatus(string level, string message)
    {
        var paragraph = AppStatusTextBox.Document.Blocks.OfType<Paragraph>().FirstOrDefault();
        if (paragraph == null)
        {
            paragraph = new Paragraph { Margin = new Thickness(0) };
            AppStatusTextBox.Document.Blocks.Add(paragraph);
        }

        var normalizedLevel = NormalizeStatusLevel(level);
        var badge = $"[{normalizedLevel}]";
        paragraph.Inlines.Add(new Run(badge)
        {
            FontWeight = FontWeights.SemiBold,
            Foreground = GetStatusBrush(normalizedLevel)
        });
        paragraph.Inlines.Add(new Run($" {message}"));
        paragraph.Inlines.Add(new LineBreak());
        AppStatusTextBox.ScrollToEnd();
    }

    private void SetConnectionStatus(string status)
    {
        StatusText.Text = status;
    }

    private static string BuildStatusMessage(string code, string message, string context)
    {
        return code switch
        {
            "SETTINGS_LOAD_SUCCESS" => "Settings loaded.",
            "PROJECT_PARSE_SUCCESS" => "Project data parsed.",
            "CONNECT_SUCCESS" => "",
            _ => message.Replace(" successfully.", ".", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static bool TryParseStatusLine(string line, out string level, out string message)
    {
        level = "INFO";
        message = line;

        if (line.StartsWith("[warn]", StringComparison.OrdinalIgnoreCase))
        {
            level = "WARN";
            message = line.Substring(6).Trim();
            return true;
        }

        if (line.StartsWith("[error]", StringComparison.OrdinalIgnoreCase))
        {
            level = "FAIL";
            message = line.Substring(7).Trim();
            return true;
        }

        if (line.StartsWith("[success]", StringComparison.OrdinalIgnoreCase))
        {
            level = "SUCCESS";
            message = line.Substring(9).Trim();
            return true;
        }

        if (line.StartsWith("[failed]", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[fail]", StringComparison.OrdinalIgnoreCase))
        {
            level = "FAIL";
            message = line[(line.IndexOf(']') + 1)..].Trim();
            return true;
        }

        if (line.StartsWith("[local]", StringComparison.OrdinalIgnoreCase))
        {
            level = "INFO";
            message = line.Substring(7).Trim();
            return true;
        }

        if (line.StartsWith("[info]", StringComparison.OrdinalIgnoreCase))
        {
            level = "INFO";
            message = line.Substring(6).Trim();
            return true;
        }

        return false;
    }

    private static string NormalizeStatusLevel(string level)
    {
        var normalized = level.Trim().ToUpperInvariant();
        return normalized switch
        {
            "FAILED" => "FAIL",
            "ERROR" => "FAIL",
            _ => normalized
        };
    }

    private static Brush GetStatusBrush(string level)
    {
        return level switch
        {
            "SUCCESS" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F7D32")),
            "WARN" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B26A00")),
            "FAIL" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B00020")),
            _ => Brushes.Black
        };
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
            _featureHealthRegistry.Update(new FeatureOperation("LoadDrivers", ip, "", OperationStatus.Pending, 0, null));
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

            ReportDriverLoadBreakdown(ip, list);
            await ForceDiagnosticsLogLevelAsync(list);
            _featureHealthRegistry.Update(new FeatureOperation("LoadDrivers", ip, "", OperationStatus.Confirmed, 0, null));
        }
        catch (Exception ex)
        {
            AppendAppStatus($"[error] Failed to load drivers: {ex.Message}");
            ReportFailure("LoadDrivers", FailureCodes.DriverLoadFailed, $"Failed to load drivers: {ex.Message}", ip);
        }
    }

    private async Task ForceDiagnosticsLogLevelAsync(IReadOnlyList<DiagnosticsTransport.DriverInfo> drivers)
    {
        if (drivers == null || drivers.Count == 0)
        {
            return;
        }

        if (!DiagnosticsDriverSelector.TryGetDiagnosticsDriverDName(drivers, out var dName))
        {
            AppendAppStatus("[warn] Diagnostics driver not found; skipping log level pin.");
            return;
        }

        try
        {
            var driver = Drivers.FirstOrDefault(d => d.DName.Equals(dName, StringComparison.OrdinalIgnoreCase));
            if (driver == null)
            {
                driver = new DriverEntry(ParseDriverId(dName), dName, dName);
                Drivers.Add(driver);
            }

            var acknowledged = await ApplyLogLevelCommandWithAckAsync(driver, 3);
            ReportLogLevelBatchStatus("connect", acknowledged ? 1 : 0, 1);
        }
        catch (Exception ex)
        {
            AppendAppStatus($"[warn] Failed to set {dName} to 3: {ex.Message}");
        }
    }

    private void ReportDriverLoadBreakdown(string ip, IReadOnlyList<DiagnosticsTransport.DriverInfo> projectDrivers)
    {
        var projectNames = projectDrivers
            .Select(driver => string.IsNullOrWhiteSpace(driver.Name) ? driver.DName : driver.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(SanitizeContextValue)
            .ToList();

        List<string> populatedSystemDrivers = new();
        List<string> missingSystemDrivers = new();
        Dispatcher.Invoke(() =>
        {
            foreach (var anchor in AnchorNames)
            {
                var existing = Drivers.FirstOrDefault(driver => driver.DName.Equals(anchor, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    missingSystemDrivers.Add(anchor);
                    continue;
                }

                populatedSystemDrivers.Add(SanitizeContextValue(string.IsNullOrWhiteSpace(existing.Name) ? anchor : existing.Name));
            }
        });

        var projectContext = $"ip={ip};count={projectDrivers.Count};drivers={JoinContextValues(projectNames)}";
        ReportSuccess("DRIVER_LOAD_PROJECT_SUCCESS", $"Loaded {projectDrivers.Count} project drivers.", projectContext);

        var systemContext =
            $"ip={ip};count={populatedSystemDrivers.Count};drivers={JoinContextValues(populatedSystemDrivers)};missing={JoinContextValues(missingSystemDrivers)}";
        ReportSuccess("DRIVER_LOAD_SYSTEM_SUCCESS", $"Loaded {populatedSystemDrivers.Count} system drivers.", systemContext);
    }

    private static string JoinContextValues(IReadOnlyCollection<string> values)
    {
        return values.Count == 0 ? "(none)" : string.Join(",", values);
    }

    private static string SanitizeContextValue(string value)
    {
        return value
            .Replace(";", ":", StringComparison.Ordinal)
            .Replace("|", "/", StringComparison.Ordinal)
            .Trim();
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
        RecordNoProfileMessages(_processedLogLines);
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

            RecordNoProfileMessage(line);
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

        RecordNoProfileMessage(line);
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

    private void RecordNoProfileMessages(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            RecordNoProfileMessage(line);
        }
    }

    private void RecordNoProfileMessage(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || !line.Contains("[No Profile!]", StringComparison.Ordinal))
        {
            return;
        }

        var raw = line.Replace(" [No Profile!]", "", StringComparison.Ordinal);
        raw = StripLeadingLineNumber(raw);
        raw = StripLeadingTimestamp(raw);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var driverName = ExtractNoProfileDriverName(raw);
        if (!_noProfileMessages.TryGetValue(driverName, out var messages))
        {
            messages = new HashSet<string>(StringComparer.Ordinal);
            _noProfileMessages[driverName] = messages;
        }

        messages.Add(raw);
    }

    private static string StripLeadingLineNumber(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var spaceIndex = text.IndexOf(' ');
        if (spaceIndex <= 0)
        {
            return text;
        }

        var prefix = text.Substring(0, spaceIndex);
        return int.TryParse(prefix, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ? text.Substring(spaceIndex + 1) : text;
    }

    private static string StripLeadingTimestamp(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var trimmed = text.TrimStart();
        if (!trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            return text;
        }

        var closeIndex = trimmed.IndexOf(']');
        if (closeIndex < 0)
        {
            return text;
        }

        var remainder = trimmed.Substring(closeIndex + 1).TrimStart();
        return string.IsNullOrWhiteSpace(remainder) ? text : remainder;
    }

    private static string ExtractNoProfileDriverName(string rawText)
    {
        var match = NoProfileDriverCommandPattern.Match(rawText);
        if (match.Success)
        {
            return match.Groups["driver"].Value.Trim();
        }

        match = NoProfileDriverEventPattern.Match(rawText);
        if (match.Success)
        {
            return match.Groups["driver"].Value.Trim();
        }

        return "Unknown Driver";
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        try
        {
            if (_noProfileMessages.Count == 0)
            {
                return;
            }

            var report = BuildNoProfileReport();
            var reportPath = WriteNoProfileReportToDesktop(report);
            MessageBox.Show(
                this,
                $"Saved no-profile driver report to your Desktop:\n{reportPath}\n\nPlease email this file to feeny.jamie@gmail.com.",
                "Beta Report",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Failed to write no-profile driver report: {ex.Message}",
                "Beta Report",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            _noProfileMessages.Clear();
        }
    }

    private NoProfileReport BuildNoProfileReport()
    {
        var drivers = _noProfileMessages
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new NoProfileDriverReport(
                entry.Key,
                entry.Value.OrderBy(message => message, StringComparer.Ordinal).ToList()))
            .ToList();

        return new NoProfileReport(
            DateTime.UtcNow,
            GetAppVersion(),
            drivers);
    }

    private static string GetAppVersion()
    {
        return typeof(MainWindow).Assembly.GetName().Version?.ToString() ?? "unknown";
    }

    private static string WriteNoProfileReportToDesktop(NoProfileReport report)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var fileName = $"oracle_no_profile_messages_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var path = Path.Combine(desktop, fileName);
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        File.WriteAllText(path, json);
        return path;
    }

    private sealed record NoProfileReport(
        DateTime CreatedUtc,
        string AppVersion,
        List<NoProfileDriverReport> Drivers);

    private sealed record NoProfileDriverReport(
        string DriverName,
        List<string> Messages);

    private void UpdateAdditionalInfoTemplateAvailability(ProjectDataExtractionResult result)
    {
        _requiredAdditionalInfoSchemas = AdditionalInfoTemplatePlanner.DetermineSchemas(result.ApexDiscoveryPreload.DriverConfigMap.Values)
            .ToList();
        DownloadAdditionalInfoTemplateMenuItem.IsEnabled = _requiredAdditionalInfoSchemas.Count > 0;

        if (_requiredAdditionalInfoSchemas.Count == 0)
        {
            return;
        }

    }

    private string BuildAdditionalInfoTemplateFileName()
    {
        if (string.IsNullOrWhiteSpace(_projectFilePath))
        {
            return "Additional Info Template.xlsx";
        }

        var baseName = Path.GetFileNameWithoutExtension(_projectFilePath);
        return $"{baseName} Additional Info Template.xlsx";
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

    private sealed class PendingLogLevelCommand
    {
        public PendingLogLevelCommand(int requestedLevel, int retryCount, CancellationTokenSource timeoutSource)
        {
            RequestedLevel = requestedLevel;
            RetryCount = retryCount;
            TimeoutSource = timeoutSource;
        }

        public int RequestedLevel { get; }
        public int RetryCount { get; }
        public CancellationTokenSource TimeoutSource { get; }
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
        var level = isOn ? driver.SelectedLevel : 0;
        await ApplyLogLevelCommandWithAckAsync(driver, level);
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

        await ApplyLogLevelCommandWithAckAsync(driver, level);
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

        var ackCount = 0;
        foreach (var driver in Drivers)
        {
            if (await ApplyLogLevelCommandWithAckAsync(driver, 3))
            {
                ackCount++;
            }
        }

        ReportLogLevelBatchStatus("all", ackCount, Drivers.Count);
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

        var ackCount = 0;
        foreach (var driver in Drivers)
        {
            var level = targets.Contains(driver.DName) ? 3 : 0;
            if (await ApplyLogLevelCommandWithAckAsync(driver, level))
            {
                ackCount++;
            }
        }

        ReportLogLevelBatchStatus("system", ackCount, Drivers.Count);
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

        var ackCount = 0;
        foreach (var driver in Drivers)
        {
            if (await ApplyLogLevelCommandWithAckAsync(driver, 0))
            {
                ackCount++;
            }
        }

        ReportLogLevelBatchStatus("none", ackCount, Drivers.Count);
    }

    private void ReportLogLevelBatchStatus(string mode, int acknowledgedCount, int totalCount)
    {
        var safeTotal = Math.Max(totalCount, 0);
        var safeAck = Math.Clamp(acknowledgedCount, 0, safeTotal);
        var message = $"Status of {safeAck}/{safeTotal} driver log levels acknowledged.";
        var context = $"mode={mode};ack={safeAck};total={safeTotal}";

        if (safeAck == safeTotal)
        {
            ReportSuccess("LOGLEVEL_BATCH_CONFIRM_SUCCESS", message, context);
            return;
        }

        AppendAppStatus("WARN", message);
        _failureNotifier.AppendOperationalResult("LOGLEVEL_BATCH_CONFIRM_WARN", "WARN", message, context);
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
        private OperationStatus _operationStatus;

        public DriverEntry(int id, string name, string dName)
        {
            Id = id;
            _name = name;
            DName = dName;
            SelectedLevel = 3;
            OperationStatus = OperationStatus.Confirmed;
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

        public OperationStatus OperationStatus
        {
            get => _operationStatus;
            set
            {
                if (_operationStatus == value)
                {
                    return;
                }

                _operationStatus = value;
                OnPropertyChanged(nameof(OperationStatus));
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
