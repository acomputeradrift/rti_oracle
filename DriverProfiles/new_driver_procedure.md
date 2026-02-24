# New Driver Procedure

## Purpose
Define the required steps to add or update a driver profile so:
- Additional Info template sheets appear when expected.
- Additional Info spreadsheet mappings are consumed.
- Processing output is traceable and test-covered.

## Scope
This procedure applies to:
- New driver profiles.
- Existing profiles gaining new mapping behavior.
- Existing profiles gaining new Additional Info schemas.

## Definitions
- Mapping: project-data substitution from `.apex` / Additional Info (index or ID to name).
- Formatting: profile-driven text shaping that does not require external lookup data.

## Required Steps

### 1) Add or update the profile definition
- Add/update the profile in `OracleByFPCLtd/DriverProfiles/<Driver>Profile.cs`.
- Ensure `Definition.DeviceName` matches diagnostics attribution text exactly.
- If needed, add aliases.
- Register the profile in catalog if new.

Done criteria:
- Profile appears in `DriverProfileCatalog.All()`.

### 2) Add mapper behavior
- Implement or update `IDriverProfileMapper.TryMap`.
- Handle command/event patterns expected from live logs.
- Preserve timestamp when reconstructing mapped text.
- Set unresolved flags honestly when mapping cannot be completed.

Clipsal C-Bus HVAC rule:
- For `Clipsal C-Bus\HVAC\HVAC Zone Setpoint Up(<GroupID>, <ZoneLabel> (<ZoneId>))`, parse:
  - `<GroupID>` as `GroupID`.
  - trailing integer in `<ZoneLabel> (<ZoneId>)` as `ZoneId`.
- Resolve zone using composite key `(GroupID, ZoneId)` from the HVAC mapping sheet.
- Use resolved `ZoneName` in output text.
- Output format:
  - `Driver Command (Clipsal C-Bus): '<ZoneName> setpoint increased.'`

Example:
- Input: `Driver - Command:'Clipsal C-Bus\HVAC\HVAC Zone Setpoint Up(1, Unswitched (0))' Sustain:NO  Sent to 'WorkShop Slave'`
- Parsed: `GroupID=1`, `ZoneId=0`
- If `(6,1) -> Garage`, output: `Driver Command (Clipsal C-Bus): 'Garage setpoint increased.'`

Done criteria:
- Supported line patterns map correctly.
- Unsupported patterns are intentionally rejected (or tagged as incomplete at pipeline level).

### 3) If Additional Info is required, define schemas
- Add `AdditionalInfoSchemas` to the profile.
- Use explicit headers and roles (`InputIndex`, `InputName`, `RelayIndex`, `RelayName`, etc.).
- Ensure schema sheet names are stable and documented.

Clipsal C-Bus HVAC Additional Info template:
- Sheet name: `Clipsal C-Bus HVAC` (or exact profile-defined equivalent).
- Required headers:
  - `GroupID`
  - `ZoneId`
  - `ZoneName`
- Mapping key is `(GroupID, ZoneId)` and value is `ZoneName`.

Done criteria:
- Schema exists in profile and matches expected spreadsheet headers.

### 4) Ensure template planner eligibility
- Confirm the driver that owns the schema is eligible for template planning.
- Internal/system profiles (for example `RTI Internal`) must be included even when they do not surface through `DriverConfigMap`.

Done criteria:
- Exported Additional Info template includes required sheets for that driver.

### 5) Ensure extractor eligibility
- Confirm extractor driver-name eligibility includes the schema owner profile name.
- Verify sheet name and schema name alignment.
- Validate no unmatched-sheet errors for expected sheets.

Done criteria:
- Filled spreadsheet values are loaded into `AdditionalData.Drivers[<DeviceName>]`.

### 6) Add tests first
At minimum:
- Profile mapping test for a representative command/event.
- Unresolved/no-map behavior test.
- Additional Info extraction test for schema headers and row mapping.
- Template planner test for expected schema inclusion.

Done criteria:
- Tests fail before implementation changes.
- Tests pass after implementation.

### 7) Update version stamp
- If mapping/formatting behavior changed, update `DriverProfileVersionCatalog`.

Done criteria:
- Driver profile timestamp reflects latest behavior change.

## RTI RCM-12 / RTI Internal Specific Guardrail
When relay-name mapping depends on `RTI Internal` Additional Info schema:
- Include `RTI Internal` in template planning and extraction driver-name eligibility.
- Verify sheet `RTI RCM-12 Relay Module` is exported and consumed.
- Verify mapped output transforms `RELAY <index>` into relay name from spreadsheet.

## Validation Checklist (must pass)
- Template export contains expected sheet(s).
- Spreadsheet import reports no unmatched expected sheet.
- Mapping output uses spreadsheet names.
- Tags are correct (`[No Map!]`, `[Incomplete Profile!]`, `[Unresolved!]`, etc.).
- Regression tests pass.

## Failure Patterns to Watch
- Profile exists but no line pattern match: `[Incomplete Profile!]`.
- Mapping expected but lookup missing: `[No Map!]`.
- Resolver failed without specific no-map context: `[Unresolved!]`.
- Formatting path missing sentence rule: `[No Format!]`.
