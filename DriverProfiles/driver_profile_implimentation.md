# Driver Profile Implementation Plan

## Purpose
- Guide Codex AI on writing code to create and integrate driver profiles.
- Ensure profiles are modular, scalable, and easy to add.
- Define how data is passed between Apex Discovery and the Analysis Engine.

## Scope
- This plan describes profile integration and data flow only.
- Apex Discovery and Analysis Engine internals are out of scope.

## Workflow Overview
- On `.apex` upload, extract driver names using deviceName (not display name).
- Use those driver names to find matching driver profiles.
- If a profile is found, use it to determine which fields to extract.
- Apex Discovery extracts fields and produces normalized lookup data.
- Analysis Engine uses the same profile plus lookup data to map/format log lines.
-
- Internal RTI profile is always included (no deviceName match required).

## Data Handoff Contract
- Apex Discovery outputs a profile-keyed lookup bundle.
- Analysis Engine consumes the profile and lookup bundle unchanged.
- Unresolved identifiers must remain explicit as `[UNRESOLVED]`.

## Modular Profile Library (Scalable)
- Store all profiles together in a dedicated directory.
- One profile per file with a clear, stable naming scheme.
- Maintain a simple index/registry for fast lookup by deviceName.
- New profiles should be added without touching existing profiles or core logic.

## Profile Template Requirements
Each profile must define:
- Identification: driver name (deviceName) and optional aliases.
- Discovery extraction: tables, fields, filters, joins.
- Discovery keys/prefixes for field selection (e.g., `GroupCount` gates `GroupName*`).
- Analysis mapping: ID resolution steps, formatting rules, unresolved handling.
- Output expectations: preserve raw identifiers and raw log line references.

## New Profile Lifecycle
1) Copy the template file.
2) Fill in all required sections.
3) Register in the profile index/registry.
4) Validate naming and expected extraction fields.

## Implementation Notes
- Profiles are documentation artifacts and must not modify `.apex` inputs.
- Profiles may only influence diagnostics verbosity via Driver Log Level settings.
- Do not invent mappings or infer missing data.

## Current Implementations (Code)
- Profile catalog: `SHPDiagnosticsViewer/DriverProfiles/DriverProfileModule.cs`
- AD-64 profile: `SHPDiagnosticsViewer/DriverProfiles/RtiAd64Profile.cs`
  - Uses `GroupCount`, `SourceCount`, `ZoneCount` to gate `GroupName*`, `SourceName*`, `ZoneName*`
  - Count keys are not included in output; they only limit extraction
- Internal RTI profile: `SHPDiagnosticsViewer/DriverProfiles/RtiInternalProfile.cs`
  - Always included and used for page mapping extraction
