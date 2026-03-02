# Multi-Agent Git Workflow

## Purpose

This document defines the git workflow for parallel Codex agents during the client/server upgrade and LOC-reduction effort.

The goals are:

- isolate each agent's work
- keep changes reversible
- reduce merge conflicts
- preserve traceable review history

## Branch Model

The default model is:

1. One planning branch
2. One branch per agent packet
3. One integration branch
4. Merge to the main working branch only after validation

## Branch Types

### Planning Branch

Use one planning branch for:

- docs
- packet definitions
- contract definitions
- sequencing decisions

Suggested name:

- `plan/client-server-upgrade`

No parallel implementation should begin until the planning branch has the packet definitions needed for that phase.

### Agent Branches

Each parallel packet gets its own branch.

Rules:

- exactly one agent per branch
- no branch sharing
- no unrelated changes
- include the packet's tests in the same branch

Suggested naming:

- `agent-1-log-filter-service`
- `agent-2-status-message-formatter`
- `agent-3-structured-message-parser`
- `agent-4-log-search-service`
- `agent-5-tagged-diagnostics-analyzer`

The branch name should describe the owned slice, not just the agent number.

### Single-Agent Branches

For integration or hotspot work, use a dedicated branch owned by one agent.

Suggested naming:

- `agent-6-mainwindow-integration`
- `agent-7-loglevel-coordinator-integration`

These are for `single-agent only` packets.

### Integration Branch

Use one integration branch to combine completed agent branches before merging to the target branch.

Suggested naming:

- `integration/client-server-phase-1`

This branch is where:

- agent branches are merged in dependency order
- merge conflicts are resolved
- full validation is run

## Required Rules

1. Every agent works on its own branch.
2. Multiple agents must never share a branch.
3. Agent branches must stay narrow and short-lived.
4. Integration merges must happen in a deliberate order.
5. Shared-file integration must not happen independently on multiple branches.

## Merge Order

Merge branches in this order:

1. Contract or shared-model branches
2. Low-risk portable service branches
3. Medium-risk portable/domain branches
4. High-risk orchestration branches
5. Final shared-file integration branches

For this repository, the preferred order is:

1. `LogFilterService`
2. `StatusMessageFormatter`
3. `StructuredMessageParser`
4. `LogSearchService`
5. `TaggedDiagnosticsAnalyzer`
6. `ProjectProcessingCoordinator`
7. `LogLevelCommandCoordinator`
8. `MainWindow` integration

## Contract Freeze Rule

If multiple agent branches depend on the same contract:

- create or update the contract first
- merge that contract branch early
- require dependent branches to build against that frozen contract

Do not allow multiple active branches to redefine the same shared contract.

## Hotspot Rule

The following files are hotspot files and should not be edited by multiple agents in parallel:

- `OracleByFPCLtd/MainWindow.xaml.cs`
- any large shared test file already assigned to another packet

Changes to hotspot files should be deferred to a dedicated single-agent branch whenever possible.

## Validation Rule

Before merging the integration branch to the target branch:

1. Confirm all packet-level targeted tests have passed.
2. Run the full repository test sequence using the standard script.
3. Resolve integration regressions on the integration branch only.

The standard validation entry point is:

- `powershell -ExecutionPolicy Bypass -File .\run_test_sequence.ps1`

## Commit Discipline

Each branch should produce:

- focused commits
- one logical change per commit where practical
- no unrelated cleanup

Do not mix multiple packets into one commit.

## Failure / Rollback Rule

If an agent branch is not viable:

- do not partially merge it
- drop or redo the branch as a complete unit

Each branch should remain independently reversible.

## Anti-Patterns To Avoid

- multiple agents editing the same branch
- long-lived branches with drifting contracts
- merging directly to the main working branch without an integration checkpoint
- overlapping packet scopes
- surprise refactors outside packet ownership

## Bottom Line

The safe default is:

- one branch per agent packet
- one branch for each single-agent integration packet
- one integration branch per major phase

That keeps the work isolated, reviewable, and reversible.
