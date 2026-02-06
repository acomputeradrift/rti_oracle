# Modularization Plan

## Diagnostics Transport (Approved)

Goal: isolate connection lifecycle, message transfer, and control commands into separate modules so work on one area does not impact others.

### Module Layout
- `DiagnosticsTransport/Connection/`
  - `IConnectionManager` (connect, disconnect, discover)
  - `WebSocketConnectionManager`
  - `TcpCaptureConnectionManager`
- `DiagnosticsTransport/Messaging/`
  - `IMessageReceiver` (raw receive loop)
  - `IMessageSender` (send payloads)
  - `WebSocketMessageReceiver`
  - `WebSocketMessageSender`
- `DiagnosticsTransport/Controls/`
  - `ILogLevelController`
  - `ISysvarSubscriptionController`
  - `LogLevelController`
  - `SysvarSubscriptionController`
- `DiagnosticsTransportFacade`
  - Single entry point used by UI; composes the above.

## UI (Approved)

Goal: separate layout from UI state/logic so changes stay localized.

### Module Layout
- `UI/Views/`
  - `MainWindow.xaml`
  - `ProjectDataPreviewWindow.xaml`
- `UI/ViewModels/`
  - `MainWindowViewModel`
  - `ProjectDataPreviewViewModel`
- `UI/Controllers/` (optional, if staying code-behind initially)
  - `DiagnosticsUiController`
  - `ProjectDataPreviewController`

### Boundaries
- Views contain layout only.
- ViewModels/Controllers own UI state and call into services.

### View Breakdown (Approved)
- Shell
  - `MainWindow` (layout + navigation only)
- Panels
  - `ConnectionPanel`
  - `ProjectDataPanel`
  - `DriverLogLevelsPanel`
  - `DiagnosticsPanel`
  - `RawOutputPanel`
  - `ProcessedOutputPanel`
- Shared Controls
  - `LogOutputView` (reusable RichTextBox host for consistent fonts/highlighting)
  - `FindBar`
  - `FilterBar`
  - `StatusBar`

### UI Consistency Goal
- Reusability and consistency across UI areas (fonts, highlighting, and layout behavior).

### DiagnosticsPanel Composition
- Contains `RawOutputPanel` and `ProcessedOutputPanel`
- Includes `FilterBar`
- Includes `Clear Logs` and `Download Logs` actions

## Project Data (Approved)

Goal: provide a single saved data bundle that separates System vs Drivers data, keeps extraction honest, and enables consistent display and mapping.

### Module Layout
- `ProjectData/Models/`
  - `ProjectDataBundle`
  - `System/` (Pages, Rooms, Ports, Macros, System Variables)
  - `Drivers/` (Driver configs + driver variables)
  - `Additional/` (spreadsheet mappings)
- `ProjectData/Extractors/`
  - `ApexSystemExtractor`
  - `ApexDriverExtractor`
  - `AdditionalDataExtractor`
- `ProjectData/Cache/`
  - `ProjectDataCacheStore`
- `ProjectData/Export/`
  - `ProjectDataExporter` (renamed from `DiagnosticsMappingExporter`)
- `ProjectData/Display/`
  - `DriverConfigDisplayTransformer` (UI-only refinement)
- `ProjectData/Preview/`
  - `ProjectDataPreviewViewModel`

### Mapping Ownership
- Mapping/enrichment logic lives in the Processing Engine:
  - `SystemMappingService`
  - `DriverMappingService`
  - `AdditionalDataMappingService`

## Processing Engine (Approved)

Goal: parse raw logs, apply mappings, and format/colorize processed output.

### Module Layout
- `ProcessingEngine/Models/`
  - `DiagnosticEvent` (normalized event)
  - `ProcessedLine` (final text output)
  - `ProcessedLineCategory` (color/style category)
- `ProcessingEngine/Parsing/`
  - `RawLogParser` (raw line → `DiagnosticEvent`)
- `ProcessingEngine/Mapping/`
  - `SystemMappingService`
  - `DriverMappingService`
  - `AdditionalDataMappingService`
- `ProcessingEngine/Formatting/`
  - `ProcessedLineFormatter` (builds output text)
  - `ProcessedLineClassifier` (assigns category/colors)
- `ProcessingEngine/Engine/`
  - `ProcessingEngine` (orchestrates parse → map → format)
  - `ProcessingEngineRunner`

### Processed Line Breakdown
- Mapping: enrich raw events with System/Driver/Additional data.
- Colorization: classify enriched lines for consistent styling.

## Driver Profiles (Approved)

Goal: identify driver types and supply configuration context without embedding driver-specific command parsing.

### Module Layout
- `DriverProfiles/Models/`
  - `DriverProfile`
  - `DriverProfileBundle`
  - `DriverConfigSnapshot`
- `DriverProfiles/Catalog/`
  - `DriverProfileCatalog`
  - `DriverProfileRegistry`
- `DriverProfiles/Matching/`
  - `DriverProfileMatcher`
- `DriverProfiles/Integration/`
  - `DriverProfileModule`
- `DriverProfiles/Services/`
  - `DriverConfigResolver`

### Boundary
- Driver-specific command parsing belongs in `ProcessingEngine/Mapping/DriverMappingService`.

## ExportProcessedLogs (Approved)

Goal: export processed logs (with applied filters) to a PDF that includes header metadata and the log body.

### Module Layout
- `ExportProcessedLogs/Models/`
  - `ExportRequest` (processed lines + filters + metadata)
  - `ExportMetadata` (Date/Time, Apex File Name, Additional Data Name)
  - `FilterSummary` (keywords, date range)
- `ExportProcessedLogs/Services/`
  - `ProcessedLogsExportService`
- `ExportProcessedLogs/Builders/`
  - `HeaderBuilder`
  - `LogSectionBuilder`
- `ExportProcessedLogs/Rendering/`
  - `PdfRenderer`
- `ExportProcessedLogs/IO/`
  - `ExportFileWriter`

### Notes
- Project name removed; use the `.apex` filename.
- PDF rendering will require a new dependency (approval needed).

## Settings (Approved)

Goal: isolate settings persistence and recent history behavior.

### Module Layout
- `Settings/Models/`
  - `OracleSettings`
  - `RecentProjectEntry`
- `Settings/Storage/`
  - `OracleSettingsStore` (load/save, file path resolution)
- `Settings/Services/`
  - `RecentProjectService`
  - `RecentIpService`
  - `AdditionalInfoService`
