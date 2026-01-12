using System;
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

namespace SHPDiagnosticsViewer;

public partial class MainWindow : Window
{
    private const int MaxLogChars = 200_000;
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _socketCts;
    private bool _isConnecting;
    private string? _currentIp;

    public ObservableCollection<DriverEntry> Drivers { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
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

    private void DiscoveredCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
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
            // ignore
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
                // ignore
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
            // Ignore JSON errors and fall through to raw output.
        }

        return raw;
    }

    private void AppendLog(string line)
    {
        Dispatcher.Invoke(() =>
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            var newText = LogTextBox.Text + line + Environment.NewLine;
            if (newText.Length > MaxLogChars)
            {
                newText = newText.Substring(newText.Length - MaxLogChars);
            }

            LogTextBox.Text = newText;
            LogTextBox.CaretIndex = LogTextBox.Text.Length;
            LogTextBox.ScrollToEnd();
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
                // One quick retry in case of slow response.
                json = await http.GetStringAsync(url);
            }
            var list = ParseDrivers(json);

            Dispatcher.Invoke(() =>
            {
                Drivers.Clear();
                foreach (var entry in list)
                {
                    Drivers.Add(entry);
                }
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

    private static bool TryBuildDriverEntry(JsonElement item, out DriverEntry entry)
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
        var name = $"DRIVER//{id}";
        entry = new DriverEntry(id, name);
        return true;
    }

    private async Task<bool> IsRtiProcessorAsync(string ip)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(750) };
            var url = $"http://{ip}:5000/diagnostics/data/drivers";
            var json = await http.GetStringAsync(url);
            var list = ParseDrivers(json);
            return list.Count > 0;
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

        var level = driver.SelectedLevel.ToString();
        await SendLogLevelAsync(driver.TypeName, toggle.IsChecked == true ? level : "0");
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
        if (_socket == null || _socket.State != WebSocketState.Open)
        {
            return;
        }

        if (driver.IsEnabled)
        {
            await SendLogLevelAsync(driver.TypeName, level.ToString());
        }
    }

    public class DriverEntry : INotifyPropertyChanged
    {
        private bool _isEnabled;
        private int _selectedLevel;

        public DriverEntry(int id, string name)
        {
            Id = id;
            Name = name;
            SelectedLevel = 3;
        }

        public int Id { get; }
        public string Name { get; }

        public string TypeName => $"DRIVER//{Id}";

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
}
