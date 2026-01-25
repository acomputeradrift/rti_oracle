# Driver Profile Purpose

## Purpose
- Define how driver profiles are used by the app.
- Describe how profiles receive data and send data to other modules.
- Provide reusable structure for new driver profiles.

## Scope and Constraints
- Profiles are documentation artifacts that describe how to interpret `.apex` inputs.
- `.apex` files and project spreadsheets are read-only inputs.
- Profiles may only influence diagnostics verbosity via Driver Log Level settings.
- Do not invent mappings; unresolved identifiers must be explicit.

## System Context

### Inputs
- `.apex` SQLite database tables referenced by each profile.
- Raw diagnostics log lines emitted by SHP drivers.

### Outputs
- Structured field extraction rules for Apex Discovery.
- Log line formatting and mapping instructions for the Analysis Engine.
- Explicit `[UNRESOLVED]` markers when data is missing.

## How Profiles Are Used

### 1) Apex Discovery Module (Field Extraction)
- Profiles tell Apex Discovery which tables and fields to extract.
- Profiles define filters and joins required to locate driver-specific data.
- Outputs are normalized lookup sets for later diagnostics mapping.

### 2) Analysis Engine (Log Formatting and Mapping)
- Profiles define how to match log lines to driver context.
- Profiles specify how to resolve IDs to human-readable names.
- If a mapping is missing, emit the raw identifier and mark as `[UNRESOLVED]`.
- Always maintain a link to the raw log line number.

## Data Flow Summary
- Apex Discovery reads `.apex` data using profile-defined queries.
- Discovery output is passed to the Analysis Engine as lookup data.
- Analysis Engine formats logs using the profile mappings.

## Reusable Profile Template

```markdown
# <Driver Name> Profile

## Purpose
- Describe what this profile extracts and why.
- State which module(s) consume the results.

## Data Sources in the `.apex` SQLite Database
- <TableName>: <what it provides>
- <TableName>: <what it provides>

## Identify Driver Rows
- <How to locate the driver rows>

SQL (filtered rows):
```sql
SELECT ...
```

## Extract Fields for Apex Discovery
- <Fields to extract>
- <Required filters>

SQL (discovery extract):
```sql
SELECT ...
```

## Mappings for Analysis Engine
- <ID -> name mapping steps>
- <Rules for unresolved tokens>

Example mapping usage:
- Input: <raw log token>
- Output: <resolved name or [UNRESOLVED]>

## Output Expectations
- Preserve original identifiers when resolution fails.
- Mark missing data explicitly as `[UNRESOLVED]`.
- Keep a reference to the raw log line number.

## Notes and Constraints
- `.apex` files are read-only inputs.
- Do not infer names; only map if data exists in `.apex`.
```

## Snippets from Existing Profiles

### System Variable Events: SYSVARREF resolution
```python
match = re.search(r"\{[A-F0-9\-]+\}", sysvarref, re.IGNORECASE)
if not match:
    raise ValueError("SYSVARREF missing GUID")
```

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

### RTI Internal: Page index mapping
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
