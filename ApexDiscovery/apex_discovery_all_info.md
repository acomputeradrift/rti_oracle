# Apex Discovery All Info (Merged)

## Scope
- Apex Discovery produces preload data for a Processing Engine.
- The preload contract should remain stable for O(1) lookups.
- Extraction is performed from `.apex` files treated as SQLite databases.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.Extract
- `.apex` files are read-only inputs.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.Extract
- Extraction outputs must be deterministic.
- Extraction outputs must be derived only from source data.
- Missing data must remain explicitly unresolved.
- Human-readable names must not be fabricated.

## Output Contract
- `pageIndexMap` uses key `deviceId|pageIndex`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadPageIndexMap
- `pageIndexMap` values are page names.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadPageIndexMap
- `sysVarRefMap` uses full SYSVARREF-style keys.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadSysVarRefMap
- `sysVarRefMap` values include `driverDeviceId`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: SysVarRefEntry
- `sysVarRefMap` values include `variableName`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: SysVarRefEntry
- `sysVarRefMap` values include `deviceId`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: SysVarRefEntry
- `driverConfigMap` is keyed by `driverDeviceId`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadDriverConfigMap
- `driverConfigMap` includes `deviceName`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: DriverConfigEntry
- `driverConfigMap` includes filtered config key-value pairs.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadDriverConfigMap
- `driverConfigMap` excludes `Debug*` keys.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadDriverConfigMap
- Include only extracted and validated values in output.
- Output metadata may include `projectId`.
- Output metadata may include `generatedAt`.
- Output metadata may include `apexPathHash`.
- Output metadata may include `schemaVersion`.

## Page Extraction
- Page extraction joins `RTIDeviceData` to `Devices` for controller context.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadPageMappings
- Page extraction scopes rows by `RTIDevicePageData.RTIAddress`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadPageMappings
- Page names resolve from `RTIDevicePageData.PageNameId` to `PageNames.PageName`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadPageMappings
- Source device resolves from `RTIDevicePageData.SourceDeviceId` to `Devices`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadPageMappings
- Source room resolves from source `Devices.RoomId` to `Rooms`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadPageMappings
- Page extraction can include devices with no pages when left joins are used.
- CONFLICT: Page index is `RTIDevicePageData.PageOrder`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadPageIndexMap
- CONFLICT: Page number is `RTIDevicePageData.PageOrder + 1`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadPageMappings
- Page index/number must not be derived from `PageId`.
- Page rows should be ordered by device then page order.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadPageMappings

## Room-Source-Page Mapping
- Room-to-source mapping uses `Devices.RoomId`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRoomMappings
- Page-to-source mapping uses `RTIDevicePageData.SourceDeviceId`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRoomMappings
- Controller device for page display resolves via `RTIDevicePageData.RTIAddress` to `RTIDeviceData` to `Devices`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRoomMappings
- Room mappings may contain nulls when source/page/controller joins are missing.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: RoomMappingEntry
- Controller device name should be retained to disambiguate duplicate page names.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: RoomMappingEntry

## Driver Config Extraction
- Driver config extraction joins `DriverConfig` to `DriverData` by `DriverDeviceId`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadDriverConfigMap
- Driver config extraction joins `DriverData` to `Devices` by `DeviceId`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadDriverConfigMap
- Driver config exports should exclude `Debug*` keys.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadDriverConfigMap
- Config groups can be bounded by `MaxZones`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.ExtractLimits
- Config groups can be bounded by `MaxSources`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.ExtractLimits
- Config groups can be bounded by `Inputs`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.ExtractLimits
- Config groups can be bounded by `Outputs`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.ExtractLimits
- If a max/bound value is missing for a group, include all entries in that group.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.ShouldIncludeConfig

## SYSVARREF Resolution
- SYSVARREF resolution parses the GUID from the reference.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadSysVarRefMap
- SYSVARREF resolution parses the token after `@`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadSysVarRefMap
- Parsed GUID resolves to `DriverData.DriverId`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadSysVarRefMap
- Full reference can be matched against `SystemVariableIds.SysVarRef`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadSysVarRefMap
- Human-readable variable names are sourced from `DriverData.SystemVariables` XML.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadSysVarRefMap
- Parser must accept standard SYSVARREF form `{GUID}#NN@Token`.
- Parser must accept device-scoped SYSVARREF form `{GUID}#<DeviceId>@Token`.
- GUID plus token is treated as the lookup key for variable metadata.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadSysVarRefMap
- Unresolved SYSVARREF fields must remain explicit.

## System Variable Events Driver
- System Variable Events config rows are filtered to non-null values.
- System Variable Events config rows exclude empty-string values.
- System Variable Events config rows exclude `(not set)` values.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.FilterSystemVariableEvents
- System Variable Events config values containing SYSVARREF should be resolvable to driver and variable names.

## Ports - Shared Rules
- Port naming data is sourced from `PortLabels.LabelName`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRelayPorts
- Port grouping/inference uses `PortLabels.LabelKey`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRelayPorts
- Expander inference uses `expander_id = LabelKey >> 16`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRelayPorts
- Expansion device context resolves from `ExpansionDevices` at `RTIAddress = 0`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRelayPorts
- Controller name resolves through `RTIDeviceData` and `Devices` at `RTIAddress = 0`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRelayPorts

## Ports - Relay
- Internal XP-8v relay labels come from `LabelKey` range `-64768..-64761`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRelayPorts
- Expansion relay labels include rows where name matches `Relay %`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRelayPorts
- Relay type for internal relays comes from `RelayTypeMap` with `ExpanderId = -1`.
- Relay mode for internal relays comes from `RelayModeMap` with `ExpanderId = -1`.
- Expansion relay type/mode may be unavailable and require explicit placeholder states.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRelayPorts
- XP-6 relay type/mode DB masks exist but may be out of scope for this setup.

## Ports - MPIO/IR
- Internal XP-8v MPIO/IR labels come from `LabelKey` range `-65536..-65529`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadMpioIrPorts
- XP-6 MPIO/IR labels come from `LabelKey` range `65536..65543`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadMpioIrPorts
- MPIO/IR `port_number` is derived from low-word key extraction.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadMpioIrPorts
- MPIO/IR naming does not require `IrData` or `IrFunction`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadMpioIrPorts

## Ports - Sense
- Internal XP-8v Sense labels come from `LabelKey` range `-65024..-65017`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadSensePorts
- XP-6 Sense labels come from `LabelKey` range `66048..66055`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadSensePorts
- Sense `port_number` is derived from low-word key extraction with Sense offset logic.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadSensePorts
- Internal Sense mode comes from `SenseModeMap` with `ExpanderId = -1`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadSensePorts
- Sense mode is determined by bitmask per port index.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadSensePorts
- Sense mode mapping applies only to the internal processor in this project context.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadSensePorts

## Ports - Trigger
- XP-6 Trigger labels come from `LabelKey` range `66307..66309`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadTriggerPorts
- Trigger number is derived from low-word key extraction with Trigger offset logic.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadTriggerPorts
- Trigger labels in this project appear on XP-6 only.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadTriggerPorts

## Ports - RS-232
- Internal XP-8v RS-232 labels come from `LabelKey` range `-65280..-65273`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRs232Ports
- XP-6 RS-232 labels come from `LabelKey` range `65792..65799`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRs232Ports
- RS-232 `port_number` is derived from low-word key extraction with RS-232 offset logic.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRs232Ports
- RS-232 naming does not require `RS232Data`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadRs232Ports

## Macros
- Macro extraction is marked unfinished.
- `Macros` provides `MacroId`.
  Value: Confirmed
  Evidence: apex_extraction_schemas.md :: Macros (UNFINISHED) > What is known (proven)
- `Macros` provides `RoomId`.
  Value: Confirmed
  Evidence: apex_extraction_schemas.md :: Macros (UNFINISHED) > What is known (proven)
- `Macros` provides `DeviceId`.
  Value: Confirmed
  Evidence: apex_extraction_schemas.md :: Macros (UNFINISHED) > What is known (proven)
- `Macros` provides `ButtonTagId`.
  Value: Confirmed
  Evidence: apex_extraction_schemas.md :: Macros (UNFINISHED) > What is known (proven)
- `Macros` provides `OutputType`.
  Value: Confirmed
  Evidence: apex_extraction_schemas.md :: Macros (UNFINISHED) > What is known (proven)
- `ButtonTagId` can resolve to `ButtonTagNames`.
  Value: Confirmed
  Evidence: apex_extraction_schemas.md :: Macros (UNFINISHED) > What is known (proven)
- Macro steps are available through `MacroSteps` and `MacroStepsView`.
  Value: Confirmed
  Evidence: apex_extraction_schemas.md :: Macros (UNFINISHED) > What is known (proven)
- A canonical macro name field is not yet proven.
- Macro scoping rules for global vs room vs source are not yet proven.

## Variables - Driver Template
- Driver variable extraction includes `driver_device_id`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: DriverTemplateVariableEntry
- Driver variable extraction includes `driver_device_name`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: DriverTemplateVariableEntry
- Driver variable extraction may include `driver_display_name`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: DriverTemplateVariableEntry
- Driver variable extraction includes `sysvar_ref`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: DriverTemplateVariableEntry
- Driver variable extraction includes `sysvar_token`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: DriverTemplateVariableEntry
- Driver variable extraction includes source driver identity from SYSVARREF GUID.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadDriverTemplateVariables
- Driver variable extraction includes variable category from XML.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.ParseDriverVariables
- Driver variable extraction includes variable name from XML.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.ParseDriverVariables
- Driver variable extraction may include variable type from XML.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.ParseDriverVariables
- Driver variable extraction may include format from XML.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.ParseDriverVariables
- `SystemVariableIds.SysVarRef` does not contain human-readable variable names.

## Variables - System Manager
- System Manager variable extraction is marked unfinished.
- System Manager variables do not have `DriverData.SystemVariables` XML.
- System Manager driver identifier is GUID `{20186C86-446C-4FC6-89E1-1931718A169B}`.
  Value: Confirmed
  Evidence: apex_extraction_schemas.md :: Variables (System Manager) (UNFINISHED) > System Manager driver identifier (proven)
- System Manager source name categories include token patterns `SourceInUse<N>` and `SourceName<N>`.
  Value: Confirmed
  Evidence: apex_extraction_schemas.md :: Variables (System Manager) (UNFINISHED) > Proven category: Source Names
- Source index mapping uses devices filtered by `ControlType IN (5, 6)` and ordered by `DeviceId`.
  Value: Implemented
  Evidence: ProjectData/ApexDiscoveryPreloadExtractor.cs :: ApexDiscoveryPreloadExtractor.LoadSourceCatalog
- System Manager display/source naming for categories beyond Source Names is not yet proven.
- If no System Manager device row exists, use explicit literal driver naming rather than inferred device rows.

## Field Glossary
- `device_id` is the controller device id from `Devices.DeviceId`.
- `device_name` is the controller device name from `Devices.Name`.
- `room_id` is the room id from `Rooms.RoomId`.
- `room_name` is the room name from `Rooms.Name`.
- `source_id` is the source device id referenced by `RTIDevicePageData.SourceDeviceId`.
- `source_name` is the source device name associated with `source_id`.
- `page_name` is the page label from `PageNames.PageName`.
- `port_label_id` is `PortLabels.PortLabelId`.
- `port_label_key` is `PortLabels.LabelKey`.
- `port_label_name` is `PortLabels.LabelName`.
- `expander_id` identifies internal or expansion context for ports.
