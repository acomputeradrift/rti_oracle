# Apex Discovery Output Format (Preload Contract)

## Purpose
- Define the preload output shape that Apex Discovery produces for the Processing Engine.
- Ensure upload-time extraction is fast and live processing is extremely fast.
- Restrict output to only the data required for Driver Profiles and diagnostics mapping.

## Output Shape (Proposed Contract)
- `pageIndexMap`: key `deviceId|pageIndex` -> `pageName`
- `sysVarRefMap`: key `SYSVARREF:{GUID}#NN@SysVar` -> `{driverDeviceId, variableName, deviceId}`
- `driverConfigMap`: key `driverDeviceId` -> `{deviceName, config: {key: value}}` (filtered, no Debug*)

## Notes
- `.apex` files are read-only inputs.
- Output should include only extracted and validated values (no inferred names).
- Missing mappings must remain unresolved (do not fabricate names).
- Keep the contract stable so Processing Engine lookups remain O(1).
