# Multi-Agent Work Rules

## Purpose

This document defines how parallel Codex agents must operate during the client/server upgrade and LOC-reduction work.

The goals are:

- clean task ownership
- minimal merge conflicts
- explicit single-agent escalation points
- traceable changes
- mandatory unit-test ownership per agent

## Core Rules

1. Every implementation packet must have exactly one owning agent.
2. Every packet must be marked as either `parallel-safe` or `single-agent only`.
3. Every agent must own the unit tests for the code it adds, extracts, or materially changes.
4. No agent may modify files outside its approved packet scope.
5. Shared-file integration happens only in designated single-agent packets.
6. Opportunistic cleanup outside packet scope is forbidden.

## Agent Numbering

Agents are assigned numeric labels for planning and execution clarity:

- `Agent #1`
- `Agent #2`
- `Agent #3`
- and so on

The agent number is a stable packet owner label for the duration of the phase.

## Packet Template

Every packet must include:

- `Agent`
- `Title`
- `Execution mode`
- `Purpose`
- `Allowed files to modify`
- `Files forbidden to modify`
- `Dependencies`
- `Deliverables`
- `Required unit tests`
- `Definition of done`

## Execution Modes

### Parallel-Safe

A packet is `parallel-safe` only if:

- it can be completed without editing a shared hotspot used by multiple other packets
- it primarily creates or changes isolated service code
- its tests can be created or updated without colliding with another packet
- it does not redefine contracts already owned by another in-flight packet

Typical examples:

- extracting a pure parser service
- extracting a pure formatter service
- adding dedicated tests for a new isolated service

### Single-Agent Only

A packet must be marked `single-agent only` if it:

- edits `MainWindow.xaml.cs`
- changes a shared contract used by multiple in-flight packets
- modifies timing-sensitive orchestration logic
- changes threading or `Dispatcher` behavior
- performs final wiring between multiple completed slices

Typical examples:

- integrating extracted services into the WPF shell
- changing reconnect flow
- changing log-level ack orchestration inside existing shared workflows

## Unit Test Ownership Rules

Each agent is responsible for:

1. Adding or updating unit tests for its own slice.
2. Preserving existing behavior through deterministic assertions.
3. Avoiding unrelated test edits.
4. Keeping test coverage local to the packet when possible.

### Test Placement Rules

- Prefer a dedicated new test file for each newly extracted service.
- If an existing test file must be updated, that file must be explicitly assigned to the packet.
- Multiple agents must not edit the same large test file in parallel.

### Test Scope Rules

Every packet test set should cover:

- happy-path behavior
- invalid or edge input
- behavior equivalence for extracted logic
- deterministic output

### Test Completion Rule

A packet is not done until:

- the implementation is complete
- the unit tests for that packet are present
- targeted tests for that packet pass

## Shared Contract Rules

If multiple packets need a shared model, interface, or common result type:

1. Define the contract first.
2. Assign the contract edit to exactly one agent.
3. Freeze the contract before dependent packets begin.
4. Do not allow multiple agents to independently evolve the same contract in parallel.

Contract changes are usually `single-agent only` unless the contract is introduced before any dependent work starts.

## File Ownership Rules

Each packet must list:

- the exact files it may modify
- the exact files it must not modify

This is mandatory for:

- service files
- test files
- documentation files
- integration files

If a needed file is outside the packet scope, the agent must stop and request packet reassignment or a new packet.

## Merge-Safety Rules

To keep diffs mergeable:

- Prefer additive extraction before replacement.
- Create new services first.
- Add tests with the new service.
- Delay shared-file rewiring until a dedicated integration packet.

This is especially important for:

- `OracleByFPCLtd/MainWindow.xaml.cs`
- large shared test files

## Single-Agent Escalation Triggers

Move work to a `single-agent only` packet when:

- two or more packets need the same shared file
- a packet needs to alter a frozen contract
- a packet touches `Dispatcher` flow or UI-thread assumptions
- a packet changes retry, timeout, reconnect, or ack sequencing
- the task is final integration of multiple extracted slices

## Review Rules

Each packet should be reviewable as one logical change:

- one responsibility
- one agent
- one clear test surface
- one reversible diff

If a packet starts expanding across multiple concerns, split it before implementation.

## Recommended Phase Structure

1. Planning phase
- docs only
- packet definitions
- contract definitions

2. Parallel extraction phase
- isolated portable services
- isolated unit tests

3. Convergence phase
- contract alignment if needed
- conflict resolution

4. Single-agent integration phase
- `MainWindow` wiring
- shared workflow integration

5. Validation phase
- full test sequence
- integration fixes

## Bottom Line

Multi-agent programming will work best here if:

- each agent owns one narrow slice
- each slice owns its own tests
- shared-file edits are centralized
- packet boundaries are enforced strictly
