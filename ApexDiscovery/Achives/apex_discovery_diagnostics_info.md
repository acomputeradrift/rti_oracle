# apex_diagnostics_info.md

## Page Index -> Page Names

Goal: Extract `DeviceId`, `PageIndex`, and `PageName` for all devices from an `.apex` SQLite database.

### Method (SQLite)
An `.apex` file is a SQLite database. The page index is stored as `RTIDevicePageData.PageOrder`. Join it to the device and page name tables.

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
