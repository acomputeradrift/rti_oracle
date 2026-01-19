## System Invariants & Non-Negotiable Rules (RTI Oracle)

> **Status:** REQUIRED  
> **Purpose:** Defines conditions that must always hold true, regardless of implementation  
> **Rule:** If any invariant is violated, the output is invalid by definition

---

## 1. Diagnostic-Only Invariant (HARD)

RTI Oracle must remain a **diagnostics-only system**.

At all times:
- The system may influence RTI SHP behavior **only** via Driver Log Level settings
- No other control, configuration, or operational commands are permitted
- No inferred control paths may be introduced later

Violation of this invariant invalidates the system.

---

## 2. Source Data Integrity Invariant

All source inputs must remain intact and traceable.

At all times:
- All diagnostic input from the SHP is treated as untrusted and must never be inferred, repaired, or silently corrected
- `.APEX` files are treated as read-only reference data
- Project spreadsheets are treated as read-only reference data
- No source data may be modified, rewritten, normalized destructively, or hidden
- Diagnostic ingestion and parsing must be stream-safe and byte-preserving; loss, reordering, or synthesis of data is forbidden.

Any transformation must preserve the ability to reconstruct the original input.

---

## 3. No-Inference Invariant (CRITICAL)

RTI Oracle must never infer data.

This includes (but is not limited to):
- Guessing names for identifiers
- Assuming relationships not explicitly present
- Filling missing fields with defaults
- Selecting “closest” or “likely” matches

If data is missing, unknown, or ambiguous, it must be surfaced as such.

---

## 4. Explicit Mapping Invariant

All mappings applied to diagnostic output must be:

- Explicit
- Traceable
- Evidence-based

For every mapped value:
- The source of truth must be identifiable
- The mapping outcome must be classifiable (`resolved`, `unmapped`, `empty_mapping`, `conflict`)
- Unresolved mappings must remain visible in output

Silent or implicit mappings are forbidden.

---

## 5. Determinism Invariant

Given the same inputs:
- SHP diagnostic capture
- `.APEX` file
- Project spreadsheet
- Selected Driver Log Levels
- Applied filters

RTI Oracle must produce the same outputs.

The system must not depend on:
- Wall-clock time (except where explicitly displayed as captured)
- Non-deterministic ordering
- External mutable state

---

## 6. Output Honesty Invariant

All outputs must be honest representations of what is known.

This applies to:
- On-screen processed logs
- Exported processed logs
- Project information reports

The system must never:
- Claim completeness where it does not exist
- Hide unknowns or gaps
- Present provisional structures as authoritative facts

---

## 7. Raw-to-Processed Traceability Invariant

Every processed log entry must be traceable to:

- A specific raw SHP diagnostic entry
- The exact mappings applied (if any)
- The filters active at the time of rendering/export

If traceability is broken, the output is invalid.

---

## 8. Provisional Structure Isolation Invariant

Structures marked **PROVISIONAL** in `data_contracts.md`:

- Must not be treated as stable
- Must not be relied on for correctness guarantees
- Must not silently harden into assumptions

Only AUTHORITATIVE sections may constrain future behavior.

---

## 9. Failure Behavior Invariant

On error, ambiguity, or partial data:

- The system must fail safely
- Evidence must be preserved
- Processing may continue where possible
- Guessing is never permitted

A partial result is acceptable. A misleading result is not.

---

## 10. Invariant Enforcement Rule

If any invariant is violated:
- The result is invalid
- Processing must stop or flag the output
- Human review is required before continuation

---

## 11. Invariants Acknowledgement

Acknowledged by: **Jamie Feeny**  
Date: **2026-01-18**

---

END OF INVARIANTS

