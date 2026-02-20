# Driver Profiles All Info

- Project context: RTI Oracle is a diagnostics and log-analysis application for RTI SHP systems.
- This section documents driver profile behavior, mapping rules, and integration expectations.
- This section includes documentation-only planning content.
- This section includes current code implementation references.
- Scope includes driver profile parsing and mapping rules for C-Bus and Vaux logs.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Scope
- Scope includes output formatting rules for resolved and unresolved mappings.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Scope
- Scope includes profile integration and data flow between Apex Discovery and Analysis Engine.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Scope
- Scope excludes Apex Discovery internals.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Scope
- Scope excludes Analysis Engine internals.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Scope
- Inputs include Project Data module lookup maps from Additional Info upload.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Inputs
- Inputs include raw log lines containing C-Bus and Vaux commands/events.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Inputs
- Inputs include DriverProfile schema definitions that specify Additional Info extraction per driver.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Inputs
- Inputs include `.apex` SQLite database tables referenced by each profile.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: System Context / Inputs
- Inputs include raw diagnostics log lines emitted by SHP drivers.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: System Context / Inputs
- `.apex` files are read-only inputs.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Scope and Constraints
- Project spreadsheets are read-only inputs.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Scope and Constraints
- Profiles may only influence diagnostics verbosity via Driver Log Level settings.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Scope and Constraints
- Do not invent mappings.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Scope and Constraints
- Do not infer missing data.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Implementation Notes
- Unresolved identifiers must remain explicit as `[UNRESOLVED]`.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Data Handoff Contract
- Missing mappings must preserve raw identifiers.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: How Profiles Are Used / Analysis Engine (Log Formatting and Mapping)
- Analysis output should keep a reference to the raw log line number.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: How Profiles Are Used / Analysis Engine (Log Formatting and Mapping)
- On `.apex` upload, extract driver names using `deviceName` and not display name.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Workflow Overview
- Use extracted driver names to find matching driver profiles.
  Value: Implemented
  Evidence: OracleByFPCLtd/DriverProfiles/Matching/DriverProfileMatcher.cs :: Find
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Workflow Overview
- If a profile is found, use it to determine which fields to extract.
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/ApexDiscoveryPreloadExtractor.cs :: LoadDriverConfigMap
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Workflow Overview
- Apex Discovery extracts fields and produces normalized lookup data.
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/ApexDiscoveryPreloadExtractor.cs :: Extract
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Workflow Overview
- Analysis Engine uses the same profile and lookup data to map and format log lines.
  Value: Implemented
  Evidence: OracleByFPCLtd/ProcessingEngine/Mapping/DriverMappingService.cs :: Map
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Workflow Overview
- Internal RTI profile is always included.
  Value: Implemented
  Evidence: OracleByFPCLtd/DriverProfiles/Catalog/DriverProfileCatalog.cs :: Internal
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Workflow Overview
- Internal RTI profile does not require `deviceName` matching.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Workflow Overview
- Apex Discovery outputs a profile-keyed lookup bundle.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Data Handoff Contract
- Analysis Engine consumes the profile and lookup bundle unchanged.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Data Handoff Contract
- Store all profiles in a dedicated directory.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Modular Profile Library (Scalable)
- Use one profile per file.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Modular Profile Library (Scalable)
- Use a clear and stable profile naming scheme.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Modular Profile Library (Scalable)
- Maintain a simple index or registry for profile lookup by `deviceName`.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Modular Profile Library (Scalable)
- New profiles should be addable without changing core logic.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Modular Profile Library (Scalable)
- New profiles should be addable without changing existing profiles.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Modular Profile Library (Scalable)
- New profile lifecycle step: copy the template file.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: New Profile Lifecycle
- New profile lifecycle step: fill all required sections.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: New Profile Lifecycle
- New profile lifecycle step: register the profile in the index or registry.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: New Profile Lifecycle
- New profile lifecycle step: validate naming and expected extraction fields.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: New Profile Lifecycle
- Profile template requirement: include identification details.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Profile Template Requirements
- Profile template identification includes driver name (`deviceName`).
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Profile Template Requirements
- Profile template identification may include aliases.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Profile Template Requirements
- Profile template requirement: define discovery extraction tables, fields, filters, and joins.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Profile Template Requirements
- Profile template requirement: define discovery keys and prefixes for field selection.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Profile Template Requirements
- Example discovery gating pattern uses count keys like `GroupCount`, `SourceCount`, and `ZoneCount`.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Profile Template Requirements
- Profile template requirement: define analysis mapping steps for ID resolution.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Profile Template Requirements
- Profile template requirement: define formatting rules.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Profile Template Requirements
- Profile template requirement: define unresolved handling behavior.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Profile Template Requirements
- Profile template requirement: define output expectations.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Profile Template Requirements
- Output expectations include preserving raw identifiers.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Profile Template Requirements
- Output expectations include preserving raw log line references.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Profile Template Requirements
- Current implementation reference: `SHPDiagnosticsViewer/DriverProfiles/DriverProfileModule.cs`.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Current Implementations (Code)
- Current implementation reference: `SHPDiagnosticsViewer/DriverProfiles/RtiAd64Profile.cs`.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Current Implementations (Code)
- Current implementation reference: `SHPDiagnosticsViewer/DriverProfiles/RtiInternalProfile.cs`.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Current Implementations (Code)
- `RtiAd64Profile` uses `GroupCount`, `SourceCount`, and `ZoneCount` to gate extraction of `GroupName*`, `SourceName*`, and `ZoneName*`.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Current Implementations (Code)
- Count keys are used to limit extraction and are not included in output.
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/ApexDiscoveryPreloadExtractor.cs :: LoadDriverConfigMap
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Current Implementations (Code)
- Apex Discovery consults profiles to filter driver config fields before output.
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/ApexDiscoveryPreloadExtractor.cs :: LoadDriverConfigMap
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Current Implementation (Code)
- Reusable code template reference: `DriverProfiles/DriverProfileTemplate.cs`.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Reusable Profile Template (Code)
- Reusable markdown template section includes profile purpose.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Reusable Profile Template
- Reusable markdown template section includes data source documentation for `.apex` SQLite tables.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Reusable Profile Template
- Reusable markdown template section includes driver row identification method.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Reusable Profile Template
- Reusable markdown template section includes filtered-row SQL.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Reusable Profile Template
- Reusable markdown template section includes discovery extract fields and filters.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Reusable Profile Template
- Reusable markdown template section includes discovery extract SQL.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Reusable Profile Template
- Reusable markdown template section includes analysis mapping rules.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Reusable Profile Template
- Reusable markdown template section includes unresolved token rules.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Reusable Profile Template
- Reusable markdown template section includes mapping input/output examples.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Reusable Profile Template
- Reusable markdown template section includes output expectations.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Reusable Profile Template
- Reusable markdown template section includes notes and constraints.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Reusable Profile Template
- Example SYSVARREF handling uses regex extraction of a GUID enclosed in braces.
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/ApexDiscoveryPreloadExtractor.cs :: SysVarGuidPattern
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Snippets from Existing Profiles / System Variable Events: SYSVARREF resolution
- Example SYSVARREF handling raises an error if GUID is missing.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Snippets from Existing Profiles / System Variable Events: SYSVARREF resolution
- Example SQL exists for System Variable Events config extraction from `DriverConfig`, `DriverData`, and `Devices`.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Snippets from Existing Profiles / System Variable Events: SYSVARREF resolution
- Example SQL exists for RTI Internal page index mapping from `RTIDeviceData`, `Devices`, `RTIDevicePageData`, and `PageNames`.
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Snippets from Existing Profiles / RTI Internal: Page index mapping
- C-Bus driver device sheet tab name must match driver device name from `.apex`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Driver Device Name Match Rules
- C-Bus sheet matching is exact.
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: Extract
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Driver Device Name Match Rules
- C-Bus matching does not allow partial matches.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Driver Device Name Match Rules
- If no sheet match exists, ignore the sheet.
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: Extract
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Driver Device Name Match Rules
- If no sheet match exists, record an error.
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: Extract
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Driver Device Name Match Rules
- C-Bus Immediate Switch example command includes `Immediate Switch(255, 22, 48)`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / General - Immediate Switch
- C-Bus Immediate Switch parsing uses tuple `(State, GroupId, AppId)`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / General - Immediate Switch
- C-Bus Immediate Switch mapping looks up `(AppId, GroupId)` in Clipsal C-Bus sheet.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / General - Immediate Switch
- C-Bus Immediate Switch output always includes `GroupRoom`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / General - Immediate Switch
- C-Bus Immediate Switch output always includes `GroupName`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / General - Immediate Switch
- C-Bus Immediate Switch state `1` maps to `Off`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / General - Immediate Switch
- C-Bus Immediate Switch non-`1` states render as `set to {state} (Unknown State)` until defined.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / General - Immediate Switch
- C-Bus Immediate Switch needs state meaning definitions for `121` and `255`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / General - Immediate Switch
- C-Bus Ramp to level example command includes `Ramp to level(2, 4, 6, 202)`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / General - Ramp to level
- C-Bus Ramp to level parameter meanings are not yet defined.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / General - Ramp to level
- C-Bus Ramp to level needs parameter definitions for AppId, GroupId, and level mapping.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / General - Ramp to level
- C-Bus HVAC Zone Setpoint Up example command includes `HVAC Zone Setpoint Up(1, Unswitched (0))`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / HVAC - HVAC Zone Setpoint Up
- C-Bus HVAC Zone Setpoint Up parsing uses tuple `(GroupId, ZoneId)`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / HVAC - HVAC Zone Setpoint Up
- C-Bus HVAC mapping looks up `(GroupId, ZoneId)` in Clipsal C-Bus HVAC sheet.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / HVAC - HVAC Zone Setpoint Up
- C-Bus HVAC output includes `GroupName`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / HVAC - HVAC Zone Setpoint Up
- C-Bus HVAC output includes `ZoneName`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / HVAC - HVAC Zone Setpoint Up
- C-Bus HVAC output phrasing needs confirmation.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / HVAC - HVAC Zone Setpoint Up
- C-Bus driver event App/Group Off parsing extracts `AppId`, `GroupId`, and state from event text.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / Driver Event - App/Group Off
- C-Bus driver event App/Group Off mapping looks up `(AppId, GroupId)` in Clipsal C-Bus sheet.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / Driver Event - App/Group Off
- C-Bus driver event App/Group Off output includes `GroupRoom`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / Driver Event - App/Group Off
- C-Bus driver event App/Group Off output includes `GroupName`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / Driver Event - App/Group Off
- C-Bus driver event state mapping includes `Off -> Off`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / Driver Event - App/Group Off
- C-Bus driver event state mapping includes `On -> On`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / Driver Event - App/Group Off
- C-Bus driver event App/Group On parsing extracts `AppId`, `GroupId`, and state from event text.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / Driver Event - App/Group On
- C-Bus driver event App/Group On mapping looks up `(AppId, GroupId)` in Clipsal C-Bus sheet.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / Driver Event - App/Group On
- C-Bus driver event App/Group On output includes `GroupRoom`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / Driver Event - App/Group On
- C-Bus driver event App/Group On output includes `GroupName`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: C-Bus Log Handling / Driver Event - App/Group On
- Vaux raw logs may spell the driver as `Vaux Lattis Matrix`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Vaux example command includes `Source Select(Route All, 13, 1)`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Vaux example command includes `Volume Up(13)`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Vaux example command includes `Output Mute(Toggle, 13)`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Vaux example command includes `Output Off(13)`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Vaux sheet tab name must match driver device name from `.apex` or be ignored.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Vaux extraction uses DriverProfile-defined Additional Info schema.
  Value: Implemented
  Evidence: OracleByFPCLtd/DriverProfiles/VauxLattisMatrixProfile.cs :: Definition
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Vaux `Input Index` should resolve to names when available.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Vaux `Output Index` should resolve to names when available.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Vaux current sheet inference maps Output `13` to `Gym`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Vaux current sheet inference maps Input `1` to `Shaw 1`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Vaux Source Select parameter mapping is unresolved.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Vaux Volume Up parameter meaning is unresolved.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Vaux Output Mute parameter rendering is unresolved.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Vaux Output Off index-to-name mapping behavior is unresolved.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Vaux Lattice Matrix Log Handling
- Open item: define meanings for Immediate Switch state values `121` and `255`.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Open Items
- Open item: define Ramp to level parameter semantics.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Open Items
- Open item: confirm HVAC command output phrasing.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Open Items
- Open item: define Vaux command parameter meanings for Source Select, Volume Up, Output Mute, and Output Off.
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Open Items
- CONFLICT: Profiles are documentation artifacts that describe how to interpret `.apex` inputs.
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Implementation Notes
- CONFLICT: Profiles are code-backed definitions used by Apex Discovery and Analysis Engine.
  Value: Implemented
  Evidence: OracleByFPCLtd/DriverProfiles/Models/DriverProfileModels.cs :: DriverProfileDefinition
  Value: Confirmed
  Evidence: driver_profile_purpose.md :: Scope and Constraints
- CONFLICT: Driver profile identification may include optional aliases.
  Value: Implemented
  Evidence: OracleByFPCLtd/DriverProfiles/Models/DriverProfileModels.cs :: DriverProfileDefinition
  Value: Confirmed
  Evidence: driver_profile_implimentation.md :: Profile Template Requirements
- CONFLICT: Sheet tab matching requires exact deviceName with no aliases.
  Value: Implemented
  Evidence: OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs :: Extract
  Value: Confirmed
  Evidence: driver_profile_update_plan.md :: Driver Device Name Match Rules
