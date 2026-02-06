# Modularization Implementation Guide

Purpose: Provide a practical, step-by-step implementation path for the modularization plan, using current code structure and responsibilities. This is a refactor-only guide; functional behavior should remain unchanged per step.

## Guiding Rules
- One logical move per step; compile after each step.
- No behavior changes during structure moves.
- Keep APIs stable until the last step of each module migration.
- Add simple adapters to bridge old call sites to new modules.

## Phase 0: Inventory (Read-Only)
Identify current anchors and responsibilities:
- UI: `SHPDiagnosticsViewer/MainWindow.xaml`, `SHPDiagnosticsViewer/MainWindow.xaml.cs`, `SHPDiagnosticsViewer/ProjectDataPreviewWindow.xaml`, `SHPDiagnosticsViewer/ProjectDataPreviewWindow.xaml.cs`
- Transport: `SHPDiagnosticsViewer/DiagnosticsTransport/*`
- Processing: `SHPDiagnosticsViewer/ProcessingEngine/*`
- Project Data: `SHPDiagnosticsViewer/ProjectData/*`
- Driver Profiles: `SHPDiagnosticsViewer/DriverProfiles/*`
- Settings: `SHPDiagnosticsViewer/Settings/*`
- Formatting: `SHPDiagnosticsViewer/WebSocketMessageFormatter.cs`

## Phase 0.5: App Rename (Required)
Goal: remove `SHPDiagnosticsViewer` branding from user-visible strings and project identifiers.
1) Rename solution/project/folder identifiers from `SHPDiagnosticsViewer` to `OracleByFPCLtd`.
2) Update application window title to `Oracle by FP&C Ltd`.
3) Update settings/cache paths to use `Oracle by FP&C Ltd`.
4) Update docs and references to avoid `SHPDiagnosticsViewer` naming.
5) Validate build and tests after rename.

## Phase 1: Diagnostics Transport Split
Goal: isolate Connection, Messaging, Controls without changing behavior.
1) Create new folders under `OracleByFPCLtd/DiagnosticsTransport/`:
   - `Connection/`, `Messaging/`, `Controls/`
2) Move logic from `LegacyWebSocketDiagnosticsTransport` into:
   - Connection: socket lifecycle, connect/disconnect, discovery methods.
   - Messaging: receive loop + raw message dispatch.
   - Controls: send log level + sysvar subscribe commands.
3) Add `DiagnosticsTransportFacade` that implements `IDiagnosticsTransport` and composes the new parts.
4) Keep `IDiagnosticsTransport` stable; update `MainWindow.xaml.cs` to use the facade.
5) Leave `TcpCaptureDiagnosticsTransport` intact (wrap or adapt only if needed).

## Phase 2: UI Decomposition
Goal: isolate panels and shared controls.
1) Extract XAML into user controls:
   - `ConnectionPanel`, `ProjectDataPanel`, `DriverLogLevelsPanel`,
     `DiagnosticsPanel`, `RawOutputPanel`, `ProcessedOutputPanel`.
2) Extract shared controls:
   - `LogOutputView` (wraps `RichTextBox` + width/scroll logic),
     `FindBar`, `FilterBar`, `StatusBar`.
3) Move Raw/Processed log rendering code into `LogOutputView`.
4) Keep code-behind in place initially (controller-like), then
   introduce `DiagnosticsUiController` if needed to reduce MainWindow size.

## Phase 3: Project Data Repackaging
Goal: single saveable bundle with System vs Drivers vs Additional data.
1) Introduce `ProjectData/Models/ProjectDataBundle`:
   - System data (Pages/Rooms/Ports/Macros/System Variables)
   - Drivers data (Driver config + driver variables)
   - Additional data (spreadsheet mappings)
2) Add adapter in `ProjectDataExtractor` to return the bundle without changing
   existing call sites (e.g., map old `ProjectDataExtractionResult` into bundle).
3) Keep extraction logic where it is, but move into:
   - `ProjectData/Extractors/ApexSystemExtractor`
   - `ProjectData/Extractors/ApexDriverExtractor`
   - `ProjectData/Extractors/AdditionalDataExtractor`
4) Update cache to store the bundle (keep current cache format compatible).
5) Rename `DiagnosticsMappingExporter` to `ProjectDataExporter` in a single step
   once references are fully centralized.

## Phase 4: Processing Engine Ownership
Goal: mapping and enrichment live in Processing Engine.
1) Create `ProcessingEngine/Parsing/RawLogParser` to normalize raw log events.
2) Create `ProcessingEngine/Mapping/` services:
   - `SystemMappingService`
   - `DriverMappingService`
   - `AdditionalDataMappingService`
3) Update `ProcessingEngine` to orchestrate:
   raw line -> parse -> map -> format -> classify.
4) Keep `ProcessedLineClassifier` as the colorization boundary.
5) Ensure all mapping uses only lookups from `ProjectDataBundle`.

## Phase 5: Driver Profiles Cleanup
Goal: keep profile logic separate from command parsing.
1) Move models to `DriverProfiles/Models/`.
2) Keep matching logic in `DriverProfiles/Matching/`.
3) Ensure `DriverProfileModule` only integrates profiles with config data;
   driver command parsing stays in `ProcessingEngine/Mapping/DriverMappingService`.

## Phase 6: Settings Isolation
Goal: clean Settings module with services.
1) Move models to `Settings/Models/`.
2) Keep `OracleSettingsStore` in `Settings/Storage/`.
3) Add `Settings/Services/` wrappers if you want to remove settings logic
   from `MainWindow.xaml.cs`.

## Phase 7: ExportProcessedLogs (New Module)
Goal: export processed logs to PDF with header metadata.
1) Add `ExportProcessedLogs/Models` for `ExportRequest`, `ExportMetadata`, `FilterSummary`.
2) Add builder services for header + body.
3) Introduce a `PdfRenderer` (new dependency, needs approval).
4) Connect export action to the UI once module is complete.

## Refactor Sequencing Recommendation
1) Diagnostics Transport (lowest UI impact).
2) Project Data bundle (core data spine).
3) Processing Engine mapping services.
4) UI decomposition (panels + shared controls).
5) Driver Profiles cleanup.
6) Settings isolation.
7) ExportProcessedLogs module.

## Mapping Note (Driver Commands + Additional Data)
Driver-specific command parsing (e.g., C-Bus state/group/app rules) should be
implemented in `ProcessingEngine/Mapping/DriverMappingService` using:
- Driver config data from `ProjectDataBundle.Drivers`.
- Additional data mappings from `ProjectDataBundle.Additional`.

## Validation Checklist (Per Phase)
- Build succeeds.
- Existing tests pass.
- UI behavior unchanged unless explicitly part of the phase.
- Data outputs remain deterministic (no inference).
