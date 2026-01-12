# RTI Diagnostics & WebSocket Reverse-Engineering Reference  
Comprehensive Technical Mapping for Building a Windows Diagnostics Application  
Updated with all discoveries to date

---

## 1. Overview

This document captures everything we have reverse-engineered from the RTI Smart Home Processor (SHP) diagnostics interfaces, including:

- HTTP diagnostics API (`/diagnostics/data/...`)
- WebSocket diagnostics stream (`/diagnosticswss`)
- Supported/unsupported resource subscriptions
- Driver IDs, variables, flags, sysvars
- TraceViewer & Integration Designer (ID11) behavior
- Port usage and traffic patterns
- Areas still requiring reverse-engineering

This document is structured for AI ingestion.

---

## 2. Processor Network Behavior

### Open Ports (confirmed)

| Port | Purpose | Notes |
|------|---------|-------|
| 80 | Diagnostics HTTP server | Hosts `/diagnostics#/` UI & JSON |
| 1234 | Diagnostics WebSocket | Main live log stream |
| 5000 | Auxiliary diagnostics HTTP | `/drivers`, `/flags`, `/zigbee`, etc |
| 50001 | Unknown | Possibly UPnP or internal service |
| 1900 | UPnP multicast | Appears inside driver port_list |

---

## 3. HTTP Diagnostics Endpoints

All under:

```
/diagnostics/data/<endpoint>
```

### Endpoint status (confirmed)

| Endpoint | Status | Description |
|----------|--------|-------------|
| dashboard | 404 | UI only, no JSON |
| drivers | 200 | Driver list + GUIDs |
| zigbee | 200 | Mesh stats + addresses |
| rtipanel | 200 | Panel channels (often empty) |
| upnp | 200 (error) | WS-only source |
| flags | 200 | Large state list |
| variables | 404 | No HTTP variable API |
| systemlog | 404 | Not exposed |
| sysvars | Binary | Variables stored in binary |

---

## 4. WebSocket Diagnostics (port 1234)

### Connection URL:
```
ws://<IP>:1234/diagnosticswss
```

### Startup messages:
1. Welcome echo  
2. LogLevels dump  
3. Echo responses to Subscribe commands  

---

## 5. Valid WebSocket Resources

| Resource | Accepts Subscribe | Notes |
|----------|------------------|-------|
| MessageLog | Yes | All logs come through here |
| LogLevel | Yes | Also accepts per-driver config |
| Sysvar | Yes | **Variables = Sysvars** |
| Flags | Yes | Mirrors HTTP flaglist |
| UPnP | Partial | Accepts subscribe, rarely returns |
| Variables | No | Not a WS resource |
| Driver | No | Subscription rejected |

---

## 6. Sysvar Discovery (Major Finding)

Sysvars **are** the project variables.

Example captured:

```
{"messageType":"Sysvar","sysvarid":327,"sysvarval":"true"}
```

This corresponds to a project variable you toggled in Integration Designer.

### Sysvar ID behavior
- `"null"` = undefined
- `"0"` / `"false"` = false
- `"1"` / `"true"` = true
- IDs go into the hundreds depending on project

### Sysvars storage
The HTTP endpoint returns BINARY:

```
/diagnostics/data/sysvars → sysvars.bin
```

This is likely a compiled structure containing all project variable names, IDs, types.

Reverse-engineering required.

---

## 7. Drivers Mapping

HTTP: driverlist shows:

```
id 0 : LS - DISPLAY
id 1 : Weather
id 2 : System Manager
id 4 : Diagnostics: Primary Processor
```

These match WebSocket dName entries:

```
DRIVER//0
DRIVER//1
DRIVER//2
```

Mapping:

| WS Name | Driver ID | Driver Name |
|---------|-----------|-------------|
| DRIVER//0 | 0 | LS - DISPLAY |
| DRIVER//1 | 1 | Weather |
| DRIVER//2 | 2 | System Manager |

---

## 8. Flags

Example:

```
flaglist: [{id:1, name:"Unnamed", state:0}, ...]
```

Notes:
- Many flags have no names
- Likely internal to RTI OS
- Not directly useful for project logic
- Still subscribable via WS

---

## 9. Project Variable Name Problem

We must obtain:  
**Sysvar ID → Variable Name**

The SHP does not reveal names via diagnostics.

### Two possible extraction paths

#### A. Decode sysvars.bin
- Appears to contain sequential sysvar records
- Likely encrypted or compressed

#### B. Parse .APEX file
- Should contain variable definitions
- May be ZIP, XML, SQLite, or custom binary

This is necessary for the “Processed View” of your future application.

---

## 10. Recommended Windows Application Architecture

### A. Live Data Capture
Use WebSocket connection:

- Subscribe to:
  - MessageLog
  - Sysvar
  - Flags
  - LogLevel

### B. Static Data Capture
Use HTTP:

- `/diagnostics/data/drivers`
- `/diagnostics/data/flags`
- `/diagnostics/data/zigbee`

### C. Raw View
Displays JSON as-is.

### D. Processed View
Cloud or local mapping engine converts logs into human language, e.g.:

> “At 12:34 PM, Kitchen Island Recessed set to 76% (CBUS Load 123).”

---

## 11. Outstanding Reverse-Engineering Tasks

| Task | Status |
|------|--------|
| Decode sysvars.bin | Not done |
| Reverse-engineer .apex | Not done |
| Map all variable names | Pending binary work |
| Confirm WS-only UPnP | Partial |
| Detect all valid WS resources | Nearly complete |
| Build Windows app | In progress |

---

## 12. JSON Message Types (known)

| Type | Meaning |
|------|---------|
| MessageLog | All button, macro, driver logs |
| LogLevels | List of current log levels |
| Sysvar | Variable ID/value updates |
| Flags | Diagnostic flags |
| echo | Server confirming your actions |
| error | Subscription or formatting errors |

---

## 13. WebSocket Command Reference

### Subscribe to MessageLog:
```
{"type":"Subscribe","resource":"MessageLog","value":"true"}
```

### Subscribe to Sysvar ID 327:
```
{"type":"Subscribe","resource":"Sysvar","value":{"id":327,"status":true}}
```

### Change log level:
```
{"type":"Subscribe","resource":"LogLevel","value":{"type":"EVENTS_INPUT","level":"3"}}
```

---

## 14. Variable Behavior Summary

- Variables do NOT come via HTTP.
- Variables ONLY generate WS Sysvar messages.
- Sysvar IDs are not exposed with names.
- Names exist only in IDE or internal storage.

---

## 15. Current Knowledge Summary (AI Ingest)

- Diagnostics architecture: HTTP static + WS dynamic
- WS supports minimal resources: MessageLog, LogLevel, Flags, Sysvar
- Variables = Sysvars
- Project variable names NOT exposed over diagnostics
- Names must come from `.apex` or binary decoding
- Logs contain enough metadata for complete project behavior mapping

---

# END OF FILE
