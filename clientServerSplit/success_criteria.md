# Success Criteria

## Purpose

This document defines how success is judged for the client/server split preparation effort.

Success must be measured at three levels:

- packet success
- phase success
- overall run success

A run is successful only if all required levels pass.

## Core Principle

The goal is not only to make changes.

The goal is to make the right changes:

- lower LOC or responsibility density
- preserve current behavior
- preserve speed and responsiveness
- preserve current capabilities
- improve reuse and readability
- move the codebase toward a future lightweight client with a server/API backend

If a change reduces LOC but weakens behavior, performance, or future architecture, it is not a success.

## Packet Success

A packet is successful only if all of the following are true:

1. The packet stayed inside its allowed scope.
2. Forbidden files were not modified.
3. The deliverables in the packet brief were actually completed.
4. The packet added or updated its own packet-owned unit tests.
5. The packet's targeted tests pass.
6. The packet reported back using the handoff template.
7. The packet did not create unapproved contract drift.
8. The packet remained one logical change and did not expand into unrelated cleanup.

If any of these fail, the packet is not complete.

## Phase Success

A phase is successful only if all of the following are true:

1. All packets in the phase are completed and accepted.
2. Packet merges happened in the documented order.
3. The integration branch is stable.
4. No unresolved contract conflicts remain.
5. The full validation run for that phase passes.
6. The code is measurably more modular than before the phase began.
7. The phase leaves the repository ready for the next planned stage without re-planning the whole structure.

If any of these fail, the phase is not accepted.

## Overall Run Success

The full run is successful only if all of the following are true:

1. Current behavior is preserved.
2. Performance and responsiveness are preserved.
3. Current capabilities are preserved.
4. The targeted hotspot files are reduced in LOC or responsibility density.
5. More deterministic, reusable logic lives outside `MainWindow.xaml.cs`.
6. `MainWindow.xaml.cs` is materially closer to a thin client orchestration shell.
7. The code structure is more compatible with a future client/server split.
8. The resulting top-level run branch is worth keeping rather than deleting.

If the branch should be discarded and retried, the run did not succeed.

## Targeted Structural Success

The refactor should move toward these structural outcomes:

1. `MainWindow.xaml.cs` carries less embedded non-UI logic.
2. Deterministic parsing, formatting, filtering, and workflow logic are extracted into reusable services.
3. Updateable or remotely managed intelligence is more isolated than before.
4. The desktop app is moving toward a thinner client role.

These do not all need to be fully complete in one phase, but the direction must be clearly improved.

## LOC Success

Success is not measured by LOC alone, but LOC change still matters.

The relevant LOC success conditions are:

1. Large hotspot files shrink directly, or
2. Large hotspot files become meaningfully less responsibility-dense because extractable logic has been moved out in a controlled next step

Target direction:

- `MainWindow.xaml.cs` should move toward the baseline target of below `1500` LOC, or below `2000` in a conservative staged approach
- major parser/formatter hotspots should move toward a `30%` to `50%` reduction through extraction and reuse

If LOC rises temporarily due to additive extraction, that is acceptable only if:

- the added code creates a real reusable seam
- the follow-up integration path is explicit
- responsibility density is already reduced or clearly about to be reduced in the next controlled step

## Test Success

Testing success requires:

1. Each packet has packet-owned tests.
2. Targeted tests for each packet pass.
3. The full repository validation sequence passes at phase acceptance.
4. No behavior-preserving claims are accepted without test support where tests are expected.

The standard full validation entry point remains:

- `powershell -ExecutionPolicy Bypass -File .\run_test_sequence.ps1`

## Orchestration Success

The process itself must also succeed.

Orchestration is successful only if:

1. The orchestrator managed packet flow without requiring constant manual intervention.
2. Worker branches stayed isolated.
3. Packet boundaries remained enforceable.
4. Handoffs were clear and usable.
5. Integration did not collapse due to avoidable overlap or poor packet definition.

If the process becomes chaotic, the run is not a full success even if some code improvements were made.

## Hard Fail Conditions

The run should be treated as failed if any of the following occur:

1. A packet edits forbidden files without approval.
2. Multiple agents create uncontrolled overlap in hotspot files.
3. Shared contracts drift in incompatible ways.
4. Behavior regresses.
5. Performance or responsiveness regresses materially.
6. Capabilities are removed or weakened.
7. The integration branch becomes too unstable to trust.
8. The top-level run branch is judged unfit to keep.

If any hard fail condition occurs, the default response is:

- stop
- evaluate whether the branch should be discarded
- fix the orchestration or packet plan
- retry in a new run if needed

## Acceptance Decision

At the end of a run, the orchestrator should make one of these decisions:

- `accept`
- `accept_with_follow_up`
- `reject_and_retry`

### Accept

Use `accept` when:

- packet, phase, and run criteria all pass
- the branch is ready to keep and continue from

### Accept With Follow-Up

Use `accept_with_follow_up` when:

- the run is structurally successful
- no hard fail condition exists
- some planned reductions are incomplete but the branch is still valid and worth keeping

### Reject And Retry

Use `reject_and_retry` when:

- hard fail conditions occurred
- the branch is not worth keeping
- the orchestration or packet plan needs correction before trying again

## Bottom Line

Success means:

- the branch is worth keeping
- the app still behaves correctly
- the code is cleaner and more modular
- the architecture is closer to the future client/server target

If those conditions are not met, the run should be treated as a failed attempt and retried with a better plan.
