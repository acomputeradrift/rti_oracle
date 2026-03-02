# Agent #3 Brief: StructuredMessageParser

## Agent

- `Agent`: Agent #3

## Packet

- `Title`: StructuredMessageParser extraction
- `Execution mode`: parallel-safe

## Objective

Extract structured diagnostics message parsing logic from `OracleByFPCLtd/MainWindow.xaml.cs` into a reusable parser service.

This packet should reduce LOC in the long term by separating data interpretation from UI orchestration and preparing the logic for future portability.

## Current Source Area

This packet is derived from structured-message logic currently embedded in:

- `OracleByFPCLtd/MainWindow.xaml.cs`

Representative responsibility area:

- JSON root parsing
- structured payload detection
- structured message interpretation
- formatted message output for structured payloads

## Scope

The target logic includes behavior equivalent to:

- parsing JSON input safely
- distinguishing structured from non-structured input
- reading structured root content
- producing structured formatted output and log-line classification decisions

The extraction should preserve behavior and avoid changing message semantics.

## Allowed Files To Modify

- new parser service file(s) under `OracleByFPCLtd`
- new packet-owned test file(s) under `OracleByFPCLtd.Tests`
- packet-specific docs in `clientServerUpgrade` only if clarification is required

Preferred pattern:

- create dedicated parser/result types if needed
- keep the API narrow and deterministic

## Forbidden Files To Modify

- `OracleByFPCLtd/MainWindow.xaml.cs`
- any file owned by another active packet
- shared integration files
- large shared test files unless explicitly reassigned

If a forbidden file is required, stop and escalate.

## Deliverables

1. A reusable structured message parser service.
2. Explicit parse/format results suitable for later client or server use.
3. Packet-owned unit tests.

## Required Unit Tests

Minimum required test coverage:

1. Valid JSON root parsing.
2. Invalid JSON handling.
3. Structured payload detection.
4. Correct handling of structured message formatting paths.
5. Deterministic classification of structured versus non-structured input.

## Behavior Requirements

- Preserve current parse acceptance and rejection behavior.
- Preserve current classification outcomes.
- Do not alter user-facing formatted message semantics during this packet.

## Architecture Requirements

- No WPF dependencies.
- No direct UI updates.
- No `Dispatcher` usage.
- No direct transport orchestration.

This packet should produce portable shared domain logic.

## Completion Criteria

This packet is complete only when:

1. The parser service exists.
2. Packet-owned tests exist.
3. Targeted tests pass.
4. No forbidden files were changed.

## Escalation Triggers

Escalate if:

- required behavior depends on shared contracts outside scope
- existing behavior is too entangled with `MainWindow` state to isolate safely
- a forbidden file would need modification
