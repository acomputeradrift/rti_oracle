# RawDiagnosticsFeeds All Info

- [SHP API All Knowledge > TESTED > WebSocket endpoint and handshake] The SHP diagnostics WebSocket endpoint is `ws://<SHP_IP>:1234/diagnosticswss`, and the server accepts an HTTP 101 upgrade handshake.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: New-WebSocketSession
- [SHP API All Knowledge > TESTED > WebSocket endpoint and handshake] After a successful diagnostics WebSocket connection, the server sends an echo welcome message to the client.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 1) WebSocket API endpoint and handshake are confirmed
- [SHP API All Knowledge > TESTED > WebSocket message types] Observed diagnostics WebSocket message types include `echo`, `LogLevels`, `MessageLog`, `Sysvar`, and `SysvarPers`.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 2) WebSocket message types seen on wire are confirmed
- [SHP API All Knowledge > TESTED > Subscribe command shapes] Message log subscription uses a `Subscribe` payload targeting `MessageLog` with a true value.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Send-Subscribe
- [SHP API All Knowledge > TESTED > Subscribe command shapes] Sysvar stream subscription uses a `Subscribe` payload targeting `Sysvar` with a true value.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Send-Subscribe
- [SHP API All Knowledge > TESTED > Subscribe command shapes] Log level writes use a `Subscribe` payload with resource `LogLevel` and `value.type` plus `value.level`.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Set-DriverLogLevel
- [SHP API All Knowledge > TESTED > Subscribe command shapes] Canonical driver write shape `{"type":"Subscribe","resource":"LogLevel","value":{"type":"DRIVER","driverId":"<id>","level":"<0-3>"}}` was validated live on 2026-02-21 against `192.168.1.143`, and the returned `OnHTTPServerData() data.websocket` ack preserved `type:"DRIVER"` with matching `driverId`.
Value: Confirmed
Evidence: Live WS probe on 2026-02-21 (Codex terminal session, target `192.168.1.143`)
- [SHP API All Knowledge > TESTED > Subscribe command shapes] Per-ID sysvar toggles use a `Subscribe` payload where `value` contains `id` and boolean `status` fields.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 3) WebSocket subscribe command shapes are confirmed
- [SHP API All Knowledge > TESTED > Subscribe command shapes] Persistent sysvar requests use a `Subscribe` payload that targets the `SysvarPers` resource.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 3) WebSocket subscribe command shapes are confirmed
- [SHP API All Knowledge > TESTED > LogLevel parsing] Log-level acknowledgement parsing supports `Setting LogLevel on DRIVER (id) to level` style message lines.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Parse-LogLevelUpdateText
- [SHP API All Knowledge > TESTED > LogLevel parsing] Log-level acknowledgement parsing also supports plain category forms such as `Setting LogLevel on EVENTS_INPUT to 3`.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Parse-LogLevelUpdateText
- [SHP API All Knowledge > TESTED > HTTP endpoints] The endpoint `http://<SHP_IP>:5000/diagnostics/data/drivers` is confirmed to return driver diagnostics data.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Parse-DriversJson
- [SHP API All Knowledge > TESTED > HTTP endpoints] The endpoint `http://<SHP_IP>:5000/diagnostics/data/system_status` is confirmed to return processor status and metrics data.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Get-ProcessorMetadata
- [SHP API All Knowledge > TESTED > HTTP endpoints] The endpoint `http://<SHP_IP>:5000/diagnostics/data/sysvars` is confirmed and may return gzip-compressed JSON.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 5) HTTP diagnostics endpoints on port 5000 are confirmed
- [SHP API All Knowledge > TESTED > HTTP web UI] The path `/diagnostics` on port 80 serves an HTML diagnostics web UI shell.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 6) HTTP web UI on port 80 is confirmed
- [SHP API All Knowledge > TESTED > HTTP web UI] The diagnostics HTML references static assets under `/diagnostics` including JavaScript and CSS bundles.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 6) HTTP web UI on port 80 is confirmed
- [SHP API All Knowledge > TESTED > Drivers metadata] Captured driver payloads include a `port_list` field that references diagnostics service ports such as 80, 5000, and 1234.
Value: Confirmed
Evidence: SMH_api_all_knowledge.md :: TESTED > 7) Additional on-wire metadata from drivers endpoint is confirmed
- [SHP API All Knowledge > THEORY > Alternate endpoints] Some SHP builds may expose alternative log-level snapshot endpoints like `/diagnostics/data/loglevels`, `/diagnostics/data/log_levels`, or `/diagnostics/data/loglevel`.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Get-LogLevelsSnapshotByHttp
- [SHP API All Knowledge > THEORY > Two-plane model] Diagnostics behavior may be split between an HTTP or WebSocket browser plane and a separate TraceViewer TCP/2113 plane.
- [SHP API All Knowledge > THEORY > Project push effect] TraceViewer debug behavior may be influenced by pushed project fields such as `DebugTrace` and `DebugLevel`.
- [SHP API All Knowledge > THEORY > SysvarPers semantics] `SysvarPers` likely returns only persistent sysvar identifiers rather than a guaranteed full sysvar universe.
- [SHP API All Knowledge > THEORY > Reconnect interval scope] The observed approximately three-second reconnect interval may be client-specific rather than an SHP-wide invariant.

- [Driver Log Status Sync > Confirmed strategy] The UI initialization flow should capture the first `LogLevels` message and use that snapshot as baseline state.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Collect-ObservedLogLevelEntries
- [Driver Log Status Sync > Confirmed strategy] After baseline initialization, the system should immediately enforce `Diagnostics: Primary Processor = 0` and `DRIVER//4 = 1`.
Value: Confirmed
Evidence: SMH_driver_log_status_sync_implementation.md :: Confirmed Strategy
- [Driver Log Status Sync > Confirmed strategy] The `Diagnostics: Primary Processor` and `DRIVER//4` controls should remain hidden from normal user-facing UI.
Value: Confirmed
Evidence: SMH_driver_log_status_sync_implementation.md :: Confirmed Strategy
- [Driver Log Status Sync > Runtime signal] Runtime confirmation for user log-level changes should come from matching `OnHTTPServerData() data.websocket = {...LogLevel...}` lines.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Parse-LogLevelUpdateText, Wait-ForDriverLevelAcks
- [Driver Log Status Sync > State model] Each UI control tracks `currentLevel`, `pendingLevel`, and a status lifecycle of `idle`, `requested`, `confirmed`, or `unconfirmed`.
- [Driver Log Status Sync > State model] On timeout without matching acknowledgement lines, the UI keeps `currentLevel` unchanged and marks the request as `unconfirmed`.
- [Driver Log Status Sync > Protected controls] Controller logic should re-assert enforced protected levels on connect, reconnect, and optional low-frequency health checks.
- [Driver Log Status Sync > Output filtering] Status synchronization should parse `OnHTTPServerData() data.websocket = ...` lines and ignore unrelated diagnostic noise.
Value: Implemented
Evidence: Archives/WebsocketDiagnostics/loglevel_probe.ps1 :: Parse-LogLevelUpdateText, Wait-ForDriverLevelAcks
- [Driver Log Status Sync > Reconnect behavior] On reconnect, the UI should enter a temporary reloading state, rebuild from first `LogLevels`, and then re-apply forced controls.
- [Driver Log Status Sync > Timeouts and retries] Per-change confirmation should use a three-to-five-second timeout with at most one automatic retry.
- [Driver Log Status Sync > Known constraint] Post-change `LogLevels` snapshots were not reliable in recent tests, so runtime sync should prefer `OnHTTPServerData` acknowledgements after baseline.
Value: Confirmed
Evidence: SMH_driver_log_status_sync_implementation.md :: Known Constraint

- [Transport Observations > Scope and authority] Transport observations are explicitly non-authoritative and do not define contracts, guarantees, or permissions.
- [Transport Observations > Methodology] Observations were derived from passive SHP monitoring, TCP stream inspection, and raw-to-decoded byte comparison.
- [Transport Observations > Methodology] Third-party tools are used only for observation and are not runtime dependencies of RTI Oracle.
- [Transport Observations > Transport characteristics] Observed diagnostics output is delivered over TCP with a default observed SHP port of 2113.
- [Transport Observations > Transport characteristics] Observations indicate SHP-to-client streaming after connect with no command channel evidence beyond log-level configuration behavior.
- [Transport Observations > Encoding and framing] Observed diagnostic payload text is UTF-16LE and uses explicit start and end framing markers.
- [Transport Observations > Encoding and framing] Observed framing delimiters are `0x01 0x00` for start and `0x03 0x00` for end.
- [Transport Observations > Stream behavior] The diagnostics feed must be parsed as a continuous byte stream because record boundaries do not align with TCP packets.
- [Transport Observations > Stream behavior] Partial records are expected and normal during stream-safe diagnostics parsing.
- [Transport Observations > Change rules] New findings may be appended with evidence, and contradictory findings must be recorded rather than erased.

- [TraceViewer Reconnect > Proven] TraceViewer connections were observed using TCP port 2113 on the SHP across multiple conversations.
Value: Confirmed
Evidence: oracle_reconnect.md :: Proven (from pcap) > P1
- [TraceViewer Reconnect > Proven] On reconnect attempts, the client sends a small ASCII `hello` probe toward the SHP.
Value: Confirmed
Evidence: oracle_reconnect.md :: Proven (from pcap) > P2
- [TraceViewer Reconnect > Proven] After reconnect, SHP responses decode as UTF-16LE log lines and continue as runtime stream output.
Value: Confirmed
Evidence: oracle_reconnect.md :: Proven (from pcap) > P3
- [TraceViewer Reconnect > Proven] Multiple short TCP sessions around reboot were observed and are consistent with reconnect behavior.
Value: Confirmed
Evidence: oracle_reconnect.md :: Proven (from pcap) > P4
- [TraceViewer Reconnect > Theory] A minimal reconnect loop likely opens TCP, sends `hello`, reads logs, and retries after disconnect.
- [TraceViewer Reconnect > Theory] Reconnect probe spacing is approximately three seconds in the captured sessions.
- [TraceViewer Reconnect > Guidance] A minimal third-party client procedure is to connect to `SHP_IP:2113`, send `hello`, decode UTF-16LE, and reconnect on closure.
- [TraceViewer Reconnect > Risks and unknowns] Unknowns include repeated-hello requirements, hidden control messages, and keepalive semantics beyond observed probes.

- [TraceViewer Capture Analysis > Log-level propagation proven] ID11 push payloads contained embedded debug configuration keys including `DebugLevel0` and `DebugTracetrue`.
Value: Confirmed
Evidence: TraceViewer_capture_analysis.md :: 1) Log-level flag propagation > 1.1 Project push stream contains explicit debug/log configuration keys
- [TraceViewer Capture Analysis > Log-level propagation proven] ID11 push payloads also contained text indicating routing behavior with `description="Routes LogInfo to Traceviewer"`.
Value: Confirmed
Evidence: TraceViewer_capture_analysis.md :: 1) Log-level flag propagation > 1.2 Project push contains explicit TraceViewer routing description
- [TraceViewer Capture Analysis > Log-level propagation limits] The capture did not prove a direct on-wire TraceViewer protocol message that enumerates driver log levels or filter acknowledgements.
- [TraceViewer Capture Analysis > Reconnect proven] Multiple sessions to `192.168.1.143:2113` showed repeated client `hello` probes and SHP UTF-16LE responses.
Value: Confirmed
Evidence: TraceViewer_capture_analysis.md :: 2) TraceViewer reconnect method > 2.1, 2.2, 2.3
- [TraceViewer Capture Analysis > Reconnect limits] The capture did not prove whether additional startup tokens beyond `hello` are required for a complete client implementation.
- [TraceViewer Capture Analysis > Appendix proven differences] Comparison captures showed `DebugTrace` flipping between true and false while `DebugLevel0` remained present in both pushes.
Value: Confirmed
Evidence: TraceViewer_capture_analysis.md :: Appendix > Proven differences in the ID11 push payload > A.1 and A.2

- [TODAY Diagnostics Plane Summary > Proven context] The analyzed reboot capture context included ID11 project push activity, SHP reboot, and post-reboot reconnection attempts.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A1
- [TODAY Diagnostics Plane Summary > Proven observations] The SHP served HTTP traffic on port 80 and included web UI-style static asset requests.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A2
- [TODAY Diagnostics Plane Summary > Proven observations] A TCP flow involving port 50339 was identified as a strong candidate for ID11 project or package push traffic.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A3
- [TODAY Diagnostics Plane Summary > Proven observations] During the reboot window, SYN traffic clustered with SHP source ports in the 50331 through 50343 range.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A4
- [TODAY Diagnostics Plane Summary > Proven limitation] Attribution of public IP destinations was explicitly marked as unprovable from IP addresses alone.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A5
- [TODAY Diagnostics Plane Summary > Decoder status] A working decoder exists for Entire Conversation captures, while Single Transport captures require a separate decoding approach.
Value: Confirmed
Evidence: TODAY_diagnostics_plane_summary_for_codex.md :: Part A - Proven > A6
- [TODAY Diagnostics Plane Summary > Decoder status] Current single-transport decoding still shows ellipsis truncation, indicating likely segment reconstruction loss across frames or lines.
- [TODAY Diagnostics Plane Summary > Methodology] The transport design should treat HTTP, raw TCP, and WebSocket as separate observation layers until evidence proves stronger coupling.
- [TODAY Diagnostics Plane Summary > Methodology] The redesigned transport approach should support multi-transport observation, raw replay persistence, and timestamp-based correlation.
- [TODAY Diagnostics Plane Summary > Theory] A two-plane diagnostics model may exist with TraceViewer-style raw TCP behavior and separate browser WebSocket or HTTP diagnostics behavior.
- [TODAY Diagnostics Plane Summary > Theory] Log-level control may use two mechanisms, with runtime browser controls separated from ID11 push-time TraceViewer configuration.
- [TODAY Diagnostics Plane Summary > Theory] Reboot-window reconnect attempts may represent TraceViewer recovery behavior but require stronger endpoint attribution evidence.
- [TODAY Diagnostics Plane Summary > Theory] Public IP traffic may represent RTI cloud or driver phone-home behavior and requires DNS or TLS metadata for proof.
- [TODAY Diagnostics Plane Summary > Theory] Diagnostics payloads may be mixed binary envelopes with UTF-16LE text segments interleaved inside stream data.
- [TODAY Diagnostics Plane Summary > Procedure] Recommended evidence capture should start before push, include full reboot, and retain at least two post-reboot minutes.
- [TODAY Diagnostics Plane Summary > Procedure] Capture artifacts should include `.pcapng` plus raw Follow TCP Stream exports for every candidate diagnostics port.
- [TODAY Diagnostics Plane Summary > Procedure] Controlled validation should time-stamp UI actions so packet evidence can be correlated to log-level changes.
