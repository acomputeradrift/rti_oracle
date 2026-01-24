# System Variable Events Driver Profile

## Purpose
- Capture configuration from the System Variable Events driver in an `.apex` file.
- Resolve internal IDs (SYSVARREF) to human-readable driver + variable names.
- Provide mappings for diagnostics output without modifying the `.apex` inputs.

## Data Sources in the `.apex` SQLite Database
- `DriverConfig`: stores driver config key/value pairs.
- `DriverData`: links driver IDs to devices and contains `SystemVariables` XML.
- `Devices`: provides human-readable device names.
- `SystemVariableIds`: maps SYSVARREF values to device IDs.

## Identify System Variable Events Driver Rows
- Filter `Devices.Name` to entries that start with `System Variable Events`.
- Only include config values that are not empty and not `(not set)`.

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

## Identify SYSVARREF References in Config
- Many config values store `SYSVARREF:{GUID}#NN@SysVar`.
- Extract all `SYSVARREF` values for later resolution.

SQL (find SYSVARREF references):
```sql
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

## Resolve SYSVARREF to Human-Readable Names
Resolution steps:
1) **Driver GUID**: extract `{GUID}` from SYSVARREF and match `DriverData.DriverId`.
2) **System variable registry**: match full SYSVARREF to `SystemVariableIds.SysVarRef`.
3) **Variable name**: search `DriverData.SystemVariables` XML for the `sysvar` token.

SQL (driver + sysvar registry):
```sql
-- Driver GUID -> driver record
SELECT DriverDeviceId, DeviceId, DriverId, DriverVersion, Author
FROM DriverData
WHERE DriverId = '{GUID_FROM_SYSVARREF}';

-- SYSVARREF -> registry entry
SELECT SysVarID, SysVarRef, DeviceId
FROM SystemVariableIds
WHERE SysVarRef = 'SYSVARREF:{GUID}#NN@SysVar';
```

Python snippet (extract variable name from SystemVariables XML):
```python
import re
import sqlite3

path = "YOUR_PROJECT.apex"
sysvarref = "SYSVARREF:{GUID}#NN@SysVar"

# Extract driver GUID and sysvar token
match = re.search(r"\{[A-F0-9\-]+\}", sysvarref, re.IGNORECASE)
if not match:
    raise ValueError("SYSVARREF missing GUID")

driver_id = match.group(0)
sysvar = sysvarref.split("@", 1)[1]

conn = sqlite3.connect(path)
cur = conn.cursor()
cur.execute("SELECT DriverDeviceId, SystemVariables FROM DriverData WHERE DriverId = ?", (driver_id,))
row = cur.fetchone()
conn.close()

if not row:
    raise ValueError("DriverId not found")

driver_device_id, xml_text = row
pattern = re.compile(r"<variable\s+name='([^']+)'\s+sysvar='" + re.escape(sysvar) + r"'", re.IGNORECASE)
match = pattern.search(xml_text or "")
if match:
    print("DriverDeviceId:", driver_device_id)
    print("Variable Name:", match.group(1))
else:
    print("Variable Name: [UNRESOLVED]")
```

## Output Mapping Expectations
- Output must preserve the original SYSVARREF or unresolved token if resolution fails.
- Mark missing resolutions explicitly (example: `[UNRESOLVED]`).
- Maintain a link to the raw log line number when the mapping is used.

## Notes and Constraints
- `.apex` files are read-only inputs.
- Do not infer names; only map if the data is present in the `.apex`.
- Filter out `Debug*` config when exporting general driver settings unless explicitly required.
