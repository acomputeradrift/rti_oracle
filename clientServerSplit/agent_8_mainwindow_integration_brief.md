# Agent #8 Brief: MainWindow Integration Pass

## Agent

- `Agent`: Agent #8

## Packet

- `Title`: MainWindow integration pass
- `Execution mode`: single-agent only

## Objective

Perform the controlled integration pass that rewires `OracleByFPCLtd/MainWindow.xaml.cs` to use the extracted services and coordinators produced by earlier packets, reducing `MainWindow` toward a thinner client-only orchestration role.

This is the key convergence step for the current phase of the refactor.

## Why This Is Single-Agent Only

This packet is `single-agent only` because:

- `MainWindow.xaml.cs` is the primary merge hotspot
- this packet depends on outputs from multiple earlier packets
- it is the highest-risk shared-file integration step
- multiple agents editing this file in parallel would create avoidable conflicts and regressions

## Current Source Area

Primary target:

- `OracleByFPCLtd/MainWindow.xaml.cs`

This packet uses the outputs of:

- Agent #1 through Agent #7 as applicable and ready

## Scope

This packet should:

- replace embedded logic with calls to extracted services/coordinators
- reduce responsibility density in `MainWindow`
- preserve `MainWindow` as the local client orchestration layer
- keep direct UI, dialog, and control-management responsibilities in `MainWindow`
- move non-UI deterministic logic out of `MainWindow` where earlier packets prepared the seam

This packet is integration, not broad redesign.

## Allowed Files To Modify

- `OracleByFPCLtd/MainWindow.xaml.cs`
- directly related integration test file(s) under `OracleByFPCLtd.Tests`
- service registration or closely related wiring files only if explicitly required
- packet-specific docs in `clientServerSplit` only if clarification is required

## Forbidden Files To Modify

- completed packet internals except for required integration fixes
- unrelated application files outside the integration path
- unrelated test files outside the packet’s integration test scope

If broader changes are required, stop and escalate.

## Deliverables

1. `MainWindow` updated to delegate to extracted services/coordinators.
2. Noticeable reduction in embedded non-UI logic in `MainWindow`.
3. Integration-focused tests updated or added as needed.
4. No drift from the current functional behavior.

## Required Unit / Integration Tests

Minimum required test coverage:

1. Regression coverage for changed `MainWindow` integration paths.
2. Verification that extracted services are invoked through preserved behavior seams.
3. Preservation of key UI-driven workflows affected by the integration.
4. No regression in status, filtering, parsing, search, tagged diagnostics, project-processing, or log-level integration paths that were rewired.

## Behavior Requirements

- Preserve all current behavior.
- Preserve performance and responsiveness.
- Preserve current capabilities.
- Do not remove safeguards, error reporting, or operator feedback.

## Architecture Requirements

- Keep `MainWindow` focused on client orchestration, UI updates, and local workflow.
- Delegate deterministic non-UI logic to extracted services.
- Do not introduce server runtime infrastructure in this packet.
- Make the resulting `MainWindow` easier to evolve into a lightweight client shell later.

## Completion Criteria

This packet is complete only when:

1. `MainWindow` is successfully rewired to use the extracted services.
2. Integration tests or regression tests for changed paths exist.
3. Targeted tests pass.
4. The changes remain tightly focused on integration.
5. The code is ready for full validation on the integration branch.

## Escalation Triggers

Escalate if:

- earlier packet outputs are incomplete or inconsistent
- integration reveals unstable shared contracts
- the packet turns into a broad refactor beyond integration
- unrelated subsystems would need to be changed to complete the wiring
