# Transport Observations — RTI SHP Diagnostics

> **Status:** OBSERVATIONAL (NON‑AUTHORITATIVE)
>
> **Purpose:** Records empirically observed behavior of the RTI SHP diagnostic transport.
>
> **IMPORTANT:** This document does **not** define contracts, guarantees, or permissions.
> Nothing in this file may be relied upon as stable, complete, or future‑proof.
> All observations are subject to change based on SHP firmware, model, or configuration.

---

## 1. Scope of Observations

This document captures what has been **observed in practice** when monitoring RTI SHP diagnostic output using third‑party tooling.

It exists to:
- Preserve empirical knowledge
- Justify architectural and invariant decisions
- Support parser development and testing

It does **not**:
- Authorize data usage
- Define data schemas
- Override `data_contracts.md`

---

## 2. Observation Methodology

Observations were derived from:
- Passive monitoring of SHP diagnostic output
- TCP stream inspection
- Comparison of raw byte streams to decoded log output

Third‑party tools were used **only** to observe behavior; they are not dependencies of RTI Oracle.

---

## 3. Transport Characteristics (Observed)

The following characteristics have been observed:

- Diagnostic output is delivered over a **TCP connection**
- Default observed port: **2113**
- Communication direction is **SHP → client only** after connection
- No evidence of bidirectional command/control beyond log level configuration

These characteristics are **observations**, not guarantees.

---

## 4. Encoding & Framing (Observed)

Observed properties include:

- Text payloads are encoded as **UTF-16LE**
- Diagnostic messages are framed using explicit **start and end markers**
- Observed framing markers:
  - Start of message: `0x01 0x00`
  - End of message: `0x03 0x00`
- Framing markers are consistently present around decoded diagnostic messages

Important clarifications:
- Framing markers delimit logical diagnostic messages but **do not imply packet boundaries**
- Framed messages may still be split across TCP segments or interleaved with other data
- Framing markers have been empirically verified but are still treated as **observed behavior**, not contractual guarantees

---

## 5. Stream Behavior Notes

Observed stream behavior:

- Diagnostic data should be treated as a **continuous byte stream**
- Record boundaries are not guaranteed to align with TCP packet boundaries
- Partial records are expected and normal

These observations justify stream‑safe parsing requirements elsewhere in the system.

---

## 6. Relationship to Other Documents

This document:
- Informs **architecture.md** (transport model decisions)
- Informs **invariants.md** (stream safety, no inference)
- Informs **runbook.md** (operator expectations)

This document does **not**:
- Add fields to `data_contracts.md`
- Modify system scope
- Create operational requirements

---

## 7. Change & Validation Rules

- New observations may be appended with evidence
- Existing observations must not be rewritten without justification
- Contradictory findings must be recorded, not erased

---

END OF TRANSPORT OBSERVATIONS

