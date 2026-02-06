# Variable Subscribe Probe

Standalone command-line tool for RTI SHP sysvar subscription testing.

## Usage

```bash
# default IP: 192.168.1.143
VariableSubscribeProbe

# explicit IP + status poll interval (seconds)
VariableSubscribeProbe --ip 192.168.1.143 --interval 2
```

## Behavior
- Loads sysvar list from `http://<ip>:5000/diagnostics/data/sysvars` (GZip JSON).
- Connects to `ws://<ip>:1234/diagnosticswss` for live Sysvar updates.
- Polls `http://<ip>:5000/diagnostics/data/system_status` for memory/sysvar load.
- ASCII menu allows multiselect driver subscriptions (on/off/toggle).

## Notes
- Subscriptions are per-variable and mirror the web UI pattern:
  - `{"type":"Subscribe","resource":"Sysvar","value":{"id":<ID>,"status":true}}`
  - `{"type":"Subscribe","resource":"Sysvar","value":{"id":<ID>,"status":false}}`
