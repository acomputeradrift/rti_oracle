# Agent #1 Brief: LogFilterService

## Agent

- `Agent`: Agent #1

## Packet

- `Title`: LogFilterService extraction
- `Execution mode`: parallel-safe

## Objective

Extract the deterministic filter logic currently living inside `OracleByFPCLtd/MainWindow.xaml.cs` into a reusable service that is:

- UI-free
- deterministic
- testable in isolation
- suitable for later use in either the client or a future server/API layer

This packet is part of the LOC-reduction and responsibility-splitting effort for a future client/server architecture.

## Current Source Area

This packet is derived from filter-related logic currently embedded in:

- `OracleByFPCLtd/MainWindow.xaml.cs`

Representative responsibility area:

- keyword filter parsing
- date range parsing
- timestamp extraction
- line include/exclude matching

## Scope

The target logic includes behavior equivalent to:

- parsing keyword filter input
- parsing filter date/time ranges
- parsing filter date/time text
- extracting timestamps from log lines
- determining whether a line matches the active filter
- assembling filter-related diagnostic details if needed

The goal is to isolate the logic, not to change behavior.

## Allowed Files To Modify

- new service file(s) under `OracleByFPCLtd`
- new packet-owned test file(s) under `OracleByFPCLtd.Tests`
- packet-specific docs in `clientServerUpgrade` only if clarification is required

Preferred pattern:

- create new service files rather than editing broad existing files
- create dedicated tests for the service rather than editing large shared test files

## Forbidden Files To Modify

- `OracleByFPCLtd/MainWindow.xaml.cs`
- any file owned by another active packet
- large shared test files unless explicitly reassigned

If implementation requires editing any forbidden file, stop and escalate to the orchestrator.

## Deliverables

1. A reusable filter service with a narrow, explicit API.
2. Packet-owned unit tests covering the extracted logic.
3. No unrelated cleanup or opportunistic refactors.

## Required Unit Tests

Minimum required test coverage:

1. Valid keyword filter parsing.
2. Invalid keyword filter parsing.
3. Valid date range parsing.
4. Invalid date range parsing.
5. Timestamp extraction from matching log lines.
6. Line inclusion and exclusion matching.
7. Deterministic handling of empty or partial filter inputs.

## Behavior Requirements

- Preserve current filtering semantics.
- Preserve current error-detection behavior for invalid input.
- Do not change user-facing filter rules during this packet.
- Keep the logic allocation-conscious and lightweight.

## Architecture Requirements

- No WPF dependencies.
- No direct control access.
- No dialog logic.
- No `Dispatcher` usage.
- No server implementation work.

This service should be portable shared domain logic.

## Completion Criteria

This packet is complete only when:

1. The service exists.
2. The service is isolated from WPF concerns.
3. Packet-owned tests exist.
4. Targeted tests pass.
5. No forbidden files were changed.

## Escalation Triggers

Escalate immediately if:

- a shared contract change is required
- a forbidden file is required
- a large shared test file must be edited
- the logic turns out to depend on UI state in a non-trivial way
