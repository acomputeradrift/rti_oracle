# Oracle Event Logging Structure

Date: 2026-02-28

## Scope

This document defines the intended event logging structure for Oracle session logs.

## Session File Rules

- Oracle writes one plain text `.log` file per app session.
- Oracle does not append multiple sessions into one shared log file.
- Oracle keeps the newest 5 session log files and deletes older files.
- Log files are append-only during the session.

## Session File Name

Filename format:

```text
yyyy-MM-dd_HH-mm_oracle_event_logs.log
```

Example:

```text
2026-02-28_12-32_oracle_event_logs.log
```

## Log Line Structure

Standard line format:

```text
<datetime> [<LEVEL>] <source module>: "Line <log line number> - <message>" <optional context>
```

Example `INFO` lines:

```text
2026-02-28 12:32 [INFO] ProcessingEngineRunner/Formatting: "Line 42 - formatted with DateTime, line number"
2026-02-28 12:32 [INFO] ProcessingEngineRunner/Processing: "Line 42 - formatted for readability"
```

Example `SUCCESS` line:

```text
2026-02-28 12:32 [SUCCESS] ProcessingEngineRunner/Processing: "Line 42 - mapped index 81 for Room Select" profile="Clipsal C-Bus"
```

## Required Formatting Rules

- The line prefix order is fixed: `datetime`, `level`, `source module`, then quoted message.
- The quoted message uses the format: `Line <number> - <message>`.
- The source module uses the existing module/phase shape when available, for example `ProcessingEngineRunner/Processing`.
- Trailing context is optional.
- If a line includes a mapped identity field, use only one identity key: either `profile="..."` or `driver="..."`, never both.

## Severity Guidance

- `INFO` records normal progress and traceable line-level processing steps.
- `SUCCESS` records completed line-level mappings or confirmed successful operations.
- `WARN` records partial failures, degraded behavior, or unexpected but recoverable conditions.
- `FAIL` records unrecoverable errors or failed operations.

## WARN And FAIL Detail Policy

For `WARN` and `FAIL` lines:

- include as much relevant detail as is available
- append all non-duplicate context as key/value pairs
- do not repeat the same field twice
- preserve the same standard prefix and quoted message structure

Typical detail fields may include:

- `driver="..."`
- `profile="..."`
- `tag="..."`
- `rawLineNumber="..."`
- `rawText="..."`
- `path="..."`
- `ip="..."`
- `op="..."`
- `exception="..."`

Example:

```text
2026-02-28 12:32 [WARN] MainWindow/TaggedReport: "Line 42 - failed to classify tagged message" driver="Clipsal C-Bus" tag="[Unknown State!]" rawLineNumber="42" rawText="Driver - Command: ..." exception="System.InvalidOperationException: ..."
```

## Performance Intent

- The event log is plain text for speed and simplicity.
- The log file does not use HTML formatting.
- The log writer should favor efficient append behavior rather than whole-file rewrites.
