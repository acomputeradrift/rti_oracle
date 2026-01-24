# apex_todo.md
# Generated: 2026-01-23T19:10:49.458114Z

# INTERNAL TODO LIST — APEX DATA EXPLORATION PLAN

## Global Rules
- Never assume schema stability
- Never infer relationships that are not directly observable
- Every attempt must be made to link an internal ID/index to a human-readable name
- Always record:
  - what was queried
  - what was returned
  - what was missing
- Explicitly note empty tables, nullable fields, duplicate rows
- Ambiguity is a blocking finding, not a workaround

---

## 1. Project-Level Metadata
**TODOs**
- Inventory all tables in the SQLite DB
- Record table names and row counts
- Identify any table or field that may represent:
  - project name
  - project version
  - creation/modification timestamps
- Determine whether project metadata is explicit, implicit, or absent

---

## 2. Devices
**TODOs**
- Query `Devices`
- Enumerate all columns
- Verify uniqueness of `DeviceId`
- Identify device types (hardware, panel, virtual)
- Determine device scope:
  - Global device (controls sources in multiple rooms)
  - Room device (controls sources only in its assigned room)
- Attempt to link:
  - DeviceId → Device Name → Device Scope

---

## 3. Rooms
**TODOs**
- Query `Rooms`
- Enumerate all columns
- Determine how rooms and devices are linked:
  - direct foreign key
  - indirect mapping
  - implicit association
- Attempt to resolve:
  - RoomId → Room Name → Associated DeviceIds

---

## 4. RTI Identity Model
**TODOs**
- Determine what an RTIDevice represents
- Determine what an RTIAddress represents
- Identify which tables use:
  - DeviceId
  - RTIAddress
- Determine whether RTIAddress is:
  - unique globally
  - shared across devices
- Document structural purpose of each identifier

---

## 5. Device-Scoped Pages
**TODOs**
- Query `RTIDevicePageData`
- Enumerate columns
- Verify how pages are linked to devices
- Determine whether PageId is global or device-scoped
- Identify devices with zero pages

---

## 6. Page Names
**TODOs**
- Query `PageNames`
- Verify uniqueness of `PageNameId`
- Identify PageNameIds referenced but not defined
- Confirm PageName resolution reliability

---

## 7. Page Structure (Layers)
**TODOs**
- Query `Layers`
- Enumerate columns
- Determine relationship between layers and pages
- Assess whether layers can be flattened for diagnostics use

---

## 8. Button Instances on Pages
**TODOs**
- Query `LayerButtons`
- Enumerate columns
- Verify ButtonId uniqueness scope
- Identify buttons lacking both tags and text

---

## 9. Button Tags and Text Resolution
**TODOs**
- Query `ButtonTagNames`
- Query `ButtonTextTags`
- Validate resolution rules:
  - ButtonTagId ≥ 0
  - ButtonTagId < 0
- Identify any ButtonIds that fail label resolution

---

## 10. Sources
**TODOs**
- Query `SourceLabels`
- Enumerate columns
- Determine how sources are linked to:
  - rooms
  - devices
- Attempt to resolve:
  - SourceLabelId → Source Name → Room(s)/Device(s)

---

## 11. Variables
**TODOs**
- Query `Variables`, `VariableNames`, `SystemVariableIds`
- Verify naming completeness
- Identify variables without names or values
- Assess whether variables are safe to expose

---

## 12. Macros
**TODOs**
- Query `Macros`
- Query `MacroSteps` / `MacroStepsView`
- Verify step ordering and references
- Identify steps referencing unknown IDs

---

## 13. Events
**TODOs**
- Query `Events`, `RoomEvents`, `SourceEvents`
- Verify naming consistency
- Identify unresolved references

---

## 14. IO / Relay / Sense Maps
**TODOs**
- Query `RelayModeMap`, `RelayTypeMap`, `SenseModeMap`
- Enumerate columns and values
- Determine whether these are pure enums or partial mappings

---

## Meta Review
**TODOs**
- Identify orphaned or unused tables
- Classify each section as:
  - Usable
  - Conditionally usable
  - Not usable with current data
