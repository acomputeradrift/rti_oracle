# APEX Project Info Summary (for Codex ingestion)

This summary captures what we learned about RTI `.apex` files and their embedded SQLite database so Codex can build modules that:
1) extract and index project metadata for mapping against diagnostics,
2) extract UI/button metadata for a freestanding testing app, and
3) generate a project report (Excel) with key entities and IDs.

This is **instructional guidance**, not code.

---

## What an `.apex` file is (proven)
- An `.apex` file is a **SQLite database**.
- Example file: `TEST - System Manager v10.apex` is SQLite 3.x, UTF‑8, with a schema of ~90 tables.

You can query it directly with SQLite tools to extract all project metadata.

---

## High‑value tables and what they represent

### Core devices / drivers
- `Devices`: device inventory (DeviceId, RoomId, Name, Manufacturer, Model, Type, etc.).
- `DriverConfig`, `DriverData`, `DriverDataReference`, `DriverScripts`: driver configuration and script resources.
- `RTIDeviceData`: ties a device to an RTIAddress (device identifier used elsewhere).
- `RTIDevicePageData`, `RTIDevicePageDefaults`, `RTIDeviceButtonData`: device‑specific page/button metadata.

### Rooms and sources
- `Rooms`: RoomId, Name, HomePageId, RoomOrder.
- `SourceLabels`: SourceLabelId, RTIAddress, LabelIndex, LabelName.
- `SourceMapping`: mapping between sources and devices/rooms (project‑specific source routing).
- `SourceEvents`: events tied to sources.
- `PortLabels`: named ports (used for mapping inputs/outputs).

### Pages and UI structures
- `PageNames`: PageNameId, PageName (human‑readable name).
- `PagesView`: a convenience view that lists pages.
- `Layers`: layers on a page (LayerId, PageId, SourceId, etc.).
- `LayerButtons`: button instances on layers (ButtonId, ButtonTagId, Text, layout, etc.).
- `AllButtons`, `AllButtonsWithTextTags`, `ButtonsAndListItems`: denormalized button views.
- `ButtonTagNames`: ButtonTagId → ButtonTagName.
- `ButtonTextTags`: ButtonId + ButtonTagId (used for text mapping).

### Variables
- `Variables`: variable definitions and values.
- `VariableNames`: variable metadata, lookup by name.
- `VariableRedirect`, `VariableRedirectView`: routing/redirect rules.
- `SystemVariableIds`: system‑defined variables.

### Macros and events
- `Macros`: macro definitions (MacroId and metadata).
- `MacroSteps`, `MacroStepsView`: macro step lists (ordered actions).
- `MacroPageLink`, `MacroPageLinkView`: page links tied to macros.
- `RoomEvents`, `SourceEvents`, `Events`: event tables.

### Diagnostics / RTiQ / IO maps
- `RTiQConfig`: RTiQ configuration.
- `RelayModeMap`, `RelayTypeMap`, `SenseModeMap`: IO maps.

---

## Proven example extractions

### Devices (from `Devices`)
- Example: `RTiPanel (iPhone X or newer)` (DeviceId 6)
- Example: `XP-8v` (DeviceId 2)

### Rooms (from `Rooms`)
- `Room 1…Room 5`, plus `Global`.

### Page association for RTiPanel (DeviceId 6)
- `RTIDeviceData` maps DeviceId 6 → `RTIAddress = 1`.
- `RTIDevicePageData` maps `RTIAddress=1` to pages with `PageId` and `PageNameId`.
- Example: first page for RTiPanel is `PageId=1`, `PageNameId=258`, which resolves to `PageNames.PageName = "Home (Global)"`.

### Button tag names for a page
- `LayerButtons` (filtered by `PageId`) gives ButtonId + ButtonTagId (and Text, if present).
- `ButtonTagNames` maps ButtonTagId → ButtonTagName.
- `ButtonTextTags` links ButtonId to text tags for rendering.

Example (RTiPanel, PageId 1):
- Tagged buttons: `Room: Room 1`, `Activity: Audio Source`, `OFF - All Rooms`, etc.
- Untagged buttons with text were found via `LayerButtons.Text` (ButtonTagId < 0), e.g. `Room Status`, `Room 1`…`Room 5`.

---

## How to map diagnostics to project metadata

Goal: Convert a runtime log like:
- `Change to page 20 on device 'RTiPanel (iPhone X or newer)'`
into:
- `Change to page "Home (Room 2)" on device 'RTiPanel (iPhone X or newer)'`

Suggested mapping flow:
1) Resolve device name to `DeviceId` from `Devices`.
2) Resolve `DeviceId` to `RTIAddress` via `RTIDeviceData`.
3) Use `RTIDevicePageData` to map `PageId`/`PageNameId` for that device.
4) Resolve `PageNameId` → `PageName` via `PageNames`.

This gives a stable mapping from runtime IDs to human‑readable labels.

---

## Extracting UI/Button metadata for a testing application

To build a freestanding UI tester, collect for **each device** and **each page**:
- Page identity: `PageId`, `PageNameId`, `PageName`.
- Button identity: `ButtonId` (unique per button instance).
- Button tag: `ButtonTagId` + `ButtonTagName`.
- Button text:
  - If `ButtonTagId >= 0`, use `ButtonTextTags` + `ButtonTagNames`.
  - If `ButtonTagId < 0`, use `LayerButtons.Text` (explicit text).
- Positioning (if needed): `LayerButtons.ButtonTop`, `ButtonLeft`, `ButtonWidth`, `ButtonHeight`.

Recommended extraction chain:
1) `RTIDevicePageData` → pages for a device (via RTIAddress).
2) `Layers` → all layers per page (LayerId).
3) `LayerButtons` → all buttons per layer.
4) `ButtonTagNames` and `ButtonTextTags` → resolve labels.

Store output as a nested structure:
- Device → Page → Button[]
  - each Button has `{button_id, tag_id, tag_name, text, position...}`.

---

## Project report (Excel‑ready) scope

For a comprehensive report, include these entity lists with IDs:

- Devices: DeviceId, Name, Manufacturer, Model, Type, RoomId
- Rooms: RoomId, Name, HomePageId
- Pages: PageId, PageNameId, PageName, RTIAddress, PageOrder
- Sources: SourceLabelId, LabelIndex, LabelName, RTIAddress
- Variables: VariableId, VariableName, Value (from `Variables`, `VariableNames`)
- Macros: MacroId, Name (from `Macros`, `MacroSteps`)
- Events: EventId, EventName (from `Events`, `RoomEvents`, `SourceEvents`)
- Button tags/text: ButtonId, ButtonTagId, ButtonTagName, Text, PageId, Device
- Ports: PortLabels

Use IDs from tables to preserve referential mapping for later correlation with diagnostic logs.

---

## Recommended output structure for Codex ingestion

1) **Entity Maps**
- `devices_by_id`, `devices_by_name`
- `rooms_by_id`
- `pages_by_device` (device → list of pages)
- `page_name_by_id`
- `sources_by_id` and `sources_by_label`
- `variables_by_id`, `variables_by_name`
- `macros_by_id`, `macro_steps_by_macro_id`
- `button_tags_by_id` and `button_text_by_button_id`

2) **UI Index**
- Nested structure: `device_id → page_id → [buttons]` with IDs, tags, and text.

3) **Reporting Tables**
- Flat tables for Excel export with foreign keys preserved.

---

## Known limits from our data
- Some tables are empty in the sample project (`NetworkConfig`, `RS232Data`, `IR`, etc.).
- Driver log‑level flags are not stored in the `.apex` file in an obvious table; those were observed in the ID11 push payload and appear to be embedded project resources.
- `NetworkConfig`/`WlanConfig` do not store device IPs in the sampled projects; device IP fields live in `RTIDeviceData`.

---

## Processor IP Address Extraction (XP main/expansion)

### Where the IP lives
- IP settings are stored per device in `RTIDeviceData` (not `NetworkConfig`/`WlanConfig`).
- Relevant columns for adapter 0:
  - `NetUseDhcp` (0 = static, 1 = DHCP)
  - `IpaddressAdapter0`, `NetmaskAdapter0`, `GatewayAdapter0`, `Dns1Adapter0`, `Dns2Adapter0`
- Relevant columns for adapter 1 (if used):
  - `DhcpEnabledAdapter1` (0 = static, 1 = DHCP)
  - `IpAddressAdapter1`, `NetmaskAdapter1`, `GatewayAdapter1`, `Dns1_Adapter1`, `Dns2_Adapter1`

### Trust rule (required)
- Only trust `IpaddressAdapter0` when `NetUseDhcp = 0`.
- Only trust `IpAddressAdapter1` when `DhcpEnabledAdapter1 = 0`.
- If DHCP is enabled, the stored IP may be stale or a default and must be treated as **unknown**.

### IP encoding
- IP fields are stored as signed 32‑bit integers.
- Convert to unsigned, then to dotted IPv4 using **big‑endian** order.
  - Example: `171974882` → `10.64.32.226`
  - Example: `-256` → `255.255.255.0`

### How to locate XP processors
- XP devices usually appear in `Devices` with names/models like `XP‑8`, `XP‑8v`, `XP‑3`, etc.
- Query example to locate XP devices:
  - `SELECT DeviceId, Name, Manufacturer, Model, Type FROM Devices WHERE Name LIKE 'XP-%' OR Model LIKE 'XP-%' OR Type LIKE 'XP-%';`
- Join those `DeviceId` values to `RTIDeviceData` to read the IP fields.

### Main vs expansion processor (current evidence)
- The `.apex` schema does not explicitly label an XP device as "main" vs "expansion."
- In sampled data, expansion hardware appears in `ExpansionDevices` (no XP rows there), while the XP controller appears as a `Devices` row.
- If a project includes multiple XP devices, they will appear as multiple `Devices` rows; assign "main" vs "expansion" only if a reliable, explicit marker exists in the DB.

---

## Practical next steps (for module authors)

- Treat the `.apex` as the authoritative project DB for **all names/IDs**.
- Build a robust resolver that can map runtime identifiers (page IDs, button IDs, device IDs) to human‑readable names.
- If multiple devices share pages, always map by `RTIAddress` and `RTIDevicePageData` to avoid collisions.

## Proven Data — APEX DB Exploration

## Global Rules
- Never assume schema stability
- Never infer relationships that are not directly observable
- Every attempt must be made to link an internal ID/index to a human-readable name
- Always record:
  - what was queried
  - what was returned
  - what was missing
- Explicitly note empty tables, nullable fields, duplicate rows
- Ambiguity is a blocking finding, not a workaround

---

## 1. Project-Level Metadata
**Proven**
- Inventory all tables in the SQLite DB
  - Query: `SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;`
  - Returned: 84 tables (includes `sqlite_sequence`, `sqlite_stat1`)
- Record table names and row counts
  - Query: `SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;` then `SELECT COUNT(*) FROM "<table>";`
  - Returned: key counts (non-exhaustive)
    - Devices 34, Rooms 6, RTIDeviceData 3, RTIDevicePageData 21, PageNames 44, Layers 262, LayerButtons 1256, RTIDeviceButtonData 148
    - Variables 86, VariableNames 768, Macros 252, MacroSteps 942, RoomEvents 35, SourceEvents 29
  - Empty tables (0 rows): BaseStation, ButtonBitmaps, ClockDefaults, Events, ExpansionDevices, IrData, IrFunction, LocationDefaults, NetworkConfig, NetworkDefaults, PageLinks, RS232Data, RS232DataStrings, RTiQAction, RTiQMonitoredDevices, ScrollingList, ScrollingListItems, SortedPages, Sounds, TemplateLayerExclusions, TemplateOptions, TemplateOptionsSupported, TemplatePageExclusions, WlanConfig, WlanConfigDefaults
- Identify any table or field that may represent:
  - project name
  - project version
  - creation/modification timestamps
  - Query: `PRAGMA table_info(JobInfo);` and `SELECT * FROM JobInfo;`
  - Returned: `JobInfo` has company/client fields; values are mostly empty in this DB
  - Query: `PRAGMA table_info(UnstructuredData);` and `SELECT Key, ValueText, length(ValueBlob) AS ValueBlobLen FROM UnstructuredData ORDER BY Key;`
  - Returned:
    - `CloudSystemID` present (UUID)
    - `DatabaseUpgrade_135/136/137` entries with timestamps, time zones, and ID11 version/build strings
    - `SaveNumber1-10` entries with timestamps, machine name, user, file paths, and DB version
- Determine whether project metadata is explicit, implicit, or absent
  - Proven: metadata is implicit via `UnstructuredData` (save history + DB upgrade log), explicit project name/version fields not found in `JobInfo`

---

## 2. Devices
**Proven**
- Query `Devices`
  - Query: `SELECT DeviceId, RoomId, Name, Manufacturer, Type, Model, DisplayName FROM Devices ORDER BY DeviceId;`
  - Returned: 34 rows, examples include:
    - DeviceId 2: `XP-8v` (Manufacturer RTI, Model XP-3)
    - DeviceId 6: `RTiPanel (iPhone X or newer)` (Manufacturer RTI)
    - DeviceId 37: `T2i` (Manufacturer RTI, DisplayName `T2i (Global)`)
    - Devices for each room: `Home (Room 1..5)`, `Audio/Video Source (Room 1..5)`, `Other Source (Room 1..5)`
    - Virtual devices: `RTI Virtual Multiroom Amp` (DisplayName `Audio Matrix`), `RTI Virtual HDMI Matrix` (DisplayName `Video Matrix`)
- Enumerate all columns
  - Query: `PRAGMA table_info(Devices);`
  - Returned columns: DeviceId (PK), RoomId, DisplayOrder, ControlType, Name, Manufacturer, Type, Model, Comment, HasCompositeController, SourceType, DisplayName
- Verify uniqueness of `DeviceId`
  - Query: `SELECT COUNT(*) AS total, COUNT(DISTINCT DeviceId) AS distinct_ids FROM Devices;`
  - Returned: 34 total, 34 distinct
- Identify device types (hardware, panel, virtual)
  - Returned (observable): hardware controllers (XP-8v), panels (RTiPanel, T2i), virtual matrices (RTI Virtual Multiroom Amp/HDMI Matrix)
- Determine device scope
  - Query: `SELECT RoomId, COUNT(*) AS device_count FROM Devices GROUP BY RoomId ORDER BY RoomId;`
  - Returned: RoomId 0 has 8 devices (global), RoomId 1..5 each have 5-6 devices
- Attempt to link:
  - DeviceId → Device Name → Device Scope
  - Proven by DeviceId list + RoomId grouping above
- Driver metadata and settings
  - Query: `PRAGMA table_info(DriverData);` and `PRAGMA table_info(DriverConfig);`
  - Returned: Driver metadata stored in `DriverData` (DriverId GUID, DriverVersion, Author, DeviceDescription, ConfigItems XML)
  - Query: `SELECT DriverDeviceId, DeviceId, Enabled, DriverId, DriverVersion, Author, DeviceDescription, ConfigItems FROM DriverData ORDER BY DriverDeviceId;`
  - Returned: 2 driver rows
    - DriverDeviceId 1 → DeviceId 24 (RTI Virtual Multiroom Amp), DriverId `{5C053AC3-B64B-4E29-B79B-2AD2B5032EC1}`, DriverVersion `1.0`, Author `Remote Technologies Inc.`, ConfigItems XML defines Zone/Source settings
    - DriverDeviceId 2 → DeviceId 30 (RTI Virtual HDMI Matrix), DriverId `{8F8977DF-761A-48FE-9A77-4904267778B5}`, DriverVersion `1.0`, Author `Remote Technologies Inc.`, ConfigItems XML defines Input/Output settings and Debug Trace
  - Query: `SELECT DriverDeviceId, Name, Value FROM DriverConfig ORDER BY DriverDeviceId, Name;`
  - Returned:
    - DriverDeviceId 1 settings: `MaxSources`, `MaxZones`, `SourceName0..7`, `ZoneName0..7`
    - DriverDeviceId 2 settings: `Inputs`, `Outputs`, `Output1name..8`, `input1name..8`, `DebugTrace`

---

## 3. Rooms
**Proven**
- Query `Rooms`
  - Query: `SELECT RoomId, Name, HomePageId, RoomOrder FROM Rooms ORDER BY RoomId;`
  - Returned: 6 rows
    - RoomId 0 Global HomePageId 1
    - RoomId 1..5 named `Room 1`..`Room 5`, HomePageId 7..11
- Enumerate all columns
  - Query: `PRAGMA table_info(Rooms);`
  - Returned columns: RoomId (PK), Name, HomePageId, RoomOrder
- Determine how rooms and devices are linked
  - Observed: `Devices.RoomId` points to `Rooms.RoomId` via FK in schema
- Attempt to resolve:
  - RoomId → Room Name → Associated DeviceIds
  - Query: `SELECT RoomId, COUNT(*) AS device_count FROM Devices GROUP BY RoomId ORDER BY RoomId;`
  - Returned device counts per room (RoomId 0..5)

---

## 4. RTI Identity Model
**Proven**
- Determine what an RTIDevice represents
  - Observed: `RTIDeviceData` maps `RTIAddress` to `DeviceId`
- Determine what an RTIAddress represents
  - Observed: `RTIAddress` is the primary key in `RTIDeviceData` and is referenced by multiple tables (e.g., `RTIDevicePageData`, `SourceLabels`, `SourceMapping`)
- Identify which tables use:
  - DeviceId: `Devices`, `RTIDeviceData`, `Variables`, `Macros`, `PageLinks`, `Activities`, etc.
  - RTIAddress: `RTIDeviceData`, `RTIDevicePageData`, `RTIDevicePageDefaults`, `SourceLabels`, `SourceMapping`, `PortLabels`, `Relay*Map`, `SenseModeMap`, `WlanConfig`, `Sounds`, `ExpansionDevices`
- Determine whether RTIAddress is:
  - unique globally: Yes, `RTIAddress` is PK in `RTIDeviceData` (3 rows: 0, 1, 2)
  - shared across devices: Not observed (each RTIAddress maps to a single DeviceId)
- Document structural purpose of each identifier
  - DeviceId: device identity in `Devices`
  - RTIAddress: device address used by device-scoped resources and UI pages

---

## 5. Device-Scoped Pages
**Proven**
- Query `RTIDevicePageData`
  - Query: `SELECT PageId, PageNameId, RTIAddress, SourceDeviceId, PageOrder, HiddenPage, TemplateType FROM RTIDevicePageData ORDER BY RTIAddress, PageOrder;`
  - Returned: 21 pages total, RTIAddress 1 has 20 pages, RTIAddress 2 has 1 page
- Enumerate columns
  - Query: `PRAGMA table_info(RTIDevicePageData);`
  - Returned columns: PageId (PK), SourceDeviceId, PageNameId, RTIAddress, BackgroundBitmapId, AltBackgroundBitmapId, PageOrder, TitleFont, TitleHorzAlign, BackgroundColor, BackgroundMode, TransitionType, BackgroundTransparent, EnableLineIn, KeyColorNormal, KeyColorActive, TemplateType, EnableHdmiAudio, SelectedLayer, HiddenPage, UseDefaultBackground
- Verify how pages are linked to devices
  - Observed: `RTIDevicePageData.RTIAddress` links to `RTIDeviceData.RTIAddress`
- Determine whether PageId is global or device-scoped
  - Query: `SELECT COUNT(*) AS total, COUNT(DISTINCT PageId) AS distinct_page_ids FROM RTIDevicePageData;`
  - Returned: 21 total, 21 distinct (PageId unique across this DB)
- Identify devices with zero pages
  - Query: `SELECT d.RTIAddress, d.DeviceId, COUNT(p.PageId) AS PageCount FROM RTIDeviceData d LEFT JOIN RTIDevicePageData p ON p.RTIAddress = d.RTIAddress GROUP BY d.RTIAddress, d.DeviceId ORDER BY d.RTIAddress;`
  - Returned: RTIAddress 0 (DeviceId 2) has 0 pages

---

## 6. Page Names
**Proven**
- Query `PageNames`
  - Query: `SELECT PageNameId, PageName FROM PageNames ORDER BY PageNameId;`
  - Returned: 44 rows (includes `Home (Global)`, `Home (Room 1..5)`, `Audio/Video/Other Source` pages)
- Verify uniqueness of `PageNameId`
  - Observed: `PageNameId` is PK in schema
- Identify PageNameIds referenced but not defined
  - Query: `SELECT COUNT(*) AS missing_page_names FROM RTIDevicePageData p LEFT JOIN PageNames n ON p.PageNameId=n.PageNameId WHERE n.PageNameId IS NULL;`
  - Returned: 0 missing
- Confirm PageName resolution reliability
  - Query: `SELECT COUNT(*) AS total, COUNT(DISTINCT PageNameId) AS distinct_page_name_ids FROM RTIDevicePageData;`
  - Returned: 21 total, 20 distinct (PageNameId 259 appears twice)

---

## 7. Page Structure (Layers)
**Proven**
- Query `Layers`
  - Query: `PRAGMA table_info(Layers);`
  - Returned columns: LayerId (PK), PageId, SourceId, SharedLayerId, LayerOrder, IsVisible, VisibilityVariable, IsLocked, ViewPortButtonId, RoomId
- Determine relationship between layers and pages
  - Observed: `Layers.PageId` links to `RTIDevicePageData.PageId`
- Assess whether layers can be flattened for diagnostics use
  - Not proven yet (no flattening query executed)

---

## 8. Button Instances on Pages
**Proven**
- Query `LayerButtons`
  - Query: `PRAGMA table_info(LayerButtons);`
  - Returned: column list includes ButtonId, ButtonTagId, Text, LayerId, PageLinkId, LinkPageId, etc.
- Verify ButtonId uniqueness scope
  - Query: `SELECT COUNT(*) AS total, COUNT(DISTINCT ButtonId) AS distinct_button_ids FROM LayerButtons;`
  - Returned: 1256 rows, 148 distinct ButtonId (ButtonId reused across multiple layer instances)
  - Query: `SELECT COUNT(*) AS total, COUNT(DISTINCT ButtonId) AS distinct_button_ids FROM RTIDeviceButtonData;`
  - Returned: 148 rows, 148 distinct ButtonId (ButtonId unique in RTIDeviceButtonData)
- Identify buttons lacking both tags and text
  - Query: `SELECT SUM(CASE WHEN ButtonTagId >= 0 THEN 1 ELSE 0 END) AS tag_ge_0, SUM(CASE WHEN ButtonTagId < 0 THEN 1 ELSE 0 END) AS tag_lt_0, SUM(CASE WHEN (ButtonTagId < 0) AND (Text IS NULL OR Text = '') THEN 1 ELSE 0 END) AS tag_lt_0_missing_text, SUM(CASE WHEN (ButtonTagId >= 0) AND (Text IS NULL OR Text = '') THEN 1 ELSE 0 END) AS tag_ge_0_missing_text FROM LayerButtons;`
  - Returned: ButtonTagId<0 rows 242; of those, 41 have empty Text (no label). ButtonTagId>=0 rows 1014; 52 of those have empty Text (expected if text comes from tags).

---

## 9. Button Tags and Text Resolution
**Proven**
- Query `ButtonTagNames`
  - Query: `PRAGMA table_info(ButtonTagNames);`
  - Returned columns: ButtonTagId (PK), ButtonTagName
- Query `ButtonTextTags`
  - Query: `PRAGMA table_info(ButtonTextTags);`
  - Returned columns: ButtonTextTagId (PK), ButtonId, ButtonTagId
- Validate resolution rules:
  - ButtonTagId ≥ 0
    - Query: `SELECT COUNT(*) AS missing_tag_names FROM LayerButtons lb LEFT JOIN ButtonTagNames btn ON lb.ButtonTagId = btn.ButtonTagId WHERE lb.ButtonTagId >= 0 AND btn.ButtonTagId IS NULL;`
    - Returned: 0 missing tag name mappings
  - ButtonTagId < 0
    - Query: see Step 8 missing text check
    - Returned: 41 with ButtonTagId < 0 have empty Text
- Identify any ButtonIds that fail label resolution
  - Query: `SELECT COUNT(*) AS tag_buttons_missing_texttags FROM LayerButtons lb LEFT JOIN ButtonTextTags btt ON lb.ButtonId = btt.ButtonId WHERE lb.ButtonTagId >= 0 AND btt.ButtonId IS NULL;`
  - Returned: 618 LayerButtons rows have ButtonTagId>=0 but no ButtonTextTags entry

---

## 10. Sources
**Proven**
- Query `SourceLabels`
  - Query: `SELECT SourceLabelId, RTIAddress, LabelIndex, LabelName FROM SourceLabels ORDER BY RTIAddress, LabelIndex;`
  - Returned: 48 rows, all `LabelName` empty (no source labels defined)
- Enumerate columns
  - Query: `PRAGMA table_info(SourceLabels);`
  - Returned columns: SourceLabelId (PK), RTIAddress, LabelIndex, LabelName
- Determine how sources are linked to:
  - rooms / devices
  - Not proven (SourceLabels has RTIAddress but LabelName is empty; no mapping to room/device verified)
- Attempt to resolve:
  - SourceLabelId → Source Name → Room(s)/Device(s)
  - Not proven (LabelName empty, SourceMapping values are all -1)

---

## 11. Variables
**Proven**
- Query `Variables`, `VariableNames`, `SystemVariableIds`
  - Query: `PRAGMA table_info(Variables);` and `PRAGMA table_info(VariableNames);`
  - Returned columns:
    - Variables: VariableId (PK), RoomId, DeviceId, ButtonTagId, ButtonText, ObjectData, ReversedData, InactiveData, VisibleData
    - VariableNames: VariableId (PK), RTIAddress, VariableIndex, VariableName
  - Query: `SELECT COUNT(*) AS total, SUM(CASE WHEN VariableName IS NULL OR VariableName='' THEN 1 ELSE 0 END) AS missing_names FROM VariableNames;`
  - Returned: 768 total, 0 missing names in VariableNames
  - Query: `SELECT COUNT(*) AS variables_without_names FROM Variables v LEFT JOIN VariableNames n ON v.VariableId = n.VariableId WHERE n.VariableId IS NULL;`
  - Returned: 11 Variables rows without a matching VariableName
- Verify naming completeness
  - Proven: VariableNames has no blank VariableName values, but Variables has 11 rows without a name mapping
- Identify variables without names or values
  - Names missing: 11 Variables rows lack VariableName mapping (see query above)
  - Values not assessed (no explicit value field in Variables; ButtonText includes templated strings)
- Assess whether variables are safe to expose
  - Not proven (requires semantic review of variable contents)

---

## 12. Macros
**Proven**
- Query `Macros`
  - Query: `PRAGMA table_info(Macros);`
  - Returned columns: MacroId (PK), SystemMacroId, RoomId, DeviceId, ButtonTagId, OutputType
- Query `MacroSteps` / `MacroStepsView`
  - Query: `PRAGMA table_info(MacroSteps);`
  - Returned columns: MacroStepId (PK), MacroId, StepIndex, Type, Level, InElseSection
- Verify step ordering and references
  - Not proven (no ordered sample query executed)
- Identify steps referencing unknown IDs
  - Not proven

---

## 13. Events
**Proven**
- Query `Events`, `RoomEvents`, `SourceEvents`
  - Query: `PRAGMA table_info(RoomEvents);` and `SELECT RoomEventsId, EventType, RoomId, SelectedMacroId, DeselectedMacroId FROM RoomEvents ORDER BY RoomEventsId;`
  - Returned: 35 RoomEvents rows with RoomId 1..5 and Macro references
  - Query: `PRAGMA table_info(SourceEvents);` and `SELECT SourceEventsId, SourceId, OnMacroId, OffMacroId FROM SourceEvents ORDER BY SourceEventsId;`
  - Returned: 29 SourceEvents rows with SourceId values; some On/Off macro IDs are null
  - Table `Events` is empty
- Verify naming consistency
  - Not proven (no name field in RoomEvents/SourceEvents; Events empty)
- Identify unresolved references
  - Not proven (no join checks executed)

---

## 14. IO / Relay / Sense Maps
**Proven**
- Query `RelayModeMap`, `RelayTypeMap`, `SenseModeMap`
  - Query: `PRAGMA table_info(RelayModeMap);` and `SELECT RelayModeId, RTIAddress, ExpanderId, Mask FROM RelayModeMap;`
  - Returned: each table has 1 row with RTIAddress 0, ExpanderId -1, Mask 0
- Enumerate columns and values
  - Returned columns: <Map>Id (PK), RTIAddress, ExpanderId, Mask
- Determine whether these are pure enums or partial mappings
  - Not proven (single placeholder row only)

---

## Meta Review
**Proven**
- Identify orphaned or unused tables
  - Observed empty tables list in Step 1 (0 rows)
- Classify each section as:
  - Usable
  - Conditionally usable
  - Not usable with current data
  - Not proven (classification not performed)
## Diagnostics Extraction

## Page Index -> Page Names

Goal: Extract `DeviceId`, `PageIndex`, and `PageName` for all devices from an `.apex` SQLite database.

### Method (SQLite)
The page index is stored as `RTIDevicePageData.PageOrder`. Join it to the device and page name tables.

SQL (recommended):
```sql
SELECT
  d.DeviceId AS DeviceId,
  p.PageOrder AS PageIndex,
  n.PageName AS PageName
FROM RTIDeviceData d
JOIN Devices dv ON d.DeviceId = dv.DeviceId
LEFT JOIN RTIDevicePageData p ON p.RTIAddress = d.RTIAddress
LEFT JOIN PageNames n ON p.PageNameId = n.PageNameId
ORDER BY d.DeviceId, p.PageOrder;
```

Notes:
- Devices with no pages will still appear (with NULL `PageIndex`/`PageName`).
- `PageOrder` is the per-device page index used by RTI page navigation.

### Method (sqlite3 CLI)
```bash
sqlite3 -header -column "YOUR_PROJECT.apex" \
"SELECT d.DeviceId AS DeviceId, p.PageOrder AS PageIndex, n.PageName AS PageName
 FROM RTIDeviceData d
 JOIN Devices dv ON d.DeviceId = dv.DeviceId
 LEFT JOIN RTIDevicePageData p ON p.RTIAddress = d.RTIAddress
 LEFT JOIN PageNames n ON p.PageNameId = n.PageNameId
 ORDER BY d.DeviceId, p.PageOrder;"
```

### Method (Python snippet)
```python
import sqlite3

path = "YOUR_PROJECT.apex"
conn = sqlite3.connect(path)
cur = conn.cursor()

cur.execute("""
SELECT d.DeviceId AS DeviceId,
       p.PageOrder AS PageIndex,
       n.PageName AS PageName
FROM RTIDeviceData d
JOIN Devices dv ON d.DeviceId = dv.DeviceId
LEFT JOIN RTIDevicePageData p ON p.RTIAddress = d.RTIAddress
LEFT JOIN PageNames n ON p.PageNameId = n.PageNameId
ORDER BY d.DeviceId, p.PageOrder;
""")

rows = cur.fetchall()
for device_id, page_index, page_name in rows:
    print(device_id, page_index, page_name)

conn.close()
```

## Driver Info

Goal: Extract driver configuration values for later mapping to diagnostics (exclude debug-only settings).

### Method (SQLite)
Driver settings are stored in `DriverConfig` and tied to devices through `DriverData` (DriverDeviceId) and `Devices` (DeviceId).

SQL (recommended):
```sql
SELECT
  dd.DriverDeviceId,
  dd.DeviceId,
  d.Name AS DeviceName,
  dc.Name AS ConfigKey,
  dc.Value AS ConfigValue
FROM DriverConfig dc
JOIN DriverData dd ON dc.DriverDeviceId = dd.DriverDeviceId
JOIN Devices d ON dd.DeviceId = d.DeviceId
WHERE dc.Name NOT LIKE 'Debug%'
ORDER BY dd.DriverDeviceId, dc.Name;
```

Notes:
- `DriverConfig.Name` holds keys like `ZoneName0`, `SourceName0`, `Outputs`, `input1name`.
- Filter out `Debug*` settings (e.g., `DebugTrace`) when exporting for diagnostics.
- If `MaxZones`, `MaxSources`, `Inputs`, or `Outputs` are present, use those limits to include only the matching `ZoneName*`, `SourceName*`, `input*name`, `Output*name` entries. If a max value is missing, include all entries for that group.

### Method (sqlite3 CLI)
```bash
sqlite3 -header -column "YOUR_PROJECT.apex" \
"WITH cfg AS (
  SELECT DriverDeviceId, Name, Value
  FROM DriverConfig
  WHERE Name NOT LIKE 'Debug%'
),
maxvals AS (
  SELECT
    DriverDeviceId,
    MAX(CASE WHEN Name='MaxZones' THEN CAST(Value AS INTEGER) END) AS MaxZones,
    MAX(CASE WHEN Name='MaxSources' THEN CAST(Value AS INTEGER) END) AS MaxSources,
    MAX(CASE WHEN Name='Inputs' THEN CAST(Value AS INTEGER) END) AS Inputs,
    MAX(CASE WHEN Name='Outputs' THEN CAST(Value AS INTEGER) END) AS Outputs
  FROM cfg
  GROUP BY DriverDeviceId
)
SELECT
  dd.DriverDeviceId,
  dd.DeviceId,
  d.Name AS DeviceName,
  c.Name AS ConfigKey,
  c.Value AS ConfigValue
FROM cfg c
JOIN maxvals m ON c.DriverDeviceId = m.DriverDeviceId
JOIN DriverData dd ON c.DriverDeviceId = dd.DriverDeviceId
JOIN Devices d ON dd.DeviceId = d.DeviceId
WHERE
  c.Name IN ('MaxZones','MaxSources','Inputs','Outputs')
  OR (c.Name LIKE 'ZoneName%' AND (m.MaxZones IS NULL OR CAST(substr(c.Name,9) AS INTEGER) < m.MaxZones))
  OR (c.Name LIKE 'SourceName%' AND (m.MaxSources IS NULL OR CAST(substr(c.Name,11) AS INTEGER) < m.MaxSources))
  OR (c.Name LIKE 'input%name' AND (m.Inputs IS NULL OR CAST(substr(c.Name,6, length(c.Name)-9) AS INTEGER) <= m.Inputs))
  OR (c.Name LIKE 'Output%name' AND (m.Outputs IS NULL OR CAST(substr(c.Name,7, length(c.Name)-10) AS INTEGER) <= m.Outputs))
ORDER BY dd.DriverDeviceId, c.Name;"
```

### Method (Python snippet)
```python
import sqlite3

path = "YOUR_PROJECT.apex"
conn = sqlite3.connect(path)
cur = conn.cursor()

cur.execute("""
WITH cfg AS (
  SELECT DriverDeviceId, Name, Value
  FROM DriverConfig
  WHERE Name NOT LIKE 'Debug%'
),
maxvals AS (
  SELECT
    DriverDeviceId,
    MAX(CASE WHEN Name='MaxZones' THEN CAST(Value AS INTEGER) END) AS MaxZones,
    MAX(CASE WHEN Name='MaxSources' THEN CAST(Value AS INTEGER) END) AS MaxSources,
    MAX(CASE WHEN Name='Inputs' THEN CAST(Value AS INTEGER) END) AS Inputs,
    MAX(CASE WHEN Name='Outputs' THEN CAST(Value AS INTEGER) END) AS Outputs
  FROM cfg
  GROUP BY DriverDeviceId
)
SELECT
  dd.DriverDeviceId,
  dd.DeviceId,
  d.Name AS DeviceName,
  c.Name AS ConfigKey,
  c.Value AS ConfigValue
FROM cfg c
JOIN maxvals m ON c.DriverDeviceId = m.DriverDeviceId
JOIN DriverData dd ON c.DriverDeviceId = dd.DriverDeviceId
JOIN Devices d ON dd.DeviceId = d.DeviceId
WHERE
  c.Name IN ('MaxZones','MaxSources','Inputs','Outputs')
  OR (c.Name LIKE 'ZoneName%' AND (m.MaxZones IS NULL OR CAST(substr(c.Name,9) AS INTEGER) < m.MaxZones))
  OR (c.Name LIKE 'SourceName%' AND (m.MaxSources IS NULL OR CAST(substr(c.Name,11) AS INTEGER) < m.MaxSources))
  OR (c.Name LIKE 'input%name' AND (m.Inputs IS NULL OR CAST(substr(c.Name,6, length(c.Name)-9) AS INTEGER) <= m.Inputs))
  OR (c.Name LIKE 'Output%name' AND (m.Outputs IS NULL OR CAST(substr(c.Name,7, length(c.Name)-10) AS INTEGER) <= m.Outputs))
ORDER BY dd.DriverDeviceId, c.Name;
""")

rows = cur.fetchall()
for row in rows:
    print(row)

conn.close()
```

## Variable Look Up

Goal: Resolve a `SYSVARREF` to the owning driver and the human-readable variable name.

Example SYSVARREF:
`SYSVARREF:{EC82485C-AF0B-4BF0-9DB1-22B290C8B814}#24@G07Temp1`

Resolution steps:
1) **Driver GUID**: extract the GUID from the SYSVARREF and match it to `DriverData.DriverId`.
2) **System variable registry**: match the full SYSVARREF to `SystemVariableIds.SysVarRef`.
3) **Driver variable name**: search the driver’s `SystemVariables` XML for the `sysvar` value (`G07Temp1`).

SQL (driver + sysvar registry):
```sql
-- Driver GUID -> driver record
SELECT DriverDeviceId, DeviceId, DriverId, DriverVersion, Author
FROM DriverData
WHERE DriverId = '{EC82485C-AF0B-4BF0-9DB1-22B290C8B814}';

-- SYSVARREF -> registry entry
SELECT SysVarID, SysVarRef, DeviceId
FROM SystemVariableIds
WHERE SysVarRef = '{EC82485C-AF0B-4BF0-9DB1-22B290C8B814}#24@G07Temp1';
```

Python snippet (extract human-readable variable name from SystemVariables XML):
```python
import sqlite3
import re

path = "YOUR_PROJECT.apex"
sysvar = "G07Temp1"
driver_id = "{EC82485C-AF0B-4BF0-9DB1-22B290C8B814}"

conn = sqlite3.connect(path)
cur = conn.cursor()
cur.execute("""
SELECT DriverDeviceId, SystemVariables
FROM DriverData
WHERE DriverId = ?;
""", (driver_id,))
row = cur.fetchone()
conn.close()

driver_device_id, xml_text = row
pattern = re.compile(r"<variable\\s+name='([^']+)'\\s+sysvar='" + re.escape(sysvar) + r"'", re.IGNORECASE)
match = pattern.search(xml_text)
if match:
    print("DriverDeviceId:", driver_device_id)
    print("Variable Name:", match.group(1))
```

Expected outcome for the example:
- Driver: Clipsal C-Bus (DriverId `{EC82485C-AF0B-4BF0-9DB1-22B290C8B814}`)
- Variable Name: `Group 7 Zone 1 Temperature` (from `SystemVariables` XML)

## System Variable Events Driver

Goal: Extract config for System Variable Events drivers, excluding empty or “(not set)” values, and resolve SYSVARREF entries to driver+variable names.

SQL (config values, filtered):
```sql
SELECT
  dc.DriverDeviceId,
  dd.DriverId,
  d.Name AS DriverDeviceName,
  dc.Name AS ConfigKey,
  dc.Value AS ConfigValue
FROM DriverConfig dc
JOIN DriverData dd ON dc.DriverDeviceId = dd.DriverDeviceId
JOIN Devices d ON dd.DeviceId = d.DeviceId
WHERE d.Name LIKE 'System Variable Events%'
  AND dc.Value IS NOT NULL
  AND dc.Value <> ''
  AND dc.Value <> '(not set)'
ORDER BY dc.DriverDeviceId, dc.Name;
```

Resolve SYSVARREF in driver config:
```sql
-- Find which System Variable Events driver config rows reference a SYSVARREF
SELECT
  dc.DriverDeviceId,
  d.Name AS DriverDeviceName,
  dc.Name AS ConfigKey,
  dc.Value AS SysVarRef
FROM DriverConfig dc
JOIN DriverData dd ON dc.DriverDeviceId = dd.DriverDeviceId
JOIN Devices d ON dd.DeviceId = d.DeviceId
WHERE d.Name LIKE 'System Variable Events%'
  AND dc.Value LIKE 'SYSVARREF:%';
```

Python snippet (resolve SYSVARREF to driver + variable name):
```python
import re
import sqlite3

path = "YOUR_PROJECT.apex"
sysvarref = "SYSVARREF:{EC82485C-AF0B-4BF0-9DB1-22B290C8B814}#24@G07Temp1"

driver_id = re.search(r"\\{[A-F0-9\\-]+\\}", sysvarref, re.IGNORECASE).group(0)
sysvar = sysvarref.split("@", 1)[1]

conn = sqlite3.connect(path)
cur = conn.cursor()
cur.execute("SELECT DriverDeviceId, SystemVariables FROM DriverData WHERE DriverId = ?", (driver_id,))
row = cur.fetchone()
conn.close()

driver_device_id, xml_text = row
pattern = re.compile(r"<variable\\s+name='([^']+)'\\s+sysvar='" + re.escape(sysvar) + r"'", re.IGNORECASE)
match = pattern.search(xml_text)
if match:
    print("DriverDeviceId:", driver_device_id)
    print("Variable Name:", match.group(1))
```
