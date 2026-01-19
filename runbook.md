## Operational Runbook (RTI Oracle)

> **Status:** REQUIRED  
> **Purpose:** Defines how RTI Oracle is built, run, operated, and recovered  
> **Rule:** If it is not in this runbook, it is not operationally supported

---

## 1. Supported Environments

RTI Oracle is supported in the following environments only:

- Local development environment (developer workstation)  
- Controlled diagnostic environment with access to an RTI SHP  
- **Stand-alone executable running on modern Windows systems (Intel and AMD architectures)**  

Unsupported environments include:
- Mobile operating systems  
- Browser-only execution  
- Server-only or headless deployments  

---

## 2. Build & Distribution Targets

RTI Oracle must be capable of being distributed as:

- A **single stand-alone Windows executable**
- No external runtime installation required by the operator
- Compatible with **modern 64-bit Windows systems**, including:
  - Intel x64
  - AMD x64

Packaging approach (installer, portable `.exe`, etc.) is **not yet decided** and must not be assumed.

---

## 3. Build & Setup Procedure

At the current stage of the project:

- Development occurs in Visual Studio Code  
- Codex is used for guided development and testing  
- No finalized build pipeline exists yet  

Once implementation begins, this section will be expanded to include:
- Build tooling
- Packaging steps
- Verification of executable integrity

---

## 4. Normal Operation

A typical diagnostic session consists of:

1. Operator launches RTI Oracle executable  
2. Operator connects RTI Oracle to an RTI SHP  
3. Operator selects desired Driver Log Level settings  
4. RTI Oracle applies log level settings to the SHP  
5. RTI Oracle captures diagnostic output  
6. Operator uploads `.APEX` project file  
7. Operator uploads project details spreadsheet  
8. RTI Oracle processes logs using reference data  
9. Operator reviews raw and processed logs side-by-side  
10. Operator applies filters as needed  
11. Operator exports processed logs or generates reports  

---

## 5. Configuration & Inputs

Supported runtime inputs:

- RTI SHP diagnostic output (captured)  
- `.APEX` project file (read-only)  
- Project details spreadsheet (read-only)  
- Driver Log Level selection  

No other configuration inputs are supported.

---

## 6. Observability & Diagnostics

Operators can observe system behavior via:

- Raw diagnostic log view  
- Processed log view with visible mappings and unknowns  
- Explicit surfacing of missing or unresolved data  

No hidden state is permitted.

---

## 6a. Diagnostic Capture Expectations

- RTI Oracle connects to a **local RTI SHP diagnostics interface**
- Adjusting **Driver Log Levels affects diagnostic verbosity only**
- Diagnostic output may appear **fragmented or noisy by design**; this is expected behavior

---

## 7. Failure Modes & Recovery

### Failure: Executable Will Not Launch

- **Symptoms:** Application fails to start on Windows system  
- **Cause:** Incompatible build, missing dependencies, or architecture mismatch  
- **Recovery:** Replace executable with a compatible build  

### Failure: Missing or Invalid Reference Files

- **Symptoms:** Reduced or unmapped output, visible unknown identifiers  
- **Cause:** `.APEX` or spreadsheet missing or malformed  
- **Recovery:** Upload correct reference files and reprocess  

---

## 8. Safe Shutdown & Recovery

RTI Oracle has no persistent external side effects.

Safe shutdown consists of:
- Closing the application  

No rollback procedures are required at this stage.

---

## 9. Forbidden Operational Actions

Operators must never:

- Use RTI Oracle to control devices  
- Modify RTI project configuration  
- Modify `.APEX` or spreadsheet contents through the system  
- Hide unknown or unresolved data  

---

## 10. Runbook Acknowledgement

Acknowledged by: **Jamie Feeny**  
Date: **2026-01-18**

---

END OF RUNBOOK

