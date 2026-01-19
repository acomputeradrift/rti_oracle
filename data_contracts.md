# data_contracts.md
## Data Contracts & Allowed Structures (RTI Oracle)

> **Status:** REQUIRED  
> **Purpose:** Defines all data RTI Oracle is allowed to read, store, derive, and output  
> **Rule:** If a field is not defined here, it does not exist  
> **Important:** Sections marked *Provisional* are explicitly non-binding and subject to change based on empirical testing

---

## 1. Source Data Inputs (AUTHORITATIVE)

### 1.1 RTI SHP Diagnostic Stream (Captured Logs)

- **Source Name:** RTI SHP Diagnostics  
- **Origin:** RTI Smart Home Processor  
- **Nature:** Streaming / captured session data  
- **Trust Level:** Untrusted  

**Allowed High-Level Fields (as captured, verbatim):**
- `timestamp`
- `messageType`
- `category`
- `priority`
- `depth`
- `groupId`
- `flags`
- `text`
- `raw`

Rules:
- Unknown fields must be preserved verbatim
- Schema stability must not be assumed
- No inference or coercion of missing fields

---

### 1.2 `.APEX` Project File

- **Source Name:** RTI APEX Project  
- **Origin:** User upload  
- **Nature:** Static reference file  
- **Trust Level:** Authoritative but untrusted  

**Allowed Extraction Scope:**
- Explicit entities present in the file
- Explicit relationships present in the file

Rules:
- Missing sections are represented as empty sets
- No inferred relationships
- No normalization beyond naming consistency

---

### 1.3 Project Details Spreadsheet

- **Source Name:** Project Details Spreadsheet  
- **Origin:** User upload  
- **Nature:** Static reference file  
- **Trust Level:** Authoritative but untrusted  

**Allowed Extraction Scope:**
- Tables exactly as defined
- Rows and columns exactly as present

Rules:
- Column names are treated as literal strings
- Missing cells must be represented explicitly
- No inferred mappings

---

## 2. Normalized Internal Structures *(PROVISIONAL)*

> **Provisional Notice:**  
> Structures in this section are placeholders used to reason about the system.  
> They are **not final** and may change once real SHP/APEX data is validated.

### 2.1 CapturedLogEntry *(Provisional)*

```
{
  id,
  source,
  timestamp,
  messageType,
  category,
  priority,
  depth,
  groupId,
  flags,
  text,
  raw,
  extra
}
```

Rules:
- No information loss
- `extra` holds unknown fields verbatim

---

### 2.2 ProjectReferenceModel *(Provisional)*

```
{
  apex,
  spreadsheet
}
```

Rules:
- Sources remain separable
- Traceability must be preserved

---

## 3. Mapping Contracts (AUTHORITATIVE)

Mapping is allowed **only** when:

- A raw identifier exists in SHP diagnostic output  
- The identifier exists explicitly in `.APEX` or spreadsheet data  
- The resolution outcome is explicitly classified  

Allowed resolution states:
- `resolved`
- `unmapped`
- `empty_mapping`
- `conflict`

Forbidden:
- Guessing
- Pattern matching
- “Closest match”
- Silent fallback

---

## 4. Derived Data & Output Models *(PROVISIONAL)*

> **Provisional Notice:**  
> Output structures are subject to revision after log capture and mapping experiments.

### 4.1 Processed Log Representation *(Provisional)*

Conceptual properties only:
- Reference to original log entry
- Human-readable rendered text
- Explicit list of mappings used
- Applied filter context

---

### 4.2 Project Information Report *(Provisional)*

Conceptual contents only:
- What sources were present
- What entities were extracted
- What relationships were known
- What mappings were unresolved

No speculative or inferred content allowed.

---

## 5. Missing / Unknown Data Handling (AUTHORITATIVE)

- Missing mappings must be visible
- Unknown identifiers must remain visible
- Processing must continue safely
- Outputs must remain honest

---

## 6. Contract Enforcement (AUTHORITATIVE)

If a feature requires data not explicitly defined in this document:
- The feature is invalid
- Work must stop
- The contract must be updated first

---

## 7. Data Contracts Acknowledgement

Acknowledged by: **Jamie Feeny**  
Date: **2026-01-18**

---

END OF DATA CONTRACTS
