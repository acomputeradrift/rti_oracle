# LOC Reduction Baseline

## Scope

Baseline uses repository `.cs` source files only.

Excluded from counting:
- Generated output under `bin/`, `obj/`, and `artifacts/`
- Temporary output under `.tmp*`
- Held generated output under `artifacts__hold/`

This baseline is intended to identify the largest maintenance hotspots before any extraction work begins.

## Top 20 Largest Files

Ranked by line count:

1. `OracleByFPCLtd/MainWindow.xaml.cs` - 5154 LOC
2. `OracleByFPCLtd/ProjectData/ApexDiscoveryPreloadExtractor.cs` - 1412 LOC
3. `OracleByFPCLtd.Tests/MainWindowProcessedOutputTests.cs` - 1115 LOC
4. `OracleByFPCLtd.Tests/ProcessingEngineMappingTests.cs` - 1084 LOC
5. `OracleByFPCLtd/DriverProfiles/Services/DriverMessageTemplateFormatter.cs` - 938 LOC
6. `OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs` - 676 LOC
7. `OracleByFPCLtd/DiagnosticsTransport/TcpCaptureDiagnosticsTransport.cs` - 615 LOC
8. `OracleByFPCLtd.Tests/MainWindowUiLayoutTests.cs` - 537 LOC
9. `OracleByFPCLtd/ProjectData/ProjectDataExtraction.cs` - 527 LOC
10. `OracleByFPCLtd/ProcessingEngine/Mapping/DriverMappingService.cs` - 492 LOC
11. `OracleByFPCLtd/DiagnosticsTransport/LegacyWebSocketDiagnosticsTransport.cs` - 474 LOC
12. `OracleByFPCLtd.Tests/MainWindowFindTests.cs` - 432 LOC
13. `OracleByFPCLtd/Logging/CentralLogger.cs` - 410 LOC
14. `OracleByFPCLtd/DriverProfiles/RtiInternalProfile.cs` - 385 LOC
15. `OracleByFPCLtd.Tests/LoggingSubsystemTests.cs` - 381 LOC
16. `OracleByFPCLtd/DriverProfiles/ClipsalCbusProfile.cs` - 380 LOC
17. `OracleByFPCLtd.Tests/AdditionalDataExtractorTests.cs` - 342 LOC
18. `OracleByFPCLtd/ProjectDataPreviewWindow.xaml.cs` - 280 LOC
19. `OracleByFPCLtd/Reliability/UnhandledTaggedReportBuilder.cs` - 268 LOC
20. `OracleByFPCLtd/DriverProfiles/SystemManagerProfile.cs` - 265 LOC

## LOC Sinks To Attack

Ranked in recommended attack order:

1. `OracleByFPCLtd/MainWindow.xaml.cs` - 5154 LOC
2. `OracleByFPCLtd/ProjectData/ApexDiscoveryPreloadExtractor.cs` - 1412 LOC
3. `OracleByFPCLtd/DriverProfiles/Services/DriverMessageTemplateFormatter.cs` - 938 LOC
4. `OracleByFPCLtd.Tests/MainWindowProcessedOutputTests.cs` - 1115 LOC
5. `OracleByFPCLtd.Tests/ProcessingEngineMappingTests.cs` - 1084 LOC
6. `OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs` - 676 LOC
7. `OracleByFPCLtd/DiagnosticsTransport/TcpCaptureDiagnosticsTransport.cs` - 615 LOC

Notes:
- The first three are the primary implementation sinks and should drive the first extraction passes.
- The next four are the remaining files currently above roughly 600 LOC in the counted source set.
- The two large test files should be reduced only when their size is driven by duplication that mirrors implementation extraction, not by cosmetic restructuring alone.

## Success Metrics

Whole-effort targets:

- Goal A: Reduce `OracleByFPCLtd/MainWindow.xaml.cs` below 1500 LOC.
- Conservative fallback for Goal A: Reduce `OracleByFPCLtd/MainWindow.xaml.cs` below 2000 LOC if a lower-risk phased extraction is required.
- Goal B: Reduce each top-three implementation sink (`MainWindow.xaml.cs`, `ApexDiscoveryPreloadExtractor.cs`, `DriverMessageTemplateFormatter.cs`) by 30% to 50% through extraction, decomposition, and reuse.
- Goal C: Do not increase project count unless the change removes more LOC than it adds.

## Working Rule

Prioritize reductions that:

- Remove duplicated parsing or formatting logic
- Isolate cohesive responsibilities into smaller units
- Reduce test duplication when it is a direct consequence of extracted shared behavior
- Preserve behavior and existing test coverage while shrinking file-level complexity
