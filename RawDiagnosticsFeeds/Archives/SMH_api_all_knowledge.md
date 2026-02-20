# SHP API All Knowledge (WebSocket + HTTP)

Generated from repository-only evidence in:
- `/Users/jamiefeeny/Desktop/Development/Oracle/RawDiagnosticsFeeds`
- `/Users/jamiefeeny/Desktop/Development/Oracle` (all subdirectories, including probes/tests)

Scope focus:
- RTI SHP WebSocket and HTTP diagnostics interfaces
- Related behavior used by Oracle/probes/tests

Classification rules:
- `TESTED`: capture-backed and/or directly validated by unit tests in this repo.
- `THEORY`: plausible but not directly proven by capture or test evidence in this repo.

---

## TESTED

### 1) WebSocket API endpoint and handshake are confirmed

Facts:
- Endpoint is `ws://<SHP_IP>:1234/diagnosticswss`.
- Browser-style WebSocket upgrade is accepted with HTTP 101.
- Server returns WebSocket echo greeting after connect.

Capture evidence:
- `RawDiagnosticsFeeds/TCPDiagnostics/Bootup/.tmp_codex_tshark/follow_tcp_stream_13.txt`
- `RawDiagnosticsFeeds/TCPDiagnostics/Bootup/.tmp_codex_tshark/follow_tcp_stream_50.txt`

Observed payloads:
- Request path: `GET /diagnosticswss HTTP/1.1`
- Header: `Upgrade: websocket`
- Response: `HTTP/1.1 101 Switching Protocols`
- Greeting: `{"messageType":"echo","message":"Welcome to the RTI Diagnostics Websocket server!"}`

Code used:
```csharp
// /Users/jamiefeeny/Desktop/Development/Oracle/OracleByFPCLtd/DiagnosticsTransport/LegacyWebSocketDiagnosticsTransport.cs
_socket.Options.SetRequestHeader("Origin", $"http://{ip}");
var uri = new Uri($"ws://{ip}:1234/diagnosticswss");
await _socket.ConnectAsync(uri, _socketCts.Token);
```

```csharp
// /Users/jamiefeeny/Desktop/Development/Oracle/VariableSubscribeProbe/Program.cs
socket.Options.SetRequestHeader("Origin", $"http://{config.Ip}");
var wsUri = new Uri($"ws://{config.Ip}:1234/diagnosticswss");
await socket.ConnectAsync(wsUri, cts.Token);
```

---

### 2) WebSocket message types seen on wire are confirmed

Facts:
- Observed message types include:
  - `echo`
  - `LogLevels`
  - `MessageLog`
  - `Sysvar`
  - `SysvarPers`

Capture/session evidence:
- `RawDiagnosticsFeeds/TCPDiagnostics/Bootup/.tmp_codex_tshark/follow_tcp_stream_13.txt`
- `Variable_Subscribe_All_Info.md`
- `SystemLog (23).txt` and similar logs in repo

Code used (format/parsing):
```csharp
// /Users/jamiefeeny/Desktop/Development/Oracle/OracleByFPCLtd/WebSocketMessageFormatter.cs
if (string.Equals(messageType, "MessageLog", StringComparison.OrdinalIgnoreCase)) { ... }
if (string.Equals(messageType, "Sysvar", StringComparison.OrdinalIgnoreCase)) { ... }
if (string.Equals(messageType, "echo", StringComparison.OrdinalIgnoreCase)) { ... }
```

```csharp
// /Users/jamiefeeny/Desktop/Development/Oracle/VariableSubscribeProbe/Program.cs
if (root.TryGetProperty("messageType", out var typeElement)
    && string.Equals(typeElement.GetString(), "Sysvar", StringComparison.OrdinalIgnoreCase))
{
    ...
}
```

---

### 3) WebSocket subscribe command shapes are confirmed

Facts:
- Log stream subscription:
  - `{"type":"Subscribe","resource":"MessageLog","value":"true"}`
- Sysvar stream subscription:
  - `{"type":"Subscribe","resource":"Sysvar","value":"true"}`
- Log level set:
  - `{"type":"Subscribe","resource":"LogLevel","value":{"type":"<DName>","level":"<0-3>"}}`
- Sysvar per-ID toggle:
  - `{"type":"Subscribe","resource":"Sysvar","value":{"id":<ID>,"status":true|false}}`
- Persistent sysvar request:
  - `{"type":"Subscribe","resource":"SysvarPers"}`

Capture/test evidence:
- Capture echo of MessageLog subscribe in `follow_tcp_stream_13.txt`/`50.txt`
- Session notes in `Variable_Subscribe_All_Info.md`
- Unit test of Sysvar payload shape:
  - `/Users/jamiefeeny/Desktop/Development/Oracle/VariableSubscribeProbe/VariableSubscribeProbe.Tests/SubscribePayloadTests.cs`

Code used:
```csharp
// /Users/jamiefeeny/Desktop/Development/Oracle/OracleByFPCLtd/DiagnosticsTransport/LegacyWebSocketDiagnosticsTransport.cs
var payload = new
{
    type = "Subscribe",
    resource = "LogLevel",
    value = new { type, level }
};
await SendJsonAsync(payload, token);
```

```csharp
// /Users/jamiefeeny/Desktop/Development/Oracle/VariableSubscribeProbe/SubscriptionRequest.cs
public static string BuildSysvarTogglePayload(int id, bool status)
{
    var payload = new
    {
        type = "Subscribe",
        resource = "Sysvar",
        value = new { id, status }
    };
    return JsonSerializer.Serialize(payload);
}
```

---

### 4) LogLevel ack parsing patterns are test-backed

Facts:
- Parser supports ack lines like:
  - `Setting LogLevel on DRIVER (36) to 0`
  - `Setting LogLevel on EVENTS_INPUT to 3`
  - embedded JSON form with `resource:"LogLevel"` and `value.type/value.level`

Unit test evidence:
- `/Users/jamiefeeny/Desktop/Development/Oracle/OracleByFPCLtd.Tests/LogLevelAckParserTests.cs`

Code used:
```csharp
// /Users/jamiefeeny/Desktop/Development/Oracle/OracleByFPCLtd/LogLevelAckParser.cs
@"Setting LogLevel on DRIVER\s*\((\d+)\)\s*to\s*(\d+)"
@"Setting LogLevel on\s+(DRIVER//\d+)\s*to\s*(\d+)"
@"Setting LogLevel on\s+([A-Z0-9_]+)\s*to\s*(\d+)"
```

---

### 5) HTTP diagnostics endpoints on port 5000 are confirmed

Facts:
- `GET http://<SHP_IP>:5000/diagnostics/data/drivers` works and returns driver data.
- `GET http://<SHP_IP>:5000/diagnostics/data/system_status` works and returns status/metrics.
- `GET http://<SHP_IP>:5000/diagnostics/data/sysvars` works; body may be gzip-compressed JSON.

Capture evidence:
- `RawDiagnosticsFeeds/TCPDiagnostics/Bootup/.tmp_codex_tshark/follow_tcp_stream_47.txt`

Probe/session evidence:
- `VariableSubscribeProbe/Program.cs`
- `Variable_Subscribe_All_Info.md`

Code used:
```csharp
// /Users/jamiefeeny/Desktop/Development/Oracle/OracleByFPCLtd/DiagnosticsTransport/LegacyWebSocketDiagnosticsTransport.cs
var url = $"http://{ip}:5000/diagnostics/data/drivers";
json = await http.GetStringAsync(url);
```

```csharp
// /Users/jamiefeeny/Desktop/Development/Oracle/VariableSubscribeProbe/Program.cs
var url = $"http://{ip}:5000/diagnostics/data/sysvars";
var bytes = await Http.GetByteArrayAsync(url, token);
...
var url = $"http://{ip}:5000/diagnostics/data/system_status";
var json = await Http.GetStringAsync(url, token);
```

---

### 6) HTTP web UI on port 80 is confirmed

Facts:
- `GET /diagnostics HTTP/1.1` on port 80 returns HTML app shell.
- HTML references diagnostics static assets and JS/CSS paths under `/diagnostics/...`.

Capture evidence:
- `RawDiagnosticsFeeds/TCPDiagnostics/Bootup/.tmp_codex_tshark/follow_tcp_stream_41.txt`

Observed examples:
- `/diagnostics/css/app.css`
- `/diagnostics/js/app.js`
- `/diagnostics/js/chunk-vendors.js`

---

### 7) Additional on-wire metadata from drivers endpoint is confirmed

Fact:
- Drivers response includes a `port_list` string in captured payload that references diagnostics service ports (including 80, 5000, 1234).

Capture evidence:
- `RawDiagnosticsFeeds/TCPDiagnostics/Bootup/.tmp_codex_tshark/follow_tcp_stream_47.txt`

Observed example fragment:
- `... "port_list":"... HTTPSVR:80 HTTPSVR:5000 HTTPSVR:1234 ..."`

---

### 8) TCP/2113 behavior is confirmed (adjacent transport context)

This is not HTTP/WebSocket, but it directly affects diagnostics API understanding.

Facts:
- Repeated `hello` probes and UTF-16LE diagnostics stream are observed on TCP/2113.
- Multiple reconnect-like short sessions exist around reboot windows.

Evidence:
- `RawDiagnosticsFeeds/TCPDiagnostics/Bootup/.tmp_codex_tshark_trace/follow_tcp_stream_0.txt`
- `RawDiagnosticsFeeds/TCPDiagnostics/Bootup/.tmp_codex_tshark_trace/conv_tcp.txt`
- `RawDiagnosticsFeeds/TCPDiagnostics/Bootup/oracle_reconnect.md`
- `RawDiagnosticsFeeds/TCPDiagnostics/Bootup/TraceViewer_capture_analysis.md`

Code used for stream decoding/parity:
```csharp
// /Users/jamiefeeny/Desktop/Development/Oracle/OracleByFPCLtd/DiagnosticsTransport/TcpCaptureDiagnosticsTransport.cs
private static readonly Regex PrefixRegex = new("(Input|Driver|System Manager|Macro|hello)", RegexOptions.Compiled);
...
if (line == "hello") { return; }
```

---

## THEORY

### 1) Alternate HTTP log level snapshot endpoints

Hypothesis:
- One of these may exist on some SHP/firmware builds:
  - `/diagnostics/data/loglevels`
  - `/diagnostics/data/log_levels`
  - `/diagnostics/data/loglevel`

Why this exists:
- Implemented as endpoint fallbacks in probe script.

Not yet proven in captures/tests in this repo.

Code reference:
```powershell
# /Users/jamiefeeny/Desktop/Development/Oracle/RawDiagnosticsFeeds/WebsocketDiagnostics/loglevel_probe.ps1
"http://${Ip}:5000/diagnostics/data/loglevels",
"http://${Ip}:5000/diagnostics/data/log_levels",
"http://${Ip}:5000/diagnostics/data/loglevel"
```

---

### 2) Two-plane diagnostics model (programmer plane vs TraceViewer/tech plane)

Hypothesis:
- Browser/API plane: HTTP + WebSocket diagnostics.
- TraceViewer plane: separate TCP/2113 stream behavior and reconnect strategy.

Evidence basis:
- Methodology/docs in:
  - `/Users/jamiefeeny/Desktop/Development/Oracle/RawDiagnosticsFeeds/TCPDiagnostics/Bootup/TODAY_diagnostics_plane_summary_for_codex.md`
  - `/Users/jamiefeeny/Desktop/Development/Oracle/RawDiagnosticsFeeds/TCPDiagnostics/Bootup/TraceViewer_capture_analysis.md`

Not fully proven as a formal product contract.

---

### 3) ID11 project push controls TraceViewer debug behavior

Hypothesis:
- Debug behavior may be affected by pushed project config fields (`DebugTrace`, `DebugLevel`) rather than active 2113 protocol negotiation.

Evidence basis:
- Comparative capture analysis docs mention payload strings and differences.

Not fully proven end-to-end in this repo by isolated A/B push tests.

---

### 4) SysvarPers semantics

Hypothesis:
- `SysvarPers` likely returns persistent sysvar IDs only (not guaranteed full universe of sysvars).

Evidence basis:
- Session notes and observed payload shape.

Not yet proven across firmware/models with controlled tests.

---

### 5) Reconnect interval generalization

Hypothesis:
- ~3s reconnect interval seen in sampled captures may be client-specific (TraceViewer) and not a guaranteed SHP/API invariant.

Evidence basis:
- Repeated timestamps in reconnect docs.

Not yet generalized by broad test matrix.

---

## Quick Index: Confirmed SHP WebSocket/HTTP API Surface

- `ws://<SHP_IP>:1234/diagnosticswss`
- `http://<SHP_IP>/diagnostics` (web UI shell)
- `http://<SHP_IP>:5000/diagnostics/data/drivers`
- `http://<SHP_IP>:5000/diagnostics/data/system_status`
- `http://<SHP_IP>:5000/diagnostics/data/sysvars`

Primary confirmed WebSocket resources/messages:
- `Subscribe/MessageLog`
- `Subscribe/Sysvar`
- `Subscribe/LogLevel`
- `Subscribe/SysvarPers`
- `messageType: echo | LogLevels | MessageLog | Sysvar | SysvarPers`

