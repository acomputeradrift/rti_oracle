using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace SHPDiagnosticsViewer;

public partial class MainWindow : Window
{
    private const int MaxLogChars = 200_000;
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _socketCts;
    private bool _isConnecting;
    private string? _currentIp;
    private readonly Dictionary<string, string> _friendlyNames = new(StringComparer.OrdinalIgnoreCase);
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
    }

    private async void DiscoverButton_Click(object sender, RoutedEventArgs e)
    {
        DiscoverButton.IsEnabled = false;
        StatusText.Text = "Discovering...";

        try
        {
            var results = await DiscoverAsync(TimeSpan.FromSeconds(2));
            DiscoveredCombo.ItemsSource = results.OrderBy(ip => ip).ToList();
            if (results.Count == 1)
            {
                IpTextBox.Text = results[0];
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
            await ConnectAsync(ip);
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
        await DisconnectAsync();
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
            IpTextBox.Text = selected;
        }
    }

    private async Task<List<string>> DiscoverAsync(TimeSpan timeout)
    {
        var candidates = await DiscoverCandidatesAsync(timeout);
        var verified = await FilterRtiProcessorsAsync(candidates);
        if (verified.Count > 0)
        {
            return verified;
        }

        var subnetHits = await ScanLocalSubnetAsync();
        return subnetHits;
    }

    private async Task<List<string>> DiscoverCandidatesAsync(TimeSpan timeout)
    {
        var results = new HashSet<string>();
        using var udp = new UdpClient(AddressFamily.InterNetwork);
        udp.EnableBroadcast = true;
        udp.MulticastLoopback = false;

        var request =
            "M-SEARCH * HTTP/1.1\r\n" +
            "HOST: 239.255.255.250:1900\r\n" +
            "MAN: \"ssdp:discover\"\r\n" +
            "MX: 1\r\n" +
            "ST: ssdp:all\r\n\r\n";

        var data = Encoding.ASCII.GetBytes(request);
        await udp.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900));

        var stopAt = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < stopAt)
        {
            var remaining = stopAt - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            var receiveTask = udp.ReceiveAsync();
            var completed = await Task.WhenAny(receiveTask, Task.Delay(remaining));
            if (completed != receiveTask)
            {
                break;
            }

            var result = await receiveTask;
            var ip = result.RemoteEndPoint.Address.ToString();
            results.Add(ip);
        }

        return results.ToList();
    }

    private async Task<List<string>> FilterRtiProcessorsAsync(List<string> candidates)
    {
        var matches = new List<string>();
        foreach (var ip in candidates)
        {
            if (await IsRtiProcessorAsync(ip))
            {
                matches.Add(ip);
            }
        }
        return matches;
    }

    private async Task<List<string>> ScanLocalSubnetAsync()
    {
        var localIp = GetLocalIPv4();
        if (string.IsNullOrWhiteSpace(localIp))
        {
            AppendLog("[error] Unable to determine local IP for subnet scan.");
            return new List<string>();
        }

        var parts = localIp.Split('.');
        if (parts.Length != 4)
        {
            return new List<string>();
        }

        var prefix = $"{parts[0]}.{parts[1]}.{parts[2]}";
        AppendLog($"[info] SSDP discovery empty, scanning subnet {prefix}.0/24");

        var candidates = Enumerable.Range(1, 254).Select(i => $"{prefix}.{i}").ToList();
        var results = new List<string>();
        using var gate = new SemaphoreSlim(32);
        var tasks = candidates.Select(async ip =>
        {
            await gate.WaitAsync();
            try
            {
                if (await IsRtiProcessorAsync(ip))
                {
                    lock (results)
                    {
                        results.Add(ip);
                    }
                }
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.Distinct().ToList();
    }

    private static string? GetLocalIPv4()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ua.Address.ToString();
                }
            }
        }

        return null;
    }

    private async Task ConnectAsync(string ip)
    {
        await DisconnectAsync();

        _currentIp = ip;
        _friendlyNames.Clear();
        _socket = new ClientWebSocket();
        _socket.Options.SetRequestHeader("Origin", $"http://{ip}");
        _socketCts = new CancellationTokenSource();

        var uri = new Uri($"ws://{ip}:1234/diagnosticswss");
        await _socket.ConnectAsync(uri, _socketCts.Token);

        AppendLog("[info] Connected to WebSocket");

        await SendSubscribeAsync("MessageLog", "true");
        await SendSubscribeAsync("Sysvar", "true");

        _ = Task.Run(() => ReceiveLoopAsync(_socket, _socketCts.Token));

        await LoadDriversAsync(ip);
    }

    private async Task DisconnectAsync()
    {
        try
        {
            _socketCts?.Cancel();
        }
        catch
        {
        }

        if (_socket != null)
        {
            try
            {
                if (_socket.State == WebSocketState.Open)
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
            catch
            {
            }
            finally
            {
                _socket.Dispose();
                _socket = null;
            }
        }
    }

    private async Task SendSubscribeAsync(string resource, string value)
    {
        var payload = new
        {
            type = "Subscribe",
            resource,
            value
        };

        await SendJsonAsync(payload);
    }

    private async Task SendLogLevelAsync(string type, string level)
    {
        var payload = new
        {
            type = "Subscribe",
            resource = "LogLevel",
            value = new
            {
                type,
                level
            }
        };

        await SendJsonAsync(payload);
    }

    private async Task SendJsonAsync<T>(T payload)
    {
        if (_socket == null || _socket.State != WebSocketState.Open)
        {
            return;
        }

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _socketCts?.Token ?? CancellationToken.None);
    }

    private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken token)
    {
        var buffer = new byte[8192];
        while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            try
            {
                var messageBuffer = new ArraySegment<byte>(buffer);
                using var stream = new System.IO.MemoryStream();
                WebSocketReceiveResult? result;
                do
                {
                    result = await socket.ReceiveAsync(messageBuffer, token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await Dispatcher.InvokeAsync(async () => await DisconnectAsync());
                        return;
                    }

                    stream.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                var text = Encoding.UTF8.GetString(stream.ToArray());
                var line = FormatMessage(text);
                AppendLog(line);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                AppendLog($"[error] WebSocket error: {ex.Message}");
                await Dispatcher.InvokeAsync(async () => await DisconnectAsync());
                return;
            }
        }
    }

    private string FormatMessage(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (root.TryGetProperty("messageType", out var messageTypeElement))
            {
                var messageType = messageTypeElement.GetString() ?? "Unknown";
                if (string.Equals(messageType, "echo", StringComparison.OrdinalIgnoreCase))
                {
                    var msg = root.TryGetProperty("message", out var msgEl) ? msgEl.GetString() ?? "" : "";
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        try
                        {
                            using var inner = JsonDocument.Parse(msg);
                            var innerRoot = inner.RootElement;
                            if (innerRoot.TryGetProperty("type", out var t) && innerRoot.TryGetProperty("resource", out var r))
                            {
                                var type = t.GetString();
                                var res = r.GetString();
                                return $"Echo {type}/{res}";
                            }
                        }
                        catch
                        {
                        }
                        return $"Echo {msg}";
                    }
                    return "Echo";
                }

                if (string.Equals(messageType, "LogLevels", StringComparison.OrdinalIgnoreCase))
                {
                    var summary = HandleLogLevels(root);
                    return summary;
                }

                if (string.Equals(messageType, "MessageLog", StringComparison.OrdinalIgnoreCase))
                {
                    var time = root.TryGetProperty("time", out var timeElement) ? timeElement.GetString() : "";
                    var text = root.TryGetProperty("text", out var textElement) ? textElement.GetString() : "";
                    return $"[{time}] {text}".Trim();
                }

                if (string.Equals(messageType, "Sysvar", StringComparison.OrdinalIgnoreCase))
                {
                    var id = root.TryGetProperty("sysvarid", out var idElement) ? idElement.ToString() : "?";
                    var val = root.TryGetProperty("sysvarval", out var valElement) ? valElement.ToString() : "?";
                    return $"Sysvar id={id} val={val}";
                }

                return $"{messageType} {raw}";
            }

            if (root.TryGetProperty("type", out var typeElement) && root.TryGetProperty("resource", out var resElement))
            {
                var type = typeElement.GetString();
                var resource = resElement.GetString();
                return $"{type}/{resource} {raw}";
            }
        }
        catch
        {
        }

        return raw;
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

    private void AppendLog(string line)
    {
        Dispatcher.Invoke(() =>
        {
            if (string.IsNullOrWhiteSpace(line))
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
        });
    }

    private async Task LoadDriversAsync(string ip)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            var url = $"http://{ip}:5000/diagnostics/data/drivers";
            string json;
            try
            {
                json = await http.GetStringAsync(url);
            }
            catch (TaskCanceledException)
            {
                json = await http.GetStringAsync(url);
            }

            var list = ParseDrivers(json);

            Dispatcher.Invoke(() =>
            {
                foreach (var entry in list)
                {
                    var existing = Drivers.FirstOrDefault(d => d.DName.Equals(entry.DName, StringComparison.OrdinalIgnoreCase));
                    if (existing == null)
                    {
                        Drivers.Add(entry);
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

    private List<DriverEntry> ParseDrivers(string json)
    {
        var results = new List<DriverEntry>();
        using var doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("drivers", out var driversElement))
        {
            root = driversElement;
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                if (TryBuildDriverEntry(item, out var entry))
                {
                    results.Add(entry);
                }
            }
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        if (TryBuildDriverEntry(item, out var entry))
                        {
                            results.Add(entry);
                        }
                    }
                }
                else if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    if (TryBuildDriverEntry(property.Value, out var entry))
                    {
                        results.Add(entry);
                    }
                }
            }
        }

        return results;
    }

    private bool TryBuildDriverEntry(JsonElement item, out DriverEntry entry)
    {
        entry = null!;
        if (item.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!item.TryGetProperty("id", out var idElement))
        {
            return false;
        }

        var id = idElement.GetInt32();
        var dName = $"DRIVER//{id}";
        var name = item.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? dName : dName;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = dName;
        }

        _friendlyNames[dName] = name;
        entry = new DriverEntry(id, name, dName);
        return true;
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

    private async Task<bool> IsRtiProcessorAsync(string ip)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(750) };
            var url = $"http://{ip}:5000/diagnostics/data/drivers";
            var json = await http.GetStringAsync(url);
            return !string.IsNullOrWhiteSpace(json);
        }
        catch
        {
            return false;
        }
    }

    private async void DriverToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton toggle || toggle.DataContext is not DriverEntry driver)
        {
            return;
        }

        if (_socket == null || _socket.State != WebSocketState.Open)
        {
            driver.IsEnabled = false;
            return;
        }

        var isOn = toggle.IsChecked == true;
        driver.IsEnabled = isOn;
        var level = isOn ? driver.SelectedLevel.ToString() : "0";
        await SendLogLevelAsync(driver.DName, level);
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
        if (_socket == null || _socket.State != WebSocketState.Open)
        {
            return;
        }

        await SendLogLevelAsync(driver.DName, level.ToString());
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
