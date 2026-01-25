# Processing Engine Plan

## Scope Confirmation
- No code changes.
- No edits to existing documents.
- Plan-only deliverable.

## References Reviewed
- `ws_processing_to_do.md`
- `SHPDiagnosticsViewer/WebSocketMessageFormatter.cs`
- `SHPDiagnosticsViewer/MainWindow.xaml.cs`
- `SHPDiagnosticsViewer/DiagnosticsTransport/LegacyWebSocketDiagnosticsTransport.cs`
- `SHPDiagnosticsViewer/ProjectData/ApexDiscoveryPreloadExtractor.cs`
- `SHPDiagnosticsViewer/DriverProfiles/DriverProfileModule.cs`
- `SHPDiagnosticsViewer/DriverProfiles/RtiInternalProfile.cs`

## Plan (For Approval)
1. Specify raw log ingestion rules
   - Mirror WebSocket message parsing behavior from `WebSocketMessageFormatter` and `MainWindow`.
   - Preserve raw line numbers and the message timestamp as shown in current output.
   - Confirm date rollover behavior when time decreases.

2. Define classification rules
   - Establish the initial rule set for log categories.
   - Only active profile trigger for now: detect “page #” pattern and classify as RTI Internal.
   - All unmatched lines default to generic formatting.

3. Define RTI Internal mapping contract
   - Use `ApexDiscoveryPreloadResult.PageIndexMap` with key `deviceId|pageIndex`.
   - On missing mapping, emit unresolved marker and preserve raw identifier.

4. Specify pipeline stages
   - Normalize → classify → select profile → map → format → emit.
   - Output always includes original line number and timestamp.

5. Define color coding output rules
   - Connect: green.
   - Disconnect: red.
   - Driver Command: light grey.
   - Macro Start and Macro End: orange.
   - Driver Event: yellow.
   - Default: white on black background.

6. Define test cases only (TEST_FIRST)
   - Page pattern detection.
   - Mapping success and unresolved fallback.
   - Timestamp preservation and rollover.
   - Stable output formatting (no unintended changes).

7. Seek explicit approval before any implementation
   - No code is written until tests are defined and approval is granted.
