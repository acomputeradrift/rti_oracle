# RTI AD-64 Profile

## Purpose
- Extract only the AD-64 groups, sources, and zones needed for diagnostics mapping.
- Provide lookup rules for the Analysis Engine without inferring missing data.

## Identification
- Primary deviceName: RTI AD-64
- Aliases: none

## Data Sources in the `.apex` SQLite Database
- Devices: device identity for DriverData (DeviceId, Name)
- DriverData: ties DriverDeviceId to DeviceId
- DriverConfig: driver configuration key/value pairs

## Identify Driver Rows
- Join Devices + DriverData by DeviceId.
- Filter by Devices.Name = 'RTI AD-64'.

SQL (filtered rows):
```sql
SELECT
  dd.DriverDeviceId,
  dd.DeviceId,
  d.Name AS DeviceName
FROM DriverData dd
JOIN Devices d ON dd.DeviceId = d.DeviceId
WHERE d.Name = 'RTI AD-64';
```

## Extract Fields for Apex Discovery
- Required counts: GroupCount, SourceCount, ZoneCount
- Required names: GroupName*, SourceName*, ZoneName*
- Use counts to include only the used names:
  - GroupName1..GroupName{GroupCount}
  - SourceName1..SourceName{SourceCount}
  - ZoneName1..ZoneName{ZoneCount}
- Exclude all other keys.

SQL (discovery extract):
```sql
SELECT
  dc.DriverDeviceId,
  dc.Name AS ConfigKey,
  dc.Value AS ConfigValue
FROM DriverConfig dc
JOIN DriverData dd ON dc.DriverDeviceId = dd.DriverDeviceId
JOIN Devices d ON dd.DeviceId = d.DeviceId
WHERE d.Name = 'RTI AD-64'
  AND (
    dc.Name IN ('GroupCount','SourceCount','ZoneCount')
    OR dc.Name LIKE 'GroupName%'
    OR dc.Name LIKE 'SourceName%'
    OR dc.Name LIKE 'ZoneName%'
  )
ORDER BY dc.DriverDeviceId, dc.Name;
```

## Mappings for Analysis Engine
- GroupName index maps group IDs to human-readable group names.
- SourceName index maps source IDs to human-readable source names.
- ZoneName index maps zone IDs to human-readable zone names.
- If a count or name is missing, emit the raw identifier and mark as `[UNRESOLVED]`.

Example mapping usage:
- Input: Group 6
- Output: Group 6 Zone Name OR `[UNRESOLVED] Group 6` if missing

## Output Expectations
- Preserve original identifiers when resolution fails.
- Mark missing data explicitly as `[UNRESOLVED]`.
- Keep a reference to the raw log line number.

## Notes and Constraints
- `.apex` files are read-only inputs.
- Do not infer names; only map if data exists in `.apex`.
