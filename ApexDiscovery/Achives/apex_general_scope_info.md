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

---

## Practical next steps (for module authors)

- Treat the `.apex` as the authoritative project DB for **all names/IDs**.
- Build a robust resolver that can map runtime identifiers (page IDs, button IDs, device IDs) to human‑readable names.
- If multiple devices share pages, always map by `RTIAddress` and `RTIDevicePageData` to avoid collisions.

