# Driver Log Status Sync Implementation

## Goal
Keep driver log-level status in sync in the UI using reliable WS signals, while preventing user actions that break observability.

## Confirmed Strategy
1. Connect to WS and capture the first `LogLevels` message.
2. Use that first `LogLevels` payload to initialize UI state.
3. Immediately enforce:
   - `Diagnostics: Primary Processor = 0`
   - `DRIVER//4 = 1`
4. Hide both controls from user-facing UI.
5. For user-initiated log-level changes, sync UI from `OnHTTPServerData() data.websocket = {...LogLevel...}` lines.

## Runtime Sync Signal
Use this line type as the UI status update signal after baseline:

`Diagnostics: Primary Processor - OnHTTPServerData() data.websocket = {"type":"Subscribe","resource":"LogLevel","value":{"type":"DRIVER","level":"1","driverId":"4"}}`

## UI State Model
Each control should track:
- `currentLevel` (last accepted level from matching `OnHTTPServerData` line)
- `pendingLevel` (requested but not yet seen in `OnHTTPServerData`)
- `status`:
  - `idle`
  - `requested`
  - `confirmed`
  - `unconfirmed` (timeout/no matching `OnHTTPServerData`)

Transition model:
1. User requests level `X`:
   - set `pendingLevel=X`
   - set `status=requested`
2. Receive matching `OnHTTPServerData() data.websocket = ...` for that target/level:
   - set `currentLevel=X`
   - clear `pendingLevel`
   - set `status=confirmed` then `idle`
3. Timeout without matching `OnHTTPServerData`:
   - keep `currentLevel` unchanged
   - keep/clear `pendingLevel` per UX choice
   - set `status=unconfirmed`

## Hidden/Protected Controls
Never expose these in normal user UI:
- `Diagnostics: Primary Processor`
- `DRIVER//4`

Controller behavior:
- Re-assert `Diagnostics: Primary Processor=0` and `DRIVER//4=1` on:
  - WS connect
  - reconnect
  - periodic health check (optional, low frequency)

## Output Filtering
For status sync logic, parse this diagnostics line:
- `OnHTTPServerData() data.websocket = ...`

Ignore for sync decisions:
- button/macro/driver runtime events
- general unrelated logs
- persistence read/write chatter (retain for diagnostics only, not status state)

## Reconnect Behavior
On reconnect:
1. Mark all controls temporarily `unknown` or `reloading`.
2. Wait for first `LogLevels` snapshot and rebuild baseline UI.
3. Re-apply forced pair (`Diagnostics=0`, `DRIVER//4=1`).
4. Resume normal ack-based status tracking.

## Timeouts and Retries (Suggested)
- Per-change confirmation timeout: 3-5 seconds.
- If no matching `OnHTTPServerData`: one retry max.
- If still no match: show `unconfirmed` and stop auto-retries.

## Probe Alignment
Existing probes should validate:
- first snapshot baseline capture works
- forced pair is applied
- `OnHTTPServerData` appears for targeted driver/category changes
- UI stays stable when unrelated MessageLog noise is present

## Known Constraint
Post-change `LogLevels` snapshots were not reliable in recent tests for reflecting writes.  
Therefore runtime sync should use `OnHTTPServerData` lines, not snapshots, after initial baseline.
