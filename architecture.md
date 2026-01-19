## 1. Architectural Overview

RTI Oracle is a diagnostics and analysis system that connects to an RTI Smart Home Processor (SHP), configures Driver Log Level output, captures structured communication data, accepts uploaded project reference files, normalizes all available data, and presents it to a human operator for inspection, filtering, export, and reporting.

Uploaded reference files are read-only inputs used to map internal identifiers and raw log fields to human-readable names and to compile a comprehensive project information view. They do not modify the RTI SHP.

The system does not control devices, does not modify configuration data, and does not issue operational commands beyond adjusting diagnostic verbosity.

The architecture is unidirectional with constrained control, reference inputs, and controlled output generation:
Human → Oracle (log level intent + file uploads) → RTI SHP (diagnostic output only) → Oracle → Human (views + exported documents)

---

## 2. Major Components

### RTI SHP (External System)
- Generates diagnostic data based on Driver Log Level settings
- Outside Oracle’s control except for diagnostic verbosity

### Diagnostic Control Interface
- Applies Driver Log Level settings only
- No operational or automation control

### File Ingestion Layer
- Accepts .APEX project files and project spreadsheets as read-only reference inputs

### Transport / Capture Layer
- Captures diagnostic data verbatim from the SHP
- Establishes a direct TCP connection to the RTI SHP diagnostic interface
- Treats incoming diagnostics as a continuous stream rather than discrete packets
- Preserves all received diagnostic bytes without transformation or inference

### Normalization & Parsing Engine
- Structures logs and extracts mapping tables from uploaded files
- Performs stream-safe parsing to reconstruct complete diagnostic records
- Handles partial, fragmented, or interleaved diagnostic messages safely

### Analysis Engine
- Maps raw logs to human-readable output
- Compiles unified project information
- Performs mapping only when explicit identifiers are present in reference data

### Presentation Layer
- Displays raw and processed logs
- Provides controls for filtering and export

### Output & Reporting Layer
- Exports processed log views with applied filters
- Generates a comprehensive project information report

### Oracle Application (Implementation Reality)

- Language: C# (.cs)
- UI Framework: WPF (XAML)
- Runtime Target: .NET 8 (`net8.0-windows`)
- Application Type: Native Windows desktop application
- Project System: SDK-style `.csproj`
- External Dependencies: None (no external NuGet packages)
- Build Artifacts: `bin/` and `obj/` directories, including generated JSON artifacts

---

## 3. Data Flow

1. User launches Oracle
2. User connects Oracle to the RTI SHP
3. User selects Driver Log Level(s)
4. Oracle applies log level settings to SHP
5. SHP emits diagnostic output
6. Oracle captures diagnostic data
7. User uploads .APEX and project spreadsheet
8. Oracle parses logs and reference files
9. Oracle maps logs to human-readable output
10. Oracle displays results
11. Oracle exports filtered logs and project reports

---

## 4. Trust & Boundary Definitions

- Oracle → SHP: diagnostic control only
- SHP → Oracle: untrusted diagnostic data
- Uploaded files → Oracle: authoritative but untrusted
- Analysis → Presentation/Output: no reinterpretation

---

## 5. External Dependencies

Allowed:
- RTI Smart Home Processor
- Local operator workstation
- Local file system

Forbidden:
- Cloud services
- Device control APIs
- Configuration endpoints beyond log levels

---

## 6. Forbidden Architectural Patterns

- Extending control beyond log levels
- Writing back to project files
- Mutating raw diagnostic data
- Bidirectional control paths

---

## 7. Architecture Change Rules

Changes require updates to:
1. mission.md
2. scope.md
3. This file
4. Explicit human approval

---

## 8. Architecture Acknowledgement

Acknowledged by: Jamie Feeny  
Date: <<<YYYY-MM-DD>>>

Acknowledgement confirms:
- Review and acceptance of architectural constraints
- Agreement that Oracle operates in diagnostics-only mode
- Approval of trust boundaries and forbidden patterns

