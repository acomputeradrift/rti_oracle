using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace VariableSubscribeProbe;

public static class Program
{
    private static readonly HttpClient Http = new();

    public static async Task<int> Main(string[] args)
    {
        var config = ParseArgs(args);
        Console.WriteLine("Variable Subscribe Probe");
        Console.WriteLine($"Target IP: {config.Ip}");

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var sysvarCatalog = await LoadSysvarCatalogAsync(config.Ip, cts.Token);
        if (sysvarCatalog.Drivers.Count == 0)
        {
            Console.WriteLine("No drivers found. Exiting.");
            return 1;
        }

        var lookup = BuildVariableLookup(sysvarCatalog);

        using var socket = new ClientWebSocket();
        socket.Options.SetRequestHeader("Origin", $"http://{config.Ip}");
        var wsUri = new Uri($"ws://{config.Ip}:1234/diagnosticswss");
        await socket.ConnectAsync(wsUri, cts.Token);

        _ = Task.Run(() => ReceiveLoopAsync(socket, lookup, cts.Token), cts.Token);
        _ = Task.Run(() => PollSystemStatusAsync(config.Ip, config.SystemStatusIntervalSeconds, cts.Token), cts.Token);

        await RunMenuAsync(sysvarCatalog, socket, cts.Token);
        cts.Cancel();
        return 0;
    }

    private static ProbeConfig ParseArgs(string[] args)
    {
        var config = new ProbeConfig();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--ip", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                config.Ip = args[++i];
                continue;
            }

            if (string.Equals(arg, "--interval", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length
                && int.TryParse(args[++i], out var interval) && interval > 0)
            {
                config.SystemStatusIntervalSeconds = interval;
            }
        }

        return config;
    }

    private static async Task<SysvarCatalog> LoadSysvarCatalogAsync(string ip, CancellationToken token)
    {
        var url = $"http://{ip}:5000/diagnostics/data/sysvars";
        var bytes = await Http.GetByteArrayAsync(url, token);
        var json = DecodeSysvarList(bytes);
        return SysvarCatalog.ParseFromJson(json);
    }

    private static string DecodeSysvarList(byte[] bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0x1F && bytes[1] == 0x8B)
        {
            using var input = new MemoryStream(bytes);
            using var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        return Encoding.UTF8.GetString(bytes);
    }

    private static Dictionary<int, string> BuildVariableLookup(SysvarCatalog catalog)
    {
        var lookup = new Dictionary<int, string>();
        foreach (var driver in catalog.Drivers)
        {
            foreach (var variable in driver.Variables)
            {
                if (!lookup.ContainsKey(variable.Id))
                {
                    lookup[variable.Id] = $"{driver.DriverName} [{variable.Name}]";
                }
            }
        }

        return lookup;
    }

    private static async Task RunMenuAsync(SysvarCatalog catalog, ClientWebSocket socket, CancellationToken token)
    {
        var subscribedDrivers = new HashSet<int>();
        while (!token.IsCancellationRequested)
        {
            Console.WriteLine();
            Console.WriteLine("Drivers:");
            for (var i = 0; i < catalog.Drivers.Count; i++)
            {
                var driver = catalog.Drivers[i];
                var status = subscribedDrivers.Contains(driver.DriverId) ? "ON" : "OFF";
                Console.WriteLine($"[{i + 1}] {driver.DriverName} ({driver.Variables.Count} vars) [{status}]");
            }

            Console.WriteLine("Enter driver numbers (e.g. 1,3-5) or 'q' to quit:");
            var selectionInput = Console.ReadLine();
            if (selectionInput is null)
            {
                continue;
            }

            if (string.Equals(selectionInput.Trim(), "q", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var selections = SelectionParser.Parse(selectionInput)
                .Where(index => index >= 1 && index <= catalog.Drivers.Count)
                .ToList();
            if (selections.Count == 0)
            {
                Console.WriteLine("No valid selections.");
                continue;
            }

            Console.WriteLine("Action: on / off / toggle / back");
            var action = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(action) || action == "back")
            {
                continue;
            }

            foreach (var index in selections)
            {
                var driver = catalog.Drivers[index - 1];
                var isOn = subscribedDrivers.Contains(driver.DriverId);
                var enable = action switch
                {
                    "on" => true,
                    "off" => false,
                    "toggle" => !isOn,
                    _ => isOn
                };

                await SetDriverSubscriptionsAsync(socket, driver, enable, token);
                if (enable)
                {
                    subscribedDrivers.Add(driver.DriverId);
                }
                else
                {
                    subscribedDrivers.Remove(driver.DriverId);
                }
            }
        }
    }

    private static async Task SetDriverSubscriptionsAsync(ClientWebSocket socket, SysvarDriver driver, bool enable, CancellationToken token)
    {
        foreach (var variable in driver.Variables)
        {
            var payload = SubscriptionRequest.BuildSysvarTogglePayload(variable.Id, enable);
            var bytes = Encoding.UTF8.GetBytes(payload);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, token);
        }

        Console.WriteLine($"{(enable ? "Subscribed" : "Unsubscribed")} {driver.DriverName} ({driver.Variables.Count} vars)");
    }

    private static async Task ReceiveLoopAsync(ClientWebSocket socket, Dictionary<int, string> lookup, CancellationToken token)
    {
        var buffer = new byte[8192];
        while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            using var stream = new MemoryStream();
            WebSocketReceiveResult? result;
            do
            {
                result = await socket.ReceiveAsync(buffer, token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                stream.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            var text = Encoding.UTF8.GetString(stream.ToArray());
            if (TryFormatSysvarMessage(text, lookup, out var message))
            {
                Console.WriteLine(message);
            }
        }
    }

    private static bool TryFormatSysvarMessage(string json, Dictionary<int, string> lookup, out string message)
    {
        message = string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("messageType", out var typeElement)
                && string.Equals(typeElement.GetString(), "Sysvar", StringComparison.OrdinalIgnoreCase))
            {
                var id = root.TryGetProperty("sysvarid", out var idElement) ? idElement.GetInt32() : -1;
                var value = root.TryGetProperty("sysvarval", out var valElement) ? valElement.ToString() : "";

                var label = lookup.TryGetValue(id, out var name) ? name : $"Sysvar {id}";
                message = $"{label} changed to {value}";
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static async Task PollSystemStatusAsync(string ip, int intervalSeconds, CancellationToken token)
    {
        var url = $"http://{ip}:5000/diagnostics/data/system_status";
        while (!token.IsCancellationRequested)
        {
            try
            {
                var json = await Http.GetStringAsync(url, token);
                var status = SystemStatus.Parse(json);
                Console.WriteLine($"[status] mem_free={status.MemoryFree} mem_load={status.MemoryLoad} sysvar_load={status.SysvarLoad} uptime={status.Uptime}");
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[status] error: {ex.Message}");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
