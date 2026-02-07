# Additional Info Implementation Guide

## Scope
This guide describes how to build the AdditionalInfo module behavior without prescribing concrete code signatures. It ties the plan to existing files and patterns and is scoped to AdditionalInfo only.

## Goal
Load Additional Info from a `.xlsx` file and prepare lookup data for downstream driverProfile mapping. Sheet tabs must match driver device names from the `.apex` file. Unmatched sheets produce errors but do not block matches.

## Existing Entry Points and Patterns
- Upload entry point: `MainWindow.xaml.cs` (`UploadAdditionalInfo_Click`)
- UI controls: `UI/Panels/ProjectDataPanel.xaml`
- Settings history: `Settings/Services/AdditionalInfoService.cs`, `Settings/Models/OracleSettings.cs`
- Project data extraction flow: `ProjectData/ProjectDataExtraction.cs`
- AdditionalInfo extractor hook: `ProjectData/Extractors/AdditionalDataExtractor.cs`
- Data container: `ProjectData/Models/ProjectDataBundle.cs` (`AdditionalData`)
- Driver profiles: `DriverProfiles/*`

## Required Inputs
- `.apex` extraction output that includes driver device names (stable, not display names).
- Additional Info `.xlsx` file.
- DriverProfile schemas (per driver) that define how to parse matched sheets.

## Implementation Flow
1) **Capture AdditionalInfo path**
   - Store the selected AdditionalInfo file path in memory in `MainWindow.xaml.cs`.
   - Keep existing recents behavior via `AdditionalInfoService` and settings.

2) **Resolve driver device names**
   - Use the `.apex` extraction output in `MainWindow.xaml.cs` to build a set of driver device names.
   - These names are the only allowed sheet tab names.

3) **Match sheets to driver device names**
   - In `AdditionalDataExtractor`, read all sheet tab names.
   - Exact match only: sheet name == driver device name.
   - For unmatched sheets, record an error and continue.

4) **Parse via driverProfile schema**
   - For each matched sheet, obtain the driverProfile schema for that driver.
   - Validate required headers and parse rows according to the schema.
   - Do not hardcode per-driver columns in AdditionalInfo; defer to the schema.

5) **Store parsed results**
   - Expand `ProjectDataBundle.Additional` to store parsed outputs keyed by driver device name.
   - Keep data structures generic, aligned to driverProfile schema outputs.

6) **Integrate into processing flow**
   - After `.apex` extraction completes, run `AdditionalDataExtractor` and attach results to the `ProjectDataBundle`.
   - Existing processing should consume the bundle without changes to unrelated features.

7) **Error handling**
   - Collect errors for unmatched sheets and schema validation failures.
   - Surface a summary error via existing UI patterns (e.g., `MessageBox`), but allow successful sheets to load.

## Behavior Constraints
- No new dependencies.
- Spreadsheet and `.apex` are read-only inputs.
- No changes to unrelated modules or features.
- No guessing or inferred mappings outside the driverProfile schema.

## Scope Confirmation
- This guide is limited to AdditionalInfo loading and preparation.
- DriverProfile parsing logic remains in the driverProfile module.
