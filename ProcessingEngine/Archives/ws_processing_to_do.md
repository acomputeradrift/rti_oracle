# WebSocket Processing Plan (Rough Outline)

## Purpose
- Define how the Processing Engine will consume the WebSocket raw feed from an SHP, classify log lines, and emit color-coded, project-specific, human-readable output.

## Scope (Today)
- WebSocket raw feed only.
- TCP feed noted for later, but not designed in this document.

## High-Level Workflow
- Connect to SHP via WebSocket and ingest raw log lines.
- Pass each line through a filtering mechanism to determine formatting category.
- When a category requires project-specific mapping, reference Driver Profiles externally.
- Apply transformation and mapping rules.
- Emit the formatted line to the processed output window with correct color coding.

## Color Coding Rules (Processed Output Window)
- Background: black
- Connect: green
- Disconnect: red
- Driver Command: light grey
- Macro Start and Macro End: orange
- Driver Event: yellow
- All else: white

## Filtering and Classification (WebSocket)
- Identify connect and disconnect events.
- Identify driver commands.
- Identify macro start and macro end markers.
- Identify driver events.
- Default category for any unmatched line.

## Driver Profile Usage (External Reference)
- Only consult Driver Profiles when a line is in a category that requires project-specific mapping.
- Keep `.APEX` and project spreadsheets read-only inputs.
- If a mapping is missing, emit the raw identifier and mark it explicitly as unresolved.
- Preload only required `.APEX` data at upload time into in-memory maps for fast lookups.

## Preload Output Shape (Proposed Contract)
- `pageIndexMap`: key `deviceId|pageIndex` -> `pageName`
- `sysVarRefMap`: key `SYSVARREF:{GUID}#NN@SysVar` -> `{driverDeviceId, variableName, deviceId}`
- `driverConfigMap`: key `driverDeviceId` -> `{deviceName, config: {key: value}}` (filtered, no Debug*)
- `meta`: `{projectId, generatedAt, apexPathHash, schemaVersion}`

## Transformation and Mapping
- Normalize raw line format as needed for consistent parsing.
- Apply category-specific transforms before mapping.
- Apply mapping rules to produce human-readable output.
- Ensure output retains the original line if mapping fails.
- Ensure every processed line includes a direct reference to its raw log line number.

## Integration With Application Code (Planned)
- Add a WebSocket ingest stage that feeds a shared processing pipeline.
- Keep classification logic isolated for testability and future TCP reuse.
- Keep mapping logic isolated so Driver Profile changes do not affect ingestion.
- Route processed output to the UI layer for color rendering.
- Persist raw line numbering so the UI can display the processed-to-raw linkage.

## Open Questions
- Exact line patterns for each category in the WebSocket feed.
- Required Driver Profile identifiers and how they map to log tokens.
- Where in the codebase the shared processing pipeline should live.
