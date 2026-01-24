```json
{
  "pageIndexMap": [
    "Open the .apex as a read-only SQLite database.",
    "Query RTIDeviceData + RTIDevicePageData + PageNames to extract DeviceId, PageIndex (PageOrder), PageName.",
    "Build map entries keyed by `deviceId|pageIndex` with value `pageName`.",
    "Keep NULL page names unresolved (do not fabricate); include only rows returned."
  ],
  "sysVarRefMap": [
    "Parse SYSVARREF tokens from diagnostics inputs (or source list if present).",
    "For each SYSVARREF, match its GUID to DriverData.DriverId to locate driver context.",
    "Resolve full SYSVARREF via SystemVariableIds.SysVarRef to get DeviceId.",
    "Read driver SystemVariables XML to map the sysvar token to a human-readable variable name.",
    "Store map entries keyed by full SYSVARREF with value `{driverDeviceId, variableName, deviceId}`; leave unresolved fields explicit if missing."
  ],
  "driverConfigMap": [
    "Extract DriverConfig joined to DriverData and Devices, excluding `Debug*` keys.",
    "Respect MaxZones/MaxSources/Inputs/Outputs to limit related Name groups; otherwise include all for that group.",
    "Build per-driver map keyed by DriverDeviceId with `{deviceName, config: {key: value}}`.",
    "Never infer values; only include extracted key/value pairs."
  ],
  "meta": [
    "Record `{projectId, generatedAt, apexPathHash, schemaVersion}`.",
    "Use a stable schemaVersion that matches the output contract.",
    "Compute apexPathHash from the .apex path (read-only)."
  ]
}
```
