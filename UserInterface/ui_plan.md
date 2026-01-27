# RTI Oracle UI vNext Plan (No Code)

## Scope Confirmation
- Create a UI plan only; no code changes.
- Do not modify any existing source files or documents.
- Follow existing UI patterns and colors; no new concepts or unrelated features.

## Layout Overview (Approved)
- ~~Title row: small logo to the left of the window title text “RTI Oracle by FP&C Ltd.”~~
  - ~~If the logo cannot sit in the title area, omit it entirely.~~
- Top control area: connect controls and project upload box on the same horizontal level (no extra vertical height).
- Discovery row remains present below the connect row.
- Driver Log Levels panel maximizes space for diagnostics by being collapsible and vertically expandable.
- ~~Diagnostics area includes a collapsible Filter bar, per-pane Find controls, and a Download Logs button on the right.~~
- Logo is centered between the Connect and Project Data boxes, sized to match the box height (or centered if it cannot be scaled).
- Connect and Project Data boxes are the same size, anchored left/right, with no extra vertical padding beyond content.
- Diagnostics filter row is inline with the Diagnostics title and buttons (no collapsible filter bar).

## Annotated Layout Drawing (ASCII)
```text
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ ┌──────────────────────────────────────────────┐        (logo)        ┌──────────────────────────────────────────────────┐                 │
│ │ SHP IP: [ 192.168.1.143 ▾ ] [Connect] [Disco] │                     │ Project Data                                      │                 │
│ │ Discover [Discover]  Discovered: [dropdown]   │                     │ [Upload .apex]  TEST - System Manager v11.3.apex   │                 │
│ └──────────────────────────────────────────────┘                     │ [Upload .xlsx]  System Manager Info v4.xlsx       │                 │
│                                                                      └──────────────────────────────────────────────────┘                 │
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Driver Log Levels ▾                                                                                                                           │
│ ┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐ │
│ │ (default height = 4 rows; vertical scroll; vertically expandable)                                                                           │ │
│ │ [Driver A] [1][2][3]   [Driver B] [1][2][3]   [Driver C] [1][2][3]   [Driver D] [1][2][3]                                                    │ │
│ │ [Driver E] [1][2][3]   [Driver F] [1][2][3]   [Driver G] [1][2][3]   [Driver H] [1][2][3]                                                    │ │
│ │ [Driver I] [1][2][3]   [Driver J] [1][2][3]   [Driver K] [1][2][3]   [Driver L] [1][2][3]                                                    │ │
│ │ [Driver M] [1][2][3]   [Driver N] [1][2][3]   [Driver O] [1][2][3]   [Driver P] [1][2][3]                                                    │ │
│ └──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Diagnostics  Filter: Keyword [ Driver Command ]  Start: [yyyy-mm-dd …] End: [yyyy-mm-dd …]  [Filter] [Clear] Count: 1306   [Download Logs] [Clear] │
│ ┌────────────────────────────────────────────────────────────────────────┐ │ ┌───────────────────────────────────────────────────────────┐ │
│ │ Raw Output  Find: [ Clipsal ] [Prev] [Next] [Clear] Count: 12           │ │ │ Processed Output  Find: [ Clipsal ] [Prev] [Next] [Clear] Count: 12 │ │
│ │ (scrolling log)                                                        │ │ │ (scrolling log, existing light-on-dark style preserved)    │ │
│ └────────────────────────────────────────────────────────────────────────┘ │ └───────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

## Detailed UI Requirements

### Top Left (Connection + Discovery)
- SHP IP input should:
  - Prepopulate the most recent IP.
  - Provide a dropdown of previously used IPs.
  - Store a history of 10 IPs, no duplicates.
- Remove these items from the top row:
  - TCP Capture (RAW) checkbox.
  - Send probe on connect (diagnostic) checkbox.
- Discovery row remains:
  - “Discover” button.
  - “Discovered:” label and dropdown.

### Top Right (Project Data Upload Box)
- Maintain current size and visual style; do not increase vertical height of the top area.
- Keep content on two single lines (no wrapping):
  - Line 1: Upload Project (.apex) button + current project file name.
  - Line 2: Upload Additional Info (.xlsx) button + current additional info file name.
- The box remains enclosed and styled like the existing Project Data box.
- Box height is content-driven (title + two rows + padding only); no extra vertical space.

### Driver Log Levels Panel
- Add a collapse/expand arrow next to the “Driver Log Levels” title.
  - Collapsed: show title row only.
  - Expanded: show content.
- Default expanded height shows no more than 4 driver rows tall.
- ~~Horizontal scrolling only (no vertical scrollbars).~~
- Drivers are laid out in a 4xN grid (fill left-to-right, then next row).
- Vertical scrolling enabled; no horizontal scrollbars.
- Panel can be expanded vertically (e.g., drag to increase height) using a hover-only splitter with no visible bar.

### Diagnostics Area
- Add “Download Logs” button on the right side of the Diagnostics header.
- ~~Add a collapsible “Filter” bar with arrow toggle next to the title.~~
- Filter row (order and wording must match) inline with the Diagnostics header:
  - `Diagnostics  Filter: Keyword [text] Start: [date/time] End: [date/time] [Filter] [Clear] Count: N  [Download Logs] [Clear]`
- Filter applies to both Raw and Processed logs simultaneously.
- Find controls move into each log pane header, separately for Raw and Processed.
  - Each pane has its own Find controls and count.
  - Find is live-as-you-type (no Find button).
  - Find navigates within that pane’s filtered results.

## Behavior Notes
- UI order and spacing should remain consistent with existing patterns.
- No new colors or themes; match current UI styling.
- Diagnostics area should be maximized by allowing other panels to collapse.

## Implementation Touchpoints (No Code Yet)
- `MainWindow.xaml`: layout updates, new controls, and collapsible sections.
- `MainWindow.xaml.cs`: state handling (collapse/expand, IP history, filter/find behavior).
- Local persistence for IP history (mechanism to be chosen per existing patterns).

## Open Questions (Require Confirmation)
- None at this time.
