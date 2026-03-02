# Agent Handoff Template

## Purpose

This document defines the standard handoff format that all agents must use when reporting back to the orchestrator.

The goals are:

- consistent status reporting
- faster orchestration decisions
- clear escalation signals
- traceable packet outcomes

Use this template whenever a packet is:

- complete
- blocked
- needs re-scope
- failed

## Required Status Values

Each handoff must declare one of these statuses:

- `complete`
- `blocked`
- `needs_rescope`
- `failed`

## Required Handoff Fields

Every handoff must include:

- `Agent`
- `Packet`
- `Branch`
- `Status`
- `Summary`
- `Files changed`
- `Files not changed`
- `Tests added or updated`
- `Targeted test status`
- `Blockers or risks`
- `Requested orchestrator decision`

## Standard Template

Use the following structure:

### Agent

- `Agent`: Agent #X

### Packet

- `Packet`: packet title

### Branch

- `Branch`: branch name

### Status

- `Status`: complete | blocked | needs_rescope | failed

### Summary

- brief statement of what was completed or what stopped progress

### Files Changed

- list every modified file in packet scope

### Files Not Changed

- explicitly confirm key forbidden files were not changed

Examples:

- `OracleByFPCLtd/MainWindow.xaml.cs` not changed
- shared test hotspots not changed

### Tests Added Or Updated

- list packet-owned test files added or changed

### Targeted Test Status

- state whether targeted tests were run
- state whether they passed
- if not run, say why

### Blockers Or Risks

- list current blockers, unresolved concerns, or known risks
- if none, state `none`

### Requested Orchestrator Decision

- state what the orchestrator needs to decide next

Examples:

- accept packet
- approve follow-up packet
- re-scope packet
- convert to single-agent only
- resolve contract conflict

## Complete Handoff Example

### Agent

- `Agent`: Agent #1

### Packet

- `Packet`: LogFilterService extraction

### Branch

- `Branch`: agent-1-log-filter-service

### Status

- `Status`: complete

### Summary

- Extracted the filter service and added packet-owned tests for parsing and line matching.

### Files Changed

- `OracleByFPCLtd/...`
- `OracleByFPCLtd.Tests/...`

### Files Not Changed

- `OracleByFPCLtd/MainWindow.xaml.cs` not changed

### Tests Added Or Updated

- packet-owned filter service test files

### Targeted Test Status

- targeted tests run and passed

### Blockers Or Risks

- none

### Requested Orchestrator Decision

- accept packet and merge in planned order

## Blocked Handoff Example

### Agent

- `Agent`: Agent #3

### Packet

- `Packet`: StructuredMessageParser extraction

### Branch

- `Branch`: agent-3-structured-message-parser

### Status

- `Status`: blocked

### Summary

- Extraction reached a required shared contract not defined in the current packet scope.

### Files Changed

- list current in-scope files only

### Files Not Changed

- `OracleByFPCLtd/MainWindow.xaml.cs` not changed

### Tests Added Or Updated

- list any packet-owned tests already added, or state none

### Targeted Test Status

- targeted tests not complete because the packet is blocked

### Blockers Or Risks

- shared parse result contract is needed before progress can continue

### Requested Orchestrator Decision

- define a contract packet or re-scope this packet

## Orchestrator Use

The orchestrator should use the handoff to decide whether to:

- accept the packet
- request fixes
- create a follow-up packet
- change execution mode
- reassign or re-scope work

No packet should be treated as complete without a handoff in this format.

## Rule

If an agent cannot clearly fill out this template, the packet definition is likely too vague and should be clarified before more implementation continues.
