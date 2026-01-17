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
