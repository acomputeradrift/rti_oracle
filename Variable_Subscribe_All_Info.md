# Variable Subscribe - All Info (Session Notes)

## Scope
- Notes from this session only.
- Evidence is based on local captures and direct HTTP response.
- No external documentation or assumptions.

## Captures Reviewed
- `Subscribe Dump.pcapng`
- `Variable Dump.pcapng`
- `system info dump.pcapng`

## WebSocket Diagnostics (Port 1234)
- WebSocket URL: `ws://<SHP_IP>:1234/diagnosticswss`
- Observed server greeting:
  - `{"messageType":"echo","message":"Welcome to the RTI Diagnostics Websocket server!"}`
- Observed subscribe command patterns:
  - `{"type":"Subscribe","resource":"Sysvar","value":{"id":265,"status":true}}`
  - `{"type":"Subscribe","resource":"Sysvar","value":{"id":265,"status":false}}`
  - `{"type":"Subscribe","resource":"SysvarPers"}`
- Observed sysvar update message:
  - `{"messageType":"Sysvar","sysvarid":265,"sysvarval":"null"}`
- Observed persistent sysvar list:
  - `{"messageType":"SysvarPers","sysvars":[690,697,417,4204,5365,4171,439,460,482,604,327,265,266,267,268,269]}`

## HTTP Endpoints (Port 5000)
### Sysvar list
- `GET http://<SHP_IP>:5000/diagnostics/data/sysvars`
- Response headers:
  - `Content-Type: text/javascript`
  - `Content-Length: 3138`
- Response body is GZip-compressed JSON.
- Decoded JSON shape (partial):
  - Top-level: `{"Drivers":[ ... ]}`
  - Driver fields:
    - `Driver Name`
    - `Driver Base Name`
    - `Driver ID`
    - `Driver Variables` (list)
  - Variable fields:
    - `Name`
    - `ID`
    - `Type`
- Example (partial):
  - `{"Driver Name":"Audio Matrix","Driver Base Name":"RTI Virtual Multiroom Amp","Driver ID":0,"Driver Variables":[{"Name":"Room One\\Source In Use","ID":260,"Type":1}, ...]}`

### System status
- `GET http://<SHP_IP>:5000/diagnostics/data/system_status`
- Response is JSON and includes:
  - `memory_free` (string like "19.6 MB")
  - `memory_load` (number)
  - `sysvar_load` (string or number)
  - `uptime` (number)
  - `memory_history` (array of `{ memory_free, memory_load, timestamp }`)
  - Other metadata (name, firmware, ip, mac, file_name, etc.)

## Behavioral Findings
- Web UI uses per-variable subscribe/unsubscribe messages with `resource:"Sysvar"` and `value:{ id, status }`.
- No "subscribe all" message was observed in the captures.
- WebSocket provides updates only; full variable list with names and types is from HTTP sysvars endpoint.
- `SysvarPers` provides a list of sysvar IDs, likely persistent only.

## Planned Probe Behavior (Agreed)
- Fetch full sysvar list from `/diagnostics/data/sysvars`.
- Decompress GZip and parse JSON into driver + variable metadata.
- Menu-driven multiselect of drivers.
- Toggle on/off for all variables of selected drivers by sending per-ID subscribe messages.
- Poll `/diagnostics/data/system_status` for memory and sysvar load while subscriptions are active.

## Open Questions
- Confirm whether `sysvar_load` is numeric or string across devices/firmware.
- Confirm if `SysvarPers` includes all variables or only persistent ones.
- Determine safe max subscription count based on processor load results.

