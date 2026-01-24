# TCP Diagnostics Feed - All Info (Verbatim Sources)

Source: RawDiagnosticsFeeds/TCPDiagnostics/shp_transport_observations.md
```markdown
# Transport Observations — RTI SHP Diagnostics

> **Status:** OBSERVATIONAL (NON‑AUTHORITATIVE)
>
> **Purpose:** Records empirically observed behavior of the RTI SHP diagnostic transport.
>
> **IMPORTANT:** This document does **not** define contracts, guarantees, or permissions.
> Nothing in this file may be relied upon as stable, complete, or future‑proof.
> All observations are subject to change based on SHP firmware, model, or configuration.

---

## 1. Scope of Observations

This document captures what has been **observed in practice** when monitoring RTI SHP diagnostic output using third‑party tooling.

It exists to:
- Preserve empirical knowledge
- Justify architectural and invariant decisions
- Support parser development and testing

It does **not**:
- Authorize data usage
- Define data schemas
- Override `data_contracts.md`

---

## 2. Observation Methodology

Observations were derived from:
- Passive monitoring of SHP diagnostic output
- TCP stream inspection
- Comparison of raw byte streams to decoded log output

Third‑party tools were used **only** to observe behavior; they are not dependencies of RTI Oracle.

---

## 3. Transport Characteristics (Observed)

The following characteristics have been observed:

- Diagnostic output is delivered over a **TCP connection**
- Default observed port: **2113**
- Communication direction is **SHP → client only** after connection
- No evidence of bidirectional command/control beyond log level configuration

These characteristics are **observations**, not guarantees.

---

## 4. Encoding & Framing (Observed)

Observed properties include:

- Text payloads are encoded as **UTF-16LE**
- Diagnostic messages are framed using explicit **start and end markers**
- Observed framing markers:
  - Start of message: `0x01 0x00`
  - End of message: `0x03 0x00`
- Framing markers are consistently present around decoded diagnostic messages

Important clarifications:
- Framing markers delimit logical diagnostic messages but **do not imply packet boundaries**
- Framed messages may still be split across TCP segments or interleaved with other data
- Framing markers have been empirically verified but are still treated as **observed behavior**, not contractual guarantees

---

## 5. Stream Behavior Notes

Observed stream behavior:

- Diagnostic data should be treated as a **continuous byte stream**
- Record boundaries are not guaranteed to align with TCP packet boundaries
- Partial records are expected and normal

These observations justify stream‑safe parsing requirements elsewhere in the system.

---

## 6. Relationship to Other Documents

This document:
- Informs **architecture.md** (transport model decisions)
- Informs **invariants.md** (stream safety, no inference)
- Informs **runbook.md** (operator expectations)

This document does **not**:
- Add fields to `data_contracts.md`
- Modify system scope
- Create operational requirements

---

## 7. Change & Validation Rules

- New observations may be appended with evidence
- Existing observations must not be rewritten without justification
- Contradictory findings must be recorded, not erased

---

END OF TRANSPORT OBSERVATIONS
```

Source: RawDiagnosticsFeeds/TCPDiagnostics/Bootup/oracle_reconnect.md
```markdown
# TraceViewer Connect/Reconnect Method (Oracle Ingestion)

This document summarizes **proven** observations and **theories** about the TraceViewer connect/reconnect behavior seen in the Wireshark captures. It is intended for AI ingestion when designing a 3rd‑party client.

---

## Proven (from pcap)

### P1) TraceViewer uses TCP port 2113 on the SHP
- Server: `192.168.1.143:2113`
- Client: `192.168.4.2:<ephemeral>`
- Observed across multiple conversations in the TraceViewer capture.

### P2) Client sends a small ASCII probe on reconnect
- Payload: literal `hello` (ASCII)
- Example timestamps (TraceViewer capture):
  - `2026-01-21 14:29:55.045780 CST`
  - `2026-01-21 14:29:58.060886 CST`
  - `2026-01-21 14:30:01.060893 CST`
  - `2026-01-21 14:30:04.061177 CST`
  - `2026-01-21 14:30:07.061407 CST`
- Direction: client → SHP

### P3) SHP responds with UTF‑16LE log lines
- Direction: SHP → client
- Payloads decode to UTF‑16LE strings, e.g.:
  - `XP8Hardw/Informat:01/21/2026 12:30:04.0 ... Waiting for sense RxThread`
  - `System Manager: Shutting Down`
- These appear as continuous log output once the connection is established.

### P4) Multiple short sessions observed around reboot
- Several separate TCP sessions to `:2113` occur in a short timeframe, consistent with reconnect attempts.

---

## Theories (supported but not directly proven)

### T1) Minimal handshake protocol
- Likely handshake: open TCP → send `hello` → receive log stream.
- No additional negotiation tokens were visible in the capture beyond `hello`.

### T2) Reconnect loop timing
- The `hello` probes are spaced ~3 seconds apart, suggesting a reconnect loop interval in that range.

### T3) Log stream framing
- Log stream appears as UTF‑16LE text with line‑like records.
- There may be a small binary prefix/suffix or framing bytes, but the dominant content is UTF‑16LE log lines.

---

## Practical guidance for a 3rd‑party client

### Minimal viable behavior (based on evidence)
1) Open TCP connection to `SHP_IP:2113`.
2) Send ASCII `hello`.
3) Read incoming data and decode as UTF‑16LE.
4) If the socket closes, wait ~3 seconds and repeat.

### Risks / unknowns
- Whether `hello` must be repeated after connection is established (not observed).
- Whether there are hidden control messages or keepalive semantics beyond `hello`.
- Whether certain log categories are suppressed without other configuration (likely controlled by project flags rather than the 2113 protocol).

---

## Evidence Sources
- `Wireshark_capture_on_reboot_TraceViewer.pcapng`
- Streams involving `192.168.1.143:2113` (e.g., `tcp.stream == 0`)
```

Source: RawDiagnosticsFeeds/TCPDiagnostics/Bootup/TraceViewer_capture_analysis.md
```markdown
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
```

Source: RawDiagnosticsFeeds/TCPDiagnostics/Bootup/TODAY_diagnostics_plane_summary_for_codex.md
```markdown

---

# Today’s Proven + Theory Summary (for Codex ingestion)

This summary is restricted to what was created/recorded today in these generated docs:
- `Wireshark Capture Analysis – Project Push + Reboot` (md created today)
- `tcp_stream_format_rules_updated.md` (md created today)
- Canvas: `Oracle → SHP Transport Reconstruction Methodology` (methodology doc created today)

If a statement below is not explicitly supported by those sources, it is placed under **Theory** and labeled as such.

---

## Part A — Proven (must be traceable to today’s docs)

### A1) The capture context (Project push + reboot)
- The capture file analyzed today is named: `Wireshark_capture_on_reboot.pcapng`.
- The capture window includes: **Integration Designer (ID11) pushing a project/package**, followed by **SHP reboot**, followed by **post-reboot connection attempts**.

### A2) HTTP observations (SHP web UI on port 80)
- Proven in today’s Wireshark analysis doc: SHP is serving HTTP on **port 80**:
  - Host: `192.168.1.143:80` (SHP)
  - Client: `192.168.4.2` (workstation)
- The HTTP traffic includes **web UI static asset style requests** (exact endpoints are not fully preserved in today’s truncated md excerpt, so Codex must recover them from the pcap).

### A3) Raw TCP observations tied to project push
- Proven in today’s Wireshark analysis doc: a TCP flow involving **TCP port 50339** is a strong candidate for the **ID11 → SHP project/package push**.
- Proven nuance in doc: the transfer appears to include **binary database/resource-like content** (wording in the doc: “transferred as databases/resources”).
- Proven nuance in doc: there is mention of a **short ~20-byte SHP response** following the push, interpreted only as “ACK/status structure candidate,” but semantics are explicitly **not provable** from the doc alone.

### A4) TCP ports during reboot window
- Proven in today’s Wireshark analysis doc: during/after reboot there is a **cluster of SYN packets** with SHP source ports in the range:
  - `50331–50343`
- Proven limitation in doc: **no completed handshakes / higher-layer payloads** are confirmed in the text excerpt; it is logged as a “remains theory” interpretation.

### A5) External IP traffic is present (attribution not provable)
- Proven in today’s Wireshark analysis doc: the workstation contacts **several public IPs**.
- Proven limitation in doc: attribution (“what service is that?”) is explicitly **not provable from IPs alone**.

### A6) TCP stream decoding status + failure mode (decoder correctness issue)
From `tcp_stream_format_rules_updated.md`:
- A working decoder exists for **“Entire Conversation”** captures (locked script kept as `Entire_Conversation`).
- A new capture type is identified: **“Single Transport communication”** raw output, requiring a separate approach/script (`Single_Transport`).
- Current failure symptom: decoded output still contains **ellipsis “...” truncation** indicating missing payload segments.
- Identified root-cause hypothesis inside the rules doc: the decoder is likely dropping or failing to reconstruct **segments split across multiple frames/lines**, especially around printable/unprintable boundaries.

### A7) Transport-layer roles (methodology-level, not implementation)
From the canvas methodology doc:
- The system must treat **HTTP, raw TCP, and WebSocket** as separate observation layers, with no assumption of authority without proof.
- The redesigned transport layer should support:
  - **multi-transport observation**
  - raw capture persistence for replay
  - cross-transport correlation by timestamp

---

## Part B — Theory (explicit hypotheses with “why” and what to look for)

### B1) Two diagnostics planes: “tech support” vs “programmer” (split by transport)
**Hypothesis**
- Diagnostics may split into:
  1) **Tech-support-level** stream(s): TraceViewer-style, likely raw TCP, richer boot/restart visibility.
  2) **Programmer-level** stream(s): browser web UI, likely WebSocket + HTTP, potentially a filtered/projection view.
**Why we think this**
- User observation: TraceViewer can capture boot/driver restart traffic and reconnect behavior differs from browser UI.
**What would prove it in pcap**
- Distinct endpoint/ports with different payload types:
  - One long-lived TCP stream with dense logs (possibly UTF-16LE or mixed binary/text)
  - One WebSocket session with structured messages (likely JSON) and client-driven filtering.

### B2) Log-level control exists in two mechanisms (and they’re different)
**Hypothesis**
- Mechanism A: **WebSocket/HTTP** log levels for the programmer web UI (runtime controls / subscriptions).
- Mechanism B: **ID11 ↔ TraceViewer** log-level settings embedded in the project file or push process, affecting TraceViewer behavior after push/reboot.
**Why we think this**
- User statement: TraceViewer “looks for flags in the ID11 project file that sets the log levels in TraceViewer after a push/reboot.”
**What would prove it in pcap**
- Evidence for A:
  - WS client→server messages that look like “set level / subscribe categories / filter”
  - Server→client confirmations or state broadcasts.
- Evidence for B:
  - During push, a payload segment that contains recognizable configuration keys/flags related to log levels,
  - Or post-reboot TCP handshake/config exchange consistent with TraceViewer negotiation.

### B3) TraceViewer reconnect attempt is the early reconnect
**Hypothesis**
- The reconnect attempts observed around reboot are likely the TraceViewer channel, because TraceViewer is designed to reconnect quickly and capture startup.
**Why we think this**
- User assessment of the reconnect behavior in the reboot capture.
**What would prove it in pcap**
- Identify the client application signature:
  - TLS SNI / HTTP UA / WS headers (if applicable),
  - Or TCP payload banner/handshake unique to TraceViewer.

### B4) Public IPs: RTI Cloud or cloud-driver “phone home”
**Hypothesis**
- Public IP traffic is either RTICloud infrastructure or cloud drivers contacting manufacturer servers.
**Why we think this**
- User expectation based on RTI ecosystem.
**What would prove it in pcap**
- DNS queries, TLS SNI, certificate CN/SAN, HTTP Host headers, or identifiable endpoints.
**What to do if not provable**
- Add a controlled capture where DNS is not encrypted and include system-wide DNS logs.

### B5) Additional TCP ports activate only during reboot
**Hypothesis**
- Additional SHP ports/flows appear only during reboot to serve diagnostics clients (TraceViewer-style).
**Why we think this**
- SYN burst from `50331–50343` range during reboot window.
**What would prove it**
- Successful TCP handshakes in that window + payload exchange (even small).
**If missing**
- Capture with longer post-reboot window and ensure no capture loss.

### B6) TCP diagnostics payload format likely mixed (binary + UTF-16LE text)
**Hypothesis**
- Diagnostics content may contain UTF-16LE human text embedded within framed/binary envelopes.
**Why we think this**
- Today’s decoding work focuses on UTF-16LE decoding and “anchor-based reconstruction” from raw TCP streams.
**What would prove it**
- In pcap, find sequences where every other byte is 0x00 around ASCII-like text (UTF-16LE hallmark), interleaved with non-text bytes.

### B7) SQLite database presence and extraction attempt path
**Hypothesis**
- SHP may store project and/or metadata in an internal SQLite database (or SQLite-like file), and Oracle should attempt to locate and extract it.
**Why we think this**
- User explicitly requested inclusion so Codex can generate a method to try.
**What would prove it**
- On-wire evidence of SQLite file transfer or SQLite magic header “SQLite format 3\x00” in push payload or HTTP download.
**Where to look**
- In the project push TCP stream (candidate port 50339) and any HTTP downloads around push/reboot.
**Capture plan if not visible**
- Add a capture that includes:
  - Any “download project” or “export” action in the web UI,
  - Any traceviewer export action,
  - Full packet bytes (no truncation) and “Follow TCP Stream” saved raw.

### B8) Required “where/how to access data” map (candidate, must be validated)
This is a **target map** for Codex to test against the pcap and future captures:

1) **HTTP :80**
   - Expected: web UI endpoints, static assets, possibly JSON/REST endpoints.
   - Look for: endpoints that enumerate drivers, projects, or diagnostics settings.

2) **TCP :50339 (candidate)**
   - Expected: bulk project/package upload from ID11 to SHP; may include binary databases/resources.
   - Look for: large payloads, file-like structure, magic headers (SQLite, zip, etc.), short SHP ACK/status response.

3) **TCP :50331–50343 (reboot window burst)**
   - Expected: early reconnect attempts / service startup ports.
   - Look for: successful handshakes; identify whether client is TraceViewer or browser.

4) **WebSocket (upgrade over HTTP, port likely 80 unless proven otherwise)**
   - Expected: programmer diagnostics stream, log-level controls, subscriptions/filters.
   - Look for: `Upgrade: websocket` requests, message frames with JSON-like structure, client→server “set level / subscribe” actions.

Note: A previously-discussed diagnostic TCP port (e.g., “1143”) is **not proven by today’s documents**; do not assert it unless found in the pcap.

---

## Part C — Step-by-step capture method to fill the biggest gaps (if pcap is insufficient)

1) Capture a full sequence with:
   - start before ID11 push,
   - include the entire push,
   - include the full reboot,
   - include at least 2 minutes post-reboot reconnect behavior.
2) Save:
   - the `.pcapng`,
   - “Follow TCP Stream” exports for each candidate port as **Raw** (not ASCII).
3) For each candidate stream:
   - attempt UTF-16LE decode,
   - if missing segments appear (“...”), verify whether Wireshark export is truncating or whether packets are missing.
4) Trigger known UI actions while capturing:
   - change log levels in browser UI,
   - connect TraceViewer and confirm it captures boot logs,
   - if possible, re-push project and observe whether TraceViewer log levels change automatically afterward.
5) Record the exact UI actions + timestamps so evidence can be correlated.

---

## Part D — Candidate updates to core specs (DO NOT APPLY; proposals only)

- **data_contracts.md**: add a decoder contract section for “Single Transport” raw stream format once validated in pcap.
- **architecture.md**: add an explicit note that log-level control may have two independent planes (browser WS vs ID11/TraceViewer), pending proof.
- **runbook.md**: add a capture runbook section listing required capture windows and artifact exports (pcap + raw stream exports).
```

Source: RawDiagnosticsFeeds/TCPDiagnostics/Bootup/Wireshark Captures/wireshark_output_7.txt
```text
68656c6c6f
68656c6c6f
010049006e007000750074002000200020002f0049006e0066006f0072006d00610074003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00340037002e003600200031003600340034003100350032002e0035003900300020005b003500350066006200630036006500200020005d00200042007500740074006f006e00200044006f0077006e0020002d0020004400650076006900630065003a002700520054006900500061006e0065006c00200028006900500068006f006e0065002000580020006f00720020006e0065007700650072002900270020005400720061006e00730070006f00720074003a00450074006800650072006e006500740020005400430050000d000a000300000000000000
010049006e007000750074002000200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00340037002e003700200031003600340034003100350032002e0036003100330020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0030005d005d0020004d006100630072006f0020002d002000530074006100720074000d000a000300000000000000
010049006e007000750074002000200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00340037002e003700200031003600340034003100350032002e0036003300380020005b004900720020004f007500740070007500740020005400680072006500610064005d00200044007200690076006500720020002d00200043006f006d006d0061006e0064003a002700530079007300740065006d0020004d0061006e0061006700650072005c005b0048006900640065005d005c00530065007400200053006f007500720063006500200042007900200052006f006f006d00280052006f006f006d00200032002c00200032002900270020005300750073007400610069006e003a004e004f000d000a000300200053007500010049006e007000750074002000200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00340037002e003700200031003600340034003100350032002e0036003400390020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0030005d005d0020004d006100630072006f0020002d00200045006e0064000d000a0003000a00030000000100440072006900760065007200200020002f0049006e0066006f0072006d00610074003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00340037002e003700200031003600340034003100350032002e0036003900300020005b005300630068006500640075006c00650064005400610073006b0073003a007400610073006b005f007300630068006500640075006c0065005f007400680072006500610064005d00200044007200690076006500720020006500760065006e00740020002700410063007400690076006900740079002000520065006100640079003a00200048006f006d0065002000280052006f006f006d00200032002900200069006e00200052006f006f006d002000320020006f006e002000520054006900500061006e0065006c00200028006900500068006f006e0065002000580020006f00720020006e006500770065007200290027000d000a0003000000000000000100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00340037002e003800200031003600340034003100350032002e0037003100350020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d0020004d006100630072006f0020002d002000530074006100720074000d000a000300000000000000
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00340037002e003800200031003600340034003100350032002e0037003300380020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d002000440065006c006100790020003500300030006d0073000d000a0003000a0003000000
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00340038002e003300200031003600340034003100350033002e0032003700330020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d0020004300680061006e0067006500200074006f00200070006100670065002000370020006f006e00200064006500760069006300650020002700520054006900500061006e0065006c00200028006900500068006f006e0065002000580020006f00720020006e006500770065007200290027000d000a000300000000000000
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00340038002e003300200031003600340034003100350033002e0032003800350020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d0020004d006100630072006f0020002d00200045006e0064000d000a000300200037002000
68656c6c6f
010049006e007000750074002000200020002f0049006e0066006f0072006d00610074003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350031002e003800200031003600340034003100350036002e0037003200390020005b003500350066006200630036006500200020005d00200042007500740074006f006e00200044006f0077006e0020002d0020004400650076006900630065003a002700520054006900500061006e0065006c00200028006900500068006f006e0065002000580020006f00720020006e0065007700650072002900270020005400720061006e00730070006f00720074003a00450074006800650072006e006500740020005400430050000d000a000300000000000000
010049006e007000750074002000200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350031002e003800200031003600340034003100350036002e0037003900300020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0030005d005d0020004d006100630072006f0020002d002000530074006100720074000d000a000300000000000000010049006e007000750074002000200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350031002e003900200031003600340034003100350036002e0038003100340020005b004900720020004f007500740070007500740020005400680072006500610064005d00200044007200690076006500720020002d00200043006f006d006d0061006e0064003a002700530079007300740065006d0020004d0061006e0061006700650072005c005b0048006900640065005d005c00530065007400200053006f007500720063006500200042007900200052006f006f006d00280052006f006f006d00200032002c002000310030002900270020005300750073007400610069006e003a004e004f000d000a000300530075007300010049006e007000750074002000200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350031002e003900200031003600340034003100350036002e0038003200350020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0030005d005d0020004d006100630072006f0020002d00200045006e0064000d000a0003000a00030000000100440072006900760065007200200020002f00440065007400610069006c00730020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003900200031003600340034003100350036002e0038003700320020005b005400570044005200560020005300630072006900700074005400680072006500610064005d002000530079007300740065006d0020004d0061006e00610067006500720020002d00200050006f0070007500700020005300790073005600610072004300680061006e00670065002000430061006c006c006200610063006b00200049006e00690074006100740065006400200077006900740068002000390035000d000a00030047004d005400
0100440072006900760065007200200020002f00440065007400610069006c00730020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003900200031003600340034003100350036002e0038003700340020005b005400570044005200560020005300630072006900700074005400680072006500610064005d002000530079007300740065006d0020004d0061006e00610067006500720020002d00200050006f0070007500700020005300790073005600610072004300680061006e00670065002000430061006c006c006200610063006b00200049006e00690074006100740065006400200077006900740068002000370039000d000a00030047004d0054000100440072006900760065007200200020002f00440065007400610069006c00730020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003900200031003600340034003100350036002e0038003700360020005b005400570044005200560020005300630072006900700074005400680072006500610064005d002000530079007300740065006d0020004d0061006e00610067006500720020002d00200050006f0070007500700020005300790073005600610072004300680061006e00670065002000430061006c006c006200610063006b00200049006e00690074006100740065006400200077006900740068002000380030000d000a00030047004d0054000100440072006900760065007200200020002f00440065007400610069006c00730020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003900200031003600340034003100350036002e0038003700380020005b005400570044005200560020005300630072006900700074005400680072006500610064005d002000530079007300740065006d0020004d0061006e00610067006500720020002d00200050006f0070007500700020005300790073005600610072004300680061006e00670065002000430061006c006c006200610063006b00200049006e00690074006100740065006400200077006900740068002000380031000d000a00030047004d0054000100440072006900760065007200200020002f0049006e0066006f0072006d00610074003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003900200031003600340034003100350036002e0038003800320020005b005300630068006500640075006c00650064005400610073006b0073003a007400610073006b005f007300630068006500640075006c0065005f007400680072006500610064005d00200044007200690076006500720020006500760065006e0074002000270041006300740069007600690074007900200053007400610072007400200069006e00200052006f006f006d002000320020006f006e002000520054006900500061006e0065006c00200028006900500068006f006e0065002000580020006f00720020006e006500770065007200290027000d000a000300580020006f000100440072006900760065007200200020002f0049006e0066006f0072006d00610074003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003900200031003600340034003100350036002e0038003800330020005b005300630068006500640075006c00650064005400610073006b0073003a007400610073006b005f007300630068006500640075006c0065005f007400680072006500610064005d00200044007200690076006500720020006500
760065006e0074002000270052006f006f006d0020004f004e00200069006e00200052006f006f006d002000320027000d000a0003006d00200032000100440072006900760065007200200020002f0049006e0066006f0072006d00610074003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003900200031003600340034003100350036002e0038003800350020005b005300630068006500640075006c00650064005400610073006b0073003a007400610073006b005f007300630068006500640075006c0065005f007400680072006500610064005d00200044007200690076006500720020006500760065006e0074002000270041007500640069006f0020004f004e00200069006e00200052006f006f006d002000320027000d000a0003002000320020000100440072006900760065007200200020002f0049006e0066006f0072006d00610074003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003900200031003600340034003100350036002e0038003800360020005b005300630068006500640075006c00650064005400610073006b0073003a007400610073006b005f007300630068006500640075006c0065005f007400680072006500610064005d00200044007200690076006500720020006500760065006e0074002000270041006300740069007600690074007900200044006500730065006c00650063007400650064003a00200048006f006d0065002000280052006f006f006d00200032002900200069006e00200052006f006f006d002000320027000d000a000300720020006e000100440072006900760065007200200020002f0049006e0066006f0072006d00610074003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003900200031003600340034003100350036002e0038003800370020005b005300630068006500640075006c00650064005400610073006b0073003a007400610073006b005f007300630068006500640075006c0065005f007400680072006500610064005d00200044007200690076006500720020006500760065006e0074002000270050006f0077006500720020004f006e00200053006f0075007200630065003a00200041007500640069006f00200053006f0075007200630065002000280052006f006f006d0020003200290027000d000a0003002000320027000100440072006900760065007200200020002f0049006e0066006f0072006d00610074003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003900200031003600340034003100350036002e0038003800380020005b005300630068006500640075006c00650064005400610073006b0073003a007400610073006b005f007300630068006500640075006c0065005f007400680072006500610064005d00200044007200690076006500720020006500760065006e00740020002700410063007400690076006900740079002000530065006c00650063007400650064003a00200041007500640069006f00200053006f0075007200630065002000280052006f006f006d00200032002900200069006e00200052006f006f006d002000320027000d000a0003007200290027000100440072006900760065007200200020002f0049006e0066006f0072006d00610074003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003900200031003600340034003100350036002e0038003900300020005b005300630068006500
64005400610073006b0073003a007400610073006b005f007300630068006500640075006c0065005f007400680072006500610064005d00200044007200690076006500720020006500760065006e00740020002700410063007400690076006900740079002000520065006100640079003a00200041007500640069006f00200053006f0075007200630065002000280052006f006f006d00200032002900200069006e00200052006f006f006d002000320020006f006e002000520054006900500061006e0065006c00200028006900500068006f006e0065002000580020006f00720020006e006500770065007200290027000d000a000300000000000000
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003100200031003600340034003100350036002e0039003900320020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d0020004d006100630072006f0020002d002000530074006100720074000d000a0003000000000000000100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003100200031003600340034003100350036002e0039003900340020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d0020004300680061006e0067006500200074006f002000700061006700650020003200300020006f006e00200064006500760069006300650020002700520054006900500061006e0065006c00200028006900500068006f006e0065002000580020006f00720020006e006500770065007200290027000d000a0003000000000000000100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003100200031003600340034003100350037002e0030003200370020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d002000440065006c006100790020003500300030006d0073000d000a000300200032003000
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350032002e003600200031003600340034003100350037002e0035003600310020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d002000440065006c006100790020003500300030006d0073000d000a000300200032003000
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350033002e003100200031003600340034003100350038002e0030003700330020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d002000530074006f00700020006d006100630072006f000d000a000300030020003200
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350033002e003200200031003600340034003100350038002e0031003400380020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d0020004d006100630072006f0020002d002000530074006100720074000d000a0003000000000000000100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350033002e003200200031003600340034003100350038002e0031003700310020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d002000440065006c006100790020003500300030006d0073000d000a0003000a0003000000
68656c6c6f
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350033002e003800200031003600340034003100350038002e0037003000380020005b004900720020004f007500740070007500740020005400680072006500610064005d00200044007200690076006500720020002d00200043006f006d006d0061006e0064003a00270052005400490020005600690072007400750061006c0020004d0075006c007400690072006f006f006d00200041006d0070005c005a006f006e006500200032005c0050006f0077006500720028004f006e002900270020005300750073007400610069006e003a004e004f000d000a000300740061006900
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350033002e003800200031003600340034003100350038002e0037003200300020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d0020004d006100630072006f0020002d00200045006e0064000d000a0003000a00030000000100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350033002e003800200031003600340034003100350038002e0037003500360020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d0020004d006100630072006f0020002d002000530074006100720074000d000a0003000000000000000100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350033002e003800200031003600340034003100350038002e0037003700390020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d002000440065006c006100790020003500300030006d0073000d000a0003000a0003000000
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350034002e003400200031003600340034003100350039002e0033003100330020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d0020004d006100630072006f0020002d00200045006e0064000d000a0003000a0003000000
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350034002e003400200031003600340034003100350039002e0033003800370020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d0020004d006100630072006f0020002d002000530074006100720074000d000a0003000000000000000100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350034002e003500200031003600340034003100350039002e0034003300310020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d002000440065006c006100790020003500300030006d0073000d000a0003000a0003000000
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350035002e003000200031003600340034003100350039002e0039003600350020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d002000440065006c006100790020003500300030006d0073000d000a0003000a0003000000
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350035002e003600200031003600340034003100360030002e0034003900390020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d0020004d006100630072006f0020002d00200045006e0064000d000a0003000a0003000000
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300033003a00350035002e003700200031003600340034003100360030002e0036003000300020005b004d0…3146 tokens truncated…00640075006c00650064005400610073006b0073003a007400610073006b005f007300630068006500640075006c0065005f007400680072006500610064005d00200044007200690076006500720020006500760065006e0074002000270052006f006f006d0020004f0066006600200043006f006d0070006c00650074006500200069006e00200052006f006f006d002000320020006f006e002000520054006900500061006e0065006c00200028006900500068006f006e0065002000580020006f00720020006e006500770065007200290027000d000a00030068006f006e00
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300034003a00300030002e003700200031003600340034003100360035002e0036003100320020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d0020004d006100630072006f0020002d002000530074006100720074000d000a0003000000000000000100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300034003a00300030002e003700200031003600340034003100360035002e0036003100340020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d0020004300680061006e0067006500200074006f002000700061006700650020003200300020006f006e00200064006500760069006300650020002700520054006900500061006e0065006c00200028006900500068006f006e0065002000580020006f00720020006e006500770065007200290027000d000a0003000000000000000100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300034003a00300030002e003700200031003600340034003100360035002e0036003600390020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d002000440065006c006100790020003500300030006d0073000d000a000300200032003000
0100440072006900760065007200200020002f00440065007400610069006c00730020003a00300031002f00310039002f0032003000320036002000310031003a00300034003a00300030002e003900200031003600340034003100360035002e0038003000380020005b005400570044005200560020005300630072006900700074005400680072006500610064005d002000530079007300740065006d0020004d0061006e00610067006500720020002d00200043006c006f0063006b003a002000550070006400610074006500540069006d006500530079007300560061007200730020006100740020004d006f006e0020004a0061006e00200031003900200032003000320036002000310031003a00300034003a0030003000200047004d0054002b0030003000300030000d000a000d000a0003000a0003002200
0100440072006900760065007200200020002f00550073006500720020002000200020003a00300031002f00310039002f0032003000320036002000310031003a00300034003a00300031002e003300200031003600340034003100360036002e0032003000330020005b004d006100630072006f0045006e00670069006e006500630061006c006c005400680072006500610064005b0035005d005d002000440065006c006100790020003500300030006d0073000d000a000300200032003000
01004400720069006…
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

Source: SHPDiagnosticsViewer/DiagnosticsTransport/TcpCaptureDiagnosticsTransport.cs
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SHPDiagnosticsViewer.DiagnosticsTransport;

public sealed class TcpCaptureDiagnosticsTransport : IDiagnosticsTransport
{
    private readonly int _port;
    private readonly bool _sendProbeOnConnect;
    private TcpClient? _client;
    private CancellationTokenSource? _cts;
    private long _bytesRead;
    private long _bytesWritten;
    private int _disconnectLogged;
    private string _disconnectReason = "";
    private List<byte> _byteBuffer = new();
    private long _recordsEmitted;
    private DecodeWsStreamDecoder _decoder = new();

    public TcpCaptureDiagnosticsTransport(int port = 2113, bool sendProbeOnConnect = false)
    {
        _port = port;
        _sendProbeOnConnect = sendProbeOnConnect;
    }

    public event EventHandler<string>? RawMessageReceived;
    public event EventHandler<string>? TransportInfo;
    public event EventHandler<string>? TransportError;

    public bool IsConnected => _client != null && _client.Connected;

    public Task<List<string>> DiscoverAsync(TimeSpan timeout)
    {
        return Task.FromResult(new List<string>());
    }

    public async Task ConnectAsync(string ip)
    {
        await DisconnectAsync();

        _client = new TcpClient();
        _cts = new CancellationTokenSource();
        _bytesRead = 0;
        _bytesWritten = 0;
        _disconnectLogged = 0;
        _disconnectReason = "";
        _byteBuffer = new List<byte>();
        _recordsEmitted = 0;
        _decoder = new DecodeWsStreamDecoder();
        await _client.ConnectAsync(ip, _port);

        EmitInfo($"[info] TCP capture connected to {ip}:{_port}");

        if (_client.Connected)
        {
            if (_sendProbeOnConnect)
            {
                var probe = new byte[] { 0x00 };
                await _client.GetStream().WriteAsync(probe, 0, 1);
                _bytesWritten += 1;
                EmitInfo("[info] TCP probe sent bytes=1");
            }
            else
            {
                EmitInfo("[info] TCP probe disabled bytes=0");
            }
        }

        _ = Task.Run(() => ReceiveLoopAsync(_client, _cts.Token));
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _cts?.Cancel();
        }
        catch
        {
        }

        _byteBuffer.Clear();
        FlushDecoder();

        if (_client != null)
        {
            try
            {
                _client.Close();
            }
            catch
            {
            }
            finally
            {
                _client = null;
            }
        }

        LogDisconnect();
        await Task.CompletedTask;
    }

    public Task SendLogLevelAsync(string type, string level)
    {
        EmitInfo("[info] TCP capture does not send log level commands.");
        return Task.CompletedTask;
    }

    public Task<List<DriverInfo>> LoadDriversAsync(string ip)
    {
        return Task.FromResult(new List<DriverInfo>());
    }

    private async Task ReceiveLoopAsync(TcpClient client, CancellationToken token)
    {
        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[4096];
            while (!token.IsCancellationRequested)
            {
                var count = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                if (count <= 0)
                {
                    break;
                }

                _bytesRead += count;
                AppendBytes(buffer, count);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _disconnectReason = ex.Message;
            EmitError($"[error] TCP capture error: {ex.Message}");
        }
        finally
        {
            FlushDecoder();
            LogDisconnect();
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

    private void AppendBytes(byte[] buffer, int count)
    {
        if (count <= 0)
        {
            return;
        }

        for (var i = 0; i < count; i++)
        {
            _byteBuffer.Add(buffer[i]);
        }
        EmitRecordsFromBytes();
    }

    private void EmitRecordsFromBytes()
    {
        var delimiter = new byte[] { 0x0D, 0x00, 0x0A, 0x00, 0x03, 0x00 };
        while (true)
        {
            var endIndex = FindDelimiter(_byteBuffer, delimiter);
            if (endIndex < 0)
            {
                return;
            }

            var recordLength = endIndex;
            if (recordLength == 0)
            {
                _byteBuffer.RemoveRange(0, endIndex + delimiter.Length);
                continue;
            }

            if (recordLength % 2 == 1)
            {
                return;
            }

            var recordBytes = _byteBuffer.GetRange(0, recordLength).ToArray();
            _byteBuffer.RemoveRange(0, endIndex + delimiter.Length);
            EmitDecodedRecord(recordBytes);
        }
    }

    private static int FindDelimiter(List<byte> buffer, byte[] delimiter)
    {
        if (buffer.Count < delimiter.Length)
        {
            return -1;
        }

        for (var i = 0; i <= buffer.Count - delimiter.Length; i++)
        {
            if (i % 2 != 0)
            {
                continue;
            }

            var match = true;
            for (var j = 0; j < delimiter.Length; j++)
            {
                if (buffer[i + j] != delimiter[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindSecondaryDelimiter(List<byte> buffer, byte[] delimiter)
    {
        if (buffer.Count < delimiter.Length + 4)
        {
            return -1;
        }

        for (var i = 0; i <= buffer.Count - delimiter.Length; i++)
        {
            if (i % 2 != 0)
            {
                continue;
            }

            var match = true;
            for (var j = 0; j < delimiter.Length; j++)
            {
                if (buffer[i + j] != delimiter[j])
                {
                    match = false;
                    break;
                }
            }

            if (!match)
            {
                continue;
            }

            var nextIndex = i + delimiter.Length;
            if (HasHeaderPrefix(buffer, nextIndex))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool HasHeaderPrefix(List<byte> buffer, int index)
    {
        if (index % 2 != 0)
        {
            return false;
        }

        if (index + 2 > buffer.Count)
        {
            return false;
        }

        var available = Math.Min(128, buffer.Count - index);
        if (available < 4)
        {
            return false;
        }

        if (available % 2 == 1)
        {
            available -= 1;
        }

        var window = new byte[available];
        for (var i = 0; i < available; i++)
        {
            window[i] = buffer[index + i];
        }

        var text = Encoding.Unicode.GetString(window, 0, window.Length);
        return MatchesHeaderPrefix(text);
    }

    private static bool MatchesHeaderPrefix(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var index = 0;
        while (index < text.Length)
        {
            var ch = text[index];
            if (!char.IsControl(ch) && !char.IsWhiteSpace(ch) && ch != '\uFFFD')
            {
                break;
            }
            index++;
        }

        if (index >= text.Length)
        {
            return false;
        }

        if (!IsAsciiAlpha(text[index]))
        {
            return false;
        }

        index += 1;
        while (index < text.Length)
        {
            var ch = text[index];
            if (ch == '/')
            {
                index++;
                break;
            }

            if (!IsAsciiAlnum(ch))
            {
                return false;
            }
            index++;
        }

        const string marker = "Informat:";
        if (index + marker.Length > text.Length)
        {
            return false;
        }

        for (var i = 0; i < marker.Length; i++)
        {
            if (text[index + i] != marker[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiAlpha(char ch)
    {
        return (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
    }

    private static bool IsAsciiAlnum(char ch)
    {
        return IsAsciiAlpha(ch) || (ch >= '0' && ch <= '9');
    }

    private static string StripControlEdges(string record)
    {
        var start = 0;
        var end = record.Length - 1;

        while (start <= end)
        {
            var ch = record[start];
            if (!char.IsControl(ch) && ch != '\uFFFD')
            {
                break;
            }
            start++;
        }

        while (end >= start)
        {
            var ch = record[end];
            if (!char.IsControl(ch) && ch != '\uFFFD')
            {
                break;
            }
            end--;
        }

        if (end < start)
        {
            return "";
        }

        var trimmed = record.Substring(start, end - start + 1);
        return trimmed.TrimEnd();
    }

    private void EmitDecodedRecord(byte[] recordBytes)
    {
        foreach (var line in _decoder.AppendRecordBytes(recordBytes))
        {
            RawMessageReceived?.Invoke(this, line);
            _recordsEmitted++;
        }
    }

    private void FlushDecoder()
    {
        foreach (var line in _decoder.Flush())
        {
            RawMessageReceived?.Invoke(this, line);
            _recordsEmitted++;
        }
    }

    private void LogDisconnect()
    {
        if (Interlocked.Exchange(ref _disconnectLogged, 1) != 0)
        {
            return;
        }

        var reason = string.IsNullOrWhiteSpace(_disconnectReason) ? "" : $" error={_disconnectReason}";
        EmitInfo($"[info] TCP capture disconnected bytes_read={_bytesRead} bytes_written={_bytesWritten} records_emitted={_recordsEmitted}{reason}");
    }

    private sealed class DecodeWsStreamDecoder
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
        private static readonly string[] Prefixes = { "Input", "Driver", "System Manager", "Macro" };
        private static readonly Regex PrefixRegex = new("(Input|Driver|System Manager|Macro|hello)", RegexOptions.Compiled);
        private static readonly Regex DatePattern = new("\\d{2}/\\d{2}/\\d{4}", RegexOptions.Compiled);
        private static readonly Regex SchedulePattern = new("\\[Schedule\\s+Driver event", RegexOptions.Compiled);
        private static readonly Regex SustainPattern = new("(Sustain:NO)\\s+[A-Za-z]{1,3}$", RegexOptions.Compiled);
        private static readonly Regex TrailingMarkerPattern = new("([)'\\\"])\\s+[A-Za-z]{1,3}$", RegexOptions.Compiled);

        private string? _currentLogicalLine;

        public IEnumerable<string> AppendRecordBytes(byte[] recordBytes)
        {
            var decoded = DecodeBytes(recordBytes);
            var cleaned = CleanText(decoded);
            if (string.IsNullOrEmpty(cleaned))
            {
                return Array.Empty<string>();
            }

            var output = new List<string>();
            var rawLines = cleaned.Split('\n');
            foreach (var raw in rawLines)
            {
                var line = raw.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                ProcessNormalizedLine(NormalizeLine(line), output);
            }

            return output;
        }

        public IEnumerable<string> Flush()
        {
            if (string.IsNullOrEmpty(_currentLogicalLine))
            {
                return Array.Empty<string>();
            }

            var output = new List<string> { CleanLogicalLine(_currentLogicalLine) };
            _currentLogicalLine = null;
            return output;
        }

        private static string DecodeBytes(byte[] data)
        {
            if (data.Length == 0)
            {
                return "";
            }

            var oddZeros = 0;
            for (var i = 1; i < data.Length; i += 2)
            {
                if (data[i] == 0)
                {
                    oddZeros++;
                }
            }

            var useUtf16 = data.Length >= 2 && oddZeros > data.Length / 4.0;
            var encodings = useUtf16
                ? new[] { Utf16Le, Utf8, Latin1 }
                : new[] { Utf8, Latin1, Utf16Le };

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
            var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace("\r", "\n", StringComparison.Ordinal);
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

        private static string NormalizeLine(string line)
        {
            var match = PrefixRegex.Match(line);
            if (match.Success && match.Index > 0)
            {
                line = line.Substring(match.Index);
            }

            return line.Trim();
        }

        private void ProcessNormalizedLine(string line, List<string> output)
        {
            if (string.IsNullOrEmpty(line) || !line.Any(char.IsLetterOrDigit))
            {
                return;
            }

            if (line == "hello")
            {
                return;
            }

            if (line.All(char.IsDigit))
            {
                return;
            }

            var hasDate = DatePattern.IsMatch(line);
            var startsWithPrefix = StartsWithPrefixes(line);
            if (!hasDate
                && !string.IsNullOrEmpty(_currentLogicalLine)
                && (line.StartsWith("Driver event", StringComparison.Ordinal)
                    || line.StartsWith("Driver - Command", StringComparison.Ordinal)
                    || line.StartsWith("System Manager", StringComparison.Ordinal)
                    || (!startsWithPrefix && !char.IsDigit(line[0]))))
            {
                _currentLogicalLine = $"{_currentLogicalLine} {line}".Trim();
                return;
            }

            if (startsWithPrefix || char.IsDigit(line[0]))
            {
                EmitCurrentLine(output);
                _currentLogicalLine = line;
                return;
            }

            if (!string.IsNullOrEmpty(_currentLogicalLine))
            {
                _currentLogicalLine = $"{_currentLogicalLine} {line}".Trim();
                return;
            }

            _currentLogicalLine = line;
        }

        private void EmitCurrentLine(List<string> output)
        {
            if (string.IsNullOrEmpty(_currentLogicalLine))
            {
                return;
            }

            output.Add(CleanLogicalLine(_currentLogicalLine));
        }

        private static bool StartsWithPrefixes(string line)
        {
            foreach (var prefix in Prefixes)
            {
                if (line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static string CleanLogicalLine(string line)
        {
            var cleaned = line.Replace("Driver e vent", "Driver event", StringComparison.Ordinal);
            cleaned = SchedulePattern.Replace(cleaned, "[ScheduledTasks] Driver event");
            cleaned = SustainPattern.Replace(cleaned, "$1");
            cleaned = TrailingMarkerPattern.Replace(cleaned, "$1");
            return cleaned;
        }
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
