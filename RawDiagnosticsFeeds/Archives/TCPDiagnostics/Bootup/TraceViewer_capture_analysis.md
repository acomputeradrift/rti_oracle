# TraceViewer Capture Analysis (Wireshark_capture_on_reboot_TraceViewer.pcapng)

This report focuses on two items:
1) **Log-level flag propagation (ID11 → TraceViewer)**
2) **TraceViewer reconnect method**

All evidence below is extracted directly from the capture and follow‑stream outputs.

---

## 1) Log‑level flag propagation (ID11 → TraceViewer)

### Proven (from capture)

**1.1 Project push stream contains explicit debug/log configuration keys**
- Transport: TCP
- Stream: `tcp.stream == 21`
- Src/Dst: `192.168.4.2:5056 → 192.168.1.143:50433`
- Direction: client → SHP (ID11 push)
- Timestamp(s): `2026-01-21 14:30:11.135714 CST` (frames containing `DebugLevel`, `DebugTrace`)
- Payload type: binary bundle with embedded ASCII strings
- Excerpts (from stream reassembly):
  - `HBDebugfalse` (neighboring config block)
  - `DebugLevel0`
  - `DebugTracetrue`

**1.2 Project push contains explicit TraceViewer routing description**
- Transport: TCP
- Stream: `tcp.stream == 21`
- Src/Dst: `192.168.4.2:5056 → 192.168.1.143:50433`
- Direction: client → SHP
- Payload type: embedded ASCII within bundled resources
- Excerpt:
  - `description="Routes LogInfo to Traceviewer"` (from a settings block alongside `Debug Level` and choices)

**1.3 TraceViewer session receives runtime log output (UTF‑16LE)**
- Transport: TCP
- Stream: `tcp.stream == 0`
- Src/Dst: `192.168.1.143:2113 → 192.168.4.2:44546`
- Direction: SHP → TraceViewer client
- Timestamp (first payloads): `2026-01-21 14:30:03.479779 CST` and following
- Payload type: UTF‑16LE text log lines
- Excerpts (decoded from UTF‑16LE):
  - `XP8Hardw/Informat:01/21/2026 12:30:04.0 ... Waiting for sense RxThread`
  - `System Manager: Shutting Down`

### What is **not** proven
- There is **no explicit on‑wire message** in this capture showing an ID11 “driver log level” flag being sent to TraceViewer or a TraceViewer acknowledgement of such flags.
- We do **not** see a TraceViewer protocol message that enumerates driver log levels or filters.

### Likely theory (supported by evidence, but not directly proven)
- The ID11 push includes a project resource/script with `DebugLevel` and `DebugTrace` flags and a description explicitly stating it “Routes LogInfo to Traceviewer.”
- When “Show debug options in ID11” is enabled, the project payload likely flips these config values (e.g., `DebugTrace=true`, `DebugLevel=<N>`), which the SHP runtime reads and uses to filter or emit driver logs.
- TraceViewer appears to be a **passive log consumer** on TCP/2113 rather than an active log‑level negotiation peer.

### What’s missing to prove it
- A capture where **the only change** is the Debug Level in ID11, with a diff of the project push payload showing the exact flag/value change.
- A TraceViewer session showing **log content changes** that correlate with the Debug Level change.

### Suggested capture steps to prove the hypothesis
1) Perform two ID11 pushes, identical except for Debug Level (e.g., “None” vs “Messages”).
2) Capture the entire push and TraceViewer session each time.
3) Extract the ID11 push stream (`tcp.stream` with large payload to SHP) and binary‑diff the payloads.
4) Verify the changed bytes align with `DebugLevel`/`DebugTrace` values.
5) Compare TraceViewer logs to confirm volume/content change.

---

## 2) TraceViewer reconnect method (SHP ↔ TraceViewer)

### Proven (from capture)

**2.1 TraceViewer connects to SHP over TCP port 2113**
- Multiple TCP conversations observed to `192.168.1.143:2113`:
  - Streams: `tcp.stream == 0, 16, 18, 22, 24` (multiple sessions)
- Example stream (0):
  - Client: `192.168.4.2:44546`
  - Server: `192.168.1.143:2113`
  - Duration: `~14:29:55 CST → 14:30:07 CST`

**2.2 Client sends a short “hello” probe on reconnect**
- Transport: TCP
- Stream: `tcp.stream == 0`
- Src/Dst: `192.168.4.2:44546 → 192.168.1.143:2113`
- Direction: client → SHP
- Timestamp(s):
  - `2026-01-21 14:29:55.045780 CST`
  - `2026-01-21 14:29:58.060886 CST`
  - `2026-01-21 14:30:01.060893 CST`
  - `2026-01-21 14:30:04.061177 CST`
  - `2026-01-21 14:30:07.061407 CST`
- Payload type: ASCII
- Excerpt: `hello`

**2.3 SHP responds with UTF‑16LE diagnostic log lines**
- Transport: TCP
- Stream: `tcp.stream == 0`
- Src/Dst: `192.168.1.143:2113 → 192.168.4.2:44546`
- Direction: SHP → client
- Timestamp(s): `2026-01-21 14:30:03.479779 CST` (first payloads)
- Payload type: UTF‑16LE text
- Example excerpts:
  - `XP8Hardw/Informat:01/21/2026 12:30:04.0 ... Waiting for sense RxThread`
  - `System Manager: Shutting Down`

### What is **not** proven
- There is no explicit protocol frame beyond the repeated `hello` probe; the server response does not include a clear banner or version string in this capture.
- We can’t confirm whether TraceViewer is the only client using port 2113, though the repeated hello probe strongly suggests TraceViewer behavior.

### Likely theory (supported by evidence, but not directly proven)
- TraceViewer reconnect is a simple TCP client loop:
  1) Open TCP connection to SHP `:2113`.
  2) Send small probe (`"hello"`).
  3) Start reading UTF‑16LE log lines.
  4) If connection drops, repeat every ~3 seconds.
- The SHP side acts as a passive log stream publisher; TraceViewer does not appear to negotiate filters on‑wire.

### What’s missing to fully replicate
- Confirmation whether additional initial tokens besides `hello` are required.
- Confirmation of framing rules (e.g., fixed header + UTF‑16LE payload) for log entries.

### Suggested capture steps to finalize a faithful client
1) Capture a clean TraceViewer session start (launch TraceViewer only, no browser).
2) Export raw TCP stream for port 2113 and inspect for framing markers around UTF‑16LE payloads.
3) Implement a small test client that sends `hello`, then prints UTF‑16LE lines; compare output to TraceViewer.

---

## Notes / Known Limitations
- This capture does not show HTTP/WS diagnostics traffic (ports 80/1234) seen in the prior capture; the focus here is ID11 push and TraceViewer logs.
- The ID11 push stream clearly embeds debug configuration text, but there is no explicit TraceViewer‑side confirmation in the capture.

---

## Appendix: Comparison with `Wireshark_capture_on_reboot_TraceViewer_no_debug_flag.pcapng`

### Proven differences in the ID11 push payload

**A.1 DebugTrace flips from true → false**  
- Debug‑enabled capture stream: `tcp.stream == 21` (`192.168.4.2:5056 → 192.168.1.143:50433`)  
- No‑debug capture stream: `tcp.stream == 16` (`192.168.4.2:5056 → 192.168.1.143:50458`)  
- In the no‑debug capture, the embedded config string is:
  - `DebugTracefalse`
- In the debug‑enabled capture, the embedded config string is:
  - `DebugTracetrue`

**A.2 DebugLevel remains present and set to 0 in both captures**  
- No‑debug capture excerpt: `DebugLevel0` (same stream as above).  
- Debug‑enabled capture excerpt: `DebugLevel0`.

### What this suggests (still not fully proven)
- The ID11 “Show debug options” checkbox appears to flip **`DebugTrace`** (true/false) in the pushed project payload.  
- The driver‑specific Debug Level selection likely affects other data not yet isolated in these two pushes (or is encoded elsewhere), since `DebugLevel0` is present in both.

### What’s still missing to prove driver‑level filtering
- A byte‑level diff of **driver‑specific log level** changes across two pushes where only per‑driver levels differ.
- A correlated change in TraceViewer output volume/content for the same session.
