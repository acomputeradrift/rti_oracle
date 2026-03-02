# Phase 1 Execution Checklist

## Purpose

This document is the execution runbook for the first implementation wave of the client/server upgrade-aligned LOC reduction.

It combines:

- packet ownership
- branch workflow
- orchestrator control
- testing expectations
- the transition from parallel work to single-agent integration

Phase 1 is focused on extracting the first portable shared-domain slices from `MainWindow` without changing application goals.

## Phase 1 Goal

Complete the first safe parallel extraction wave by delivering:

- isolated portable services
- packet-owned unit tests
- no uncontrolled shared-file overlap
- readiness for a later single-agent integration pass

Phase 1 does not require broad `MainWindow` rewiring at the same time as parallel service extraction.

## Phase 1 Included Packets

These packets are in scope for the first wave:

1. `Agent #1` - LogFilterService extraction
2. `Agent #2` - StatusMessageFormatter extraction
3. `Agent #3` - StructuredMessageParser extraction
4. `Agent #4` - LogSearchService extraction
5. `Agent #5` - TaggedDiagnosticsAnalyzer extraction

These are the preferred first-wave packets because they are the cleanest `parallel-safe` slices.

## Phase 1 Excluded Packets

These are explicitly not part of the first parallel wave:

- `Agent #6` - ProjectProcessingCoordinator extraction
- `Agent #7` - LogLevelCommandCoordinator extraction
- `Agent #8` - MainWindow integration pass

These are deferred because they are higher-risk and should be handled in controlled single-agent steps after the first portable service slices exist.

## Step 1: Orchestrator Preparation

The orchestrator must complete these checks before any implementation begins:

1. Confirm packet docs are current.
2. Confirm file ownership is explicit for all active packets.
3. Confirm no two active packets overlap in unsafe ways.
4. Confirm each packet has required unit-test expectations.
5. Confirm branch names are assigned.
6. Confirm `MainWindow.xaml.cs` is not in scope for the first five packets.

If any of these are unclear, do not start implementation.

## Step 2: Branch Creation

Create one branch per active packet:

1. `agent-1-log-filter-service`
2. `agent-2-status-message-formatter`
3. `agent-3-structured-message-parser`
4. `agent-4-log-search-service`
5. `agent-5-tagged-diagnostics-analyzer`

Rules:

- one agent per branch
- no shared branches
- no implementation on the planning branch

## Step 3: Agent Briefing

Before each agent starts, the orchestrator must provide:

- packet title
- execution mode
- allowed files
- forbidden files
- expected deliverables
- required unit tests
- branch name

Each agent should be told explicitly:

- do not modify `OracleByFPCLtd/MainWindow.xaml.cs`
- do not modify files owned by another active packet
- stop and escalate if the packet requires scope expansion

## Step 4: Parallel Execution

Agents #1 through #5 may work in parallel only while the following remain true:

- no packet requires shared hotspot edits
- no packet requires a new shared contract outside its assigned scope
- no packet needs a test file already owned by another active packet

During execution, each agent must:

1. Stay inside packet scope.
2. Produce its service extraction.
3. Produce its packet-owned unit tests.
4. Keep the change narrowly focused.
5. Report blockers immediately.

## Step 5: Orchestrator Monitoring

While parallel execution is active, the orchestrator should track:

- packet status: not started, in progress, blocked, complete
- file overlap risks
- test ownership conflicts
- any discovered contract drift

The orchestrator must intervene immediately if:

- two agents need the same file
- a packet expands beyond one responsibility
- a packet starts reaching into `MainWindow`
- a packet needs a shared contract change

If this happens, the orchestrator must either:

- split the work
- create a new packet
- reassign scope
- convert the work to `single-agent only`

## Step 6: Packet-Level Completion Check

Each packet is only considered complete when:

1. The intended service exists.
2. The packet-owned tests exist.
3. The packet’s targeted tests pass.
4. No forbidden files were modified.
5. No unresolved contract question remains.

If any of these are missing, the packet remains open.

## Step 7: Merge To Integration Branch

After the first-wave packets are complete, create and use:

- `integration/client-server-phase-1`

Merge order:

1. `agent-1-log-filter-service`
2. `agent-2-status-message-formatter`
3. `agent-3-structured-message-parser`
4. `agent-4-log-search-service`
5. `agent-5-tagged-diagnostics-analyzer`

The orchestrator or designated integration agent should perform these merges in order.

## Step 8: Integration Review

On the integration branch, confirm:

1. The service slices coexist cleanly.
2. There are no contract collisions.
3. There are no overlapping test changes that need consolidation.
4. The branch is ready for the next single-agent extraction phase.

This is still not the `MainWindow` wiring step unless explicitly approved.

## Step 9: Validation Gate

Before the phase is accepted:

1. Confirm each packet’s targeted tests were completed.
2. Run the full repository test sequence.
3. Resolve any cross-slice regressions on the integration branch.

Standard validation entry point:

- `powershell -ExecutionPolicy Bypass -File .\run_test_sequence.ps1`

The phase is not complete until full validation passes.

## Step 10: Transition To Single-Agent Work

After Phase 1 passes validation, the orchestrator chooses the next controlled packet.

The preferred next step is one of:

1. `Agent #6` - ProjectProcessingCoordinator extraction
2. `Agent #7` - LogLevelCommandCoordinator extraction
3. `Agent #8` - MainWindow integration pass

The default recommendation is:

1. Do `Agent #6` first.
2. Then `Agent #7`.
3. Do `Agent #8` only after the extracted services are stable.

## Stop Conditions

Pause the phase immediately if:

- packet boundaries stop being reliable
- multiple active agents need hotspot files
- shared contracts are drifting
- targeted tests are missing
- integration failures indicate the slices were defined too broadly or too narrowly

If the phase is paused, the orchestrator must revise the packet plan before more implementation continues.

## Deliverables At End Of Phase 1

Phase 1 should leave the repository with:

- the first five portable service slices extracted
- tests owned by each packet
- an integration branch that has passed validation
- no uncontrolled edits to `MainWindow.xaml.cs`
- a cleaner path into the next single-agent extraction steps

## Bottom Line

Phase 1 succeeds if it creates reusable, tested, parallel-built service slices without triggering shared-file chaos.

The discipline is:

- parallelize portable service extraction
- centralize orchestration decisions
- delay shared-file integration
- validate before moving forward
