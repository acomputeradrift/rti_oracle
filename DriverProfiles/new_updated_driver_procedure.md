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

Done criteria:
- Supported line patterns map correctly.
- Unsupported patterns are intentionally rejected (or tagged as incomplete at pipeline level).

### 3) If Additional Info is required, define schemas
- Add `AdditionalInfoSchemas` to the profile.
- Use explicit headers and roles (`InputIndex`, `InputName`, `RelayIndex`, `RelayName`, etc.).
- Ensure schema sheet names are stable and documented.

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

## Unhandled Report Evidence Requirement

### Purpose
Ensure every unhandled/tagged processed message can be traced back to the exact raw diagnostic line used to produce it.

### Required Output Shape
- Unhandled report JSON must include `SchemaVersion: 2`.
- Grouping remains by `DriverName` and `Tag`.
- Each tag group must contain `Entries` (not duplicate `Messages` arrays).
- Each entry must contain:
  - `ProcessedMessage`
  - `RawSamples` (0..n)
- Each `RawSamples` item must include:
  - `RawLineNumber`
  - `RawText` (full raw text, no truncation)

### Correlation Rules
- Correlate processed tagged lines to raw lines by numbered log line index.
- Preserve the complete raw line payload exactly as captured after the line number.
- Deduplicate raw samples by `(RawLineNumber, RawText)` within each `ProcessedMessage`.
- Do not guess or synthesize raw payloads when correlation is missing; leave `RawSamples` empty.

### Done Criteria
- A tagged processed line in the unhandled report can be traced to full raw line evidence.
- Report reviewers can infer command arguments (for example app/group/state tuples) directly from `RawText`.

## Failure Patterns to Watch
- Profile exists but no line pattern match: `[Incomplete Profile!]`.
- Mapping expected but lookup missing: `[No Map!]`.
- Resolver failed without specific no-map context: `[Unresolved!]`.
- Formatting path missing sentence rule: `[No Format!]`.

## Readability Formatting Rules

### Purpose
Define consistent, driver-agnostic wording for processed diagnostic output so similar actions read the same across driver profiles.

### Scope
- Applies to readability formatting output only.
- Does not change mapping logic, unresolved tagging, or profile matching behavior.
- Intended to be updated incrementally as new patterns are confirmed.

### Core Principles
- Prefer short, plain-language sentences.
- Keep sentence structure stable across drivers.
- Use one action verb style per intent.
- Keep unresolved data explicit; do not guess names.
- End formatted sentences with a period.

### Canonical Sentence Shape
- `<Target> <action phrase>.`

Examples:
- `Gym volume decreased.`
- `Master Pendant turned Off.`
- `Source set to Apple TV.`

### Canonical Intents And Wording

#### 1. Binary Power / Switch / Relay
- On: `turned On`
- Off: `turned Off`
- Toggle: `toggled`

Examples:
- `Spa turned Off.`
- `Entry Lights turned On.`
- `Garage Relay toggled.`

#### 2. Volume (Delta)
- Increase: `volume increased`
- Decrease: `volume decreased`
- With amount: `volume increased by <amount>` / `volume decreased by <amount>`

Examples:
- `Zone A volume increased.`
- `Main volume decreased by 1.0 dB.`

#### 3. Volume (Absolute)
- `volume set to <value>`

Examples:
- `Main volume set to -30 dB.`

#### 4. Source / Input / Routing
- `source set to <source>`
- `input set to <input>`

Examples:
- `Source set to Apple TV.`
- `Theater input set to HDMI 2.`

#### 5. Dimming / Level
- Absolute: `dimmed to <level>`
- Ramp: `ramped to <level> over <duration>`

Examples:
- `Kitchen Pendants dimmed to 50%.`
- `Hallway ramped to 100% over 2 seconds.`

#### 6. Mute
- Absolute: `mute set to On|Off`
- Toggle: `mute toggled`

Examples:
- `Gym mute toggled.`
- `Main mute set to Off.`

#### 7. Scene / Task / Trigger
- Scene: `scene <name> activated`
- Task: `task <name> executed`

Examples:
- `Scene Evening activated.`
- `Task AUDIO - Hallway Button LED OFF executed.`

### Unresolved And Tagging Rules
- Formatting should not inject diagnostic tags as readability placeholders.
- Tags such as `[No Map!]`, `[Unknown State!]`, `[No Profile!]`, `[Incomplete Profile!]` reflect mapping/profile state and remain separate from sentence wording.
- Preserve unresolved identifiers when lookup data is missing.

### Consistency Rules
- Use `increased/decreased` for directional adjustments.
- Use `set to` only for absolute assignment.
- Use `turned On/Off` for binary state transitions.
- Do not mix equivalent verbs for the same intent in different profiles.

### Driver-Specific Exceptions
- If a driver command has unique semantics that do not fit canonical intents, document the exception in this procedure before implementation.

### Change Process
1. Add/adjust tests first for expected wording.
2. Update formatter behavior.
3. Validate no regressions in existing formatter and mapping tests.
4. Record any approved exception in this procedure.
