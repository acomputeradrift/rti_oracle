- `Processing Engine Plan > Scope Confirmation:` This step allows no code changes and no edits to existing documents.
- `Processing Engine Plan > Scope Confirmation:` The required deliverable for this step is a plan-only output.
- `Processing Engine Plan > Plan:` Raw log ingestion must mirror current WebSocket parsing behavior in `WebSocketMessageFormatter` and `MainWindow`.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\MainWindow.xaml.cs :: MainWindow.FormatMessage`; `..\OracleByFPCLtd\WebSocketMessageFormatter.cs :: WebSocketMessageFormatter.Format`
- `Processing Engine Plan > Plan:` The processing output must preserve each raw line number and message timestamp.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\MainWindow.xaml.cs :: MainWindow.Transport_RawMessageReceived`; `..\OracleByFPCLtd\WebSocketMessageFormatter.cs :: WebSocketMessageFormatter.FormatMessageLog`; `..\OracleByFPCLtd.Tests\WebSocketMessageFormatterTests.cs :: WebSocketMessageFormatterTests.MessageLogLinesAreMarkedForNumberingWithDatedTimestamp`
- `Processing Engine Plan > Plan:` The engine must confirm and preserve date rollover behavior when parsed time values decrease.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\WebSocketMessageFormatter.cs :: WebSocketMessageFormatter.FormatMessageLog`; `..\OracleByFPCLtd.Tests\WebSocketMessageFormatterTests.cs :: WebSocketMessageFormatterTests.MessageLogDateRollsOverAtMidnight`; `..\OracleByFPCLtd.Tests\WebSocketMessageFormatterTests.cs :: WebSocketMessageFormatterTests.MessageLogDateDoesNotAdvanceForOutOfOrderTimes`
- `Processing Engine Plan > Plan:` Classification must use an initial rule set with unmatched lines defaulting to generic formatting.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\ProcessingEngine\ProcessedLineClassifier.cs :: ProcessedLineClassifier.DetermineCategory`; `..\OracleByFPCLtd.Tests\ProcessedLineClassifierTests.cs :: ProcessedLineClassifierTests.ClassifiesLines`
- `Processing Engine Plan > Plan:` The only active profile trigger currently is the `page #` pattern classified as RTI Internal.
- `Processing Engine Plan > Plan:` RTI Internal page mapping must use `ApexDiscoveryPreloadResult.PageIndexMap` keyed by `deviceId|pageIndex`.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\ProcessingEngine\Mapping\SystemMappingService.cs :: SystemMappingService.Map`; `..\OracleByFPCLtd.Tests\ProcessingEngineMappingTests.cs :: ProcessingEngineMappingTests.SystemMappingServiceMapsPageName`
- `Processing Engine Plan > Plan:` Missing mappings must preserve the raw identifier and add an explicit unresolved marker.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\ProcessingEngine\Mapping\SystemMappingService.cs :: SystemMappingService.Map`; `..\OracleByFPCLtd.Tests\ProcessingEngineMappingTests.cs :: ProcessingEngineMappingTests.SystemMappingServiceMarksUnresolvedMappings`; `..\OracleByFPCLtd.Tests\ProcessingEngineTests.cs :: ProcessingEngineTests.UnresolvedPageMappingIsMarked`
- `Processing Engine Plan > Plan:` The processing pipeline sequence is normalize, classify, select profile, map, format, and emit.
- `WebSocket Processing Plan > Color Coding Rules:` Connect events must render in green in the processed output window.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\ProcessingEngine\ProcessedLineClassifier.cs :: ProcessedLineClassifier.GetBrush`; `..\OracleByFPCLtd.Tests\ProcessedLineClassifierColorTests.cs :: ProcessedLineClassifierColorTests.CategoriesUseBrandColors`
- `WebSocket Processing Plan > Color Coding Rules:` Disconnect events must render in red in the processed output window.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\ProcessingEngine\ProcessedLineClassifier.cs :: ProcessedLineClassifier.GetBrush`; `..\OracleByFPCLtd.Tests\ProcessedLineClassifierColorTests.cs :: ProcessedLineClassifierColorTests.CategoriesUseBrandColors`
- `WebSocket Processing Plan > Color Coding Rules:` Driver Command events must render in light grey in the processed output window.
- `WebSocket Processing Plan > Color Coding Rules:` Macro Start and Macro End events must render in orange in the processed output window.
- `WebSocket Processing Plan > Color Coding Rules:` Driver Event events must render in yellow in the processed output window.
- `WebSocket Processing Plan > Color Coding Rules:` Unmatched events must render in white on a black background in the processed output window.
- `WebSocket Processing Plan > Driver Profile Usage:` Driver Profiles are consulted only for categories that require project-specific mapping.
- `WebSocket Processing Plan > Driver Profile Usage:` The `.APEX` file and project spreadsheets are read-only inputs.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\ProjectData\ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.Extract`; `..\OracleByFPCLtd\ProjectData\Extractors\AdditionalDataExtractor.cs :: AdditionalDataExtractor.Extract`
- `WebSocket Processing Plan > Driver Profile Usage:` Required `.APEX` data must be preloaded at upload time into in-memory maps for fast lookups.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\MainWindow.xaml.cs :: MainWindow.UploadProject_Click`; `..\OracleByFPCLtd\ProjectData\ProjectDataExtraction.cs :: ProjectDataExtraction.Extract`; `..\OracleByFPCLtd\MainWindow.xaml.cs :: MainWindow.InitializeProcessing`
- `WebSocket Processing Plan > Preload Output Shape:` The preload contract defines `pageIndexMap` as `deviceId|pageIndex` to `pageName`.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\ProjectData\ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadResult.PageIndexMap`; `..\OracleByFPCLtd.Tests\ApexDiscoveryPreloadTests.cs :: ApexDiscoveryPreloadTests.PageIndexMapIncludesDevicePageNames`
- `WebSocket Processing Plan > Preload Output Shape:` The preload contract defines `sysVarRefMap` as `SYSVARREF:{GUID}#NN@SysVar` to `{driverDeviceId, variableName, deviceId}`.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\ProjectData\ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadResult.SysVarRefMap`; `..\OracleByFPCLtd.Tests\ApexDiscoveryPreloadTests.cs :: ApexDiscoveryPreloadTests.SysVarRefMapResolvesDriverAndVariableName`
- `WebSocket Processing Plan > Preload Output Shape:` The preload contract defines `driverConfigMap` as `driverDeviceId` to filtered config data with Debug keys removed.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\ProjectData\ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadResult.DriverConfigMap`; `..\OracleByFPCLtd\ProjectData\ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadDriverConfigMap`; `..\OracleByFPCLtd.Tests\ApexDiscoveryPreloadTests.cs :: ApexDiscoveryPreloadTests.DriverConfigMapExcludesDebugKeys`
- `WebSocket Processing Plan > Preload Output Shape:` The preload contract includes metadata fields for project ID, generation time, path hash, and schema version.
- `WebSocket Processing Plan > Transformation and Mapping:` Raw lines must be normalized as needed to support consistent parsing.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\WebSocketMessageFormatter.cs :: WebSocketMessageFormatter.FormatMessageLog`; `..\OracleByFPCLtd\ProcessingEngine\Parsing\RawLogParser.cs :: RawLogParser.TryParseNumberedLine`
- `WebSocket Processing Plan > Transformation and Mapping:` Category-specific transforms must run before project mapping is applied.
- `WebSocket Processing Plan > Transformation and Mapping:` Each processed line must keep a direct reference to its original raw line number.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\ProcessingEngine\Mapping\SystemMappingService.cs :: SystemMappingService.Map`; `..\OracleByFPCLtd\ProcessingEngine\Mapping\DriverMappingService.cs :: DriverMappingService.Map`; `..\OracleByFPCLtd.Tests\ProcessingEngineTests.cs :: ProcessingEngineTests.MapsPageNumberToPageNameWithTimestamp`
- `WebSocket Processing Plan > Integration With Application Code:` Classification logic must stay isolated for testability and future TCP reuse.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\ProcessingEngine\ProcessedLineClassifier.cs :: ProcessedLineClassifier`; `..\OracleByFPCLtd.Tests\ProcessedLineClassifierTests.cs :: ProcessedLineClassifierTests.ClassifiesLines`
- `WebSocket Processing Plan > Integration With Application Code:` Mapping logic must stay isolated so Driver Profile changes do not affect ingestion behavior.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\ProcessingEngine\Mapping\SystemMappingService.cs :: SystemMappingService`; `..\OracleByFPCLtd\ProcessingEngine\Mapping\DriverMappingService.cs :: DriverMappingService`; `..\OracleByFPCLtd\WebSocketMessageFormatter.cs :: WebSocketMessageFormatter`
- `WebSocket Processing Plan > Integration With Application Code:` Processed output must be routed to the UI layer for color rendering.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\MainWindow.xaml.cs :: MainWindow.AppendProcessedRunsWithHighlights`; `..\OracleByFPCLtd\ProcessingEngine\ProcessedLineClassifier.cs :: ProcessedLineClassifier.GetBrush`; `..\OracleByFPCLtd.Tests\MainWindowProcessedOutputTests.cs :: MainWindowProcessedOutputTests.AppendLogAppendsProcessedLineForNumberedEntries`
- `Processing Engine Implementation Guide > Purpose:` Implementation must add no new behavior beyond the approved plan and avoid unrelated feature changes.
- `Processing Engine Implementation Guide > Hard Safety Boundary:` Oracle control of SHP output is restricted to Driver Log Level settings only.
- `Processing Engine Implementation Guide > Hard Safety Boundary:` Oracle must not introduce control features beyond diagnostic verbosity.
- `Processing Engine Implementation Guide > Implementation Outline:` Timestamp normalization must follow the existing parsing formats and preserve the `[yyyy-MM-dd hh:mm:ss.fff]` shape.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\WebSocketMessageFormatter.cs :: WebSocketMessageFormatter.FormatMessageLog`; `..\OracleByFPCLtd.Tests\WebSocketMessageFormatterTests.cs :: WebSocketMessageFormatterTests.MessageLogLinesAreMarkedForNumberingWithDatedTimestamp`
- `Processing Engine Implementation Guide > Implementation Outline:` The original raw line number tracked in `MainWindow.xaml.cs` must be preserved.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd\MainWindow.xaml.cs :: MainWindow._rawLineNumber`; `..\OracleByFPCLtd\MainWindow.xaml.cs :: MainWindow.Transport_RawMessageReceived`
- `Processing Engine Implementation Guide > Implementation Outline:` `ApexDiscoveryPreloadExtractor.Extract` output must be treated as read-only and never mutated.
- `Processing Engine Implementation Guide > Implementation Outline:` `DriverProfileModule.Integrate` must determine driver-to-profile matches before profile-specific mapping runs.
- `Processing Engine Implementation Guide > Required Tests:` Tests must cover page pattern detection and RTI Internal profile selection.
- `Processing Engine Implementation Guide > Required Tests:` Tests must cover page mapping success and unresolved fallback behavior.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd.Tests\ProcessingEngineMappingTests.cs :: ProcessingEngineMappingTests.SystemMappingServiceMapsPageName`; `..\OracleByFPCLtd.Tests\ProcessingEngineMappingTests.cs :: ProcessingEngineMappingTests.SystemMappingServiceMarksUnresolvedMappings`
- `Processing Engine Implementation Guide > Required Tests:` Tests must cover timestamp preservation and date rollover behavior.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd.Tests\WebSocketMessageFormatterTests.cs :: WebSocketMessageFormatterTests.MessageLogLinesAreMarkedForNumberingWithDatedTimestamp`; `..\OracleByFPCLtd.Tests\WebSocketMessageFormatterTests.cs :: WebSocketMessageFormatterTests.MessageLogDateRollsOverAtMidnight`
- `Processing Engine Implementation Guide > Required Tests:` Tests must cover output formatting stability with retained line number and timestamp.
  Value: Implemented
  Evidence: `..\OracleByFPCLtd.Tests\ProcessingEngineRunnerTests.cs :: ProcessingEngineRunnerTests.ProcessesOnlyNumberedLines`; `..\OracleByFPCLtd.Tests\ProcessingEngineTests.cs :: ProcessingEngineTests.MapsPageNumberToPageNameWithTimestamp`
- `Processing Engine Implementation Guide > Explicit Approval Gate:` Tests must be defined before any implementation work begins.
- `Processing Engine Implementation Guide > Explicit Approval Gate:` Implementation requires explicit approval after tests are defined.
