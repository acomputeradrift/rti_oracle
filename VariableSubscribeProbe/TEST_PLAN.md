# VariableSubscribeProbe Test Plan

## Goals
- Verify sysvar list parsing (drivers + variables) from decoded JSON.
- Verify system status parsing (memory + sysvar load metrics).
- Verify subscribe/unsubscribe payload formatting.
- Verify multiselect parsing for driver menu.

## Tests
- `SysvarCatalogTests.ParseFromJsonExtractsDriversAndVariables`
  - Parses sample JSON into driver + variable records.
- `SystemStatusTests.ParseExtractsKeyMetrics`
  - Extracts memory_free, memory_load, sysvar_load, uptime, memory history.
- `SubscribePayloadTests.BuildSysvarTogglePayloadUsesExpectedShape`
  - Ensures payload matches capture shape for `Sysvar` subscribe/unsubscribe.
- `SelectionParserTests.ParseHandlesRangesAndSingles`
  - Ensures comma + range selection is parsed correctly.

## Out of Scope
- Live WebSocket integration tests (manual, requires hardware).
- Load/performance tests (manual).
