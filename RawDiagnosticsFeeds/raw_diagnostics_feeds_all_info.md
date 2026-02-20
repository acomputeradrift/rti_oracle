- The SHP diagnostics WebSocket endpoint is `ws://<SHP_IP>:1234/diagnosticswss` and accepts an HTTP 101 upgrade handshake.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: New-WebSocketSession
- After a successful WebSocket connection, the server sends an echo welcome message as JSON.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 1) WebSocket API endpoint and handshake are confirmed
- Observed WebSocket message types include `echo`, `LogLevels`, `MessageLog`, `Sysvar`, and `SysvarPers`.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 2) WebSocket message types seen on wire are confirmed
- The log stream subscription payload uses `{"type":"Subscribe","resource":"MessageLog","value":"true"}`.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Send-Subscribe
- The sysvar stream subscription payload uses `{"type":"Subscribe","resource":"Sysvar","value":"true"}`.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Send-Subscribe
- The log level set payload uses `{"type":"Subscribe","resource":"LogLevel","value":{"type":"<DName>","level":"<0-3>"}}`.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Set-DriverLogLevel
- The sysvar per-ID toggle payload uses `{"type":"Subscribe","resource":"Sysvar","value":{"id":<ID>,"status":true|false}}`.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 3) WebSocket subscribe command shapes are confirmed
- The persistent sysvar request payload uses `{"type":"Subscribe","resource":"SysvarPers"}`.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 3) WebSocket subscribe command shapes are confirmed
- LogLevel acknowledgements are parsed from `Setting LogLevel on ... to ...` textual patterns.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Parse-LogLevelUpdateText
- The HTTP endpoint `http://<SHP_IP>:5000/diagnostics/data/drivers` returns driver-related diagnostics data.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: $driversUri, Parse-DriversJson
- The HTTP endpoint `http://<SHP_IP>:5000/diagnostics/data/system_status` returns processor status and metrics data.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: $systemStatusUri, Get-ProcessorMetadata
- The HTTP endpoint `http://<SHP_IP>:5000/diagnostics/data/sysvars` returns sysvar data and may return gzip-compressed JSON.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 5) HTTP diagnostics endpoints on port 5000 are confirmed
- The SHP serves a diagnostics web UI shell on port 80 at `/diagnostics`.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 6) HTTP web UI on port 80 is confirmed
- Diagnostics HTML on port 80 references static CSS and JavaScript assets under `/diagnostics/...` paths.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 6) HTTP web UI on port 80 is confirmed
- Captured driver endpoint payloads include a `port_list` field that references ports including 80, 5000, and 1234.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 7) Additional on-wire metadata from drivers endpoint is confirmed
- Alternate HTTP log-level snapshot endpoints are hypothesized as `/diagnostics/data/loglevels`, `/diagnostics/data/log_levels`, and `/diagnostics/data/loglevel`.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Get-LogLevelsSnapshotByHttp
- TraceViewer traffic repeatedly uses TCP port 2113 on the SHP side.
Value: Confirmed
Evidence: oracle_reconnect.md :: Proven (from pcap) > P1) TraceViewer uses TCP port 2113 on the SHP
- TraceViewer reconnect attempts send a small ASCII `hello` probe from client to SHP.
Value: Confirmed
Evidence: oracle_reconnect.md :: Proven (from pcap) > P2) Client sends a small ASCII probe on reconnect
- SHP responses on the TraceViewer channel decode as UTF-16LE diagnostic log text.
Value: Confirmed
Evidence: oracle_reconnect.md :: Proven (from pcap) > P3) SHP responds with UTF-16LE log lines
- Multiple short TCP sessions to port 2113 occur around reboot windows and match reconnect-like behavior.
Value: Confirmed
Evidence: oracle_reconnect.md :: Proven (from pcap) > P4) Multiple short sessions observed around reboot
- The likely minimal TraceViewer handshake is open TCP, send `hello`, then read log stream.
- Reconnect probe timing near three-second intervals is observed and likely client-specific behavior.
- A minimal third-party client procedure is connect to `SHP_IP:2113`, send `hello`, decode UTF-16LE, then retry after disconnect.
- It remains unknown whether `hello` must be resent after the connection is already established.
- It remains unknown whether hidden control or keepalive messages exist beyond the observed `hello` probe.
- It remains unknown whether category suppression is controlled by protocol negotiation or by project flags.
- Observed diagnostics transport behavior is explicitly documented as empirical and non-authoritative.
- Observational transport notes are not intended to define contracts, guarantees, permissions, or stable schemas.
- Third-party tools are used only for observation and are not project runtime dependencies.
- Observed diagnostics output is delivered over TCP with an observed default port of 2113.
- CONFLICT: Observed transport notes state communication is SHP-to-client only after connection except for log-level configuration.
- CONFLICT: Capture evidence shows the client sends a `hello` payload to SHP on reconnect attempts over TCP/2113.
Value: Confirmed
Evidence: oracle_reconnect.md :: Proven (from pcap) > P2) Client sends a small ASCII probe on reconnect
- Observed payload text is UTF-16LE and messages use explicit start and end framing markers.
- Observed framing markers are start `0x01 0x00` and end `0x03 0x00` around decoded messages.
- Framing markers delimit logical messages but do not guarantee TCP packet boundary alignment.
- Partial records are expected because stream record boundaries do not reliably align with TCP segments.
- Contradictory transport findings must be recorded rather than silently replacing older observations.
- A capture analyzed today includes an ID11 project push, an SHP reboot, and post-reboot connection attempts.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A1) The capture context (Project push + reboot)
- A candidate ID11 push flow is associated with TCP port 50339 and appears to carry binary resource content.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A3) Raw TCP observations tied to project push
- A short SHP response of about twenty bytes after project push is observed but its semantics remain unproven.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A3) Raw TCP observations tied to project push
- During the reboot window, SYN clusters are observed with SHP source ports in the 50331 to 50343 range.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A4) TCP ports during reboot window
- Workstation traffic to several public IP addresses is observed, but service attribution is not provable from IPs alone.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A5) External IP traffic is present (attribution not provable)
- A working decoder exists for entire-conversation captures while single-transport capture decoding needs a separate approach.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A6) TCP stream decoding status + failure mode (decoder correctness issue)
- Current single-transport decoding still shows ellipsis truncation, indicating missing reconstructed payload segments.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A6) TCP stream decoding status + failure mode (decoder correctness issue)
- A documented root-cause hypothesis is dropped or unreconstructed split segments near printable and non-printable boundaries.
- Methodology guidance requires treating HTTP, raw TCP, and WebSocket as separate observation layers.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A7) Transport-layer roles (methodology-level, not implementation)
- Proposed transport redesign requires multi-transport observation, raw capture persistence, and timestamp correlation.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A7) Transport-layer roles (methodology-level, not implementation)
- The ID11 push stream contains embedded debug-related strings including `DebugLevel0` and `DebugTrace` values.
Value: Confirmed
Evidence: TraceViewer_capture_analysis.md :: 1) Log-level flag propagation (ID11 -> TraceViewer) > 1.1 Project push stream contains explicit debug/log configuration keys
- The ID11 push stream also contains a TraceViewer routing description that references routing log info to TraceViewer.
Value: Confirmed
Evidence: TraceViewer_capture_analysis.md :: 1) Log-level flag propagation (ID11 -> TraceViewer) > 1.2 Project push contains explicit TraceViewer routing description
- No explicit on-wire message proves driver-level filter negotiation or acknowledgement between ID11 and TraceViewer.
- TraceViewer appears to behave as a passive TCP/2113 log consumer rather than an active on-wire filter negotiator.
- The no-debug comparison capture shows `DebugTracefalse` while the debug-enabled capture shows `DebugTracetrue`.
Value: Confirmed
Evidence: TraceViewer_capture_analysis.md :: Appendix > A.1 DebugTrace flips from true -> false
- DebugLevel appears present as `DebugLevel0` in both compared captures, so driver-specific filtering remains unproven.
Value: Confirmed
Evidence: TraceViewer_capture_analysis.md :: Appendix > A.2 DebugLevel remains present and set to 0 in both captures
- A proof procedure requires paired captures where only Debug Level changes and both payload diff and resulting logs are compared.
- CONFLICT: One methodology note expects diagnostics WebSocket upgrades likely on port 80 unless evidence proves otherwise.
- CONFLICT: Capture-backed evidence confirms diagnostics WebSocket traffic at `ws://<SHP_IP>:1234/diagnosticswss`.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 1) WebSocket API endpoint and handshake are confirmed
- Post-change `LogLevels` snapshots are documented as unreliable for write confirmation in recent tests.
Value: Confirmed
Evidence: SMH_driver_log_status_sync_implementation.md :: Known Constraint
- The first `LogLevels` message is used as the baseline snapshot for initial UI state construction.
Value: Confirmed
Evidence: SMH_driver_log_status_sync_implementation.md :: Confirmed Strategy
- Runtime sync after baseline should use `OnHTTPServerData() data.websocket = ... LogLevel ...` lines as confirmation signals.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Parse-LogLevelUpdateText, Wait-ForDriverLevelAcks
- UI state tracking is defined with `currentLevel`, `pendingLevel`, and status values `idle`, `requested`, `confirmed`, and `unconfirmed`.
- On reconnect, controls should enter temporary unknown state, rebuild from first `LogLevels`, then resume acknowledgement-based tracking.
- The controls `Diagnostics: Primary Processor` and `DRIVER//4` must be forced to levels 0 and 1 and hidden from normal UI.
- Forced protected levels should be reasserted on connect, reconnect, and optional low-frequency health checks.
- Sync logic should parse `OnHTTPServerData() data.websocket = ...` lines and ignore unrelated runtime log noise.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Parse-LogLevelUpdateText, Wait-ForDriverLevelAcks
- Suggested confirmation timeout is three to five seconds with one retry before marking a change unconfirmed.
