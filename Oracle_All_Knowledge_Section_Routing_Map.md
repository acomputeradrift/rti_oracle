# Oracle Section Routing Map

This file defines the **only allowed section tags** and the **exact destination file** for Session Wrap-Up routing.

## Rules

- Each Session Wrap-Up item must use **exactly one** section tag from this list.

- If the correct section is uncertain: output `INVALID`.

- Session Wrap-Up **does not append**. Appending happens only after approval.

## Sections (Tags) and Destinations

### ApexDiscovery

- Path: `\\Mac\Home\Desktop\Development\Oracle\ApexDiscovery\apex_discovery_all_info.md`

- Scope (what belongs here): *(fill in later if needed; keep it short)*

- Excludes (what does not belong here): *(fill in later if needed; keep it short)*

### AdditionalInfo

- Path: `\\Mac\Home\Desktop\Development\Oracle\AdditionalInfo\additional_info_all_info.md`

- Scope (what belongs here): *(fill in later if needed; keep it short)*

- Excludes (what does not belong here): *(fill in later if needed; keep it short)*

### DriverLogLevels

- Path: `\\Mac\Home\Desktop\Development\Oracle\DriverLogLevels\driver_log_levels_all_info.md`

- Scope (what belongs here): *(fill in later if needed; keep it short)*

- Excludes (what does not belong here): *(fill in later if needed; keep it short)*

### RawDiagnosticFeeds

- Path: `\\Mac\Home\Desktop\Development\Oracle\RawDiagnosticFeeds\raw_diagnostic_feeds_all_info.md`

- Scope (what belongs here): *(fill in later if needed; keep it short)*

- Excludes (what does not belong here): *(fill in later if needed; keep it short)*

### DriverProfiles

- Path: `\\Mac\Home\Desktop\Development\Oracle\DriverProfiles\driver_profiles_all_info.md`

- Scope (what belongs here): *(fill in later if needed; keep it short)*

- Excludes (what does not belong here): *(fill in later if needed; keep it short)*

### ProcessingEngine

- Path: `\\Mac\Home\Desktop\Development\Oracle\ProcessingEngine\processing_engine_all_info.md`

- Scope (what belongs here): *(fill in later if needed; keep it short)*

- Excludes (what does not belong here): *(fill in later if needed; keep it short)*

### Filter/Find

- Path: `\\Mac\Home\Desktop\Development\Oracle\Filter/Find\filter__find_all_info.md`

- Scope (what belongs here): *(fill in later if needed; keep it short)*

- Excludes (what does not belong here): *(fill in later if needed; keep it short)*

### FileExport

- Path: `\\Mac\Home\Desktop\Development\Oracle\FileExport\file_export_all_info.md`

- Scope (what belongs here): *(fill in later if needed; keep it short)*

- Excludes (what does not belong here): *(fill in later if needed; keep it short)*

### UserInterface

- Path: `\\Mac\Home\Desktop\Development\Oracle\UserInterface\user_interface_all_info.md`

- Scope (what belongs here): *(fill in later if needed; keep it short)*

- Excludes (what does not belong here): *(fill in later if needed; keep it short)*

### ApplicationPackaging

- Path: `\\Mac\Home\Desktop\Development\Oracle\ApplicationPackaging\application_packaging_all_info.md`

- Scope (what belongs here): *(fill in later if needed; keep it short)*

- Excludes (what does not belong here): *(fill in later if needed; keep it short)*

## Session Wrap-Up Prompt (Phase 1 — Extract + Assign, no writing)

```text

Session Wrap-Up.


Extract only:

- Confirmed

- Implemented

- Rejected


For each item:

- Assign exactly one valid section tag from the Routing Map found here ->\\mac\Home\Desktop\Development\Oracle\Oracle_All_Knowledge_Section_Routing_Map.md.

- One sentence only.

- No interpretation.

- No merging.

- If uncertain, output INVALID.


Do not append anything yet.

```

## Append Prompt (Phase 2 — Append approved items only)

```text

Append the approved Session Wrap-Up items to the destination file for their section tag as defined in the Routing Map found here ->\\mac\Home\Desktop\Development\Oracle\Oracle_All_Knowledge_Section_Routing_Map.md.

Append only.

Do not modify existing content.

```
