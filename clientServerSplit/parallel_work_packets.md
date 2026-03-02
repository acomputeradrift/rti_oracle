# Parallel Work Packets

## Purpose

This document defines the initial agent-owned implementation packets for the client/server upgrade-aligned LOC reduction.

These packets are designed to:

- minimize overlap
- support parallel execution
- assign explicit ownership
- assign unit-test responsibility
- identify when work must move to a single-agent integration phase

## Packet 1

- `Agent`: Agent #1
- `Title`: LogFilterService extraction
- `Execution mode`: parallel-safe
- `Purpose`: Extract filter parsing, date parsing, timestamp extraction, and line-matching logic from `MainWindow`.
- `Allowed files to modify`:
  - `OracleByFPCLtd` service file(s) created for log filtering
  - packet-specific tests in `OracleByFPCLtd.Tests`
  - packet docs if needed
- `Files forbidden to modify`:
  - `OracleByFPCLtd/MainWindow.xaml.cs`
  - any files owned by another in-flight packet
- `Dependencies`: none
- `Deliverables`:
  - reusable filter service
  - deterministic public API for filtering logic
  - packet-owned unit tests
- `Required unit tests`:
  - valid keyword parsing
  - invalid keyword parsing
  - valid date range parsing
  - invalid date range parsing
  - timestamp extraction
  - line inclusion/exclusion matching
- `Definition of done`:
  - service exists
  - tests exist
  - targeted tests pass

## Packet 2

- `Agent`: Agent #2
- `Title`: StatusMessageFormatter extraction
- `Execution mode`: parallel-safe
- `Purpose`: Extract status parsing, status normalization, severity mapping, and message formatting helpers from `MainWindow`.
- `Allowed files to modify`:
  - `OracleByFPCLtd` service file(s) created for status formatting
  - packet-specific tests in `OracleByFPCLtd.Tests`
- `Files forbidden to modify`:
  - `OracleByFPCLtd/MainWindow.xaml.cs`
  - any files owned by another in-flight packet
- `Dependencies`: none
- `Deliverables`:
  - reusable status formatter service
  - packet-owned unit tests
- `Required unit tests`:
  - status line parsing
  - invalid status parsing
  - severity mapping
  - normalization behavior
  - suppression rule behavior
- `Definition of done`:
  - service exists
  - tests exist
  - targeted tests pass

## Packet 3

- `Agent`: Agent #3
- `Title`: StructuredMessageParser extraction
- `Execution mode`: parallel-safe
- `Purpose`: Extract JSON/root parsing and structured diagnostics message interpretation helpers from `MainWindow`.
- `Allowed files to modify`:
  - `OracleByFPCLtd` service file(s) created for structured message parsing
  - packet-specific tests in `OracleByFPCLtd.Tests`
- `Files forbidden to modify`:
  - `OracleByFPCLtd/MainWindow.xaml.cs`
  - files assigned to another in-flight packet
- `Dependencies`: none
- `Deliverables`:
  - reusable parser service
  - packet-owned unit tests
- `Required unit tests`:
  - valid JSON root parsing
  - invalid JSON handling
  - structured message formatting outcomes
  - non-structured fallback behavior for the extracted methods
- `Definition of done`:
  - service exists
  - tests exist
  - targeted tests pass

## Packet 4

- `Agent`: Agent #4
- `Title`: LogSearchService extraction
- `Execution mode`: parallel-safe
- `Purpose`: Extract non-UI search mechanics and find-state calculations from `MainWindow`.
- `Allowed files to modify`:
  - `OracleByFPCLtd` service file(s) created for log search
  - packet-specific tests in `OracleByFPCLtd.Tests`
- `Files forbidden to modify`:
  - `OracleByFPCLtd/MainWindow.xaml.cs`
  - files assigned to another in-flight packet
- `Dependencies`: none
- `Deliverables`:
  - reusable search-state service
  - packet-owned unit tests
- `Required unit tests`:
  - query match indexing
  - no-match behavior
  - reset behavior
  - next/previous navigation state logic
- `Definition of done`:
  - service exists
  - tests exist
  - targeted tests pass

## Packet 5

- `Agent`: Agent #5
- `Title`: TaggedDiagnosticsAnalyzer extraction
- `Execution mode`: parallel-safe
- `Purpose`: Extract unresolved tag detection, tagged driver extraction, line normalization, and report-assembly helpers from `MainWindow`.
- `Allowed files to modify`:
  - `OracleByFPCLtd` service file(s) created for tagged diagnostics analysis
  - packet-specific tests in `OracleByFPCLtd.Tests`
- `Files forbidden to modify`:
  - `OracleByFPCLtd/MainWindow.xaml.cs`
  - files assigned to another in-flight packet
- `Dependencies`: none
- `Deliverables`:
  - reusable tagged diagnostics analyzer
  - packet-owned unit tests
- `Required unit tests`:
  - tag detection
  - tag extraction
  - tagged driver name extraction
  - line normalization
  - report assembly for grouped tagged messages
- `Definition of done`:
  - service exists
  - tests exist
  - targeted tests pass

## Packet 6

- `Agent`: Agent #6
- `Title`: ProjectProcessingCoordinator extraction
- `Execution mode`: single-agent only
- `Purpose`: Extract project-processing orchestration seams from `MainWindow`, including processing initialization and additional-info workflow coordination.
- `Allowed files to modify`:
  - new or existing project-processing service files
  - packet-owned tests
  - explicitly assigned orchestration files
- `Files forbidden to modify`:
  - files owned by active parallel packets
- `Dependencies`:
  - preferred after Packets 1 to 5 are complete or stabilized
- `Deliverables`:
  - reusable coordinator or orchestration service
  - packet-owned unit tests for deterministic logic
- `Required unit tests`:
  - initialization sequencing
  - additional-info load behavior
  - behavior preservation for extracted deterministic steps
- `Definition of done`:
  - coordinator exists
  - tests exist
  - targeted tests pass

Why single-agent:

- This work is more likely to touch shared orchestration flow and progress behavior.

## Packet 7

- `Agent`: Agent #7
- `Title`: LogLevelCommandCoordinator extraction
- `Execution mode`: single-agent only
- `Purpose`: Extract pending command tracking, ack correlation, and retry/result logic from `MainWindow`.
- `Allowed files to modify`:
  - new or existing log-level coordination files
  - packet-owned tests
  - explicitly assigned shared orchestration files
- `Files forbidden to modify`:
  - files owned by active parallel packets
- `Dependencies`:
  - should begin after earlier portable helper contracts are stable
- `Deliverables`:
  - reusable log-level coordination component
  - packet-owned unit tests
- `Required unit tests`:
  - ack key generation
  - pending command resolution
  - retry decision behavior
  - success/failure state transitions
- `Definition of done`:
  - coordinator exists
  - tests exist
  - targeted tests pass

Why single-agent:

- This is timing-sensitive and tightly coupled to existing workflow behavior.

## Packet 8

- `Agent`: Agent #8
- `Title`: MainWindow integration pass
- `Execution mode`: single-agent only
- `Purpose`: Integrate completed extracted services into `MainWindow` and reduce code-behind to client-only orchestration.
- `Allowed files to modify`:
  - `OracleByFPCLtd/MainWindow.xaml.cs`
  - directly related integration tests
  - any service registration/wiring files explicitly assigned to this packet
- `Files forbidden to modify`:
  - completed packet internals unless required for integration fixes
- `Dependencies`:
  - Packets 1 through 7 complete or explicitly approved as ready
- `Deliverables`:
  - `MainWindow` reduced to thinner orchestration role
  - integration test updates as required
- `Required unit tests`:
  - targeted tests for any changed integration behavior
  - regression coverage for replaced `MainWindow` logic paths
- `Definition of done`:
  - integration complete
  - targeted tests pass
  - full suite ready for validation

Why single-agent:

- `MainWindow.xaml.cs` is the primary merge hotspot and should not be edited by multiple agents in parallel.

## Validation Packet

- `Agent`: One designated validation agent
- `Title`: Full validation and convergence
- `Execution mode`: single-agent only
- `Purpose`: Merge completed work on the integration branch, run the full test sequence, and resolve cross-slice regressions.
- `Allowed files to modify`:
  - integration branch conflict resolutions
  - narrowly scoped regression fixes
- `Files forbidden to modify`:
  - unrelated files outside integration needs
- `Dependencies`:
  - completed implementation packets
- `Deliverables`:
  - integrated branch with passing validation
- `Required unit tests`:
  - packet-level targeted tests already complete
  - full suite execution required here

## Notes

- Packets 1 through 5 are the preferred first parallel phase.
- Packets 6 through 8 are controlled single-agent phases.
- Every packet owner is responsible for its own unit tests.
- If a packet needs a file outside its allowed scope, stop and create a new packet or reassign scope before proceeding.
