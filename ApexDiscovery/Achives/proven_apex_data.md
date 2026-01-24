# proven_apex_data.md
# Generated: 2026-01-23T19:10:49.458114Z

# PROVEN DATA — APEX DB EXPLORATION

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
