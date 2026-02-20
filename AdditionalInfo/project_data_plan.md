# Project Data Plan - Additional Info Upload

## Scope
This plan covers:
- Uploading the Additional Info spreadsheet.
- Validating sheet names and headers.
- Building lookup data used by downstream mapping.

This plan does not modify any code. It documents intended behavior only.

## Inputs
- Additional Info spreadsheet (.xlsx).
- Raw log files (used only to confirm sheet needs and indices).

## Spreadsheet Sheets and Schemas
- Sheet tab name MUST match the driver device name from the .apex file (stable, not display name).
- If a sheet tab does not match a driver device name, do not load that sheet and record an error.
- Loading continues for all matching sheets.
- Per-sheet schema is defined by the matching DriverProfile (not hardcoded in Project Data).

## Upload and Ingestion Plan
- Accept a single .xlsx file via the existing Additional Info upload flow.
- Read sheet tab names and validate they match driver device names from the .apex file.
- For each matching sheet, resolve the DriverProfile and use its schema to parse rows.
- For each non-matching sheet, record a load error and continue.
- Build in-memory lookup maps based on the DriverProfile schema outputs.
- Attach the lookup maps to ProjectDataBundle.Additional for use by the Processing Engine.
- Do not modify .apex data or other unrelated project data paths.

## Output Contract to Driver Profile Module
- Provide read-only lookup maps as defined by DriverProfile schema outputs.

## Open Items
- Confirm DriverProfile schema format for Additional Info extraction.
- Confirm whether missing entries should return explicit "UNRESOLVED" markers.

## Scope Confirmation
- No code changes made.
- No source documents modified.
- Plan limited to Additional Info upload and lookup preparation.
