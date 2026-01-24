# WebSocket Diagnostics Feed - All Info (Verbatim Sources)

Source: RawDiagnosticsFeeds/WebsocketDiagnostics/decode_ws.py
```python
import re
import sys


def decode_hex_line(line: str) -> str:
    line = line.strip()
    if not line:
        return ""
    try:
        data = bytes.fromhex(line)
    except ValueError:
        return ""

    if len(data) >= 2 and sum(1 for b in data[1::2] if b == 0) > len(data) / 4:
        for encoding in ("utf-16le", "utf-8", "latin1"):
            try:
                return data.decode(encoding, errors="ignore")
            except Exception:
                continue
    else:
        for encoding in ("utf-8", "latin1", "utf-16le"):
            try:
                return data.decode(encoding, errors="ignore")
            except Exception:
                continue

    return ""


def clean_text(text: str) -> str:
    text = text.replace("\r\n", "\n").replace("\r", "\n")
    cleaned = []
    for ch in text:
        if ch == "\n":
            cleaned.append(ch)
            continue
        codepoint = ord(ch)
        if 32 <= codepoint <= 126:
            cleaned.append(ch)
    return "".join(cleaned)


def main() -> int:
    input_path = sys.argv[1] if len(sys.argv) > 1 else "wireshark_output_7.txt"
    try:
        with open(input_path, "r", encoding="ascii", errors="ignore") as handle:
            raw_lines = handle.readlines()
    except OSError as exc:
        print(f"Failed to read {input_path}: {exc}", file=sys.stderr)
        return 1

    decoded_chunks = []
    for raw_line in raw_lines:
        decoded = decode_hex_line(raw_line)
        cleaned = clean_text(decoded)
        if cleaned:
            decoded_chunks.append(cleaned)

    full_text = "\n".join(decoded_chunks)
    raw_lines = [line.strip() for line in full_text.splitlines() if line.strip()]

    prefixes = ("Input", "Driver", "System Manager", "Macro")
    normalized_lines = []
    for line in raw_lines:
        match = re.search(r"(Input|Driver|System Manager|Macro|hello)", line)
        if match and match.start() > 0:
            line = line[match.start():]
        normalized_lines.append(line.strip())

    date_pattern = re.compile(r"\d{2}/\d{2}/\d{4}")
    logical_lines = []
    for line in normalized_lines:
        if not line or not any(ch.isalnum() for ch in line):
            continue
        if line.strip() == "hello":
            continue
        if line.strip().isdigit():
            continue
        if (
            not date_pattern.search(line)
            and logical_lines
            and (
                line.startswith("Driver event")
                or line.startswith("Driver - Command")
                or line.startswith("System Manager")
                or (not line.startswith(prefixes) and not line[0].isdigit())
            )
        ):
            logical_lines[-1] = f"{logical_lines[-1]} {line}".strip()
            continue
        if line.startswith(prefixes) or line[0].isdigit():
            logical_lines.append(line)
        else:
            if logical_lines:
                logical_lines[-1] = f"{logical_lines[-1]} {line}".strip()
            else:
                logical_lines.append(line)

    cleaned_lines = []
    for line in logical_lines:
        line = line.replace("Driver e vent", "Driver event")
        line = re.sub(r"\[Schedule\s+Driver event", "[ScheduledTasks] Driver event", line)
        line = re.sub(r"(Sustain:NO)\s+[A-Za-z]{1,3}$", r"\1", line)
        line = re.sub(r"([)\'\"])\s+[A-Za-z]{1,3}$", r"\1", line)
        cleaned_lines.append(line)

    for idx, line in enumerate(cleaned_lines, 1):
        print(f"{idx}: {line}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
```

Source: SHPDiagnosticsViewer/DiagnosticsTransport/IDiagnosticsTransport.cs
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SHPDiagnosticsViewer.DiagnosticsTransport;

public interface IDiagnosticsTransport
{
    event EventHandler<string>? RawMessageReceived;
    event EventHandler<string>? TransportInfo;
    event EventHandler<string>? TransportError;

    bool IsConnected { get; }

    Task<List<string>> DiscoverAsync(TimeSpan timeout);
    Task ConnectAsync(string ip);
    Task DisconnectAsync();
    Task SendLogLevelAsync(string type, string level);
    Task<List<DriverInfo>> LoadDriversAsync(string ip);
}
```

Source: SHPDiagnosticsViewer/DiagnosticsTransport/DriverInfo.cs
```csharp
namespace SHPDiagnosticsViewer.DiagnosticsTransport;

public sealed class DriverInfo
{
    public DriverInfo(int id, string name, string dName)
    {
        Id = id;
        Name = name;
        DName = dName;
    }

    public int Id { get; }
    public string Name { get; }
    public string DName { get; }
}
```

Source: SHPDiagnosticsViewer/DiagnosticsTransport/LegacyWebSocketDiagnosticsTransport.cs
```csharp
using System;
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

namespace SHPDiagnosticsViewer.DiagnosticsTransport;

// Legacy transport retained for compatibility with existing behavior.
public sealed class LegacyWebSocketDiagnosticsTransport : IDiagnosticsTransport
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _socketCts;

    public event EventHandler<string>? RawMessageReceived;
    public event EventHandler<string>? TransportInfo;
    public event EventHandler<string>? TransportError;

    public bool IsConnected => _socket != null && _socket.State == WebSocketState.Open;

    public async Task<List<string>> DiscoverAsync(TimeSpan timeout)
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

    public async Task ConnectAsync(string ip)
    {
        await DisconnectAsync();

        _socket = new ClientWebSocket();
        _socket.Options.SetRequestHeader("Origin", $"http://{ip}");
        _socketCts = new CancellationTokenSource();

        var uri = new Uri($"ws://{ip}:1234/diagnosticswss");
        await _socket.ConnectAsync(uri, _socketCts.Token);

        EmitInfo("[info] Connected to WebSocket");

        await SendSubscribeAsync("MessageLog", "true");
        await SendSubscribeAsync("Sysvar", "true");

        _ = Task.Run(() => ReceiveLoopAsync(_socket, _socketCts.Token));
    }

    public async Task DisconnectAsync()
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

    public async Task SendLogLevelAsync(string type, string level)
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

    public async Task<List<DriverInfo>> LoadDriversAsync(string ip)
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

        return ParseDrivers(json);
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
            EmitError("[error] Unable to determine local IP for subnet scan.");
            return new List<string>();
        }

        var parts = localIp.Split('.');
        if (parts.Length != 4)
        {
            return new List<string>();
        }

        var prefix = $"{parts[0]}.{parts[1]}.{parts[2]}";
        EmitInfo($"[info] SSDP discovery empty, scanning subnet {prefix}.0/24");

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
                        await DisconnectAsync();
                        return;
                    }

                    stream.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                var text = Encoding.UTF8.GetString(stream.ToArray());
                RawMessageReceived?.Invoke(this, text);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                EmitError($"[error] WebSocket error: {ex.Message}");
                await DisconnectAsync();
                return;
            }
        }
    }

    private List<DriverInfo> ParseDrivers(string json)
    {
        var results = new List<DriverInfo>();
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
                if (TryBuildDriverInfo(item, out var entry))
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
                        if (TryBuildDriverInfo(item, out var entry))
                        {
                            results.Add(entry);
                        }
                    }
                }
                else if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    if (TryBuildDriverInfo(property.Value, out var entry))
                    {
                        results.Add(entry);
                    }
                }
            }
        }

        return results;
    }

    private static bool TryBuildDriverInfo(JsonElement item, out DriverInfo entry)
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

        entry = new DriverInfo(id, name, dName);
        return true;
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

    private void EmitInfo(string message)
    {
        TransportInfo?.Invoke(this, message);
    }

    private void EmitError(string message)
    {
        TransportError?.Invoke(this, message);
    }
}
```

Source: SHPDiagnosticsViewer/MainWindow.xaml.cs
```csharp
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Microsoft.Win32;
using SHPDiagnosticsViewer.DiagnosticsTransport;

namespace SHPDiagnosticsViewer;

public partial class MainWindow : Window
{
    private const int MaxLogChars = 200_000;
    private IDiagnosticsTransport _transport;
    private bool _isConnecting;
    private bool _useTcpCapture;
    private int _rawLineNumber = 1;
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

        var formattedLine = FormatMessage(raw);
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
    }

    private void ClearDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        RawLogTextBox.Clear();
        _rawLineNumber = 1;
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
```

Source: SHPDiagnosticsViewer/MainWindow.xaml
```xml
<Window x:Class="SHPDiagnosticsViewer.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="RTI Oracle by FP&amp;C Ltd." Height="720" Width="1000"
        MinHeight="480" MinWidth="720">
  <Window.Resources>
    <Style x:Key="LevelButtonStyle" TargetType="Button">
      <Setter Property="Width" Value="28" />
      <Setter Property="Height" Value="22" />
      <Setter Property="Margin" Value="6,0,0,0" />
      <Setter Property="Padding" Value="0" />
      <Setter Property="Background" Value="#F3F3F3" />
      <Setter Property="BorderBrush" Value="#BDBDBD" />
      <Setter Property="BorderThickness" Value="1" />
    </Style>

    <Style x:Key="DriverToggleStyle" TargetType="ToggleButton">
      <Setter Property="Background" Value="#F3F3F3" />
      <Setter Property="BorderBrush" Value="#BDBDBD" />
      <Setter Property="BorderThickness" Value="1" />
      <Setter Property="Height" Value="30" />
      <Style.Triggers>
        <Trigger Property="IsChecked" Value="True">
          <Setter Property="Background" Value="#D9F2D9" />
        </Trigger>
      </Style.Triggers>
    </Style>
  </Window.Resources>
  <Grid Margin="12">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto" />
      <RowDefinition Height="Auto" />
      <RowDefinition Height="Auto" />
      <RowDefinition Height="*" />
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
      <ColumnDefinition Width="*" />
      <ColumnDefinition Width="320" />
    </Grid.ColumnDefinitions>

    <StackPanel Orientation="Horizontal" Grid.Row="0" Grid.Column="0" Margin="0,0,0,8">
      <TextBlock Text="SHP IP:" VerticalAlignment="Center" Margin="0,0,8,0" />
      <TextBox x:Name="IpTextBox" Width="220" Height="26" Margin="0,0,8,0" />
      <Button x:Name="ConnectButton" Content="Connect" Width="90" Height="26" Margin="0,0,8,0" Click="ConnectButton_Click" />
      <Button x:Name="DisconnectButton" Content="Disconnect" Width="90" Height="26" IsEnabled="False" Click="DisconnectButton_Click" />
      <TextBlock x:Name="StatusText" Text="Idle" VerticalAlignment="Center" Margin="12,0,0,8" />
      <CheckBox x:Name="TcpCaptureCheckBox" Content="TCP Capture (RAW)" VerticalAlignment="Center" Margin="12,0,0,0" />
      <CheckBox x:Name="SendProbeCheckBox" Content="Send probe on connect (diagnostic)" VerticalAlignment="Center" Margin="12,0,0,0" />
    </StackPanel>

    <StackPanel Orientation="Horizontal" Grid.Row="1" Grid.Column="0" Margin="0,0,0,8">
      <Button x:Name="DiscoverButton" Content="Discover" Width="90" Height="26" Margin="0,0,8,0" Click="DiscoverButton_Click" />
      <TextBlock Text="Discovered:" VerticalAlignment="Center" Margin="0,0,8,0" />
      <ComboBox x:Name="DiscoveredCombo" Width="260" Height="26" SelectionChanged="DiscoveredCombo_SelectionChanged" />
    </StackPanel>

    <Border Grid.Row="2" Grid.Column="0" Grid.ColumnSpan="2" BorderBrush="#CCCCCC" BorderThickness="1" CornerRadius="4" Padding="6" Margin="0,0,0,8">
      <StackPanel>
        <TextBlock Text="Driver Log Levels" FontWeight="SemiBold" Margin="0,0,0,6" />
        <ScrollViewer Height="240"
                      HorizontalScrollBarVisibility="Auto"
                      VerticalScrollBarVisibility="Disabled">
          <ItemsControl ItemsSource="{Binding Drivers}">
            <ItemsControl.ItemsPanel>
              <ItemsPanelTemplate>
                <WrapPanel Orientation="Vertical"
                           ItemWidth="360"
                           ItemHeight="30" />
              </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
              <DataTemplate>
                <Grid Width="360" Height="30" Margin="0,0,8,0">
                  <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="240" />
                    <ColumnDefinition Width="Auto" />
                    <ColumnDefinition Width="Auto" />
                    <ColumnDefinition Width="Auto" />
                  </Grid.ColumnDefinitions>
                  <ToggleButton Grid.Column="0" Content="{Binding Name}" Margin="0,0,4,0"
                                IsChecked="{Binding IsEnabled, Mode=TwoWay}" Click="DriverToggle_Click"
                                Style="{StaticResource DriverToggleStyle}" />
                  <Button Grid.Column="1" Content="1" Tag="1" Click="DriverLevelButton_Click">
                    <Button.Style>
                      <Style TargetType="Button" BasedOn="{StaticResource LevelButtonStyle}">
                        <Style.Triggers>
                          <MultiDataTrigger>
                            <MultiDataTrigger.Conditions>
                              <Condition Binding="{Binding IsEnabled}" Value="True" />
                              <Condition Binding="{Binding SelectedLevel}" Value="1" />
                            </MultiDataTrigger.Conditions>
                            <Setter Property="Background" Value="#DDEBFF" />
                          </MultiDataTrigger>
                        </Style.Triggers>
                      </Style>
                    </Button.Style>
                  </Button>
                  <Button Grid.Column="2" Content="2" Tag="2" Click="DriverLevelButton_Click">
                    <Button.Style>
                      <Style TargetType="Button" BasedOn="{StaticResource LevelButtonStyle}">
                        <Style.Triggers>
                          <MultiDataTrigger>
                            <MultiDataTrigger.Conditions>
                              <Condition Binding="{Binding IsEnabled}" Value="True" />
                              <Condition Binding="{Binding SelectedLevel}" Value="2" />
                            </MultiDataTrigger.Conditions>
                            <Setter Property="Background" Value="#DDEBFF" />
                          </MultiDataTrigger>
                        </Style.Triggers>
                      </Style>
                    </Button.Style>
                  </Button>
                  <Button Grid.Column="3" Content="3" Tag="3" Click="DriverLevelButton_Click">
                    <Button.Style>
                      <Style TargetType="Button" BasedOn="{StaticResource LevelButtonStyle}">
                        <Style.Triggers>
                          <MultiDataTrigger>
                            <MultiDataTrigger.Conditions>
                              <Condition Binding="{Binding IsEnabled}" Value="True" />
                              <Condition Binding="{Binding SelectedLevel}" Value="3" />
                            </MultiDataTrigger.Conditions>
                            <Setter Property="Background" Value="#DDEBFF" />
                          </MultiDataTrigger>
                        </Style.Triggers>
                      </Style>
                    </Button.Style>
                  </Button>
                </Grid>
              </DataTemplate>
            </ItemsControl.ItemTemplate>
          </ItemsControl>
        </ScrollViewer>
      </StackPanel>
    </Border>

    <Border Grid.Row="3" Grid.Column="0" Grid.ColumnSpan="2" BorderBrush="#CCCCCC" BorderThickness="1" CornerRadius="4" Padding="6">
      <Grid>
        <Grid.RowDefinitions>
          <RowDefinition Height="Auto" />
          <RowDefinition Height="*" />
        </Grid.RowDefinitions>
        <Grid Margin="0,0,0,6">
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="Auto" />
          </Grid.ColumnDefinitions>
          <TextBlock Text="Diagnostics" FontWeight="SemiBold" VerticalAlignment="Center" />
          <Button Grid.Column="1"
                  Content="Clear"
                  Padding="10,2"
                  Margin="8,0,0,0"
                  Click="ClearDiagnostics_Click" />
        </Grid>
        <Grid Grid.Row="1">
          <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="8" />
            <ColumnDefinition Width="*" />
          </Grid.ColumnDefinitions>

          <Border Grid.Column="0" BorderBrush="#DDDDDD" BorderThickness="1" CornerRadius="3" Padding="6">
            <Grid>
              <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
              </Grid.RowDefinitions>
              <TextBlock Text="Raw Output" FontWeight="SemiBold" Margin="0,0,0,6" />
              <TextBox x:Name="RawLogTextBox"
                       Grid.Row="1"
                       FontFamily="Consolas"
                       FontSize="12"
                       IsReadOnly="True"
                       TextWrapping="NoWrap"
                       Padding="0"
                       VerticalScrollBarVisibility="Auto"
                       HorizontalScrollBarVisibility="Auto"
                       AcceptsReturn="True" />
            </Grid>
          </Border>

          <GridSplitter Grid.Column="1"
                        Width="8"
                        HorizontalAlignment="Stretch"
                        VerticalAlignment="Stretch"
                        Background="#E0E0E0"
                        ShowsPreview="False" />

          <Border Grid.Column="2" BorderBrush="#DDDDDD" BorderThickness="1" CornerRadius="3" Padding="6">
            <Grid>
              <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
              </Grid.RowDefinitions>
              <TextBlock Text="Processed Output" FontWeight="SemiBold" Margin="0,0,0,6" />
              <TextBox x:Name="ProcessedLogTextBox"
                       Grid.Row="1"
                       FontFamily="Consolas"
                       FontSize="12"
                       IsReadOnly="True"
                       TextWrapping="Wrap"
                       VerticalScrollBarVisibility="Auto"
                       HorizontalScrollBarVisibility="Auto"
                       AcceptsReturn="True"
                       Text="No processed information available" />
            </Grid>
          </Border>
        </Grid>
      </Grid>
    </Border>

    <Border Grid.Row="0" Grid.RowSpan="2" Grid.Column="1" BorderBrush="#CCCCCC" BorderThickness="1" CornerRadius="4" Padding="8" Margin="12,0,0,8">
      <StackPanel>
        <TextBlock Text="Project Data" FontWeight="SemiBold" Margin="0,0,0,8" />
        <Button Content="Upload Project (.apex)" Width="180" Height="28" Click="UploadProject_Click" />
      </StackPanel>
    </Border>
  </Grid>
</Window>
```

Source: SHPDiagnosticsViewer.Tests/DecodeWsTests.cs
```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace SHPDiagnosticsViewer.Tests;

public sealed class DecodeWsTests
{
    [Fact]
    public void DecodingIsDeterministicAndOrdered()
    {
        var input = SampleHexLines();

        var first = DecodeWsReference.DecodeToOutputLines(input);
        var second = DecodeWsReference.DecodeToOutputLines(input);

        Assert.Equal(first, second);
        Assert.Equal(6, first.Count);
        Assert.Equal("1: Input: Button 1 Pressed", first[0]);
        Assert.Equal("2: Driver status: Ready", first[1]);
        Assert.Equal("3: Driver event: Start", first[2]);
        Assert.Equal("4: Input [ScheduledTasks] Driver event test]", first[3]);
        Assert.Equal("5: 09/15/2024 10:11:13 Sustain:NO", first[4]);
        Assert.Equal("6: 09/15/2024 10:11:14 Value)", first[5]);
    }

    [Fact]
    public void OutputHasOneLogicalEntryPerLineWithNoTruncation()
    {
        var output = DecodeWsReference.DecodeToOutputLines(SampleHexLines());

        Assert.All(output, line =>
        {
            Assert.DoesNotContain("\n", line, StringComparison.Ordinal);
            Assert.DoesNotContain("\r", line, StringComparison.Ordinal);
            Assert.DoesNotContain("...", line, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void MissingInputFileFailsClearly()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"decode_ws_missing_{Guid.NewGuid():N}.txt");

        var success = DecodeWsReference.TryDecodeFile(missingPath, out var error, out _);

        Assert.False(success);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void InvalidHexLinesYieldNoOutput()
    {
        var output = DecodeWsReference.DecodeToOutputLines(new[] { "not-hex", "123" });

        Assert.Empty(output);
    }

    private static List<string> SampleHexLines()
    {
        var encoding = Encoding.Unicode;
        var lines = new[]
        {
            "09/15/2024 10:11:12 Input: Button 1 Pressed",
            "Driver status: Ready",
            "Driver e vent: Start",
            "Input [Schedule   Driver event test]",
            "09/15/2024 10:11:13 Sustain:NO abc",
            "09/15/2024 10:11:14 Value) xx",
            "hello"
        };

        return lines.Select(line => DecodeWsReference.ToHexLine(line, encoding)).ToList();
    }
}

internal static class DecodeWsReference
{
    private static readonly Encoding Utf8 = Encoding.GetEncoding(
        "utf-8",
        EncoderFallback.ExceptionFallback,
        new DecoderReplacementFallback(""));
    private static readonly Encoding Utf16Le = Encoding.GetEncoding(
        "utf-16le",
        EncoderFallback.ExceptionFallback,
        new DecoderReplacementFallback(""));
    private static readonly Encoding Latin1 = Encoding.Latin1;
    private static readonly Regex PrefixRegex = new Regex("(Input|Driver|System Manager|Macro|hello)", RegexOptions.Compiled);
    private static readonly Regex DatePattern = new Regex("\\d{2}/\\d{2}/\\d{4}", RegexOptions.Compiled);
    private static readonly Regex SchedulePattern = new Regex("\\[Schedule\\s+Driver event", RegexOptions.Compiled);
    private static readonly Regex SustainPattern = new Regex("(Sustain:NO)\\s+[A-Za-z]{1,3}$", RegexOptions.Compiled);
    private static readonly Regex TrailingMarkerPattern = new Regex("([)'\\\"])\\s+[A-Za-z]{1,3}$", RegexOptions.Compiled);

    public static List<string> DecodeToOutputLines(IEnumerable<string> rawLines)
    {
        var decodedChunks = new List<string>();
        foreach (var rawLine in rawLines)
        {
            var decoded = DecodeHexLine(rawLine);
            var cleaned = CleanText(decoded);
            if (!string.IsNullOrEmpty(cleaned))
            {
                decodedChunks.Add(cleaned);
            }
        }

        var fullText = string.Join("\n", decodedChunks);
        var lines = fullText
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();

        var prefixes = new[] { "Input", "Driver", "System Manager", "Macro" };
        var normalizedLines = new List<string>();
        foreach (var line in lines)
        {
            var match = PrefixRegex.Match(line);
            var normalized = line;
            if (match.Success && match.Index > 0)
            {
                normalized = line.Substring(match.Index);
            }
            normalizedLines.Add(normalized.Trim());
        }

        var logicalLines = new List<string>();
        foreach (var line in normalizedLines)
        {
            if (string.IsNullOrEmpty(line) || !line.Any(char.IsLetterOrDigit))
            {
                continue;
            }

            if (line.Trim() == "hello")
            {
                continue;
            }

            if (line.Trim().All(char.IsDigit))
            {
                continue;
            }

            if (!DatePattern.IsMatch(line)
                && logicalLines.Count > 0
                && (line.StartsWith("Driver event", StringComparison.Ordinal)
                    || line.StartsWith("Driver - Command", StringComparison.Ordinal)
                    || line.StartsWith("System Manager", StringComparison.Ordinal)
                    || (!prefixes.Any(prefix => line.StartsWith(prefix, StringComparison.Ordinal))
                        && !char.IsDigit(line[0]))))
            {
                logicalLines[^1] = $"{logicalLines[^1]} {line}".Trim();
                continue;
            }

            if (prefixes.Any(prefix => line.StartsWith(prefix, StringComparison.Ordinal)) || char.IsDigit(line[0]))
            {
                logicalLines.Add(line);
            }
            else if (logicalLines.Count > 0)
            {
                logicalLines[^1] = $"{logicalLines[^1]} {line}".Trim();
            }
            else
            {
                logicalLines.Add(line);
            }
        }

        var cleanedLines = new List<string>();
        foreach (var line in logicalLines)
        {
            var cleaned = line.Replace("Driver e vent", "Driver event", StringComparison.Ordinal);
            cleaned = SchedulePattern.Replace(cleaned, "[ScheduledTasks] Driver event");
            cleaned = SustainPattern.Replace(cleaned, "$1");
            cleaned = TrailingMarkerPattern.Replace(cleaned, "$1");
            cleanedLines.Add(cleaned);
        }

        var output = new List<string>();
        for (var i = 0; i < cleanedLines.Count; i++)
        {
            output.Add(FormattableString.Invariant($"{i + 1}: {cleanedLines[i]}"));
        }

        return output;
    }

    public static bool TryDecodeFile(string path, out string error, out List<string> output)
    {
        try
        {
            var rawLines = File.ReadAllLines(path, Encoding.ASCII);
            output = DecodeToOutputLines(rawLines);
            error = "";
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            output = new List<string>();
            error = ex.Message;
            return false;
        }
    }

    public static string ToHexLine(string text, Encoding encoding)
    {
        var bytes = encoding.GetBytes(text);
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var value in bytes)
        {
            builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
        }
        return builder.ToString();
    }

    private static string DecodeHexLine(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0)
        {
            return "";
        }

        if (!TryParseHex(trimmed, out var data))
        {
            return "";
        }

        if (data.Length >= 2 && data.Where((value, index) => index % 2 == 1 && value == 0).Count() > data.Length / 4.0)
        {
            return TryDecode(data, Utf16Le, Utf8, Latin1);
        }

        return TryDecode(data, Utf8, Latin1, Utf16Le);
    }

    private static bool TryParseHex(string input, out byte[] data)
    {
        var noWhitespace = string.Concat(input.Where(ch => !char.IsWhiteSpace(ch)));
        if (noWhitespace.Length == 0 || noWhitespace.Length % 2 != 0)
        {
            data = Array.Empty<byte>();
            return false;
        }

        try
        {
            data = Convert.FromHexString(noWhitespace);
            return true;
        }
        catch (FormatException)
        {
            data = Array.Empty<byte>();
            return false;
        }
    }

    private static string TryDecode(byte[] data, params Encoding[] encodings)
    {
        foreach (var encoding in encodings)
        {
            try
            {
                return encoding.GetString(data);
            }
            catch (DecoderFallbackException)
            {
            }
        }

        return "";
    }

    private static string CleanText(string text)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (ch == '\n')
            {
                builder.Append(ch);
                continue;
            }

            var codepoint = (int)ch;
            if (codepoint >= 32 && codepoint <= 126)
            {
                builder.Append(ch);
            }
        }
        return builder.ToString();
    }
}
```
