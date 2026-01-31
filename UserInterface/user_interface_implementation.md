# UI Implementation Plan (UI-Only)

## Scope
- UI structure and layout only.
- No code changes or behavioral logic in this plan.
- Follow existing patterns and styles from the current UI.
~~- No code changes or behavioral logic in this plan.~~
- No code changes; UI behavior notes are included only to document agreed requirements.

## Reference Documents
- `ui_plan.md` (approved layout + drawing)
- `Screenshot 2026-01-26 at 3.21.59 PM.png`

## Primary UI Files (to be edited by implementation agent)
- `MainWindow.xaml` (all layout/controls in the main window)

## Layout Changes Mapped to `MainWindow.xaml`

### Title Area
- ~~Place the small logo beside the window title (“RTI Oracle by FP&C Ltd.”).~~
- ~~If the logo cannot be placed in the title area, omit it entirely.~~
- Place the logo centered between the Connect box and Project Data box, sized to match the box height (or vertically centered if it cannot scale).

### Top Left: Connection Row
- Replace the IP `TextBox` with an editable IP `ComboBox` to allow a dropdown (UI element only).
- Remove these UI items from the top row:
  - “TCP Capture (RAW)” checkbox.
  - “Send probe on connect (diagnostic)” checkbox.
- Keep the rest of the row as-is (SHP IP label, Connect/Disconnect, Status).
~~- Replace the IP `TextBox` with an editable IP `ComboBox` to allow a dropdown (UI element only).~~
- Keep the IP as a plain editable field (no dropdown).
- Add a `Connect` title aligned with the `Project Data` title.
- Align the SHP IP row with the Upload Project row; align the Discover row with the Upload Additional Info row.

### Discovery Row
- Preserve the existing “Discover” button + “Discovered:” label + dropdown row.

### Top Right: Project Data Box
- Keep the box at the same vertical height and style.
- Reformat the contents into two single-line rows (no wrapping):
  - ~~Row 1: “Upload Project (.apex)” button + current project file name.~~
  - Row 1: “Upload Project (.apex)” button + recent project dropdown + `Show` button.
  - Row 2: “Upload Additional Info (.xlsx)” button + current additional info file name.
- Ensure the box stays aligned horizontally with the connection row (same top band).
- Box height is content-driven with minimal padding; no extra vertical space.
- `Show` button should match other button sizes and open the same diagnostics preview used on upload.

### Driver Log Levels Panel
- Add a collapse/expand arrow next to “Driver Log Levels”.
- Default height shows no more than 4 driver rows.
- ~~Horizontal scroll only; no vertical scrollbars.~~
- Use a 4-column grid layout (left-to-right, then next row).
- Vertical scrolling enabled; no horizontal scrollbars.
- Panel must be vertically expandable with a hover-only splitter (no visible bar).

### Diagnostics Area
- Add a “Download Logs” button to the right of the Diagnostics header (same row as “Clear”).
- ~~Add a collapsible “Filter” bar under the Diagnostics header.~~
- Place the filter row inline with the Diagnostics header:
  - `Diagnostics  Filter: Keyword [text] Start: [date/time] End: [date/time] [Filter] [Clear] Count: N  [Download Logs] [Clear]`
- Move Find controls into each log pane header (Raw and Processed):
  - `Find: [text] [Prev] [Next] [Clear] Count: N`
- Keep the visible divider between Raw and Processed panes.
- Filter keyword parsing:
  - Comma-separated terms.
  - `-term` excludes; plain terms include.
  - Invalid input disables Filter and shows a tooltip explaining the error.
- Date/time pickers:
  - Calendar + hour/minute only (no seconds/milliseconds).
  - Format matches log timestamps (`yyyy-MM-dd HH:mm`).
  - Min/Max from raw logs; disable calendar buttons if no raw logs.
- Find behavior:
  - Debounced live search.
  - Highlight all matches; focused match uses a distinct highlight color.
  - Count label uses `Match: 1/10` or `Match: None`.
  - Raw and Processed find behavior must be identical but independent.
- Log panes:
  - No line wrapping.
  - Horizontal scrollbars only when needed (hidden otherwise).
  - Raw and Processed panes must behave identically for long lines.
- `Clear Logs` label uses full wording (not `Clear`).

## Visual Constraints
- Do not increase the vertical height of the top control band.
- Keep button sizing and spacing consistent with existing UI.
- Preserve current colors and typography.
- Ensure filter inputs are fully visible (no clipped right edges).

## Implementation Notes (Context Only)
- Behavior for Filter/Find, IP history, and panel expand/collapse is out of scope for this plan and should be handled by the implementation agent using existing patterns.
~~- Behavior for Filter/Find, IP history, and panel expand/collapse is out of scope for this plan and should be handled by the implementation agent using existing patterns.~~
- Behavior notes included for accurate UI specification only; implementation remains out of scope here.
