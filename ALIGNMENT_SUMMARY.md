=== INSTRUCTION ALIGNMENT SUMMARY ===

1. CONFIRMED ALIGNMENTS
- WPF .NET 8 desktop application with no external NuGet dependencies, aligning with `architecture.md` implementation reality and `runbook.md` supported environments.
- UI includes manual IP entry, connect/disconnect controls, driver log level controls, and side-by-side raw/processed log areas, aligning with `mission.md` and `scope.md` high-level capabilities.
- No device control features beyond log level adjustments are present, aligning with `mission.md`, `scope.md`, and `invariants.md` diagnostic-only boundary.

2. REQUIRED CHANGES
- Replace WebSocket/HTTP diagnostics transport with a direct TCP stream capture layer that preserves bytes and stream framing; violates `architecture.md` Transport/Capture requirements and `invariants.md` Source Data Integrity because current code uses `ws://{ip}:1234/diagnosticswss` and HTTP polling.
- Remove JSON `messageType` parsing as a required path for diagnostics ingestion; violates `data_contracts.md` (allowed fields only, no schema assumptions) and `invariants.md` (no inference, stream-safe parsing) because current ingestion assumes JSON payloads and fields not defined in contracts.
- Ensure raw log display never outputs raw JSON or inferred structures; violates `data_contracts.md` and `invariants.md` Output Honesty and Raw-to-Processed Traceability because current fallback emits raw JSON strings on parse failure.
- Implement read-only `.APEX` and spreadsheet ingestion and use them for explicit mapping only; violates `mission.md`, `scope.md`, `architecture.md`, and `data_contracts.md` because current code does not ingest these inputs or apply explicit mapping states.
- Implement required processed log mapping with explicit resolution states (`resolved`, `unmapped`, `empty_mapping`, `conflict`) and visible unknowns; violates `data_contracts.md` and `invariants.md` Explicit Mapping and No-Inference rules because current processed output is not mapped via reference data.
- Add mandatory unit, integration, replay/determinism, and negative tests mapped to requirements; violates `testing_strategy.md` because no tests exist to prove determinism, traceability, or no-inference behavior.

3. INVALID OR UNSAFE ASSUMPTIONS
- Diagnostics are available via WebSocket and HTTP endpoints (`ws://{ip}:1234/diagnosticswss`, `http://{ip}:5000/diagnostics/data/drivers`) without any contract in `data_contracts.md`, `architecture.md`, or `runbook.md`.
- WebSocket resources `MessageLog`, `Sysvar`, and `LogLevel` exist and behave as implemented without any contract authorization, conflicting with `data_contracts.md` and `invariants.md` no-inference rules.
- Inbound diagnostics are JSON with `messageType`, `time`, `text`, `sysvarid`, and `sysvarval` fields, which are not defined in `data_contracts.md` and conflict with `architecture.md` stream capture requirements.
- Emitting raw JSON to the raw log is acceptable when parsing fails, which conflicts with `data_contracts.md` and `invariants.md` Output Honesty and traceability rules.
- Treating observed transport characteristics as guaranteed would be unsafe; `shp_transport_observations.md` explicitly states observations are non-authoritative and do not create contracts.

4. MISSING OR AMBIGUOUS INSTRUCTION DETAILS
- No authoritative transport contract (port, framing markers, encoding) is defined in the authoritative instruction set; `shp_transport_observations.md` is explicitly non-authoritative.
- No authoritative specification for how Driver Log Level commands are transported or formatted beyond the boundary rules in `mission.md` and `invariants.md`.
- No authoritative schema for `.APEX` or spreadsheet parsing beyond explicit-entity rules, leaving extraction shapes underspecified for implementation and testing.
- No authoritative definition of processed log export format beyond “exactly as displayed,” leaving PDF structure and metadata requirements ambiguous.

5. INSTRUCTION-LEVEL RISKS
- Current transport and parsing paths violate stream integrity and no-inference invariants, making captured data invalid under `invariants.md` and unprovable under `testing_strategy.md`.
- Lack of required test layers prevents proof of determinism, traceability, and honesty, violating `testing_strategy.md` and undermining `mission.md` success criteria.
- Use of undocumented endpoints and message schemas risks dishonest output and untraceable mappings, violating `data_contracts.md` and `invariants.md`.
