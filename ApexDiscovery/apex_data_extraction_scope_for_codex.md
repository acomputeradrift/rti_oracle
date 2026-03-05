# APEX Data Extraction Scope for Codex

## Purpose

This document defines the known scope of data that can be extracted from an RTI `.apex` file for use in another application.

It is written for AI-assisted analysis and implementation planning.

The goal is to give Codex a reliable map of:

- what is confirmed extractable now
- what is partially understood
- what schema areas exist but are not fully explored
- which exact identifiers and relationships must be preserved

## Evidence Base

This document is derived from:

- all docs in `ApexDiscovery`
- root repository docs that define `.apex` constraints and data contracts
- current extraction code in the codebase
- current tests in the codebase
- direct schema inspection of sample `.apex` files in `ApexDiscovery/Assets`

Primary doc evidence:

- `ApexDiscovery/apex_discovery_all_info.md`
- `ApexDiscovery/Achives/apex_extraction_schemas.md`
- `ApexDiscovery/Achives/proven_apex_data.md`
- `ApexDiscovery/Achives/apex_general_scope_info.md`
- `ApexDiscovery/Achives/apex_discovery_diagnostics_info.md`
- `data_contracts.md`

## Non-Negotiable Facts

- A `.apex` file is a SQLite database.
- The code treats `.apex` as read-only input.
- The main extraction path opens the file with `Mode=ReadOnly`.
- The broader extractor first copies the `.apex` file to a temp `.apex` path, then reads the temp copy in read-only mode.
- Missing or unresolved data is expected to remain explicit; names must not be fabricated.
- Current extraction logic is deterministic and based on direct joins, string parsing, XML parsing, and stable lookup maps.

## Current Extraction Entry Points

### 1. Confirmed primary extractor: `ApexDiscoveryPreloadExtractor.Extract`

This is the repository's current authoritative extraction path for reusable `.apex` preload data.

It returns an `ApexDiscoveryPreloadResult` with these top-level data sets:

- `PageIndexMap`
- `SysVarRefMap`
- `DriverConfigMap`
- `PageMappings`
- `RelayPorts`
- `MpioIrPorts`
- `SensePorts`
- `TriggerPorts`
- `Rs232Ports`
- `RoomMappings`
- `SourceCatalog`
- `SystemManagerSourceCatalog`
- `DriverTemplateVariables`
- `ExpansionDeviceTypes`

### 2. Confirmed broader extractor: `ProjectDataExtractor.Extract`

This is a second, broader extraction path that still reads directly from the `.apex` database.

It produces:

- `DiagnosticsMapping`
- `ProjectReport`
- `ProjectTest`
- `ApexDiscoveryPreload` (by calling the primary extractor)

This path is useful because it proves additional extraction methods beyond the preload contract, especially for:

- page identity beyond page names
- source labels
- layers
- button instances
- flat report-style entity export

### 3. Confirmed projection wrappers

These do not read the database themselves. They re-project already extracted `.apex` data:

- `ApexSystemExtractor.Extract`
- `ApexDriverExtractor.Extract`

They confirm that the repository expects `.apex` data to be reusable as structured system-level and driver-level data.

## Confirmed Extractable Now

The following are confirmed by current code and backed by passing tests where noted.

### A. Page index to page name mapping

Status: `confirmed extractable now`

Output:

- key: `deviceId|pageIndex`
- value: `pageName`

Source chain:

- `RTIDeviceData.DeviceId`
- `RTIDeviceData.RTIAddress`
- `RTIDevicePageData.PageOrder`
- `RTIDevicePageData.PageNameId`
- `PageNames.PageName`

Important rules:

- `PageOrder` is the true per-device page index.
- `PageNumber` in user-facing lists is `PageOrder + 1`.
- `PageId` must not be used as the page index.
- Clone-aware logic exists:
  - if `RTIDeviceData.CloneRTIAddress` exists and is greater than zero, page lookup uses that address instead of the device's own `RTIAddress`.

This clone behavior is explicitly tested.

### B. Page mappings with device, room, and source context

Status: `confirmed extractable now`

Output fields:

- `device_id`
- `device_name`
- `room_id`
- `room_name`
- `source_id`
- `source_name`
- `page_number`
- `page_name`

Source chain:

- controller device: `RTIDeviceData -> Devices`
- page rows: `RTIDevicePageData`
- source device: `RTIDevicePageData.SourceDeviceId -> Devices`
- room: source `Devices.RoomId -> Rooms`
- page name: `PageNames`

This is the repository's strongest current page-context extraction model.

### C. Room to source to page to controller mapping

Status: `confirmed extractable now`

Output fields:

- `room_id`
- `room_name`
- `source_id`
- `source_name`
- `controller_device_id`
- `controller_device_name`
- `page_id`
- `page_name`

Source chain:

- `Rooms`
- source device via `Devices.RoomId`
- page via `RTIDevicePageData.SourceDeviceId`
- controller via `RTIDevicePageData.RTIAddress -> RTIDeviceData -> Devices`

This is useful when the target app needs room-centric navigation instead of device-centric navigation.

### D. Driver config extraction

Status: `confirmed extractable now`

Output shape:

- key: `driverDeviceId`
- value:
  - `deviceName`
  - `deviceDisplayName`
  - `config` key/value map

Source chain:

- `DriverConfig`
- `DriverData`
- `Devices`

Confirmed behaviors:

- `Debug*` config keys are excluded.
- `DisplayName` falls back to `Name` when blank.
- Config groups are bounded by discovered limits when applicable:
  - `MaxZones`
  - `MaxSources`
  - `Inputs`
  - `Outputs`
- Prefix/count filtering also exists for matched driver profiles.
- Special-case filtering exists for `System Variable Events` devices.

This is tested.

### E. `System Variable Events` filtered config extraction

Status: `confirmed extractable now`

This is a special case inside driver config extraction.

Confirmed filtering rules:

- drop `Config_PersistEnabledStates`
- drop values equal to `(not set)`
- drop boolean config groups when the paired `Macro` value is blank
- drop integer config groups when the paired `Macro` value is blank or `0`

This prevents exporting dead config groups that exist structurally but are not actually configured.

### F. SYSVARREF registry and human-readable variable resolution

Status: `confirmed extractable now`

Output shape:

- key: normalized full SYSVARREF
- value:
  - `driverDeviceId`
  - `driverName`
  - `variableName`
  - `deviceId`

Confirmed parsing behavior:

- stored keys are normalized to `SYSVARREF:{GUID}#...@Token`
- GUID is parsed from the SYSVARREF string
- token is parsed as the substring after `@`
- driver lookup is done through `DriverData.DriverId`
- human-readable variable names come from `DriverData.SystemVariables` XML
- device identity comes from `SystemVariableIds.DeviceId`

Supported forms:

- standard form: `{GUID}#NN@Token`
- device-scoped form: `{GUID}#<DeviceId>@Token`

This is tested.

### G. Driver template variable extraction

Status: `confirmed extractable now`

Output fields:

- `driver_device_id`
- `driver_device_name`
- `driver_display_name`
- `sysvar_ref`
- `sysvar_token`
- `source_driver_id`
- `source_driver_name`
- `variable_category`
- `variable_name`
- `variable_type`
- `format`

Source chain:

- driver instance: `Devices + DriverData`
- SYSVARREF population: `SystemVariableIds`
- variable metadata: parsed from `DriverData.SystemVariables` XML

Confirmed XML parsing rules:

- looks for `<variable ...>`
- takes `name`
- takes `type` or `datatype`
- takes `format`
- takes nearest ancestor `<category name="...">`

This is tested.

### H. Source catalog extraction

Status: `confirmed extractable now`

Output fields:

- `device_id`
- `room_id`
- `control_type`
- `source_name`
- `source_display_name`

Current source definition:

- `Devices` rows where `ControlType IN (5, 6)`

Ordering rule:

- ordered by `DeviceId`

This is the base source list used by current System Manager source resolution.

### I. System Manager source catalog

Status: `confirmed extractable now`

Output fields:

- `source_index`
- `source_name`

Current logic:

- count System Manager tokens from `SystemVariableIds` matching:
  - `SourceInUse<N>`
  - `SourceName<N>`
- build the visible catalog by:
  - optional prefix sources discovered from `SourceName*` values in driver config
  - then append the device-derived `SourceCatalog`
- fill missing slots with explicit blanks
- store as zero-based indices

Important implication:

- the current code does not use the older simple `Devices ordered by DeviceId` assumption by itself; it builds a token-space-aware catalog.

### J. Expansion device type detection

Status: `confirmed extractable now`

Output:

- set of distinct `ExpansionDevices.DeviceType` values for `RTIAddress = 0`

Current extractor does not only infer expansion model names from labels; it explicitly captures raw device type integers.

This is tested.

### K. Port extraction: relay

Status: `confirmed extractable now`

Output fields:

- `controller_device_name`
- `expander_device_type`
- `expander_name`
- `relay_name`
- `relay_type`
- `relay_mode`

Source chain:

- controller: `RTIDeviceData(RTIAddress=0) -> Devices`
- labels: `PortLabels`
- expansion context: `ExpansionDevices`

Confirmed label rules:

- internal relay labels use `LabelKey` range `-64768..-64761`
- expansion relay rows also include labels where `LabelName LIKE 'Relay %'`

Confirmed inference:

- `expander_id = LabelKey >> 16`

Current model names hard-coded in code:

- `3 -> ESC-2`
- `5 -> RCM-4`
- `6 -> XP-6`

Current limitations:

- relay type/mode are hard-coded per current known cases, not generically decoded for all expansion hardware

This is tested for internal relay state.

### L. Port extraction: MPIO/IR

Status: `confirmed extractable now`

Output fields:

- `controller_device_name`
- `expander_device_type`
- `expander_name`
- `port_number`
- `port_name`

Confirmed label rules:

- internal range: `-65536..-65529`
- XP-6 range: `65536..65543`

Confirmed inference:

- `expander_id = LabelKey >> 16`
- `port_number = (LabelKey & 65535) % 256 + 1`

This is tested.

### M. Port extraction: sense

Status: `confirmed extractable now`

Output fields:

- `controller_device_name`
- `expander_device_type`
- `expander_name`
- `port_number`
- `port_name`
- `sense_mode_state`

Confirmed label rules:

- internal range: `-65024..-65017`
- XP-6 range: `66048..66055`

Confirmed inference:

- `expander_id = LabelKey >> 16`
- `port_number = (LabelKey & 65535) - 512 + 1`

Confirmed sense mode decoding:

- `SenseModeMap` at `RTIAddress = 0`, `ExpanderId = -1`
- each internal port reads one bit from the mask
- bit `1` -> `Sense Closure`
- bit `0` -> `Sense Voltage`

This is tested.

### N. Port extraction: trigger

Status: `confirmed extractable now`

Output fields:

- `controller_device_name`
- `expander_device_type`
- `expander_name`
- `trigger_number`
- `trigger_name`

Confirmed label rules:

- current trigger range: `66307..66309`

Confirmed inference:

- `expander_id = LabelKey >> 16`
- `trigger_number = (LabelKey & 65535) - 770`

Current code assumes the observed trigger rows are XP-6-context trigger labels.

This is tested.

### O. Port extraction: RS-232

Status: `confirmed extractable now`

Output fields:

- `controller_device_name`
- `expander_device_type`
- `expander_name`
- `port_number`
- `port_name`

Confirmed label rules:

- internal range: `-65280..-65273`
- XP-6 range: `65792..65799`

Confirmed inference:

- `expander_id = LabelKey >> 16`
- `port_number = (LabelKey & 65535) - 256 + 1`

This is tested.

### P. Broader diagnostics mapping extraction

Status: `confirmed extractable now`

This comes from `ProjectDataExtractor`, not the preload contract.

Output fields:

- `DeviceId`
- `DeviceName`
- `DeviceDisplayName`
- `RtiAddress`
- `PageIndex`
- `PageId`
- `PageNameId`
- `PageName`

Important behavior:

- diagnostics mapping is built against each device's effective RTI address
- clone-aware address selection exists here too

This path is important if the new app needs both page names and raw page identifiers.

### Q. Flat report extraction

Status: `confirmed extractable now`

This comes from `ProjectDataExtractor.ProjectReport`.

Current exported entity types:

- `Room`
- `Device`
- `Port`
- `Page`
- `Source`

This is not a full schema export, but it proves a simple flat report model can be built directly from `.apex`.

### R. Button/source test index extraction

Status: `confirmed extractable now`

This comes from `ProjectDataExtractor.ProjectTest`.

Current output fields:

- `DeviceId`
- `DeviceName`
- `RtiAddress`
- `SourceLabelId`
- `SourceLabelIndex`
- `SourceLabelName`
- `PageId`
- `PageNameId`
- `PageName`
- `ButtonId`
- `ButtonTagId`
- `ButtonText`

Confirmed source chain:

- device -> `RTIDeviceData`
- pages -> `RTIDevicePageData`
- page layers -> `Layers`
- source label lookup -> `SourceLabels`
- button rows -> `RTIDeviceButtonData`

This is the strongest current evidence that button-level extraction is feasible, even though button text/tag resolution is not fully generalized in production code.

## Partially Supported or Incomplete

These areas are known and partially documented, but are not fully generalized in current implementation.

### A. Macro extraction

Status: `partially supported / incomplete`

Known now:

- `Macros` provides structural IDs and scope-like fields:
  - `MacroId`
  - `SystemMacroId`
  - `RoomId`
  - `DeviceId`
  - `ButtonTagId`
  - `OutputType`
- `MacroSteps` and `MacroStepsView` hold step sequences.
- `ButtonTagId` can sometimes be mapped through `ButtonTagNames`.

Not yet proven:

- a canonical macro name field
- a universal rule for global vs room vs source macro scoping
- a complete typed action model for every macro subtype table

### B. Generic variables beyond driver template variables

Status: `partially supported / incomplete`

Known now:

- `Variables`
- `VariableNames`
- `SystemVariableIds`
- `VariableRedirect`
- `VariableRedirectView`

Current code fully exploits only:

- `SystemVariableIds` for SYSVARREF resolution
- driver XML metadata for named variables

Not yet fully productized:

- a complete extract of user variables from `Variables + VariableNames`
- safe semantic interpretation of variable values
- redirect-aware variable tracing

### C. System Manager variables beyond source-name resolution

Status: `partially supported / incomplete`

Known now:

- System Manager uses GUID `{20186C86-446C-4FC6-89E1-1931718A169B}`.
- Current code handles only source catalog reconstruction.

Not yet proven:

- room source categories
- selected room state naming
- layer visibility variables
- popup-related variables
- time-related variables
- a complete naming taxonomy for all System Manager token families

### D. Source labels vs true source routing

Status: `partially supported / incomplete`

Known now:

- `SourceLabels` is read by `ProjectDataExtractor`.
- `SourceCatalog` is currently derived from `Devices.ControlType IN (5,6)`.

Not yet proven:

- whether `SourceLabels` can be treated as authoritative source naming across projects
- how `SourceMapping` should modify, override, or contextualize source relationships
- whether source labels are complete when blank or partially populated

### E. Buttons and UI label resolution

Status: `partially supported / incomplete`

Known now:

- relevant schema is present:
  - `Layers`
  - `LayerButtons` view
  - `RTIDeviceButtonData`
  - `ButtonTagNames`
  - `ButtonTextTags`
  - `AllButtons`
  - `AllButtonsWithTextTags`
  - `ButtonsAndListItems`
- docs show practical extraction methods for page -> layer -> button traversal

Current code only partially uses this:

- `ProjectDataExtractor` uses `Layers` and `RTIDeviceButtonData`
- it does not yet perform a complete button text/tag resolution pipeline

Not yet generalized:

- reliable final button label precedence rules across all layouts
- denormalized view selection strategy (`LayerButtons` view vs raw tables)

### F. Project metadata extraction

Status: `partially supported / incomplete`

Known now from docs:

- `JobInfo` contains company/client style fields
- `UnstructuredData` can contain:
  - cloud IDs
  - database upgrade markers
  - save history
  - timestamps
  - usernames
  - machine names
  - file paths
  - DB version traces

Current code does not extract this.

This is a strong candidate for future cross-project metadata extraction.

### G. IO maps beyond current hard-coded port logic

Status: `partially supported / incomplete`

Known now:

- `RelayModeMap`
- `RelayTypeMap`
- `SenseModeMap`
- `RS232Data`
- `RS232DataStrings`
- `IrData`
- `IrFunction`
- `ExpansionDevices`

Current code uses:

- `SenseModeMap` directly
- `ExpansionDevices` directly
- `PortLabels` directly
- hard-coded rules for several device types and ranges

Not yet generalized:

- broad support for additional expansion hardware
- authoritative decoding of all relay mask variants
- richer IR and RS-232 capability extraction beyond names

## Known Schema Paths That Exist But Are Not Fully Explored

These schema areas are present in sample `.apex` files and are relevant to a future extraction app, but are not currently exploited in a robust way.

### A. High-value unexplored or underused tables

- `Activities`
- `AutoprogrammedButtons`
- `ControllerRoomList`
- `DriverDataReference`
- `DriverScripts`
- `Events`
- `GraphProperties`
- `LicenseKeys`
- `MacroBacklight`
- `MacroBeep`
- `MacroButtonHold`
- `MacroComment`
- `MacroDelay`
- `MacroDeviceCommand`
- `MacroEventControl`
- `MacroEventTest`
- `MacroFindRemote`
- `MacroFlag`
- `MacroFunctionCall`
- `MacroLedControl`
- `MacroOSDCommand`
- `MacroPageLink`
- `MacroPopup`
- `MacroPowerSense`
- `MacroRedirect`
- `MacroRelay`
- `MacroRepeat`
- `MacroRoomOff`
- `MacroSelectRoom`
- `MacroSelectSource`
- `MacroShowMenu`
- `MacroTimeRange`
- `MacroVariableTest`
- `PageLinks`
- `RTiQAction`
- `RTiQConfig`
- `RTiQMonitoredDevices`
- `SharedLayers`
- `SourceMapping`
- `VariableRedirect`

These likely contain meaningful relationship or behavior data but are not yet converted into a stable extraction contract here.

### B. High-value unexplored or underused views

- `AllButtons`
- `AllButtonsWithTextTags`
- `AllListItems`
- `AutoprogramInfo`
- `ButtonsAndListItems`
- `ClonePageData`
- `DevicesView`
- `LayerButtons`
- `MacroPageLinkView`
- `MacroRedirectView`
- `MacroRoomView`
- `MacroStepsView`
- `PageLinkView`
- `PagesView`
- `RoomMacrosWithRedirect`
- `RoomVariablesWithRedirect`
- `VariableRedirectView`
- `VariableRoomView`

These may provide faster or cleaner extraction paths than raw table joins, but the current code mostly avoids relying on them.

### C. Empty-or-conditional schema paths

Some schema objects may be empty in one project and populated in another. They should not be dismissed as irrelevant only because a sample file leaves them empty.

Examples already called out in docs:

- `Events`
- `NetworkConfig`
- `NetworkDefaults`
- `RS232Data`
- `RS232DataStrings`
- `IrData`
- `IrFunction`
- `Sounds`
- `WlanConfig`
- `WlanConfigDefaults`

Interpretation:

- these are known paths
- they are not proven universally useful yet
- they may become critical in a different `.apex` corpus

## Naming and Relationship Rules Codex Must Preserve

These are the most important exact naming and relationship rules for future extraction work.

### A. Identity keys

- `DeviceId` is not the same thing as `RTIAddress`.
- `RTIAddress` is the device-scoped address used by pages, ports, source labels, and other address-bound structures.
- `DriverDeviceId` is not the same thing as `DeviceId`.
- `PageId` is not the same thing as `PageOrder`.
- `PageNameId` is a lookup key, not the page index.

### B. Stable high-value joins

- controller page context: `RTIDeviceData.RTIAddress -> RTIDevicePageData.RTIAddress`
- page name: `RTIDevicePageData.PageNameId -> PageNames.PageNameId`
- source device on page: `RTIDevicePageData.SourceDeviceId -> Devices.DeviceId`
- room from source device: `Devices.RoomId -> Rooms.RoomId`
- driver instance: `DriverConfig.DriverDeviceId -> DriverData.DriverDeviceId`
- driver device: `DriverData.DeviceId -> Devices.DeviceId`
- SYSVARREF registry: `SystemVariableIds.SysVarRef`
- driver variable names: `DriverData.SystemVariables` XML

### C. Important extraction conventions

- Normalize SYSVARREF keys with `SYSVARREF:` prefix when storing lookup keys.
- Prefer `DisplayName`, but fall back to `Name` when display name is blank.
- When joins fail, preserve blanks or null-like states explicitly.
- Do not invent names for unmapped pages, variables, sources, or macro entities.

## Second Pass: Macros, Buttons, and UI Structure

This section narrows the scope to UI composition and action wiring only.

All findings below use the same classification model:

- `confirmed extractable now`
- `partially supported / incomplete`
- `known schema paths that exist but are not fully explored`

### Confirmed Extractable Now

#### A. Button instance geometry and raw visual properties

Status: `confirmed extractable now`

`RTIDeviceButtonData` is a rich button-instance table, not just a button ID list.

Confirmed extractable fields include:

- identity:
  - `ButtonId`
  - `SharedLayerId`
  - `ButtonOrder`
  - `ButtonTagId`
- text:
  - `Text`
- positioning:
  - `ButtonTop`
  - `ButtonLeft`
  - `ButtonHeight`
  - `ButtonWidth`
  - alternate geometry fields (`ButtonTopAlt`, `ButtonLeftAlt`, `ButtonHeightAlt`, `ButtonWidthAlt`)
- styling/state:
  - `FrameNumber`
  - `ButtonStyle`
  - color fields
  - font/alignment fields
- command payload storage:
  - `Command` (BLOB)
  - `TWParams` (BLOB)

Practical implication:

- a new app can reconstruct a button layout grid and preserve raw visual/button metadata without decoding command blobs yet.

#### B. Page-to-layer-to-button traversal

Status: `confirmed extractable now`

The relationship chain is explicit and populated:

- `RTIDevicePageData.PageId`
- `Layers.PageId`
- `Layers.SharedLayerId`
- `RTIDeviceButtonData.SharedLayerId`

This means a page's button set can be reconstructed by joining:

- page
- layer
- shared layer
- button rows

Current repository evidence:

- `ProjectDataExtractor` already uses this path in a reduced form
- `AllButtons` and `LayerButtons` views encode the same chain more directly

#### C. Shared layer metadata

Status: `confirmed extractable now`

`SharedLayers` provides reusable UI-layer metadata.

Confirmed extractable fields:

- `SharedLayerId`
- `Name`
- `ProductId`
- portrait and landscape dimensions
- `IsKeypadLayer`
- `IsShared`

Observed in current sample:

- `SharedLayers` is populated
- some rows are explicitly marked shared
- some rows are explicitly marked keypad layers

Practical implication:

- the UI can be modeled as reusable layer templates, not just flattened per-page controls.

#### D. Layer-level visibility and viewport structure

Status: `confirmed extractable now`

`Layers` provides more than page grouping.

Confirmed extractable fields:

- `LayerId`
- `PageId`
- `SourceId`
- `SharedLayerId`
- `LayerOrder`
- `IsVisible`
- `VisibilityVariable`
- `IsLocked`
- `ViewPortButtonId`
- `RoomId`

Observed in current sample:

- many layers have non-empty `VisibilityVariable`
- many layers use `ViewPortButtonId`

Practical implication:

- dynamic layer visibility is explicitly modeled
- viewport-driven nested UI composition exists and is not hypothetical

#### E. Button tag naming

Status: `confirmed extractable now`

`ButtonTagNames` is a direct tag-name registry.

Confirmed extractable fields:

- `ButtonTagId`
- `ButtonTagName`

This is the primary stable path for human-readable tag names when a button has a non-negative tag ID.

#### F. Button text-tag indirection

Status: `confirmed extractable now`

`ButtonTextTags` is a second explicit button-label path.

Confirmed extractable fields:

- `ButtonTextTagId`
- `ButtonId`
- `ButtonTagId`

Confirmed meaning from docs and view definitions:

- a button can acquire text through `ButtonTextTags` even when the displayed text is not stored directly as `RTIDeviceButtonData.Text`

Practical implication:

- there are at least two parallel label mechanisms:
  - direct literal text
  - tag-based text indirection

#### G. Denormalized button views already encode useful UI relationships

Status: `confirmed extractable now`

The sample `.apex` contains populated views that already flatten button/UI relationships:

- `LayerButtons`
- `AllButtons`
- `AllButtonsWithTextTags`
- `ButtonsAndListItems`

These are not theoretical. They are populated in the sample corpus and carry relationship logic.

What they confirm:

- button rows can be lifted into page context
- button tag names can be prejoined
- source context can be carried with buttons
- redirect context can be carried with buttons
- list items are treated as button-like UI entities in some views

#### H. `LayerButtons` exposes action-oriented button metadata

Status: `confirmed extractable now`

The `LayerButtons` view is especially important because it already joins raw button rows to action relationships.

Confirmed exposed columns include:

- all `RTIDeviceButtonData` fields
- `DeviceMacroId`
- `OutputType`
- `GlobalMacroId`
- `GlobalMacroRouting`
- `GlobalMacroExpanderId`
- `LinkType`
- `PageLinkId`
- `LinkPageId`
- `DeviceVariableId`
- `GlobalVariableId`
- `LayerId`
- `SourceId`
- `VisibilityVariable`
- `RoomId`
- `ButtonTagName`
- `DeviceId`

Observed in current sample:

- many rows have `GlobalMacroId`
- hundreds have `PageLinkId` and `LinkPageId`

Practical implication:

- the DB already contains a prejoined button-to-action graph
- a new app can likely use `LayerButtons` as the fastest path for UI/action discovery

#### I. Page link extraction

Status: `confirmed extractable now`

`PageLinks` and `PageLinkView` provide explicit page-navigation mappings.

Confirmed extractable fields:

- `PageLinkId`
- `DeviceId`
- `ButtonTagId`
- `LinkType`
- `PageId`

`PageLinkView` adds:

- `PageName`
- `PageGroupName`

Practical implication:

- page navigation is not only implicit in button command blobs
- part of it is explicitly modeled through tag-linked page-link tables

#### J. Page views with display-ready page metadata

Status: `confirmed extractable now`

`PagesView` is a populated denormalized page view.

Confirmed additions beyond `RTIDevicePageData`:

- default background fields from `RTIDevicePageDefaults`
- resolved `PageName`
- `RoomId` from the source device

Practical implication:

- page rendering metadata can be extracted from the view without reproducing all joins by hand

#### K. Macro identity and step sequencing

Status: `confirmed extractable now`

The core macro structure is explicit and populated.

`Macros` confirms:

- `MacroId`
- `SystemMacroId`
- `RoomId`
- `DeviceId`
- `ButtonTagId`
- `OutputType`

`MacroSteps` confirms:

- `MacroStepId`
- `MacroId`
- `StepIndex`
- `Type`
- `Level`
- `InElseSection`

Practical implication:

- macro sequencing is formally represented
- branches/conditional structure are at least partially represented through `Level` and `InElseSection`

#### L. Macro action detail is already flattened in `MacroStepsView`

Status: `confirmed extractable now`

`MacroStepsView` is a major evidence source because it joins `MacroSteps` to many specialized action tables.

Confirmed exposed action families include fields from:

- comments
- delays
- device commands
- IR data and IR function metadata
- RS-232 strings and serial settings
- flags
- button hold conditions
- page-link actions
- repeat actions
- power-sense actions
- beep actions
- relay actions
- function-call / macro-call actions
- time ranges
- event control
- event tests
- find-remote
- variable tests
- LED control
- backlight control
- room selection
- source selection
- room off
- OSD commands
- popup actions

Practical implication:

- the schema already supports a typed macro action model
- a future parser can classify steps by `Type` and then read the populated column family for that step

### Partially Supported / Incomplete

#### A. Final button label resolution rules

Status: `partially supported / incomplete`

The repository proves multiple label channels but does not yet define one final, universal precedence rule.

Known channels:

- direct button literal text in `RTIDeviceButtonData.Text`
- tag name via `ButtonTagNames`
- text-tag indirection via `ButtonTextTags`
- denormalized tag text through `AllButtonsWithTextTags`

Known guidance from docs:

- if `ButtonTagId < 0`, direct `Text` is often the usable label
- if `ButtonTagId >= 0`, tag-based text may be the correct source

Best current precedence model for extraction:

1. If `ButtonTagId < 0` and `ButtonTextTags` has rows for the same `ButtonId`, treat `RTIDeviceButtonData.Text` as a template string and use `ButtonTextTags -> ButtonTagNames` to enumerate the placeholder tags embedded in that text.
2. If `ButtonTagId < 0` and there are no `ButtonTextTags` rows, use direct `Text` as the button label.
3. If `ButtonTagId >= 0` and `ButtonTagName` is present, use `ButtonTagName` as the primary label.
4. If `ButtonTagId >= 0` but no tag name resolves, mark the label unresolved instead of fabricating one.

Evidence from the current sample:

- many buttons with `ButtonTagId < 0` also have non-empty direct text
- some buttons with `ButtonTagId < 0` also have `ButtonTextTags`, proving that direct text can be a tag-template string rather than final literal text
- buttons with non-negative tag IDs commonly resolve through `ButtonTagNames`

Not yet proven:

- the exact final precedence rule across all button styles, text-tag cases, and list items

#### B. Button action classification

Status: `partially supported / incomplete`

`LayerButtons` shows that buttons may connect to:

- device macros
- global macros
- page links
- variables

Not yet generalized:

- a single stable classification rule for "this button does X"
- how to rank competing action signals if multiple are populated
- whether raw `Command` blobs contain actions not represented by the joined views

#### C. Macro naming

Status: `partially supported / incomplete`

The macro graph is rich, but a canonical human-readable macro name is still not proven.

Current best-known candidates:

- `Macros.ButtonTagId -> ButtonTagNames.ButtonTagName`
- action semantics inferred from the first or dominant step in `MacroStepsView`

Neither is yet proven to be a universal macro name.

#### D. Macro step type decoding

Status: `partially supported / incomplete`

`MacroSteps.Type` is clearly meaningful and heavily used, but the repository does not currently define a complete authoritative numeric type map.

What is known:

- step types are enumerable and frequent
- `MacroStepsView` exposes different column families depending on the step subtype

Best current inferred type map from populated `MacroStepsView` rows:

- `Type 1` -> device command
  Evidence:
  - `DeviceId` and `Function` are populated on nearly all rows
- `Type 3` -> delay
  Evidence:
  - `Delay` is populated
- `Type 8` -> page-link / page-target navigation step
  Evidence:
  - `TargetPageId`, `TargetPageIndex`, and `TargetPageNameId` are populated
  - those values come from `MacroPageLinkView`
- `Type 13` -> relay action
  Evidence:
  - `RelayPort` and `RelayCommand` are populated
- `Type 14` -> macro call / function call
  Evidence:
  - `CommandMacroId` is populated
- `Type 15` -> flag action
  Evidence:
  - `FlagIndex` and `FlagType` are populated
- `Type 16` -> flag-related action
  Evidence:
  - `FlagIndex` is populated, but `FlagType` is not consistently populated
- `Type 17` -> comment
  Evidence:
  - `CommentText` is populated
- `Type 22` -> variable test
  Evidence:
  - `VariableDeviceId` and `Variable` are populated
- `Type 24` -> select room
  Evidence:
  - `SelectRoomId` is populated
- `Type 26` -> select source
  Evidence:
  - `SelectSourceId` and `SelectSourceRoomId` are populated
- `Type 27` -> room off
  Evidence:
  - `RoomOffId` is populated

Types observed but not decoded in the current sample:

- `Type 7`
- `Type 28`
- `Type 29`

What is not yet proven:

- a complete mapping such as `Type N -> semantic step class`
- whether the inferred type meanings above are stable across all `.apex` versions and project types

#### E. Page-link semantics

Status: `partially supported / incomplete`

`PageLinks.LinkType` is clearly meaningful.

What is proven:

- it exists
- it is used in `PageLinkView`
- buttons can bind to page links by tag and device

What is not proven:

- the complete semantic meaning of each `LinkType` value
- whether `PageLinks.PageId` always means target page versus group/device semantics

Best current navigation interpretation:

- rows where `LayerButtons.PageLinkId` is populated represent explicit tag-based page navigation wiring
- rows where `MacroStepsView.Type = 8` represent macro-driven navigation wiring
- `PageLinkView` is the easiest direct source for button-tag-to-page targets
- `MacroPageLinkView` is the easiest direct source for macro-step-to-page targets

Observed constraints:

- some `PageLinkView` rows resolve a direct `PageName`
- some rows do not resolve `PageName`, which means the target may be device/group-oriented or otherwise requires extra interpretation

#### F. Redirect-aware UI resolution

Status: `partially supported / incomplete`

The denormalized views prove redirect concepts exist:

- `ButtonsAndListItems` carries `MacroRedirect.SourceId` as `RedirectDeviceId`
- `AllButtonsWithTextTags` carries `VariableRedirect.SourceId` as `RedirectDeviceId`

Not yet generalized:

- when redirect context should override base `SourceDeviceId`
- whether redirect semantics should be modeled as source remapping, UI scoping, or variable scoping

#### G. View-first versus raw-table-first extraction strategy

Status: `partially supported / incomplete`

There are two viable approaches:

- build from raw tables (`RTIDeviceButtonData`, `Layers`, `PageLinks`, `Macros`, etc.)
- consume denormalized views (`LayerButtons`, `AllButtons`, `MacroStepsView`, `PagesView`, etc.)

The repository currently mixes both ideas but does not define when each is safer.

This matters because:

- views are richer and faster for discovery
- raw tables may be safer if view logic differs across `.apex` versions

#### H. Page navigation tree construction

Status: `partially supported / incomplete`

A practical navigation graph can already be built, but it is not yet fully lossless or fully semantic.

Best current graph model:

1. Node type: page
   Fields:
   - `PageId`
   - `PageName`
   - `RTIAddress`
   - `PageOrder`
2. Edge type: explicit button page link
   Source:
   - `LayerButtons.PageLinkId`
   - `LayerButtons.ButtonId`
   - `LayerButtons.ButtonTagId`
   - `LayerButtons.ButtonTagName`
   - `LayerButtons.LinkPageId`
   - `PageLinkView`
3. Edge type: macro-driven page link
   Source:
   - button -> `DeviceMacroId` or `GlobalMacroId` from `LayerButtons`
   - macro step -> `MacroStepsView` rows where `Type = 8`
   - target pages -> `TargetPageId`, `TargetPageIndex`, `TargetPageNameId`

What this supports now:

- page-to-page navigation edges from explicit page links
- button-to-page navigation edges
- button-to-macro-to-page navigation edges

What still blocks a complete final tree:

- unresolved `LinkType` semantics
- unresolved global versus device macro precedence
- multi-target macro page steps, where one macro step expands to many target pages

### Known Schema Paths That Exist But Are Not Fully Explored

#### A. Macro subtype tables

Status: `known schema paths that exist but are not fully explored`

The schema has many specialized macro tables that likely map to distinct step types:

- `MacroBacklight`
- `MacroBeep`
- `MacroButtonHold`
- `MacroComment`
- `MacroDelay`
- `MacroDeviceCommand`
- `MacroEventControl`
- `MacroEventTest`
- `MacroFindRemote`
- `MacroFlag`
- `MacroFunctionCall`
- `MacroLedControl`
- `MacroOSDCommand`
- `MacroPageLink`
- `MacroPopup`
- `MacroPowerSense`
- `MacroRedirect`
- `MacroRelay`
- `MacroRepeat`
- `MacroRoomOff`
- `MacroSelectRoom`
- `MacroSelectSource`
- `MacroShowMenu`
- `MacroTimeRange`
- `MacroVariableTest`

These are the highest-value path for turning macros into a typed action language.

#### B. UI/list and button aggregation views

Status: `known schema paths that exist but are not fully explored`

These populated views deserve dedicated analysis:

- `AllButtons`
- `AllButtonsWithTextTags`
- `AllListItems`
- `ButtonsAndListItems`
- `LayerButtons`

They likely encode the intended RTI-side interpretation of:

- button scope
- list-item scope
- text-tag expansion
- source redirects
- macro redirects

#### C. Page and navigation structure views

Status: `known schema paths that exist but are not fully explored`

Relevant paths:

- `PagesView`
- `PageLinks`
- `PageLinkView`
- `ClonePageData`
- `SharedLayers`

These likely allow a stronger UI navigation model than the current code uses.

#### D. Variable-aware UI state paths

Status: `known schema paths that exist but are not fully explored`

Relevant paths:

- `Variables`
- `VariableRedirect`
- `VariableRedirectView`
- `RoomVariablesWithRedirect`
- `VariableRoomView`

These likely matter for:

- layer visibility
- dynamic labels
- source redirection
- page state that changes by room/source context

### Immediate Codex Guidance For This Area

If Codex is asked to extract UI structure from an `.apex` file, the best current sequence is:

1. Start with `PagesView` for page metadata.
2. Join `Layers` and `SharedLayers` to understand page composition and reusable layers.
3. Use `LayerButtons` as the first-choice button/action view.
4. Fall back to `RTIDeviceButtonData` when raw geometry, text, blob fields, or view discrepancies matter.
5. Use `PageLinkView` for explicit navigation links.
6. Use `MacroStepsView` for typed macro step discovery.
7. Treat button labels as unresolved until all applicable sources have been checked (`Text`, `ButtonTagNames`, `ButtonTextTags`, denormalized views).

## Practical Scope for a New App

If the new application needs a stable first-version `.apex` extractor, the safest currently-proven scope is:

- devices
- rooms
- RTI address mappings
- page identities and page names
- page-to-source-to-room relationships
- driver config values
- SYSVARREF registry and variable names
- driver template variable catalog
- source catalogs
- expansion device types
- relay/MPIO/sense/trigger/RS-232 port names and basic derived metadata
- button/source test indexing if raw button traversal is needed

## Best Next Expansion Areas

If the goal is to teach Codex to find more specific naming and relationships, the highest-value next schema investigations are:

1. Macro naming and macro step typing across all `Macro*` tables and views.
2. Full button label resolution using `LayerButtons`, `ButtonTagNames`, and `ButtonTextTags`.
3. True source routing semantics using `SourceLabels` plus `SourceMapping`.
4. System Manager variable families beyond source-name tokens.
5. Project metadata extraction from `UnstructuredData` and `JobInfo`.
6. Denormalized view audit to decide when views are safer than raw table joins.
7. IR and RS-232 functional extraction, not just port naming.

## Current Confidence Summary

- `confirmed extractable now`: strong
- `partially supported / incomplete`: significant and useful
- `suspected but not yet implemented`: extensive, especially in macro/UI/view metadata

The repository already proves that `.apex` can support far more than simple page-name mapping. The current codebase reliably extracts page, room, source, driver, sysvar, and port data now, while the schema and archive docs show several larger unexplored paths for macros, UI/button structures, variables, routing, and project metadata.
