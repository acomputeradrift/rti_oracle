# Variables Feature Implementation Plan (Code-Ready, Test-First)

## Scope
- Add a Variables tab alongside Raw Output in the left diagnostics pane.
- Display sysvar subscription updates with the same timestamp prefix format as logs, plus a Variables-specific line number.
- Subscriptions are driven by Driver Log Levels: any driver level ON subscribes all sysvars for that driver; all OFF unsubscribes.
- No filter/find controls in the Variables tab.
- No unrelated UI or behavior changes.

## Constraints
- Follow existing patterns and styles in `SHPDiagnosticsViewer`.
- Use existing sysvar subscription payload shape and WebSocket transport.
- Do not add dependencies without approval.
- Implementation must be test-first and requires explicit approval before writing production code.

## Target Files (Implementation)
- UI: `MainWindow.xaml`
- UI logic: `MainWindow.xaml.cs`
- Transport/formatting: `DiagnosticsTransport/LegacyWebSocketDiagnosticsTransport.cs`, `WebSocketMessageFormatter.cs` (if needed)
- New helpers (if needed, same project): e.g., `SysvarCatalog`-style parsing and lookup

## Tests (Write First)
1) Sysvar catalog parsing (copied shape from probe)
   - Parse `Drivers[]` / `Driver Variables[]` into driver + variable records.
2) Sysvar lookup formatting
   - Given id + lookup table, formats `"{DriverName} [VariableName] changed to {value}"`.
   - Fallback uses `Sysvar {id} changed to {value}`.
3) Variables feed line formatting
   - Uses log-style datetime prefix `[yyyy-MM-dd HH:mm:ss.fff]`.
   - Appends `[#]` line number (per Variables tab) and formatted body.
4) Subscription mapping from log levels
   - When any level is enabled for a driver, subscribe all its sysvars.
   - When all levels are disabled, unsubscribe all its sysvars.

## Implementation Steps (After Approval)

### 1) Add Variables Tab (UI)
Update `MainWindow.xaml` left pane to use a tab control.
```xml
<TabControl x:Name="DiagnosticsLeftTab">
  <TabItem Header="Raw Output">
    <!-- existing Raw Output content -->
  </TabItem>
  <TabItem Header="Variables">
    <!-- new variables feed control -->
  </TabItem>
</TabControl>
```
Notes:
- Keep the right `Processed Output` pane unchanged.
- No filter/find controls for Variables.

### 2) Sysvar Catalog + Lookup (Model/Parsing)
Reuse the probe’s JSON contract for sysvar list:
- Source: `http://<ip>:5000/diagnostics/data/sysvars` (GZip JSON).
- Parse the same `Drivers` -> `Driver Variables` structure.

Suggested helper shape (keep local to project):
```csharp
public sealed record SysvarDriver(string DriverName, int DriverId, List<SysvarVariable> Variables);
public sealed record SysvarVariable(string Name, int Id);
```

### 3) Variables Feed Formatting
Format like log entries with datetime and a per-tab line number.
Example output:
```
[2026-01-27 13:41:08.123] [Var 0042] Living Room [Power] changed to On
```
Rules:
- Datetime uses same prefix style as log lines.
- Line number is per Variables tab session (resets on disconnect).
- Label uses lookup when available, else `Sysvar {id}`.

### 4) Hook Sysvar Messages Into Variables Feed
`LegacyWebSocketDiagnosticsTransport` already subscribes to `Sysvar`.
- Ensure sysvar messages are routed to Variables feed, not Raw Output.
- If Raw Output currently receives all messages, filter sysvar entries into Variables tab display.

### 5) Driver Log Levels -> Sysvar Subscribe/Unsubscribe
When a driver log level is toggled:
- If any level for the driver is ON, subscribe all sysvars for that driver.
- If all levels for the driver are OFF, unsubscribe all sysvars for that driver.

Suggested payload shape (matches probe):
```csharp
new {
  type = "Subscribe",
  resource = "Sysvar",
  value = new { id = sysvarId, status = true /* or false */ }
}
```

### 6) Connection Lifecycle
- On connect: load sysvar catalog, build lookup map, init Variables line counter.
- On disconnect: clear variables feed and reset line counter.
- Respect existing connect/disconnect UI flow.

## Acceptance Checklist
- Variables tab exists; Raw Output and Processed Output remain unchanged.
- Variables feed shows log-style datetime prefix and line numbers.
- Variable names resolve via sysvar catalog when available; unknowns are labeled.
- Subscriptions are controlled by Driver Log Levels only.
- No filter/find controls added to Variables.

## Approval Gate
- Do not write implementation code until tests are written and explicit approval is given.
