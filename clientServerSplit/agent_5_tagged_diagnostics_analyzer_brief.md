# Agent #5 Brief: TaggedDiagnosticsAnalyzer

## Agent

- `Agent`: Agent #5

## Packet

- `Title`: TaggedDiagnosticsAnalyzer extraction
- `Execution mode`: parallel-safe

## Objective

Extract unresolved-tag and tagged-diagnostics analysis logic from `OracleByFPCLtd/MainWindow.xaml.cs` into a reusable analyzer service.

This packet is strategically important because it supports:

- LOC reduction
- responsibility separation
- future handling of unknown driver profile data
- eventual server-backed intelligence for unresolved diagnostics

## Current Source Area

This packet is derived from tagged diagnostics logic currently embedded in:

- `OracleByFPCLtd/MainWindow.xaml.cs`

Representative responsibility area:

- diagnostic tag detection
- tag extraction
- tagged driver name extraction
- tagged line normalization
- grouped report assembly for tagged/unhandled messages

## Scope

The target logic includes behavior equivalent to:

- detecting whether a line contains known diagnostic tags
- extracting the tags from a line
- extracting a driver identity from tagged content
- normalizing tagged lines for grouping
- assembling grouped tagged-message reports from collected data

The packet should isolate the analysis logic, not change the diagnostic meaning.

## Allowed Files To Modify

- new analyzer service file(s) under `OracleByFPCLtd`
- new packet-owned test file(s) under `OracleByFPCLtd.Tests`
- packet-specific docs in `clientServerUpgrade` only if clarification is required

Preferred pattern:

- keep the analysis logic self-contained
- keep report assembly deterministic

## Forbidden Files To Modify

- `OracleByFPCLtd/MainWindow.xaml.cs`
- any file owned by another active packet
- shared integration files
- large shared test files unless explicitly reassigned

If implementation requires a forbidden file, stop and escalate.

## Deliverables

1. A reusable tagged diagnostics analyzer service.
2. Deterministic grouped-report assembly logic.
3. Packet-owned unit tests.

## Required Unit Tests

Minimum required test coverage:

1. Tag detection.
2. Tag extraction.
3. Tagged driver name extraction.
4. Tagged line normalization.
5. Grouped tagged report assembly.
6. Handling of lines with no recognized tags.

## Behavior Requirements

- Preserve current recognized tag semantics.
- Preserve current grouping and normalization behavior.
- Do not alter the meaning of unresolved diagnostics during this packet.

## Architecture Requirements

- No WPF dependencies.
- No dialog logic.
- No `Dispatcher` usage.
- No direct UI reporting in this packet.

This packet should produce portable shared domain logic that may later support server-backed unknown-profile analysis.

## Completion Criteria

This packet is complete only when:

1. The analyzer service exists.
2. Packet-owned tests exist.
3. Targeted tests pass.
4. No forbidden files were changed.

## Escalation Triggers

Escalate if:

- report assembly depends on a shared contract outside scope
- a forbidden file must be edited
- current behavior is more tightly coupled to UI state than expected
