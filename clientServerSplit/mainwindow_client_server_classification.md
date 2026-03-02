# MainWindow Client/Server Classification

## Scope

This document classifies the current responsibilities inside `OracleByFPCLtd/MainWindow.xaml.cs` into three future-facing buckets:

- Must stay client
- Likely server later
- Portable shared domain

This is an architecture and extraction guide only. It does not change code.

## Baseline Observation

`MainWindow.xaml.cs` is currently a mixed-responsibility file at 5154 LOC.

It combines:

- WPF event wiring and UI state
- diagnostics connection orchestration
- filtering and find behavior
- log rendering and highlighting
- JSON and text parsing
- project data load orchestration
- export orchestration
- driver log level control
- settings load and persistence
- event log / status reporting
- reconnect handling

That makes it the highest-value file to decompose before deeper LOC reduction.

## Current Responsibility Groups

Based on method and dependency review, the file currently contains these major groups:

1. UI composition and event hookup
2. filter and find UI behavior
3. log display layout, zoom, and highlighting
4. local connection / reconnect orchestration
5. transport event handling
6. project file and additional info file selection
7. project-data initialization and reprocessing orchestration
8. export dialog and export request assembly
9. driver log level UI and command workflow
10. status output, event log writes, and failure display
11. parsing helpers for JSON, timestamps, filter text, and tagged messages
12. local model helpers (`DriverEntry`, comparer, UI sorting)

## Must Stay Client

These responsibilities should remain in the desktop client because they are directly tied to WPF, local operator workflow, or local device/session control.

### UI composition and event routing

This includes:

- `WirePanelHandlers`
- control event subscriptions
- direct use of WPF controls, windows, popups, calendars, and text boxes
- visual state changes such as button text, visibility, and brushes

Why it stays client:

- It is inseparable from the local desktop experience.
- A remote API should not own WPF interaction flow.

### Local display behavior

This includes:

- diagnostics zoom controls
- log textbox sizing and width measurement
- rich-text selection visibility
- highlight rendering for find results
- placeholder display logic

Representative methods:

- `ConfigureLogOutputBoxes`
- `InitializeDiagnosticsZoom`
- `ApplyDiagnosticsZoom`
- `AdjustProcessedDocumentWidth`
- `AdjustRawDocumentWidth`
- `AppendRunsWithHighlights`

Why it stays client:

- This is purely presentation logic.
- It must stay responsive even when no server is available.

### Local user workflows and dialogs

This includes:

- file open/save dialogs
- message boxes
- about window and profile window launch
- preview window launch

Representative methods:

- `UploadProject_Click`
- `UploadAdditionalInfo_Click`
- `DownloadLogsButton_Click`
- `DownloadAdditionalInfoTemplateMenuItem_Click`
- `AboutMenuItem_Click`
- `DriverProfilesMenuItem_Click`
- `ShowMessageOnUiThread`

Why it stays client:

- These are local operator interactions and shell concerns.

### Local diagnostics session orchestration

This includes:

- connect/disconnect button flow
- discovery trigger
- reconnect loop scheduling
- local transport selection
- local connection status updates

Representative methods:

- `CreateWebSocketTransport`
- `CreateTcpCaptureTransport`
- `DiscoverButton_Click`
- `ConnectButton_Click`
- `DisconnectButton_Click`
- `StartReconnectLoop`
- `ReconnectLoopAsync`
- `SetConnectionStatus`

Why it stays client:

- The user initiates and observes these actions locally.
- The client likely remains the direct operator console even after a server exists.

## Likely Server Later

These responsibilities are strong candidates for eventual server ownership because they represent centrally managed knowledge, remotely updateable behavior, or cross-installation intelligence.

### Driver profile intelligence and updateable rules

This includes:

- profile timestamp display dependencies
- profile-aware message shaping
- profile-driven name resolution and inventory interpretation
- logic tied to centrally updateable driver knowledge

Representative methods or responsibility edges:

- `GetProfileTimestampText`
- `ResolveProjectDriverName`
- driver profile catalog usage
- tagged driver interpretation that depends on profile knowledge

Why it is server-likely:

- You explicitly want remote driver profile updates.
- This is the kind of knowledge that benefits from one remotely managed source of truth.

### Unknown or incomplete driver intelligence

This includes:

- detection and normalization around unresolved / no-profile / incomplete-profile tags
- tagged message collection and report generation for unhandled driver outputs

Representative methods:

- `RecordTaggedMessages`
- `RecordTaggedMessage`
- `ExtractTaggedDriverName`
- `HasDiagnosticTag`
- `ExtractDiagnosticTags`
- `NormalizeTaggedLine`
- `BuildTaggedMessagesReport`

Why it is server-likely:

- You want unknown driver profile data automatically.
- Central collection and analysis of unknown patterns is better handled remotely.

### Remotely improvable feature logic

This includes logic that may initially remain local but should be designed so it can later be backed by a service:

- profile-derived formatting behavior
- rule updates for message interpretation
- centrally distributed improvements to mapping or interpretation logic

Why it is server-likely:

- You want feature improvements to be pushed remotely without relying on a new desktop release for every update.

## Portable Shared Domain

These responsibilities are the best immediate extraction targets. They are deterministic, UI-free, and can run locally now while remaining movable to a future API later.

### Filter parsing and line matching

Representative methods:

- `TryParseKeywordFilter`
- `TryParseDateRange`
- `TryParseDateTime`
- `LineMatchesFilter`
- `LineMatchesKeywordFilter`
- `TryExtractTimestamp`
- `BuildFilterLogDetails`

Why it is portable:

- This logic is data-in, result-out.
- It has no WPF dependency.
- It can remain local for responsiveness or be reused server-side later.

### Find/search state mechanics

Representative methods:

- `UpdateFindState`
- `ResetFindState`
- `UpdateMatchLabel`
- `SplitLines`

Why it is portable:

- The UI presentation stays client-side, but the matching/index logic itself is reusable and deterministic.

### Structured message parsing

Representative methods:

- `FormatMessage`
- `TryHandleStructuredMessage`
- `TryHandleStructuredRoot`
- `TryParseJsonRoot`
- `TryParseJsonRootFromText`

Why it is portable:

- This is message interpretation logic, not UI logic.
- It should be isolated from the window and callable from either local or server execution.

### Log-level command state modeling

Representative methods:

- `ParseLogLevel`
- `BuildLogLevelSuccessContext`
- `GetLogLevelAckCandidateKeys`
- `BuildLogLevelAckKey`
- `ParseDriverId`
- `JoinContextValues`
- `BuildDriverInventoryValue`
- `BuildDiagnosticsDriverCandidateValue`
- `SanitizeContextValue`

Why it is portable:

- These are deterministic helpers around command semantics and reporting.
- The UI triggering stays client-side, but the command/result model should be shared and transport-agnostic.

### Status and event message shaping

Representative methods:

- `BuildStatusMessage`
- `TryParseStatusLine`
- `NormalizeStatusLevel`
- `BuildStatusText`
- `MapStatusSeverity`
- `ShouldSuppressStatusInUi`

Why it is portable:

- The presentation target is client-side, but status formatting and classification are shared logic.

### Project-data processing orchestration seams

Representative methods:

- `InitializeProcessing`
- `InitializeProcessingAsync`
- `LoadAdditionalData`
- `UpdateAdditionalInfoTemplateAvailability`
- `BuildAdditionalInfoTemplateFileName`

Why it is portable:

- The user-triggered file selection stays client-side.
- The deterministic transformation and processing steps should move into reusable services that could later run in a server process as well.

## Mixed Areas That Must Be Split Before Final Placement

These parts of `MainWindow.xaml.cs` currently mix multiple buckets and should not remain in one file if the architecture is going to support the future server-backed model.

### Transport event handling

Representative methods:

- `Transport_RawMessageReceived`
- `Transport_TransportInfo`
- `Transport_TransportError`
- `Transport_OperationStateChanged`
- `HandleTransportFailure`

Current mix:

- client concern: dispatch to UI and update view state
- portable concern: parse and classify incoming payloads
- future server concern: some interpretation logic may become remotely managed

Required split:

- keep UI updates in client
- move message interpretation and operation-state translation into portable services

### Log-level workflows

Representative methods:

- `HandleLogLevels`
- `UpdateDriverFromLogLevel`
- `ApplyLogLevelCommandWithAckAsync`
- `WaitForLogLevelAckAsync`
- `TryResolvePendingLogLevelCommand`
- batch log-level button handlers

Current mix:

- client concern: button clicks, visible driver list, local status display
- portable concern: ack correlation, command state, retry/result logic
- future server concern: centrally managed policy for protected or recommended levels may evolve later

Required split:

- keep command initiation and UI state local
- move ack matching, command lifecycle, and status modeling into a portable coordinator

### Project-data load flow

Representative methods:

- `HandleProjectSelected`
- `LoadProjectDataForProcessingAsync`
- `LoadProjectDataForProcessingCoreAsync`
- `LoadProjectFromPath`

Current mix:

- client concern: file selection and progress display
- portable concern: deterministic load / process sequencing
- future server concern: knowledge-based mapping behavior may later be remotely improved

Required split:

- keep path selection and progress overlay local
- move orchestration and transformation logic into reusable services

## Proposed Extraction Boundaries

These are the best boundaries for future refactor work while preserving current behavior.

1. `MainWindowUiCoordinator`
- Owns event wiring, control state, dialog launch, and visual updates only.

2. `DiagnosticsSessionController`
- Owns connect/disconnect/reconnect orchestration and transport lifecycle without WPF dependencies.
- Client window calls it and renders the results.

3. `LogFilterService`
- Owns filter parsing, date-range parsing, timestamp extraction, and line-matching.

4. `LogSearchService`
- Owns find-state calculation and match navigation state, excluding rich-text rendering.

5. `StructuredMessageParser`
- Owns JSON/root parsing and structured message normalization.

6. `LogLevelCommandCoordinator`
- Owns pending command tracking, ack correlation, retry decisions, and command result shaping.

7. `TaggedDiagnosticsAnalyzer`
- Owns unresolved/unknown tag detection, normalization, and report assembly.
- This should be designed as portable now and may become server-backed later.

8. `ProjectProcessingCoordinator`
- Owns project-data initialization workflow, additional-info load sequencing, and reprocessing orchestration.
- UI keeps only file selection and progress display.

9. `StatusMessageFormatter`
- Owns status line formatting, parsing, severity mapping, and shared status text rules.

## Recommended Refactor Order

To reduce LOC while preparing for the future split, the extraction order should be:

1. Extract `LogFilterService` and `StatusMessageFormatter`
- Lowest architectural risk
- High amount of deterministic helper logic

2. Extract `StructuredMessageParser`
- Removes parsing logic from the window
- Creates a server-portable seam immediately

3. Extract `LogLevelCommandCoordinator`
- High LOC and high complexity
- Important future boundary for remote-aware workflows

4. Extract `ProjectProcessingCoordinator`
- Reduces workflow sprawl
- Prepares project processing for future backend-backed improvements

5. Extract `TaggedDiagnosticsAnalyzer`
- Aligns with the future automatic unknown-profile data path

6. Reduce `MainWindow` to a thin client coordinator
- After the portable services exist, the remaining code-behind can shrink toward client-only concerns

## Risks To Watch

The refactor should preserve:

- no behavioral regression in filtering, log display, and log-level control
- no slower UI response during log append, search, or rendering
- no loss of explicit failure/status reporting
- no change to local session control semantics

Specific risk areas:

- repeated `Dispatcher.Invoke` and `Dispatcher.BeginInvoke` use means extraction must not break thread-affinity assumptions
- rich-text rendering and highlight code must stay efficient
- reconnect and log-level ack timing logic must remain deterministic
- project reprocessing progress updates must preserve current operator feedback

## Bottom Line

Yes, the future server-backed path should change the refactor shape now.

For `MainWindow.xaml.cs`, the correct move is not just to split it into smaller files. The correct move is to:

- keep only client UI orchestration in `MainWindow`
- extract deterministic logic into portable shared services
- isolate updateable driver/profile intelligence so it can become server-backed later

That path reduces LOC now and avoids a second structural rewrite when the API-backed system is introduced.
