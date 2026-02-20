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

