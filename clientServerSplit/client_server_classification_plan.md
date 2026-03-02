# Client/Server Classification Plan

## Purpose

This document defines how to classify the current RTI Oracle codebase into three future-facing buckets so LOC-reduction work supports the later move to a server-backed desktop client.

The three buckets are:

- Must stay client
- Likely server later
- Portable shared domain

This is a classification exercise first, not an implementation split.

## What The Buckets Mean

### Must Stay Client

Code belongs here if it is tightly coupled to the local desktop app, local operator workflow, or local machine/device interaction.

Typical traits:

- Depends on WPF controls, windows, or UI composition
- Owns local user interaction and screen state
- Starts or stops local diagnostic sessions
- Handles local rendering, filtering, searching, or export initiation
- Needs to remain usable even if a remote service is unavailable

Examples in this repository likely include:

- `MainWindow.xaml.cs`
- WPF panel and control code-behind files
- Window-specific UI coordination

### Likely Server Later

Code belongs here if it represents centrally managed knowledge, updateable intelligence, or functionality that benefits from remote control, remote distribution, or centralized aggregation.

Typical traits:

- Driver profile updates can be distributed remotely
- Feature improvements can be rolled out remotely
- Unknown profile data can be collected and analyzed centrally
- Behavior is valuable to update without shipping a new desktop executable
- Logic benefits from one source of truth

Examples in this repository likely include:

- Driver profile catalogs and match data
- Driver message templating rules
- Unknown/unmapped driver profile handling
- Feature logic that may be remotely improved over time

### Portable Shared Domain

Code belongs here if it is deterministic, UI-free, and can run either inside the current desktop app or behind a future API with minimal change.

Typical traits:

- Pure parsing, mapping, classification, or formatting logic
- Explicit inputs and explicit outputs
- No WPF dependencies
- No direct file-dialog, message-box, or control dependencies
- No assumption about in-process execution

Examples in this repository likely include:

- Parsing services
- Mapping services
- Formatting services
- Reusable command/result models

This is the highest-value bucket for current refactoring because it reduces LOC now and prepares code for later server migration.

## How The Classification Happens

Classify one area at a time, not the whole codebase at once.

For each large file or subsystem:

1. Identify the file's responsibilities.
2. Mark which parts are UI-only, domain-only, transport-only, or persistence-only.
3. Mark all external dependencies:
   - WPF/UI types
   - filesystem
   - network
   - local settings
   - driver profile data
4. Ask whether the behavior must run locally for responsiveness, offline use, or direct device control.
5. Ask whether the behavior would benefit from central remote management or remote updates.
6. If the logic is deterministic and independent of UI, classify it as portable shared domain even if it currently runs only in-process.
7. If a file mixes multiple bucket types, split by responsibility and classify the extracted pieces separately.

Do not classify an entire file into one bucket if the file is actually a mixed-responsibility monolith.

## Decision Rules

Use these rules in order:

1. If the code directly controls WPF views or local user interaction, it is `must stay client`.
2. If the code is updateable knowledge, centrally managed rules, or server-worthy intelligence, it is `likely server later`.
3. If the code is deterministic logic that could run either locally or remotely, it is `portable shared domain`.
4. If a component both orchestrates UI and performs domain logic, split the orchestration from the domain logic before final classification.

## What This Means For LOC Reduction

The classification should guide the refactor before LOC work begins:

- Reduce `MainWindow.xaml.cs` by stripping out portable domain logic and leaving only client orchestration.
- Reduce parser and formatter files by extracting reusable domain primitives, not by burying logic deeper inside the UI layer.
- Avoid creating new projects unless the new boundary clearly removes more code and complexity than it adds.
- Favor service extraction that keeps logic deployable locally now and movable to a server later.

## Suggested First Pass

Apply this classification to these files first:

1. `OracleByFPCLtd/MainWindow.xaml.cs`
2. `OracleByFPCLtd/ProjectData/ApexDiscoveryPreloadExtractor.cs`
3. `OracleByFPCLtd/DriverProfiles/Services/DriverMessageTemplateFormatter.cs`
4. `OracleByFPCLtd/ProjectData/Extractors/AdditionalDataExtractor.cs`
5. `OracleByFPCLtd/DiagnosticsTransport/TcpCaptureDiagnosticsTransport.cs`

These files are the best starting point because they combine high LOC pressure with likely architectural impact.

## Deliverable Format For Each Future Classification Doc

Each per-file or per-subsystem classification doc should contain:

- Current responsibilities
- Client-only responsibilities
- Server-likely responsibilities
- Portable shared domain responsibilities
- Proposed extraction boundaries
- Refactor order
- Risks to performance or behavior

This keeps the later refactor traceable and prevents LOC reduction from drifting away from the future client/server target.
