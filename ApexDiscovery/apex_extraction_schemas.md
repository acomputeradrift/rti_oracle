# APEX Extraction Schemas

Purpose: Define extraction schemas, relationships, and SQL needed to build deterministic data pulls from `.apex` (SQLite) files. This is for Codex ingestion only; no implementation code.

---

## Pages

### Output fields (authoritative names)
- `device_id` (from `Devices.DeviceId`)
- `device_name` (from `Devices.Name`)
- `room_id` (from `Rooms.RoomId`)
- `room_name` (from `Rooms.Name`)
- `source_id` (from `Devices.DeviceId`, via `RTIDevicePageData.SourceDeviceId`)
- `source_name` (from `Devices.Name`, via `RTIDevicePageData.SourceDeviceId`)
- `page_number` (per-device index: `RTIDevicePageData.PageOrder + 1`)
- `page_name` (from `PageNames.PageName`)

Note: `page_number` is **not** `PageId + 1`. The true per-device page index is `PageOrder`, used by RTI page navigation.

### Relationship graph (required)
```
Devices (controller)            RTIDeviceData           RTIDevicePageData
DeviceId ─────────────────────► RTIAddress ───────────► PageId / PageNameId / PageOrder / SourceDeviceId
   │                                                         │                 │
   │                                                         │                 └─► Devices (source) → Rooms
   │                                                         └─► PageNames
   └─► Devices (controller) via RTIDeviceData.DeviceId

Rooms
RoomId ───────────────────────────────────────────────────────────────► Devices.RoomId (source device)
```

### Extraction rules
1) **Controller device** is resolved via `RTIDeviceData.DeviceId` → `Devices`.
2) **Page list** is scoped by `RTIDevicePageData.RTIAddress` (device-scoped pages).
3) **Page name** resolves via `RTIDevicePageData.PageNameId` → `PageNames.PageName`.
4) **Source device** resolves via `RTIDevicePageData.SourceDeviceId` → `Devices` (this is the “source” seen in ID11).
5) **Room** resolves via `SourceDeviceId` → `Devices.RoomId` → `Rooms`.
6) **Page number** is `PageOrder + 1` (1-based UI index). Never derive from `PageId`.
7) If any join is missing, leave the unresolved field explicitly blank (do not infer).

### SQL (authoritative)
```sql
SELECT
  d.DeviceId AS device_id,
  d.Name AS device_name,
  sd.RoomId AS room_id,
  r.Name AS room_name,
  p.SourceDeviceId AS source_id,
  sd.Name AS source_name,
  (p.PageOrder + 1) AS page_number,
  n.PageName AS page_name
FROM RTIDeviceData rd
JOIN Devices d ON rd.DeviceId = d.DeviceId
JOIN RTIDevicePageData p ON p.RTIAddress = rd.RTIAddress
LEFT JOIN Devices sd ON p.SourceDeviceId = sd.DeviceId
LEFT JOIN Rooms r ON sd.RoomId = r.RoomId
LEFT JOIN PageNames n ON p.PageNameId = n.PageNameId
ORDER BY d.DeviceId, p.PageOrder;
```

### Validation queries (recommended)
Check for missing source links:
```sql
SELECT COUNT(*) AS missing_sources
FROM RTIDevicePageData p
LEFT JOIN Devices sd ON p.SourceDeviceId = sd.DeviceId
WHERE sd.DeviceId IS NULL;
```

Check for missing page names:
```sql
SELECT COUNT(*) AS missing_page_names
FROM RTIDevicePageData p
LEFT JOIN PageNames n ON p.PageNameId = n.PageNameId
WHERE n.PageNameId IS NULL;
```

### Notes
- `RTIDevicePageData.SourceDeviceId` is the strongest observed link between a page and its “source” in ID11.
- `SourceLabels`/`SourceMapping` exist but may be empty or unmapped; do **not** depend on them for this schema unless proven.

---

## Ports

### Relay Subsection (authoritative for this project)

### Output fields (authoritative names)
- `controller_device_name` (from `Devices.Name` via `RTIDeviceData` for `RTIAddress=0`)
- `expander_device_type` (model label; see mapping below)
- `expander_name` (from `ExpansionDevices.Name`; `Internal` for XP-8v internal relays)
- `relay_name` (from `PortLabels.LabelName`)
- `relay_type` (state: `Contact Closure` or `Voltage Trigger`, if applicable)
- `relay_mode` (state: `Normally Open` or `Normally Closed`, if applicable)

### Relationship graph (required)
```
Devices (controller)             RTIDeviceData
DeviceId ──────────────────────► RTIAddress (=0 for XP-8v)
                                  │
                                  ├─► PortLabels (relay names)
                                  ├─► RelayTypeMap (internal type state; ExpanderId=-1)
                                  └─► RelayModeMap (internal mode state; ExpanderId=-1)

ExpansionDevices (RTIAddress=0)
ExpanderId ─────────────────────► PortLabels (expander relay names inferred via LabelKey)
```

### Relay name sources (proven)
- **Internal XP-8v relays**: `PortLabels` rows where `RTIAddress=0` and `LabelKey` is in `-64768..-64761` (these map to Relay 1..8 names from the UI).
- **Expansion relays**: `PortLabels` rows where `RTIAddress=0` and `LabelName LIKE 'Relay %'`.

### Relay expander inference (required)
For expansion relays, infer `expander_id` using the high word of `LabelKey`:
- `expander_id = LabelKey >> 16`
- Join to `ExpansionDevices.ExpanderId` (with `RTIAddress=0`) to get `expander_name` and `DeviceType`.

This inference is proven in this project by matching group labels with expansion names (e.g., WorkShop Slave).

### Relay state mapping (from project UI + masks)
- Internal relay **type** uses `RelayTypeMap` with `RTIAddress=0`, `ExpanderId=-1`.
  - Mask `0` corresponds to `Contact Closure` for all internal relays in this project.
- Internal relay **mode** uses `RelayModeMap` with `RTIAddress=0`, `ExpanderId=-1`.
  - Mask `0` corresponds to `Normally Open` for all internal relays in this project.
- Expansion devices in this project do not expose relay type/mode in the UI; mark as `N/A`.
- XP-6 (WorkShop Slave) relay type/mode are present in the DB as masks but are not used in this setup; mark as `Unknown` unless a project-specific mapping is proven.

### DeviceType model mapping (inferred)
- `DeviceType 3` → `ESC-2`
- `DeviceType 5` → `RCM-4`
- `DeviceType 6` → `XP-6`
- Internal relays use `Internal`

### SQL (authoritative)
Relay extraction for this project:
```sql
WITH relay_labels AS (
  SELECT
    (LabelKey >> 16) AS expander_id,
    LabelName AS relay_name
  FROM PortLabels
  WHERE RTIAddress = 0
    AND (
      LabelKey BETWEEN -64768 AND -64761
      OR LabelName LIKE 'Relay %'
    )
),
controller AS (
  SELECT d.Name AS controller_device_name
  FROM RTIDeviceData rd
  JOIN Devices d ON rd.DeviceId = d.DeviceId
  WHERE rd.RTIAddress = 0
)
SELECT
  c.controller_device_name,
  CASE
    WHEN r.expander_id = -1 THEN 'Internal'
    WHEN e.DeviceType = 5 THEN 'RCM-4'
    WHEN e.DeviceType = 3 THEN 'ESC-2'
    WHEN e.DeviceType = 6 THEN 'XP-6'
    ELSE CAST(e.DeviceType AS TEXT)
  END AS expander_device_type,
  CASE
    WHEN r.expander_id = -1 THEN 'Internal'
    ELSE e.Name
  END AS expander_name,
  r.relay_name,
  CASE
    WHEN r.expander_id = -1 THEN 'Contact Closure'
    WHEN r.expander_id = 1 THEN 'Unknown'
    ELSE 'N/A'
  END AS relay_type,
  CASE
    WHEN r.expander_id = -1 THEN 'Normally Open'
    WHEN r.expander_id = 1 THEN 'Unknown'
    ELSE 'N/A'
  END AS relay_mode
FROM relay_labels r
CROSS JOIN controller c
LEFT JOIN ExpansionDevices e
  ON e.RTIAddress = 0 AND e.ExpanderId = r.expander_id
ORDER BY expander_device_type, expander_name, r.relay_name;
```

### Notes
- Internal relay names are stored in `PortLabels` with negative `LabelKey` values (`-64768..-64761`).
- Expansion relay names are stored as `Relay 1..4` and are linked to expansion devices by `LabelKey >> 16`.
- Relay type/mode are treated as global internal settings for the XP-8v in this project; do not apply them to expansion devices.

---

### MPIO/IR Subsection (authoritative for this project)

### Output fields (authoritative names)
- `controller_device_name` (from `Devices.Name` via `RTIDeviceData` for `RTIAddress=0`)
- `expander_device_type` (model label; see mapping below)
- `expander_name` (from `ExpansionDevices.Name`; `Internal` for XP-8v internal MPIO/IR)
- `port_number` (1-based port index)
- `port_name` (from `PortLabels.LabelName`)

### Relationship graph (required)
```
Devices (controller)             RTIDeviceData
DeviceId ──────────────────────► RTIAddress (=0 for XP-8v)
                                  │
                                  └─► PortLabels (MPIO/IR names)

ExpansionDevices (RTIAddress=0)
ExpanderId ─────────────────────► PortLabels (MPIO/IR names inferred via LabelKey)
```

### MPIO/IR name sources (proven)
- **Internal XP-8v MPIO/IR**: `PortLabels` rows where `RTIAddress=0` and `LabelKey` is in `-65536..-65529` (8 ports).
- **XP-6 WorkShop Slave MPIO/IR**: `PortLabels` rows where `RTIAddress=0` and `LabelKey` is in `65536..65543` (8 ports), where `expander_id = LabelKey >> 16 = 1`.

### Port index inference (required)
- `expander_id = LabelKey >> 16`
- `port_number = (LabelKey & 65535) + 1` for positive label keys
- `port_number = (LabelKey & 65535) + 1` also works for the negative internal range in this project (low word `0..7`)

### DeviceType model mapping (inferred)
- `DeviceType 6` → `XP-6`
- Internal ports use `Internal`

### SQL (authoritative)
MPIO/IR extraction for this project:
```sql
WITH mpio_labels AS (
  SELECT
    (LabelKey >> 16) AS expander_id,
    (LabelKey & 65535) AS port_key,
    LabelName AS port_name
  FROM PortLabels
  WHERE RTIAddress = 0
    AND (
      LabelKey BETWEEN -65536 AND -65529
      OR LabelKey BETWEEN 65536 AND 65543
    )
),
controller AS (
  SELECT d.Name AS controller_device_name
  FROM RTIDeviceData rd
  JOIN Devices d ON rd.DeviceId = d.DeviceId
  WHERE rd.RTIAddress = 0
)
SELECT
  c.controller_device_name,
  CASE
    WHEN m.expander_id = -1 THEN 'Internal'
    WHEN e.DeviceType = 6 THEN 'XP-6'
    ELSE CAST(e.DeviceType AS TEXT)
  END AS expander_device_type,
  CASE
    WHEN m.expander_id = -1 THEN 'Internal'
    ELSE e.Name
  END AS expander_name,
  (m.port_key % 256) + 1 AS port_number,
  m.port_name
FROM mpio_labels m
CROSS JOIN controller c
LEFT JOIN ExpansionDevices e
  ON e.RTIAddress = 0 AND e.ExpanderId = m.expander_id
ORDER BY expander_device_type, expander_name, port_number;
```

### Notes
- MPIO/IR port names are stored in `PortLabels`; `IrData` and `IrFunction` are control libraries and are not used for port naming.

---

### Sense Subsection (authoritative for this project)

### Output fields (authoritative names)
- `controller_device_name` (from `Devices.Name` via `RTIDeviceData` for `RTIAddress=0`)
- `expander_device_type` (model label; see mapping below)
- `expander_name` (from `ExpansionDevices.Name`; `Internal` for XP-8v internal Sense)
- `port_number` (1-based port index)
- `port_name` (from `PortLabels.LabelName`)
- `sense_mode_state` (`Sense Voltage` or `Sense Closure` for internal; `N/A` for expansion devices)

### Relationship graph (required)
```
Devices (controller)             RTIDeviceData
DeviceId ──────────────────────► RTIAddress (=0 for XP-8v)
                                  │
                                  └─► PortLabels (Sense names)

ExpansionDevices (RTIAddress=0)
ExpanderId ─────────────────────► PortLabels (Sense names inferred via LabelKey)
```

### Sense name sources (proven)
- **Internal XP-8v Sense**: `PortLabels` rows where `RTIAddress=0` and `LabelKey` is in `-65024..-65017` (8 ports).
- **XP-6 WorkShop Slave Sense**: `PortLabels` rows where `RTIAddress=0` and `LabelKey` is in `66048..66055` (8 ports), where `expander_id = LabelKey >> 16 = 1`.

### Port index inference (required)
- `expander_id = LabelKey >> 16`
- `port_number = (LabelKey & 65535) - 512 + 1`

### Sense mode mapping (proven by UI + mask)
- Internal sense mode is stored in `SenseModeMap` for `RTIAddress=0`, `ExpanderId=-1`.
- Mask bit `1` = `Sense Closure`
- Mask bit `0` = `Sense Voltage`
- Bit index is `port_number - 1`

### DeviceType model mapping (inferred)
- `DeviceType 6` → `XP-6`
- Internal ports use `Internal`

### SQL (authoritative)
Sense extraction for this project:
```sql
WITH sense_labels AS (
  SELECT
    (LabelKey >> 16) AS expander_id,
    (LabelKey & 65535) AS port_key,
    LabelName AS port_name
  FROM PortLabels
  WHERE RTIAddress = 0
    AND (
      LabelKey BETWEEN -65024 AND -65017
      OR LabelKey BETWEEN 66048 AND 66055
    )
),
controller AS (
  SELECT d.Name AS controller_device_name
  FROM RTIDeviceData rd
  JOIN Devices d ON rd.DeviceId = d.DeviceId
  WHERE rd.RTIAddress = 0
),
sense_mask AS (
  SELECT Mask AS sense_mode_mask
  FROM SenseModeMap
  WHERE RTIAddress = 0 AND ExpanderId = -1
)
SELECT
  c.controller_device_name,
  CASE
    WHEN s.expander_id = -1 THEN 'Internal'
    WHEN e.DeviceType = 6 THEN 'XP-6'
    ELSE CAST(e.DeviceType AS TEXT)
  END AS expander_device_type,
  CASE
    WHEN s.expander_id = -1 THEN 'Internal'
    ELSE e.Name
  END AS expander_name,
  (s.port_key - 512) + 1 AS port_number,
  s.port_name,
  CASE
    WHEN s.expander_id = -1 THEN
      CASE
        WHEN ((sm.sense_mode_mask >> ((s.port_key - 512))) & 1) = 1 THEN 'Sense Closure'
        ELSE 'Sense Voltage'
      END
    ELSE 'N/A'
  END AS sense_mode_state
FROM sense_labels s
CROSS JOIN controller c
LEFT JOIN ExpansionDevices e
  ON e.RTIAddress = 0 AND e.ExpanderId = s.expander_id
LEFT JOIN sense_mask sm
  ON 1 = 1
ORDER BY expander_device_type, expander_name, port_number;
```

### Notes
- Sense port names are stored in `PortLabels`; `SenseModeMap` provides bitmasks only and does not hold labels.
- Sense mode applies only to the internal processor in this project.

---

### Trigger Subsection (authoritative for this project)

### Output fields (authoritative names)
- `controller_device_name` (from `Devices.Name` via `RTIDeviceData` for `RTIAddress=0`)
- `expander_device_type` (model label; see mapping below)
- `expander_name` (from `ExpansionDevices.Name`)
- `trigger_number` (1-based trigger index)
- `trigger_name` (from `PortLabels.LabelName`)

### Relationship graph (required)
```
Devices (controller)             RTIDeviceData
DeviceId ──────────────────────► RTIAddress (=0 for XP-8v)
                                  │
                                  └─► PortLabels (Trigger names)

ExpansionDevices (RTIAddress=0)
ExpanderId ─────────────────────► PortLabels (Trigger names inferred via LabelKey)
```

### Trigger name sources (proven)
- **XP-6 WorkShop Slave Trigger**: `PortLabels` rows where `RTIAddress=0` and `LabelKey` is in `66307..66309` (3 triggers), where `expander_id = LabelKey >> 16 = 1`.

### Trigger index inference (required)
- `expander_id = LabelKey >> 16`
- `trigger_number = (LabelKey & 65535) - 770`

### DeviceType model mapping (inferred)
- `DeviceType 6` → `XP-6`

### SQL (authoritative)
Trigger extraction for this project:
```sql
WITH trig_labels AS (
  SELECT
    (LabelKey >> 16) AS expander_id,
    (LabelKey & 65535) AS port_key,
    LabelName AS trigger_name
  FROM PortLabels
  WHERE RTIAddress = 0
    AND LabelKey BETWEEN 66307 AND 66309
),
controller AS (
  SELECT d.Name AS controller_device_name
  FROM RTIDeviceData rd
  JOIN Devices d ON rd.DeviceId = d.DeviceId
  WHERE rd.RTIAddress = 0
)
SELECT
  c.controller_device_name,
  CASE
    WHEN e.DeviceType = 6 THEN 'XP-6'
    ELSE CAST(e.DeviceType AS TEXT)
  END AS expander_device_type,
  e.Name AS expander_name,
  (t.port_key - 770) AS trigger_number,
  t.trigger_name
FROM trig_labels t
CROSS JOIN controller c
LEFT JOIN ExpansionDevices e
  ON e.RTIAddress = 0 AND e.ExpanderId = t.expander_id
ORDER BY expander_device_type, expander_name, trigger_number;
```

### Notes
- Trigger labels are stored in `PortLabels` and appear only for the XP-6 in this project.

---

### RS-232 Subsection (authoritative for this project)

### Output fields (authoritative names)
- `controller_device_name` (from `Devices.Name` via `RTIDeviceData` for `RTIAddress=0`)
- `expander_device_type` (model label; see mapping below)
- `expander_name` (from `ExpansionDevices.Name`; `Internal` for XP-8v internal RS-232)
- `port_number` (1-based port index)
- `port_name` (from `PortLabels.LabelName`)

### Relationship graph (required)
```
Devices (controller)             RTIDeviceData
DeviceId ──────────────────────► RTIAddress (=0 for XP-8v)
                                  │
                                  └─► PortLabels (RS-232 names)

ExpansionDevices (RTIAddress=0)
ExpanderId ─────────────────────► PortLabels (RS-232 names inferred via LabelKey)
```

### RS-232 name sources (proven)
- **Internal XP-8v RS-232**: `PortLabels` rows where `RTIAddress=0` and `LabelKey` is in `-65280..-65273` (8 ports).
- **XP-6 WorkShop Slave RS-232**: `PortLabels` rows where `RTIAddress=0` and `LabelKey` is in `65792..65799` (8 ports), where `expander_id = LabelKey >> 16 = 1`.

### Port index inference (required)
- `expander_id = LabelKey >> 16`
- `port_number = (LabelKey & 65535) - 256 + 1`

### DeviceType model mapping (inferred)
- `DeviceType 6` → `XP-6`
- Internal ports use `Internal`

### SQL (authoritative)
RS-232 extraction for this project:
```sql
WITH rs_labels AS (
  SELECT
    (LabelKey >> 16) AS expander_id,
    (LabelKey & 65535) AS port_key,
    LabelName AS port_name
  FROM PortLabels
  WHERE RTIAddress = 0
    AND (
      LabelKey BETWEEN -65280 AND -65273
      OR LabelKey BETWEEN 65792 AND 65799
    )
),
controller AS (
  SELECT d.Name AS controller_device_name
  FROM RTIDeviceData rd
  JOIN Devices d ON rd.DeviceId = d.DeviceId
  WHERE rd.RTIAddress = 0
)
SELECT
  c.controller_device_name,
  CASE
    WHEN r.expander_id = -1 THEN 'Internal'
    WHEN e.DeviceType = 6 THEN 'XP-6'
    ELSE CAST(e.DeviceType AS TEXT)
  END AS expander_device_type,
  CASE
    WHEN r.expander_id = -1 THEN 'Internal'
    ELSE e.Name
  END AS expander_name,
  (r.port_key - 256) + 1 AS port_number,
  r.port_name
FROM rs_labels r
CROSS JOIN controller c
LEFT JOIN ExpansionDevices e
  ON e.RTIAddress = 0 AND e.ExpanderId = r.expander_id
ORDER BY expander_device_type, expander_name, port_number;
```

### Notes
- RS-232 port names are stored in `PortLabels`; `RS232Data` contains control settings and is not used for naming.

---

## Rooms

### Output fields (authoritative names)
- `room_id` (from `Rooms.RoomId`)
- `room_name` (from `Rooms.Name`)
- `source_id` (from `Devices.DeviceId`)
- `source_name` (from `Devices.Name`)
- `controller_device_id` (from `RTIDeviceData.DeviceId`)
- `controller_device_name` (from `Devices.Name`, via `RTIDeviceData`)
- `page_id` (from `RTIDevicePageData.PageId`)
- `page_name` (from `PageNames.PageName`)

### Relationship graph (required)
```
Rooms
RoomId ─────────────────────► Devices.RoomId (source devices)
                                │
                                └─► RTIDevicePageData.SourceDeviceId (pages per source)
                                       │
                                       ├─► PageNames.PageNameId
                                       └─► RTIDeviceData.RTIAddress → Devices (controller)
```

### Extraction rules
1) Room-to-source mapping is via `Devices.RoomId`.
2) Page-to-source mapping is via `RTIDevicePageData.SourceDeviceId`.
3) Controller device (panel/processor showing the page) is resolved via `RTIDevicePageData.RTIAddress → RTIDeviceData → Devices`.
4) Do not assume every room has sources or pages; leave nulls if missing.

### SQL (authoritative)
Room → Source → Page mapping for this project:
```sql
SELECT
  r.RoomId AS room_id,
  r.Name AS room_name,
  s.DeviceId AS source_id,
  s.Name AS source_name,
  dv.DeviceId AS controller_device_id,
  dv.Name AS controller_device_name,
  p.PageId AS page_id,
  n.PageName AS page_name
FROM Rooms r
LEFT JOIN Devices s
  ON s.RoomId = r.RoomId
LEFT JOIN RTIDevicePageData p
  ON p.SourceDeviceId = s.DeviceId
LEFT JOIN RTIDeviceData rd
  ON p.RTIAddress = rd.RTIAddress
LEFT JOIN Devices dv
  ON rd.DeviceId = dv.DeviceId
LEFT JOIN PageNames n
  ON p.PageNameId = n.PageNameId
ORDER BY r.RoomId, s.DeviceId, dv.DeviceId, p.PageId;
```

### Notes
- Page names may appear duplicated across controller devices; keep `controller_device_name` to disambiguate.

---

## Macros (**UNFINISHED**)

Status: Macro naming and scoping are only partially understood. This section is a placeholder and must be revisited.

### What is known (proven)
- `Macros` provides `MacroId`, `RoomId`, `DeviceId`, `ButtonTagId`, `OutputType`.
- `ButtonTagId` links to `ButtonTagNames` and can be used as a **tag name** for a macro when present.
- Macro actions live in `MacroSteps` / `MacroStepsView` (step details, device commands, parameters).

### What is unknown (blocking)
- A canonical macro **name** field is not present in the schema.
- It is unclear how to distinguish **global vs room vs source** macros purely from IDs without additional rules.

### Partial extraction (current best effort)
```sql
SELECT
  m.MacroId,
  m.RoomId,
  r.Name AS RoomName,
  m.DeviceId,
  d.Name AS DeviceName,
  m.ButtonTagId,
  btn.ButtonTagName AS TagName,
  m.OutputType
FROM Macros m
LEFT JOIN Rooms r ON m.RoomId = r.RoomId
LEFT JOIN Devices d ON m.DeviceId = d.DeviceId
LEFT JOIN ButtonTagNames btn ON m.ButtonTagId = btn.ButtonTagId
ORDER BY m.MacroId;
```

### Example: resolving a tag to macro steps
```sql
SELECT
  mv.MacroId,
  mv.StepIndex,
  mv.Type,
  mv.DeviceId,
  dv.Name AS DeviceName,
  mv.Function,
  mv.Parameter1,
  mv.Parameter2,
  mv.Parameter3,
  mv.Parameter4
FROM MacroStepsView mv
LEFT JOIN Devices dv ON mv.DeviceId = dv.DeviceId
WHERE mv.MacroId = ? -- supply MacroId
ORDER BY mv.StepIndex;
```

---

## Variables (Driver Template)

### Output fields (authoritative names)
- `driver_device_id` (from `DriverData.DriverDeviceId`)
- `driver_device_name` (from `Devices.Name`)
- `driver_display_name` (from `Devices.DisplayName`, if set)
- `sysvar_ref` (full SYSVARREF key)
- `sysvar_token` (portion after `@`)
- `source_driver_id` (GUID inside SYSVARREF)
- `source_driver_name` (from `DriverData` matched by `DriverId`)
- `variable_category` (from `DriverData.SystemVariables` XML category/group name)
- `variable_name` (from `DriverData.SystemVariables` XML variable name)
- `variable_type` (from XML `type`/`datatype`, if present)
- `format` (from XML `format`, if present)

### SYSVARREF forms (distinction required)
- Standard: `{GUID}#NN@Token`
- Device-scoped: `{GUID}#<DeviceId>@Token` (e.g., System Variable Events uses the device id segment)

The parser must accept both forms and treat the GUID + token as the lookup key.

### Resolution flow (proven)
1) Parse GUID from `sysvar_ref`.
2) Parse `sysvar_token` from `sysvar_ref` (portion after `@`).
3) Find `DriverData` row by `DriverId = GUID`.
4) Parse `DriverData.SystemVariables` XML to map `sysvar_token` → `variable_category`, `variable_name`, `variable_type`, `format`.

### SQL (authoritative)
```sql
-- Driver instance (name + display name) plus sysvar keys
SELECT
  d.DeviceId,
  d.Name AS driver_device_name,
  d.DisplayName AS driver_display_name,
  sv.SysVarRef AS sysvar_ref
FROM Devices d
JOIN DriverData dd ON dd.DeviceId = d.DeviceId
JOIN SystemVariableIds sv ON sv.SysVarRef LIKE '%' || dd.DriverId || '%'
ORDER BY d.DeviceId, sv.SysVarID;
```

### Notes
- `SystemVariableIds.SysVarRef` does not include the human-readable name; names come from `DriverData.SystemVariables`.
- Some drivers (e.g., System Variable Events) are device-scoped and use the device id in the SYSVARREF.

---

## Variables (System Manager) **UNFINISHED**

System Manager variables do not have a `DriverData.SystemVariables` XML payload, so names and categories must be inferred from token patterns and lookup tables.

### Output fields (authoritative names)
- `sysvar_key` (from `SystemVariableIds.SysVarID`)
- `driver_name` (`System Manager`, or from `Devices` if a matching device exists)
- `driver_display_name` (`System Manager`, or from `Devices.DisplayName` if present)
- `variable_category` (UI group name; e.g., `Source Names`)
- `variable_name` (constructed from token + resolved names)
- `variable_type` (`UNKNOWN` until proven)
- `format` (blank until proven)

### System Manager driver identifier (proven)
`SystemVariableIds.SysVarRef` uses GUID `{20186C86-446C-4FC6-89E1-1931718A169B}` for System Manager variables.

### Proven category: Source Names
Tokens under this category use the patterns:
- `SourceInUse<N>`
- `SourceName<N>`

#### Source index resolution (proven)
`<N>` maps to a 1-based ordered list of devices:
```
Devices
WHERE ControlType IN (5, 6)
ORDER BY DeviceId
```

This ordering matches the UI list (e.g., `Source 1 In Use (Home)`, `Source 2 In Use (Home/Lights (Bed 3 Loft))`, ...).

#### Variable name construction
- For `SourceInUse<N>`: `Source <N> In Use (<SourceName>)`
- For `SourceName<N>`: `Source <N> (<SourceName>)` (unverified label; UI list displays only “In Use” entries)

### SQL (authoritative for Source Names)
```sql
WITH ordered_sources AS (
  SELECT
    ROW_NUMBER() OVER (ORDER BY DeviceId) AS source_index,
    DeviceId,
    Name AS source_name
  FROM Devices
  WHERE ControlType IN (5, 6)
),
sysvars AS (
  SELECT
    SysVarID AS sysvar_key,
    SysVarRef AS sysvar_ref,
    SUBSTR(SysVarRef, INSTR(SysVarRef, '@') + 1) AS token
  FROM SystemVariableIds
  WHERE SysVarRef LIKE '{20186C86-446C-4FC6-89E1-1931718A169B}#%'
)
SELECT
  s.sysvar_key,
  'System Manager' AS driver_name,
  'System Manager' AS driver_display_name,
  'Source Names' AS variable_category,
  CASE
    WHEN s.token LIKE 'SourceInUse%' THEN
      'Source ' || o.source_index || ' In Use (' || o.source_name || ')'
    ELSE
      'Source ' || o.source_index || ' (' || o.source_name || ')'
  END AS variable_name,
  'UNKNOWN' AS variable_type,
  '' AS format
FROM sysvars s
JOIN ordered_sources o
  ON s.token = 'SourceInUse' || o.source_index
  OR s.token = 'SourceName' || o.source_index
ORDER BY o.source_index;
```

### Notes
- The System Manager device row was not found in `Devices` for this project. Use literal `System Manager` unless a specific device is proven.
- Additional categories shown in the UI (e.g., `Room N (Name) Sources`, `Selected Room`, `Layer Visibility`, `Popups`, `Time`) are not yet mapped and must be proven before extraction.

---

## Field Glossary

Purpose: Keep field names consistent across extraction sections.

- `device_id`: Controller device identifier (`Devices.DeviceId`) referenced by `RTIDeviceData.DeviceId`.
- `device_name`: Controller device name (`Devices.Name`).
- `room_id`: Room identifier (`Rooms.RoomId`), usually reached via a source device’s `Devices.RoomId`.
- `room_name`: Human-readable room name (`Rooms.Name`).
- `source_id`: Source device identifier (`Devices.DeviceId`) referenced by `RTIDevicePageData.SourceDeviceId`.
- `source_name`: Source device name (`Devices.Name`) referenced by `source_id`.
- `page_number`: 1-based per-device page index (`RTIDevicePageData.PageOrder + 1`).
- `page_name`: Human-readable page name (`PageNames.PageName`).
- `port_label_id`: Port label row identifier (`PortLabels.PortLabelId`).
- `port_label_key`: Port label index/key (`PortLabels.LabelKey`).
- `port_label_name`: Human-readable port label (`PortLabels.LabelName`).
- `expander_id`: Expansion port/expander identifier (`ExpansionDevices.ExpanderId`, or `RS232Data.ExpanderId`, or `IrData.ExpanderId`).
