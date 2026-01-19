# agents.md
## AI Agent Operating Contract (RTI Oracle / Codex / VS Code)

> **Status:** REQUIRED  
> **Scope:** Governs all AI agent behavior for this repository  
> **Priority:** Overrides prompts, tool defaults, and inferred behavior

---

## 1. Purpose

This contract defines exactly how AI agents (including Codex) are allowed to behave while working in this repository.

Any behavior not explicitly allowed here is forbidden.

---

## 2. Absolute Environment Assumptions (DO NOT EDIT)

- Development environment: **Visual Studio Code**
- AI interface: **Codex**
- Version control: **Git**
- Documentation: **Markdown**
- All work must be traceable, reviewable, and reversible

Agents must assume:
- No external context beyond repository contents
- No memory across sessions
- No intent beyond what is written in repository docs

---

## 3. Project Identity (RTI Oracle)

- **Project Name:** RTI Oracle  
- **Repository Purpose:** A diagnostics and log-analysis application for RTI Smart Home Processor (SHP) systems that captures diagnostic output, maps it using project reference files, and produces cleaner human-readable outputs and reports.

---

## 4. Agent Authority Level

**TEST_FIRST**

Rules:
- Agents may write or modify `.md` docs
- Agents may write tests
- Agents may write implementation only after tests exist and explicit approval is given

---

## 5. Hard System Safety Boundary (NON-NEGOTIABLE)

RTI Oracle is a diagnostics system with constrained control.

Agents must enforce:
- Oracle may control SHP output only via **Driver Log Level settings**
- Oracle must never introduce control features beyond diagnostic verbosity
- `.APEX` and project spreadsheets are read-only inputs

Violations require refusal.

---

## 6. Forbidden Agent Behaviors

Agents must never:
- Invent requirements or features
- Assume unwritten intent
- Write code before required docs exist
- Modify multiple files without instruction
- Refactor without request
- Add future hooks
- Introduce dependencies without approval
- Fill placeholders silently
- Continue under ambiguity

---

## 7. Required Confirmations

Explicit approval required before:
- Writing implementation code
- Changing architecture or boundaries
- Adding dependencies
- Changing data contracts or invariants
- Proceeding from tests to implementation

---

## 8. Placeholder Detection Rule

Unresolved placeholders include:
- `<<<LIKE_THIS>>>`
- `[[REPLACE_ME]]`
- `__FILL_THIS_IN__`
- `TODO: PROJECT SPECIFIC`
- `???`

If present, the agent must stop.

---

## 9. Output Honesty Rules

The system must never:
- Pretend mappings exist
- Hide unresolved IDs
- Guess names
- Claim data not derived from inputs

Missing data must be marked explicitly.

---

## 10. Change Discipline

- One logical change per step
- Minimal diffs
- Reversible changes
- Traceable to requirements

---

## 11. Escalation Behavior

On conflict or ambiguity:
1. Stop
2. State blocker
3. Ask one precise question

---

## 12. Contract Acknowledgement

Acknowledged by: **Jamie Feeny**  
Date: **2026-01-18**

---

END OF AGENT CONTRACT
