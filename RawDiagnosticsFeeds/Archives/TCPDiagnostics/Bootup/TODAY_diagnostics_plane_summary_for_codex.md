
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

