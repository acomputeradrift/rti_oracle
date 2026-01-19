# testing_strategy.md
## Testing Strategy & Proof of Correctness (RTI Oracle)

> **Status:** REQUIRED  
> **Purpose:** Defines how RTI Oracle correctness is proven  
> **Rule:** Code may not exist unless it is provably correct under this strategy

---

## 1. Testing Philosophy

RTI Oracle correctness is proven by **determinism, traceability, and honesty**, not by coverage metrics.

Tests must demonstrate that:
- Given the same inputs, the system produces the same outputs
- Every processed output can be traced back to raw source data
- Missing, unknown, or ambiguous data is surfaced and never inferred

Passing tests are necessary but not sufficient.  
**Invariants override tests.**

---

## 2. Required Test Layers

### 2.1 Unit Tests (MANDATORY)

Unit tests must validate:

- Parsing of SHP diagnostic entries without schema assumptions  
- Defensive extraction of `.APEX` data (missing sections allowed)  
- Spreadsheet ingestion with literal column handling  
- Mapping classification (`resolved`, `unmapped`, `empty_mapping`, `conflict`)  

Unit tests must never:
- Depend on real SHP connections  
- Depend on file system state outside test fixtures  
- Assert implementation details  

---

### 2.2 Integration Tests (MANDATORY)

Integration tests must validate:

- End-to-end flow from captured SHP log → processed log entry  
- Use of `.APEX` and spreadsheet data together without merging sources implicitly  
- Preservation of raw data alongside processed representations  

Integration tests must use:
- Static, versioned fixture files  
- Known SHP diagnostic samples  
- Explicitly documented expected outputs  

---

### 2.3 Replay / Determinism Tests (MANDATORY)

Replay tests must demonstrate that:

- Reprocessing the same inputs produces identical outputs  
- Ordering of log entries is stable and deterministic  
- Filters do not alter underlying data, only presentation  

Replay tests are required for:
- Processed log export  
- Project information report generation  

---

### 2.4 Negative & Failure Tests (MANDATORY)

Failure tests must cover:

- Missing `.APEX` file  
- Missing spreadsheet  
- Partial or malformed SHP diagnostic entries  
- Identifiers present in logs but absent from reference data  
- Conflicting mappings across sources  

Failure tests must assert:
- No guessing occurred  
- Unknowns remain visible  
- Processing continues safely where possible  

---

## 3. Test-to-Requirement Mapping

Every test must map explicitly to at least one of:

- `mission.md`
- `scope.md`
- `data_contracts.md`
- `invariants.md`

Tests without a documented requirement mapping are invalid.

---

## 4. Determinism & Environment Control

Tests must be:

- Independent of wall-clock time  
- Independent of machine locale  
- Independent of execution order  
- Independent of external network state  

All inputs must be:
- Explicit  
- Versioned  
- Reproducible  

---

## 5. Provisional Structure Handling

Tests must treat **PROVISIONAL** structures as unstable.

Rules:
- Tests may assert behavior, not structure  
- Tests must not lock provisional fields into permanence  
- Structural assertions must be revisited once provisional status is removed  

---

## 6. Forbidden Testing Patterns

Tests must never:

- Assert guessed or inferred values  
- Mask invariant violations  
- Depend on execution order  
- “Fix” failures by loosening assertions  
- Skip validation for speed or convenience  

A test that hides uncertainty is worse than no test.

---

## 7. Test Execution Rules

- All required tests must pass before implementation is allowed  
- Partial test execution is invalid  
- Flaky tests are treated as failures  
- Test failures block progress until resolved or explicitly waived  

---

## 8. Testing Strategy Acknowledgement

Acknowledged by: **Jamie Feeny**  
Date: **2026-01-18**

---

END OF TESTING STRATEGY
