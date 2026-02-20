- Scope: Driver profile parsing and mapping rules apply to C-Bus and Vaux logs.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\Catalog\DriverProfileCatalog.cs :: DriverProfileCatalog.All
- Scope: Output formatting rules cover both resolved and unresolved mappings.
- Scope: This plan documents intended behavior and does not modify code.
- Inputs: Project Data lookup maps from Additional Info uploads are required inputs.
- Inputs: Raw log lines with C-Bus and Vaux commands are required inputs.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProcessingEngine\Mapping\DriverMappingService.cs :: DriverMappingService.Map
- Inputs: DriverProfile schemas define Additional Info extraction rules per driver.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProjectData\Extractors\AdditionalDataExtractor.cs :: AdditionalDataExtractor.ApplySchema
- Driver Device Name Match Rules: Sheet tab names must match `.apex` driver device names exactly.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProjectData\Extractors\AdditionalDataExtractor.cs :: AdditionalDataExtractor.Extract
- Driver Device Name Match Rules: Matching does not allow aliases or partial names.
- Driver Device Name Match Rules: A non-matching sheet is ignored and records an error.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProjectData\Extractors\AdditionalDataExtractor.cs :: AdditionalDataExtractor.Extract
- C-Bus General Immediate Switch Parsing Rule: The tuple is parsed as `State`, `GroupId`, and `AppId`.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.TryMapCommand
- C-Bus General Immediate Switch Mapping Rule: The mapper looks up `AppId` and `GroupId` in the Clipsal C-Bus sheet.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.MapCbusGroup
- C-Bus General Immediate Switch Mapping Rule: Output always includes both `GroupRoom` and `GroupName`.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.FormatGroupName
- C-Bus General Immediate Switch State Mapping: State value `1` maps to Off.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.MapImmediateSwitchState
- C-Bus General Immediate Switch State Mapping: Other states render as unknown until definitions exist.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.MapImmediateSwitchState
- C-Bus General Immediate Switch Open Item: State values `121` and `255` still need defined meanings.
- C-Bus General Ramp to Level Parsing Rule: Parameter meanings are not yet defined.
- C-Bus General Ramp to Level Mapping Rule: Encoding for `AppId`, `GroupId`, and level still requires analysis.
- C-Bus General Ramp to Level Output Format: Output remains a placeholder until parameter meanings are confirmed.
- C-Bus HVAC Zone Setpoint Up Parsing Rule: The tuple is parsed as `GroupId` and `ZoneId`.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.TryMapCommand
- C-Bus HVAC Zone Setpoint Up Mapping Rule: The mapper looks up `GroupId` and `ZoneId` in the HVAC sheet.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.MapHvacZone
- C-Bus HVAC Zone Setpoint Up Mapping Rule: Output always includes both `GroupName` and `ZoneName`.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd.Tests\ProcessingEngineMappingTests.cs :: ProcessingEngineMappingTests.DriverMappingServiceMapsCbusHvacSetpointUpUnknownState
- C-Bus HVAC Zone Setpoint Up Open Item: Output phrasing for HVAC commands still needs confirmation.
- C-Bus Driver Event App Group Off Parsing Rule: Event text extraction includes `AppId`, `GroupId`, and state.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.TryMapEvent
- C-Bus Driver Event App Group Off Mapping Rule: The mapper looks up `AppId` and `GroupId` in the C-Bus sheet.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.ResolveGroupReplacement
- C-Bus Driver Event App Group Off Mapping Rule: Output always includes both `GroupRoom` and `GroupName`.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.FormatGroupName
- C-Bus Driver Event App Group Off State Mapping: Off maps to Off and On maps to On.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.TryMapEvent
- C-Bus Driver Event App Group On Parsing Rule: Event text extraction includes `AppId`, `GroupId`, and state.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.TryMapEvent
- C-Bus Driver Event App Group On Mapping Rule: The mapper looks up `AppId` and `GroupId` in the C-Bus sheet.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.ResolveGroupReplacement
- C-Bus Driver Event App Group On Mapping Rule: Output always includes both `GroupRoom` and `GroupName`.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.FormatGroupName
- C-Bus Driver Event App Group On State Mapping: On maps to On and Off maps to Off.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.TryMapEvent
- Vaux Lattice Matrix Mapping Plan: Sheet tab names must match `.apex` driver device names or be ignored.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProjectData\Extractors\AdditionalDataExtractor.cs :: AdditionalDataExtractor.Extract
- Vaux Lattice Matrix Mapping Plan: DriverProfile definitions provide the schema for Additional Info extraction.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\VauxLattisMatrixProfile.cs :: VauxLattisMatrixProfile.Definition
- Vaux Lattice Matrix Mapping Plan: Input and output indexes should resolve to names when available.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\VauxLattisMatrixProfile.cs :: VauxLattisMatrixMapper.ResolveInputName
- Vaux Lattice Matrix Mapping Plan: Current sheet inference maps output `13` to Gym.
- Vaux Lattice Matrix Mapping Plan: Current sheet inference maps input `1` to Shaw 1.
- Vaux Lattice Matrix Placeholders: Source Select parameter mapping to routing and indexes still needs analysis.
- Vaux Lattice Matrix Placeholders: Volume Up parameter semantics still require confirmation.
- Vaux Lattice Matrix Placeholders: Output Mute rendering for Toggle and output index still needs definition.
- Vaux Lattice Matrix Placeholders: Output Off mapping from output index to output name still needs definition.
- Open Items: Immediate Switch state meanings for `121` and `255` remain undefined.
- Open Items: Ramp to level parameter semantics remain undefined.
- Open Items: Preferred HVAC command output phrasing remains unconfirmed.
- Open Items: Vaux command parameter meanings remain undefined for Source Select, Volume Up, Output Mute, and Output Off.
- Scope Confirmation: No code changes were made within this update plan.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Scope Confirmation
- Scope Confirmation: No source documents were modified within this update plan.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Scope Confirmation
- Scope Confirmation: The plan is limited to parsing and mapping behavior for driver profiles.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Scope Confirmation
- Purpose: The implementation plan guides code creation and integration for driver profiles.
- Purpose: The implementation plan requires profiles to be modular, scalable, and easy to add.
- Purpose: The implementation plan defines data handoff between Apex Discovery and the Analysis Engine.
- Scope: The implementation plan covers only profile integration and data flow.
- Scope: Apex Discovery internals and Analysis Engine internals are out of scope.
- Workflow Overview: `.apex` upload processing extracts driver names from `deviceName`, not display name.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProjectData\ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadDriverConfigMap
- Workflow Overview: Extracted driver names are used to find matching driver profiles.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProjectData\ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadDriverConfigMap
- Workflow Overview: A matched profile determines which fields Apex Discovery extracts.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProjectData\ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadDriverConfigMap
- Workflow Overview: Apex Discovery produces normalized lookup data from extracted fields.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProjectData\ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.Extract
- Workflow Overview: Analysis Engine maps and formats log lines using the same profile and lookup data.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProcessingEngine\Mapping\DriverMappingService.cs :: DriverMappingService.Map
- Workflow Overview: The internal RTI profile is always included without requiring `deviceName` matching.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\Catalog\DriverProfileCatalog.cs :: DriverProfileCatalog.Internal
- Data Handoff Contract: Apex Discovery outputs a lookup bundle keyed by profile.
- Data Handoff Contract: Analysis Engine consumes the profile and lookup bundle without changes.
- Data Handoff Contract: Unresolved identifiers must remain explicit as `[UNRESOLVED]`.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProcessingEngine\Mapping\SystemMappingService.cs :: SystemMappingService.Map
- Modular Profile Library: All profiles should be stored together in a dedicated directory.
- Modular Profile Library: Each profile should exist in one file with stable naming.
- Modular Profile Library: A simple registry should provide fast lookup by `deviceName`.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\Matching\DriverProfileMatcher.cs :: DriverProfileMatcher.Find
- Modular Profile Library: New profiles should be added without touching existing profiles or core logic.
- Profile Template Requirements: Each profile must define driver identification with `deviceName` and optional aliases.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\Models\DriverProfileModels.cs :: DriverProfileDefinition
- Profile Template Requirements: Each profile must define discovery extraction tables, fields, filters, and joins.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\Models\DriverProfileModels.cs :: DriverProfileDefinition
- Profile Template Requirements: Each profile must define discovery keys and prefixes for field selection.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\Models\DriverProfileModels.cs :: DriverProfileDefinition
- Profile Template Requirements: Each profile must define analysis mapping, formatting rules, and unresolved handling.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\Models\DriverProfileModels.cs :: DriverProfileDefinition
- Profile Template Requirements: Each profile must define output expectations including raw identifiers and raw line references.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\Models\DriverProfileModels.cs :: DriverProfileDefinition
- New Profile Lifecycle: Creating a profile starts by copying the template file.
- New Profile Lifecycle: Creating a profile requires filling all required template sections.
- New Profile Lifecycle: Creating a profile requires registration in the profile index or registry.
- New Profile Lifecycle: Creating a profile requires validation of naming and expected extraction fields.
- CONFLICT: Implementation Notes: Profiles are documentation artifacts and must not modify `.apex` inputs.
- Scope and Constraints: `.apex` files and project spreadsheets are read-only inputs.
- Scope and Constraints: Profiles may only influence diagnostics verbosity through Driver Log Level settings.
- Scope and Constraints: Mappings must not be invented, and unresolved identifiers must stay explicit.
- CONFLICT: Scope and Constraints: Profiles are code-backed definitions used by Apex Discovery and Analysis Engine.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProcessingEngine\Mapping\DriverMappingService.cs :: DriverMappingService.Map
- System Context Inputs: Profile processing uses `.apex` SQLite tables referenced by each profile.
- System Context Inputs: Profile processing uses raw diagnostics log lines emitted by SHP drivers.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProcessingEngine\Mapping\DriverMappingService.cs :: DriverMappingService.Map
- System Context Outputs: Profiles provide structured field extraction rules for Apex Discovery.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProjectData\ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.Extract
- System Context Outputs: Profiles provide log formatting and mapping instructions for the Analysis Engine.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProcessingEngine\Mapping\DriverMappingService.cs :: DriverMappingService.Map
- System Context Outputs: Missing data must be marked explicitly with `[UNRESOLVED]`.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProcessingEngine\Mapping\SystemMappingService.cs :: SystemMappingService.Map
- How Profiles Are Used Apex Discovery: Profiles define tables and fields that Apex Discovery extracts.
- How Profiles Are Used Apex Discovery: Profiles define filters and joins needed for driver-specific data.
- How Profiles Are Used Apex Discovery: Outputs are normalized lookup sets for later diagnostics mapping.
- How Profiles Are Used Analysis Engine: Profiles define matching rules from log lines to driver context.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\Models\DriverProfileModels.cs :: IDriverProfileMapper.TryMap
- How Profiles Are Used Analysis Engine: Profiles define ID resolution to human-readable names.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\ClipsalCbusProfile.cs :: ClipsalCbusMapper.ResolveGroupReplacement
- How Profiles Are Used Analysis Engine: Missing mappings emit raw identifiers marked as `[UNRESOLVED]`.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProcessingEngine\Mapping\SystemMappingService.cs :: SystemMappingService.Map
- How Profiles Are Used Analysis Engine: Outputs always keep a reference to the raw log line number.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProcessingEngine\Mapping\DriverMappingService.cs :: DriverMappingService.Map
- Data Flow Summary: Apex Discovery reads `.apex` data using profile-defined queries.
- Data Flow Summary: Discovery output is passed to Analysis Engine as lookup data.
- Data Flow Summary: Analysis Engine formats logs using profile mappings.
- Current Implementation Code: Profile definitions live in code files for `RtiAd64Profile` and `RtiInternalProfile`.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\RtiAd64Profile.cs :: RtiAd64Profile
- Current Implementation Code: Profile catalog and lookup live in `DriverProfileModule` code.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\Integration\DriverProfileModule.cs :: DriverProfileModule
- Current Implementation Code: Apex Discovery consults profiles to filter driver config fields before output.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\ProjectData\ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadDriverConfigMap
- Current Implementation Code: The internal RTI profile is always included and not tied to a device name.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\Catalog\DriverProfileCatalog.cs :: DriverProfileCatalog.Internal
- Reusable Profile Template Output Expectations: Failed resolutions preserve original identifiers in output.
- Reusable Profile Template Output Expectations: Missing data is marked explicitly as `[UNRESOLVED]`.
- Reusable Profile Template Output Expectations: Output keeps a reference to the raw log line number.
- Reusable Profile Template Notes and Constraints: `.apex` files are treated as read-only inputs.
- Reusable Profile Template Notes and Constraints: Name mapping is allowed only when `.apex` data exists.
- Reusable Profile Template Code: `DriverProfiles/DriverProfileTemplate.cs` is the code template for new profiles.
- Snippets from Existing Profiles System Variable Events: SYSVARREF parsing raises an error when the GUID is missing.
- Snippets from Existing Profiles System Variable Events: SQL filtering excludes null, empty, and `(not set)` config values.
- Snippets from Existing Profiles RTI Internal: Page index mapping joins RTI device, page, and page name tables.
  Value: Implemented
  Evidence: ..\OracleByFPCLtd\DriverProfiles\RtiInternalProfile.cs :: RtiInternalProfile.Definition
