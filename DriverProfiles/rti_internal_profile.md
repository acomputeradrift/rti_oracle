# RTI Internal Profile (Page Index Mapping)

## Purpose
- Extract per-device page index and page name mappings from an `.apex` file.
- Use the mapping to format diagnostics log lines that reference RTI page navigation.
- Keep `.apex` inputs read-only and avoid inferred names.

## Data Sources in the `.apex` SQLite Database
- `RTIDeviceData`: links RTI addresses to device IDs.
- `RTIDevicePageData`: contains `PageOrder` per RTI address.
- `PageNames`: resolves page name IDs to human-readable names.
- `Devices`: provides device metadata for cross-checking.

## Mapping: Page Index -> Page Name (Per Device)
- `RTIDevicePageData.PageOrder` is the page index.
- Join `RTIDevicePageData` to `RTIDeviceData` on `RTIAddress`.
- Join `PageNames` on `PageNameId` to get `PageName`.
- Include devices that have no pages (NULL `PageIndex` / `PageName`).

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

## Usage in Diagnostics Formatting
- When a log line references a page index and device context, look up `DeviceId + PageIndex`.
- Replace the raw page index with the mapped `PageName` when available.
- If the mapping is missing, output the raw page index and mark the name as `[UNRESOLVED]`.
- Always retain a direct link to the raw log line number in processed output.

## Output Expectations
- Do not fabricate names or indices that are not present in the `.apex`.
- Keep unresolved identifiers visible in output for traceability.

## Notes and Constraints
- `.apex` files are read-only inputs.
- Mapping is per-device; do not assume page indices are global.
- If multiple pages share the same index (unexpected), emit all candidates and mark as ambiguous.
