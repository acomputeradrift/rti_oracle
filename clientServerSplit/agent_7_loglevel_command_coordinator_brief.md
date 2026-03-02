# Agent #7 Brief: LogLevelCommandCoordinator

## Agent

- `Agent`: Agent #7

## Packet

- `Title`: LogLevelCommandCoordinator extraction
- `Execution mode`: single-agent only

## Objective

Extract log-level command coordination logic from `OracleByFPCLtd/MainWindow.xaml.cs` into a reusable coordinator responsible for:

- pending command tracking
- ack correlation
- retry decisions
- success/failure state transitions

This packet targets one of the more complex workflow areas and should reduce both LOC and responsibility density in `MainWindow`.

## Why This Is Single-Agent Only

This packet is `single-agent only` because it is tightly tied to:

- timing-sensitive behavior
- ack and retry flow
- connection-state assumptions
- shared UI-driven workflows in `MainWindow`

Parallel edits here would create high merge and regression risk.

## Current Source Area

This packet is derived from log-level workflow logic currently embedded in:

- `OracleByFPCLtd/MainWindow.xaml.cs`

Representative responsibility area:

- log-level command dispatch coordination
- pending-command bookkeeping
- ack wait logic
- ack key correlation
- result/status handling

## Scope

The target logic includes behavior equivalent to:

- creating or resolving pending command state
- correlating acknowledgements
- handling retry decisions
- shaping deterministic success/failure outcomes
- supporting batch log-level operations through a coordinator seam

The packet should isolate coordination logic, not change the current feature set.

## Allowed Files To Modify

- new coordinator/service file(s) under `OracleByFPCLtd`
- packet-owned test file(s) under `OracleByFPCLtd.Tests`
- `OracleByFPCLtd/MainWindow.xaml.cs` only as required for extraction and wiring
- packet-specific docs in `clientServerSplit` only if clarification is required

## Forbidden Files To Modify

- files owned by any active parallel packet
- unrelated transport or UI files outside the packet’s assigned scope
- unrelated test files outside the packet’s owned test surface

If broader changes are required, stop and escalate.

## Deliverables

1. A log-level command coordinator with a clear API.
2. Packet-owned unit tests for the extracted deterministic coordination logic.
3. Minimal, focused `MainWindow` edits required for the extraction seam.

## Required Unit Tests

Minimum required test coverage:

1. Ack key generation.
2. Pending command registration and resolution.
3. Retry decision behavior.
4. Success transition behavior.
5. Failure transition behavior.
6. Deterministic handling of unresolved or late ack scenarios where extractable without changing runtime timing assumptions.

## Behavior Requirements

- Preserve current log-level workflow semantics.
- Preserve existing retry and ack handling behavior.
- Do not reduce log-level control capabilities.
- Do not alter timing behavior except where structurally necessary and behavior-preserving.

## Architecture Requirements

- Keep UI click handling outside the coordinator.
- Keep deterministic coordination logic reusable and transport-agnostic where possible.
- Do not introduce speculative abstractions that add more LOC than they remove.
- Keep the design compatible with future client/server separation of command policy from UI triggers.

## Completion Criteria

This packet is complete only when:

1. The coordinator exists.
2. Packet-owned tests exist.
3. Targeted tests pass.
4. `MainWindow` changes are narrow and justified.
5. No forbidden files were changed.

## Escalation Triggers

Escalate if:

- timing-sensitive behavior cannot be isolated safely
- extraction would require broad reconnect or transport changes
- a shared contract change affects other packets
- broader `MainWindow` restructuring is required
