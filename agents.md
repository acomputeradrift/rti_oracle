# agents.md  
RTI Diagnostics Application — AI Execution Rules

## 1. Purpose of This File

This file defines **how the AI must behave** while working on this project.

It exists to:
- Enforce correct development order
- Prevent scope creep
- Require tests and validation
- Eliminate undocumented assumptions
- Ensure deterministic, reviewable output

If a task is not explicitly permitted here, the AI must ask before proceeding.

---

## 2. High-Level Agent Mandate

The AI acts as a **deterministic engineering agent**, not a product designer.

The AI must:
- Implement only what is explicitly scoped
- Work in small, reviewable steps
- Prefer clarity over cleverness
- Never assume undocumented RTI behavior
- Never silently skip validation or testing

---

## 3. Allowed Task Categories

The AI is allowed to perform only the following categories of work:

1. Documentation updates (scope-consistent)
2. Architecture definition (non-implementation)
3. Code implementation (after approval)
4. Unit test creation
5. Refactoring (scope-preserving)
6. Bug fixing
7. Test failure analysis
8. Build and run instructions
9. Logging and diagnostics instrumentation

Anything else requires explicit approval.

---

## 4. Required Development Order

The AI must follow this order strictly:

1. **Clarify inputs and outputs**
2. **Define interfaces and data shapes**
3. **Write unit tests**
4. **Write implementation**
5. **Run tests**
6. **Fix failures**
7. **Request review before proceeding**

Skipping steps is not allowed.

---

## 5. Testing Rules (Mandatory)

### 5.1 Unit Tests

- Every non-trivial function must have unit tests
- Tests must be written **before or alongside** implementation
- Tests must:
  - Use realistic RTI diagnostics samples
  - Include edge cases
  - Include malformed input cases

### 5.2 Test Constraints

- Tests must be deterministic
- No network calls in unit tests
- No reliance on real RTI hardware in tests
- Mock all external dependencies

If a function cannot be unit tested, the AI must explain why.

---

## 6. Logging Rules

The AI must add logging at:

- SHP connection lifecycle events
- WebSocket subscribe / unsubscribe actions
- Incoming diagnostics message boundaries
- Parsing failures
- Mapping failures
- Filter application
- Export operations

Logging must:
- Be structured
- Be human-readable
- Never leak raw JSON into the UI layer

---

## 7. Error Handling Rules

The AI must:

- Fail loudly on invalid assumptions
- Surface errors to the UI in a controlled way
- Never silently swallow parsing or mapping errors
- Distinguish between:
  - Transport errors
  - Data errors
  - Mapping errors
  - User input errors

---

## 8. State & Data Integrity Rules

- Raw diagnostics data is immutable once captured
- Processed data must always be derivable from raw data
- No hidden mutation of log state
- No cross-session data leakage

If caching is introduced, it must be explicit and documented.

---

## 9. UI Discipline Rules

The AI must respect the **five defined UI areas**:

1. Discovery / Connection / File Uploads  
2. Driver Log Level Controls  
3. Session Controls / Filtering / Export  
4. Raw Logs  
5. Processed Logs  

Rules:
- No UI element may perform multiple responsibilities
- Filtering applies to both log windows
- Raw Logs never show JSON
- Processed Logs never show raw data

---

## 10. External Knowledge Constraints

The AI must **not**:

- Invent undocumented RTI APIs
- Assume access to sysvar names
- Assume APEX internals beyond what is parsed
- Assume stability of diagnostics formats without validation

If external knowledge is required:
- The AI must ask explicitly
- The AI must not proceed until confirmed

---

## 11. Change Control

Before making **any** of the following, the AI must ask:

- Adding a new feature
- Changing UI layout
- Introducing persistence
- Introducing concurrency
- Introducing background workers
- Changing data flow direction

---

## 12. Output Discipline

When responding, the AI must:

- State exactly what it is about to do
- Limit changes to the approved scope
- Provide copy-pastable output
- Avoid partial or implied implementations

---

## 13. Stopping Rule

If requirements are ambiguous, conflicting, or missing:

- The AI must stop
- Ask a clarifying question
- Wait for instruction

Proceeding under uncertainty is forbidden.

---

## END OF FILE
