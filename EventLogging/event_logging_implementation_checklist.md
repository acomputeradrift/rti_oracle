# Oracle Event Logging Implementation Checklist

Date: 2026-02-28

## Scope

This checklist defines the required implementation work for replacing the current structured event logging path with the approved session-based plain text event log format.

This is a planning and audit document only. No runtime code changes are defined here.

## Implementation Goal

Oracle must produce a fast, append-only, per-session event log that:

- captures all valuable operational milestones
- never allows silent failures
- never shows a status-area message without a more verbose corresponding event log record
- preserves high-detail `WARN` and `FAIL` diagnostics
- avoids the current performance bottleneck caused by repeated whole-file rewrites

## Confirmed Current Risk

The current implementation does not fully guarantee that every status-area message is also present in the event log.

Examples already confirmed in code:

- `AppendStatusFromChild(...)` writes to the UI status area using `logToFile: false`
- `AppendAppStatus(string line, ...)` parses text and then writes to the UI status area using `logToFile: false`
- direct `AppendAppStatus("INFO", ...)` calls may write only to the UI if the caller does not pass details through the logging path

Relevant code paths:

- `OracleByFPCLtd/MainWindow.xaml.cs :: AppendAppStatus(string line, bool allowEmpty = false)`
- `OracleByFPCLtd/MainWindow.xaml.cs :: AppendAppStatus(string level, string message, ..., bool logToFile = true, ...)`
- `OracleByFPCLtd/MainWindow.xaml.cs :: AppendStatusFromChild(string level, string message)`
- `OracleByFPCLtd/ProjectDataPreviewWindow.xaml.cs :: ReportStatus`

## Required Guardrail

The implementation must enforce these invariants:

- the event log is the primary system of record
- every status-area message must originate from an event that was already logged, or is logged as part of the same operation

The UI may display only selected milestones and may use a shorter or friendlier summary, but the event log must always be the authoritative and more verbose source.

## Implementation Checklist

### 1. Replace File Strategy

- Remove the shared multi-session HTML event log behavior.
- Replace it with one append-only plain text `.log` file per Oracle session.
- Stop rewriting the full log file for each event.
- Eliminate HTML-specific formatting, escaping, and insertion logic from the event log path.

### 2. Create Session File Naming

- Generate the event log filename once at session start.
- Use the approved filename pattern:

```text
yyyy-MM-dd_HH-mm_oracle_event_logs.log
```

- Store and reuse the same resolved path for the life of the session.
- Do not roll to a new file mid-session.

### 3. Add Session Retention

- On session start, enumerate existing Oracle event log files in the log directory.
- Sort by newest first.
- Keep the newest 5 files.
- Delete older files best-effort.
- If retention cleanup fails, record a `WARN` in the current session log and continue.

### 4. Implement Append-Only Writer

- Replace full-file read/insert/write logic with append-only line writes.
- Prefer a long-lived buffered writer for the session.
- Ensure the writer flush policy is safe enough for diagnostics without reintroducing heavy I/O.
- Ensure write failures never mutate raw diagnostics behavior.
- If a write fails, the failure must be surfaced through a fallback path and must not be silently swallowed.

### 5. Implement Approved Line Format

- Emit lines using the approved standard format:

```text
<datetime> [<LEVEL>] <source module>: "Line <log line number> - <message>" <optional context>
```

- Use a consistent date/time display format for all event lines.
- Keep the prefix order fixed.
- Keep the quoted message shape fixed.
- Preserve one optional identity field only: `profile="..."` or `driver="..."`, never both.

### 6. Implement Severity Detail Rules

- `INFO` and `SUCCESS` should log useful milestones and line-level processing progress.
- `WARN` and `FAIL` must include all relevant non-duplicate context available at the call site.
- If an exception exists, log the full exception text.
- If a path, IP, driver, profile, raw line number, operation code, tag, or state is known, include it.

### 7. Separate High-Frequency And Milestone Logging

- Review all high-frequency per-line event emissions.
- Remove or reduce non-essential per-line `INFO` events from the live capture path.
- Keep line-level logs only where they are intentional and low-cost.
- Preserve milestone-level logging for connect, load, map, export, save, and failure boundaries.

### 8. Guarantee No Silent Failure

- Audit all catch blocks that currently swallow exceptions.
- For every swallowed exception, decide whether it must emit `WARN` or `FAIL`.
- Replace silent suppression with explicit best-effort failure logging where operationally safe.
- If logging itself fails, that failure must still be observable somewhere appropriate.

### 9. Enforce Event-First Status Consistency

- Refactor status UI helpers so they cannot create status-only messages.
- Event logging must happen first or be the primary action within the same operation.
- The status area must consume selected milestone summaries derived from already-logged events.
- Keep the UI status message text unchanged for now where requested.
- Ensure the event log version is always more verbose than the displayed status text.

### 10. Preserve Existing UI Messaging For Now

- Do not change current status area wording during the logging rewrite unless required for correctness.
- Use the event log backend change to add verbosity without altering user-facing milestone text.
- Where the UI uses a truncated summary, derive it from the corresponding event and log the full detail payload in the session log.

### 11. Centralize Event Emission Rules

- Define one canonical event logging entry point for session log writes.
- Define one canonical path for status-area messages that also guarantees event log emission.
- Avoid ad hoc direct writes that bypass the canonical path.
- Reduce duplicate formatting logic across `MainWindow`, helper services, and notifier classes.

### 12. Add Tests First

- Add tests for session file naming.
- Add tests for retention of newest 5 files.
- Add tests for append-only plain text output shape.
- Add tests for `WARN` and `FAIL` context expansion.
- Add tests proving status-area messages also produce a corresponding event log record.
- Add tests proving child-window status messages also produce event log entries.
- Add tests proving event log write failures do not crash log processing and do not disappear silently.

## Logging Coverage Audit

## Areas That Already Emit Valuable Logs

- Transport errors are logged through `MainWindow.Transport_TransportError(...)`.
- Reconnect success and failure milestones are logged through `EmitPhaseStatus(...)`.
- Driver load completion and empty-result conditions are logged in `LoadDriversAsync(...)`.
- Diagnostics baseline success/failure milestones are logged in the protected log-level setup path.
- Processing initialization and batch formatting/mapping completion paths emit success/failure milestones.
- `ReportFailure(...)` and `ReportSuccess(...)` already attempt to route failures and successes into status plus event logging.
- `MainWindowFailureNotifier` emits operational result logs for tracked feature failures.

## Areas With Confirmed Or Likely Logging Gaps

- `AppendStatusFromChild(...)` writes to the status area but explicitly disables file logging.
- `ProjectDataPreviewWindow.ReportStatus(...)` depends on `AppendStatusFromChild(...)`, so those status messages can be UI-only.
- `AppendAppStatus(string line, ...)` converts parsed status text into status UI entries with `logToFile: false`.
- Direct informational status calls such as `AppendAppStatus("INFO", "No driver profiles are currently registered.")` rely on caller behavior and may be insufficiently detailed.
- Tagged report save/open-folder status messages use `AppendAppStatus(...)` with UI-facing text and may need a more explicit event log payload.
- Catch blocks that intentionally suppress exceptions in logging-related classes must be audited so that logging failures do not become invisible.

## Areas To Review For Missing Critical Success Logs

- Initial connect flow before the system reaches `Ready`
- project upload and parse completion milestones
- additional info load, cache, and schema resolution milestones
- driver profile registration and profile-availability milestones
- export completion milestones
- report generation completion milestones
- any operation that materially changes application readiness or operator trust

## Areas To Review For Insufficient WARN/FAIL Detail

- reconnect failure loops
- project parse failures
- mapping failures
- export failures
- report save/open failures
- baseline/ack timeout failures
- operational result notifications that currently collapse context into short UI text

## Specific Code Review Targets

- `OracleByFPCLtd/MainWindow.xaml.cs`
- `OracleByFPCLtd/Logging/CentralLogger.cs`
- `OracleByFPCLtd/Reliability/MainWindowFailureNotifier.cs`
- `OracleByFPCLtd/ProjectDataPreviewWindow.xaml.cs`
- `OracleByFPCLtd/ProcessingEngine/ProcessingEngineRunner.cs`
- `OracleByFPCLtd/DiagnosticsTransport/*`
- `OracleByFPCLtd/Settings/*`
- `OracleByFPCLtd/ProjectData/*`
- `OracleByFPCLtd/ExportProcessedLogs/*`

## Acceptance Criteria

- Oracle writes one plain text event log file per session.
- The event log is append-only.
- The newest 5 session log files are retained.
- No status-area message can appear unless it is backed by a matching event that was logged first or as part of the same operation.
- `WARN` and `FAIL` records always include all non-duplicate relevant context available at emission time.
- Silent failures in operational paths are eliminated.
- High-frequency logging no longer creates a measurable live-render slowdown.
