## 1. In-Scope Capabilities

RTI Oracle explicitly includes:

1. Capturing diagnostic output from RTI SHPs at user-selected Driver Log Level settings  
2. Establishing a direct connection between Oracle and the RTI SHP for diagnostics only  
3. Accepting `.APEX` project files and project detail spreadsheets as read-only inputs  
4. Parsing and normalizing diagnostic logs, `.APEX` data, and spreadsheet data  
5. Mapping raw log identifiers to human-readable names using project reference files  
6. Displaying raw and processed logs side-by-side  
7. Applying filters to processed logs for inspection and export  
8. Exporting the processed log view exactly as displayed  
9. Generating a compiled project information report from all known inputs

---

## 2. Out-of-Scope Capabilities

RTI Oracle explicitly excludes:

1. Device or automation control of any kind  
2. Editing, deploying, or modifying RTI projects  
3. Writing back to `.APEX` files or spreadsheets  
4. Cloud-based control or modification of RTI systems  
5. Inferring or guessing missing project data

---

## 3. Deferred (Not in Current Scope)

The following are intentionally deferred:

1. Multi-user collaboration features  
2. Automated remediation or “fix suggestions”  
3. Persistent cloud storage of project data  

Deferred items must not influence architecture or implementation.

---

## 4. Explicitly Forbidden Work

Agents must refuse to:

- Extend system control beyond Driver Log Level settings  
- Introduce configuration editing features  
- Add speculative “future” extensibility hooks  
- Hide unresolved or missing mappings

---

## 5. Scope Change Rules

Scope may change only if:

1. `mission.md` is updated first  
2. This document is updated second  
3. Explicit human approval is given

No retroactive justification is allowed.

---

## 6. Scope Acknowledgement

Acknowledged by: **Jamie Feeny**  
Date: **2026-01-18**