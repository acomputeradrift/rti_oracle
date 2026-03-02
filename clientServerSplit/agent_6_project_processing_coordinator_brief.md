# Agent #6 Brief: ProjectProcessingCoordinator

## Agent

- `Agent`: Agent #6

## Packet

- `Title`: ProjectProcessingCoordinator extraction
- `Execution mode`: single-agent only

## Objective

Extract the project-processing orchestration seams from `OracleByFPCLtd/MainWindow.xaml.cs` into a reusable coordinator that separates:

- local UI/file-selection concerns
- deterministic project-processing workflow logic
- additional-info load and reprocessing sequencing

This packet supports LOC reduction and cleaner boundaries for a future client/server split while preserving current app behavior.

## Why This Is Single-Agent Only

This packet is `single-agent only` because it is likely to touch:

- shared orchestration flow
- progress/update sequencing
- processing initialization paths
- code that interacts closely with existing `MainWindow` state

The merge and behavior risk is higher than the first-wave portable service packets.

## Current Source Area

This packet is derived from project-processing workflow logic currently embedded in:

- `OracleByFPCLtd/MainWindow.xaml.cs`

Representative responsibility area:

- project selection handling
- processing initialization
- additional-info load sequencing
- reprocessing orchestration and progress updates

## Scope

The target logic includes behavior equivalent to:

- coordinating project-data load for processing
- sequencing processing initialization
- coordinating additional-info loading
- handling deterministic parts of reprocessing workflow
- preserving progress-state transitions where they can be cleanly separated from WPF display

UI shell behavior such as dialogs, overlays, and direct control updates should remain outside the core coordinator unless strictly needed for the integration seam.

## Allowed Files To Modify

- new coordinator/service file(s) under `OracleByFPCLtd`
- packet-owned test file(s) under `OracleByFPCLtd.Tests`
- `OracleByFPCLtd/MainWindow.xaml.cs` only as required for extraction and wiring
- packet-specific docs in `clientServerSplit` only if clarification is required

## Forbidden Files To Modify

- files owned by any active parallel packet
- unrelated UI files outside the packet’s project-processing scope
- unrelated test files outside the packet’s owned test surface

If implementation requires broader scope, stop and escalate.

## Deliverables

1. A project-processing coordinator with a narrow, explicit API.
2. Clear separation between UI-triggered file selection and deterministic workflow logic.
3. Packet-owned unit tests for the extracted deterministic logic.
4. Minimal, focused `MainWindow` changes only as needed for the extraction seam.

## Required Unit Tests

Minimum required test coverage:

1. Project-processing initialization sequencing.
2. Additional-info load behavior.
3. Reprocessing workflow steps that can be asserted deterministically.
4. Error-path behavior for missing or unusable processing inputs where applicable to the extracted logic.
5. Preservation of deterministic outcomes for extracted workflow logic.

## Behavior Requirements

- Preserve current processing behavior.
- Preserve current sequencing and state-transition semantics.
- Do not reduce current capabilities related to project data or additional info.
- Do not introduce slower or more complicated processing flow.

## Architecture Requirements

- Keep WPF display concerns out of the coordinator as much as possible.
- Keep deterministic workflow logic UI-free where possible.
- Do not add server infrastructure.
- Favor a seam that can later support client-local execution or server-backed execution.

## Completion Criteria

This packet is complete only when:

1. The coordinator exists.
2. Packet-owned tests exist.
3. Targeted tests pass.
4. `MainWindow` changes are narrow and justified.
5. No forbidden files were changed.

## Escalation Triggers

Escalate if:

- required workflow logic is too entangled with direct UI updates
- scope expands beyond project-processing coordination
- a shared contract change affects another packet
- broader `MainWindow` restructuring would be required
