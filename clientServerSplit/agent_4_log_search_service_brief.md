# Agent #4 Brief: LogSearchService

## Agent

- `Agent`: Agent #4

## Packet

- `Title`: LogSearchService extraction
- `Execution mode`: parallel-safe

## Objective

Extract the non-UI log search and find-state mechanics from `OracleByFPCLtd/MainWindow.xaml.cs` into a reusable service.

This packet is intended to separate state calculation from visual rendering so the search logic becomes smaller, clearer, and reusable.

## Current Source Area

This packet is derived from find/search logic currently embedded in:

- `OracleByFPCLtd/MainWindow.xaml.cs`

Representative responsibility area:

- query matching
- match indexing
- selection movement state
- reset behavior
- no-match handling

## Scope

The target logic includes behavior equivalent to:

- maintaining or computing match positions
- next/previous movement state
- reset rules when the query changes
- count and focus index calculations

Visual highlighting and WPF selection display are not part of this packet.

## Allowed Files To Modify

- new service file(s) under `OracleByFPCLtd`
- new packet-owned test file(s) under `OracleByFPCLtd.Tests`
- packet-specific docs in `clientServerUpgrade` only if clarification is required

Preferred pattern:

- extract only the calculation/state logic
- leave visual rendering out of scope

## Forbidden Files To Modify

- `OracleByFPCLtd/MainWindow.xaml.cs`
- any file owned by another active packet
- UI rendering files
- large shared test files unless explicitly reassigned

If implementation requires a forbidden file, stop and escalate.

## Deliverables

1. A reusable log search service or state engine.
2. Packet-owned unit tests.
3. A clean separation between search calculations and UI highlighting concerns.

## Required Unit Tests

Minimum required test coverage:

1. Query match indexing.
2. No-match behavior.
3. Reset behavior when the query changes.
4. Next-match navigation state.
5. Previous-match navigation state.
6. Deterministic count/focus calculations.

## Behavior Requirements

- Preserve current search semantics.
- Preserve current navigation ordering.
- Do not change highlight rendering behavior in this packet.

## Architecture Requirements

- No WPF dependencies.
- No direct `RichTextBox` handling.
- No `Dispatcher` usage.
- No UI formatting responsibilities.

This packet should produce portable shared domain logic.

## Completion Criteria

This packet is complete only when:

1. The service exists.
2. Packet-owned tests exist.
3. Targeted tests pass.
4. No forbidden files were changed.

## Escalation Triggers

Escalate if:

- current logic depends on UI objects in a way that prevents clean isolation
- a shared contract is needed outside the assigned scope
- a forbidden file must be edited
