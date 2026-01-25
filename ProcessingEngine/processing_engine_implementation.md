# Processing Engine Implementation Guide

## Purpose
- Describe how to implement the ProcessingEngine by referencing existing code and patterns.
- No new behavior beyond the approved plan; no changes to unrelated features.

## References (Source of Truth)
- `processing_engine_plan.md`
- `ws_processing_to_do.md`
- `SHPDiagnosticsViewer/WebSocketMessageFormatter.cs`
- `SHPDiagnosticsViewer/MainWindow.xaml.cs`
- `SHPDiagnosticsViewer/DiagnosticsTransport/LegacyWebSocketDiagnosticsTransport.cs`
- `SHPDiagnosticsViewer/ProjectData/ApexDiscoveryPreloadExtractor.cs`
- `SHPDiagnosticsViewer/DriverProfiles/DriverProfileModule.cs`
- `SHPDiagnosticsViewer/DriverProfiles/RtiInternalProfile.cs`

## Hard Safety Boundary (Must Preserve)
- Oracle may control SHP output only via Driver Log Level settings.
- No control features beyond diagnostic verbosity.
- `.APEX` and project spreadsheets are read-only inputs.

## Implementation Outline (Code-Linked)
1. Ingestion and timestamp normalization
   - Use the logic in `WebSocketMessageFormatter.Format` and `FormatMessageLog` as the reference for:
     - JSON `messageType` parsing.
     - Time parsing formats and date rollover.
     - The `[yyyy-MM-dd hh:mm:ss.fff]` timestamp shape.
   - Preserve the original raw line number as shown in `MainWindow.xaml.cs` (`_rawLineNumber`).

2. Classification
   - Mirror the existing classification approach from `ws_processing_to_do.md`.
   - For now, the only profile trigger is the “page #” pattern; map it as RTI Internal.
   - All unmatched lines remain generic.

3. Preload data usage
   - Use `ApexDiscoveryPreloadExtractor.Extract` to obtain:
     - `PageIndexMap` for page name mapping.
     - `SysVarRefMap` and `DriverConfigMap` for later profile-specific mappings.
   - Do not mutate the preload data; treat it as read-only.

4. Driver profile scope integration
   - Use `DriverProfileModule.Integrate` to determine which drivers match which profiles.
   - For RTI Internal, only apply the page mapping rule for “page #” entries.
   - Driver profile formatting rules are hand-written later per profile.

5. Mapping and formatting
   - Replace the page number with the page name using:
     - Key: `deviceId|pageIndex` into `PageIndexMap`.
   - If no mapping exists:
     - Preserve the raw identifier.
     - Mark the output explicitly as unresolved (no guessing).
   - Keep output readable and consistent with the existing log line format.

6. Output color coding
   - Follow `ws_processing_to_do.md` exactly:
     - Connect: green.
     - Disconnect: red.
     - Driver Command: light grey.
     - Macro Start and Macro End: orange.
     - Driver Event: yellow.
     - Default: white on black background.

## Required Tests (Before Any Implementation)
- Page pattern detection and RTI Internal selection.
- Page mapping success and unresolved fallback behavior.
- Timestamp preservation and date rollover.
- Output formatting stability (line number + timestamp retained).

## Explicit Approval Gate
- Tests must be defined first.
- Implementation requires explicit approval after tests.
