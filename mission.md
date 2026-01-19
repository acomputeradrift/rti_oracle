# mission.md
## Project Mission & Success Contract (RTI Oracle)

> **Status:** REQUIRED  
> **Purpose:** Defines what RTI Oracle is allowed to do  
> **Rule:** If it is not permitted here, it does not exist

---

## 1. Mission Statement

RTI Oracle is a diagnostics and analysis application whose purpose is to observe, capture, normalize, and analyze diagnostic output from RTI Smart Home Processors (SHPs), enrich that output using project reference files, and present clear, honest, human-readable information to assist programmers in troubleshooting and understanding system behavior.

RTI Oracle is not a control system.

---

## 2. Primary Users

- Residential and commercial smart-home programmers  
- RTI system integrators  
- Technical operators performing diagnostics and analysis  

---

## 3. Core Capabilities

RTI Oracle must be able to:

1. Capture diagnostic output from an RTI SHP based on selected Driver Log Level settings  
2. Accept `.APEX` project files and project detail spreadsheets as read-only reference inputs  
3. Normalize and correlate raw diagnostic logs with project data to produce human-readable output  
4. Display raw logs and processed logs side-by-side with filtering applied  
5. Export the processed log view exactly as displayed  
6. Generate a comprehensive project information report compiled from all known inputs  

---

## 4. Explicit Non-Goals

RTI Oracle must NOT:

- Control or operate devices connected to the RTI system  
- Modify RTI project configuration, drivers, or logic  
- Write back to `.APEX` files or project spreadsheets  
- Guess or infer missing project data  
- Hide unresolved mappings or unknown identifiers  

---

## 5. Success Criteria

RTI Oracle is successful when:

- A programmer can trace a processed log entry back to its raw source  
- All mappings used in output are explicit and explainable  
- Missing or unknown data is surfaced rather than hidden  
- Exported outputs match the on-screen representation exactly  

---

## 6. Failure Conditions

RTI Oracle is considered to have failed if:

- It presents guessed or inferred data as fact  
- It hides unresolved identifiers or mappings  
- It alters or misrepresents diagnostic source data  
- It performs actions outside its defined diagnostic scope  

---

## 7. Hard Constraints

- Control of the RTI SHP is limited strictly to Driver Log Level settings  
- All project reference files are read-only  
- Diagnostic data must be treated as untrusted input  
- Output must remain deterministic and replayable  

---

## 8. Out-of-Scope Forever

The following are permanently excluded:

- Device or automation control  
- Project editing or deployment  
- Cloud-based modification of RTI systems  

---

## 9. Authority Statement

This mission is authoritative.  
Any behavior, feature, or output that conflicts with this document is invalid.

---

## 10. Mission Acknowledgement

Acknowledged by: **Jamie Feeny**  
Date: **2026-01-18**

---

END OF MISSION
