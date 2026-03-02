# Agent #2 Brief: StatusMessageFormatter

## Agent

- `Agent`: Agent #2

## Packet

- `Title`: StatusMessageFormatter extraction
- `Execution mode`: parallel-safe

## Objective

Extract status parsing, normalization, severity mapping, and status text formatting logic from `OracleByFPCLtd/MainWindow.xaml.cs` into a reusable service.

This packet supports:

- LOC reduction
- separation of responsibilities
- clearer client-only versus portable-domain boundaries
- future compatibility with a server-backed architecture

## Current Source Area

This packet is derived from status-related helper logic currently embedded in:

- `OracleByFPCLtd/MainWindow.xaml.cs`

Representative responsibility area:

- status line parsing
- status level normalization
- status message shaping
- severity mapping
- UI suppression decision rules

## Scope

The target logic includes behavior equivalent to:

- parsing status lines
- normalizing status levels
- mapping status levels to shared severity meaning
- building formatted status text/messages
- applying suppression decision rules for status lines

The packet should isolate shared logic, not change operational behavior.

## Allowed Files To Modify

- new service file(s) under `OracleByFPCLtd`
- new packet-owned test file(s) under `OracleByFPCLtd.Tests`
- packet-specific docs in `clientServerUpgrade` only if clarification is required

Preferred pattern:

- create a dedicated formatter or helper service
- use dedicated tests rather than broad edits to existing shared files

## Forbidden Files To Modify

- `OracleByFPCLtd/MainWindow.xaml.cs`
- any file owned by another active packet
- large shared test files unless explicitly reassigned

If implementation requires editing a forbidden file, stop and escalate.

## Deliverables

1. A reusable status formatting/parsing service.
2. Packet-owned unit tests that prove equivalent deterministic behavior.
3. A narrow, explicit API suitable for later use by either client or server-side components.

## Required Unit Tests

Minimum required test coverage:

1. Valid status line parsing.
2. Invalid status line handling.
3. Status level normalization.
4. Severity mapping behavior.
5. Status text/message formatting.
6. Suppression decision behavior.

## Behavior Requirements

- Preserve current status interpretation behavior.
- Do not change message meaning or severity semantics.
- Do not change logging policy in this packet.

## Architecture Requirements

- No WPF dependencies.
- No `Brush` or UI object ownership in the core portable formatter logic.
- No `Dispatcher` usage.
- No integration into `MainWindow` in this packet.

This packet should produce portable shared domain logic.

## Completion Criteria

This packet is complete only when:

1. The service exists.
2. Packet-owned tests exist.
3. Targeted tests pass.
4. No forbidden files were changed.

## Escalation Triggers

Escalate if:

- severity mapping depends on a shared contract not yet defined
- a forbidden file must be edited
- UI-only concerns are too entangled to isolate cleanly without a follow-up packet
