# Phase 2 Execution Checklist

## Purpose

This document is the execution runbook for the second implementation wave of the client/server split preparation.

Phase 2 begins after the first parallel-safe service extraction wave is complete and validated.

This phase covers the controlled single-agent steps that:

- extract higher-risk orchestration seams
- reduce responsibility density in `MainWindow`
- prepare the final integration path

## Phase 2 Goal

Complete the higher-risk single-agent extraction and integration work in a controlled order so that:

- the portable services from Phase 1 are actually used
- more workflow logic is removed from `MainWindow`
- the code moves closer to a thin-client structure
- current behavior, performance, and capability are preserved

## Phase 2 Included Packets

Phase 2 includes:

1. `Agent #6` - ProjectProcessingCoordinator extraction
2. `Agent #7` - LogLevelCommandCoordinator extraction
3. `Agent #8` - MainWindow integration pass

These packets are deliberately single-agent only.

## Phase 2 Prerequisites

Do not start Phase 2 until all of the following are true:

1. Phase 1 packet work is complete.
2. Phase 1 targeted packet tests have passed.
3. Phase 1 integration branch exists and has passed full validation.
4. The extracted service seams from Agents #1 through #5 are considered stable enough for reuse.
5. The orchestrator confirms there are no unresolved contract conflicts from Phase 1.

If any prerequisite is missing, stop and resolve it before Phase 2 begins.

## Step 1: Orchestrator Readiness Check

Before any Phase 2 implementation starts, the orchestrator must confirm:

1. Packet scopes for Agents #6, #7, and #8 are still valid.
2. No hidden overlap exists with unresolved Phase 1 follow-up work.
3. The integration target is clear.
4. `MainWindow.xaml.cs` edits are reserved for controlled single-agent packets only.
5. Any shared contract dependencies needed for Phase 2 are known.

If the packet definitions need revision, revise the docs first.

## Step 2: Branch Creation

Create the dedicated single-agent branches:

1. `agent-6-project-processing-coordinator`
2. `agent-7-loglevel-command-coordinator`
3. `agent-8-mainwindow-integration`

Rules:

- one agent per branch
- no parallel sharing of these branches
- `agent-8-mainwindow-integration` should not start until earlier Phase 2 work is ready

## Step 3: Execute Agent #6

`Agent #6` should run first by default.

Primary focus:

- extract project-processing coordination seams
- isolate deterministic workflow logic
- keep UI shell concerns out of the coordinator where possible

Completion checks:

1. The coordinator exists.
2. Packet-owned tests exist.
3. Targeted tests pass.
4. `MainWindow` edits remain narrow and justified.

If the packet expands beyond project-processing coordination, stop and escalate.

## Step 4: Execute Agent #7

`Agent #7` should run after Agent #6 is complete or explicitly deemed non-conflicting and stable.

Primary focus:

- extract log-level command coordination
- isolate ack and retry workflow logic
- preserve timing-sensitive behavior

Completion checks:

1. The coordinator exists.
2. Packet-owned tests exist.
3. Targeted tests pass.
4. `MainWindow` edits remain narrow and justified.

If the packet begins to require broad reconnect or transport changes, stop and escalate.

## Step 5: Intermediate Integration Check

Before starting Agent #8, the orchestrator should confirm:

1. Agent #6 output is stable.
2. Agent #7 output is stable.
3. Shared contracts are still consistent.
4. `MainWindow` is ready for the focused integration pass.

This checkpoint exists to prevent the final integration packet from becoming a hidden redesign.

## Step 6: Execute Agent #8

`Agent #8` performs the controlled `MainWindow` integration pass.

Primary focus:

- rewire `MainWindow` to use extracted services and coordinators
- reduce embedded non-UI logic
- keep `MainWindow` focused on client orchestration and UI responsibilities

Completion checks:

1. `MainWindow` delegates to the extracted services/coordinators where intended.
2. The integration remains focused.
3. Regression/integration tests for changed paths exist.
4. Targeted tests pass.

If this packet turns into a broad redesign instead of integration, stop and re-scope.

## Step 7: Merge To Phase 2 Integration Branch

Create or use a dedicated Phase 2 integration branch, such as:

- `integration/client-server-phase-2`

Recommended merge order:

1. `agent-6-project-processing-coordinator`
2. `agent-7-loglevel-command-coordinator`
3. `agent-8-mainwindow-integration`

The orchestrator or designated integration agent should manage this sequence.

## Step 8: Phase 2 Integration Review

On the Phase 2 integration branch, confirm:

1. The extracted coordinators and services coexist correctly.
2. `MainWindow` is thinner and more focused on client orchestration.
3. No unstable or conflicting contracts remain.
4. No unrelated application areas were pulled into the change.

## Step 9: Validation Gate

Before Phase 2 is accepted:

1. Confirm packet-level targeted tests for Agents #6, #7, and #8 are complete.
2. Run the full repository test sequence.
3. Resolve cross-slice regressions on the integration branch only.

Standard validation entry point:

- `powershell -ExecutionPolicy Bypass -File .\run_test_sequence.ps1`

Phase 2 is not complete until the full validation run passes.

## Step 10: Phase 2 Exit Criteria

Phase 2 should leave the repository with:

- portable services from Phase 1 still intact and validated
- project-processing coordination reduced inside `MainWindow`
- log-level coordination reduced inside `MainWindow`
- `MainWindow` measurably closer to a thin-client orchestration role
- a clearer foundation for future client/server separation

## Stop Conditions

Pause Phase 2 immediately if:

- `MainWindow` changes become too broad to review safely
- packet boundaries collapse into general refactoring
- timing-sensitive behavior becomes unstable
- unresolved contract drift appears between extracted components
- performance or responsiveness regressions are detected

If the phase is paused, the orchestrator must re-scope before more work continues.

## Bottom Line

Phase 2 succeeds if the high-risk workflow and integration changes happen in a controlled single-agent sequence, not as ad hoc cleanup.

The discipline is:

- extract higher-risk orchestration deliberately
- integrate only after stable seams exist
- keep `MainWindow` moving toward a client-only shell
- validate fully before accepting the phase
