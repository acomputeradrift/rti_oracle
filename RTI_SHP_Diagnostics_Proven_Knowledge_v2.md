# RTI Smart Home Processor (SHP)
## Diagnostics Interfaces — Proven Knowledge Reference v2
## HTTP + WebSocket (Merged, Non-Truncated)

This document is a **single source of proven knowledge** derived by merging:
- RTI Diagnostics & WebSocket Reverse‑Engineering Reference
- RTI Smart Home Processor — Diagnostics API Reference

⚠️ Rules for this document:
- ONLY directly observed, probed, or verified behavior is included
- NO speculation, guesses, or inferred behavior
- Anything unproven is excluded here and belongs in a separate TODO file
- Structure is optimized for **AI ingestion and automated tooling**

---

## 1. Overview

The RTI Smart Home Processor (SHP) exposes a diagnostics subsystem used by:
- RTI Diagnostics Web UI
- RTI TraceViewer
- Integration Designer (partially)

This subsystem consists of:
- An HTTP diagnostics API (static data)
- A WebSocket diagnostics stream (real‑time events)

These interfaces are sufficient to reconstruct **nearly all runtime behavior**
of an RTI project, except for **project variable names**, which are intentionally hidden.

---

## 2. Processor Network Behavior

### 2.1 Confirmed Open Ports

| Port | Protocol | Purpose | Notes |
|----:|---------|--------|------|
| 80 | HTTP | Diagnostics Web UI | HTML only |
| 5000 | HTTP | Diagnostics REST API | `/diagnostics/data/*` |
| 1234 | WebSocket | Live diagnostics stream | `/diagnosticswss` |
| 50001 | TCP | Unknown internal service | Seen in driver port lists |
| 1900 | UDP | UPnP SSDP multicast | Appears in driver port lists |

---

## 3. HTTP Diagnostics API (Port 5000)

### 3.1 Base Path

```
http://<SHP_IP>:5000/diagnostics/data/<endpoint>
```

### 3.2 Working Endpoints (Confirmed)

| Endpoint | Status | Description |
|---------|--------|------------|
| `/drivers` | 200 | Driver metadata, CPU, memory, GUIDs |
| `/flags` | 200 | Full list of flags and states |
| `/zigbee` | 200 | Zigbee mesh stats and devices |
| `/rtipanel` | 200 | Connected RTiPanel channels |

### 3.3 Non‑Existent Endpoints (Confirmed 404)

| Endpoint |
|---------|
| `/dashboard` |
| `/variables` |
| `/systemlog` |

There is **no HTTP API** for project variables.

---

## 4. Driver Metadata (HTTP `/drivers`)

Returned fields include:
- id
- name
- guid
- ticks
- cpu_percent
- cpu_average
- thread_id
- memory_peak
- memory_allocated
- port_list

Example port_list values:
```
MCAST:1900 TCPSRV:50001 HTTPSVR:80 HTTPSVR:5000 HTTPSVR:1234
```

---

## 5. WebSocket Diagnostics API (Port 1234)

### 5.1 Connection

```
ws://<SHP_IP>:1234/diagnosticswss
Origin: http://<SHP_IP>
```

### 5.2 Message Envelope

```json
{
  "type": "<Command>",
  "resource": "<Resource>",
  "value": <Value>
}
```

---

## 6. WebSocket Startup Behavior

Upon connection:
1. Welcome / echo message
2. LogLevels dump
3. Echo responses to Subscribe commands

This matches TraceViewer behavior.

---

## 7. Valid WebSocket Resources (Confirmed)

| Resource | Subscribable | Notes |
|---------|-------------|------|
| MessageLog | Yes | Primary event stream |
| LogLevel | Yes | Read & set log verbosity |
| Sysvar | Yes | Project variables |
| Flags | Yes | Live flag updates |
| UPnP | Partial | Rarely emits events |

### 7.1 Invalid / Non‑Existent Resources

Subscribing produces an error:

| Resource |
|---------|
| Variables |
| Driver |
| Drivers |
| Zigbee |
| RTiPanel |
| SystemLog |

---

## 8. MessageLog (Core Runtime Stream)

### 8.1 Subscription

```json
{ "type":"Subscribe","resource":"MessageLog","value":"true" }
```

### 8.2 Message Structure (Stable)

```json
{
  "messageType": "MessageLog",
  "id": 243,
  "category": 29,
  "priority": 7,
  "depth": 2,
  "groupId": 228,
  "flags": 0,
  "text": "Driver - Command:'Layer Switch...' Sustain:NO",
  "time": "09:27:10.484"
}
```

MessageLog includes:
- Button presses
- Driver commands
- Macros
- Scheduled events
- System actions

---

## 9. LogLevel Subsystems

Observed identifiers:

| Subsystem |
|----------|
| DRIVER//0 |
| DRIVER//1 |
| DRIVER//2 |
| EVENTS_DRIVER |
| EVENTS_INPUT |
| EVENTS_SCHEDULED |
| EVENTS_PERIODIC |
| EVENTS_SENSE |
| DEVICES_RTIPANEL |
| DEVICES_EXPANSION |
| USER_GENERAL |

### Set Log Level Example

```json
{
  "type":"Subscribe",
  "resource":"LogLevel",
  "value":{"type":"EVENTS_INPUT","level":"3"}
}
```

---

## 10. Sysvars = Project Variables (Major Proven Finding)

Project variables are exposed **only** as Sysvars via WebSocket.

### 10.1 Example Sysvar Event

```json
{
  "messageType":"Sysvar",
  "sysvarid":327,
  "sysvarval":"true"
}
```

### 10.2 Value Semantics

| Value | Meaning |
|------|--------|
| "null" | Undefined |
| "0" / "false" | False |
| "1" / "true" | True |

Sysvar IDs may range into the hundreds depending on project size.

---

## 11. Sysvar Storage (Binary)

### Endpoint

```
/diagnostics/data/sysvars
```

Behavior:
- Returns binary (`sysvars.bin`)
- Contains all project variable metadata
- Variable names are **not exposed anywhere else**

Mapping required:
```
Sysvar ID → Variable Name
```

---

## 12. Drivers: HTTP ↔ WebSocket Mapping

### HTTP Driver IDs

| ID | Name |
|---:|------|
| 0 | LS - DISPLAY |
| 1 | Weather |
| 2 | System Manager |
| 4 | Diagnostics: Primary Processor |

### WebSocket Identifiers

```
DRIVER//0
DRIVER//1
DRIVER//2
```

IDs align directly.

---

## 13. Flags

### HTTP
```
/diagnostics/data/flags
```

### WebSocket
```json
{ "type":"Subscribe","resource":"Flags","value":"true" }
```

Notes:
- Many flags unnamed
- Likely OS‑internal
- Fully extractable but limited project value

---

## 14. UPnP

- Not available via HTTP
- Subscribable via WebSocket
- Emits events only on network changes
- Processor listens on UDP 1900

---

## 15. What Is Explicitly NOT Available

| Data | Availability |
|-----|-------------|
| Variable names | ❌ |
| Variable list via HTTP | ❌ |
| Variable metadata via WS | ❌ |
| Driver metadata via WS | ❌ |
| Full system log | ❌ |

---

## 16. Recommended AI Extraction Architecture

### Live (WebSocket)
- MessageLog
- Sysvar
- Flags
- LogLevel

### Static (HTTP)
- drivers
- flags
- zigbee
- rtipanel
- sysvars (binary)

---

## 17. Final Proven Summary

- HTTP = static metadata
- WebSocket = runtime truth
- Variables = Sysvars
- Names hidden in binary / project files
- Logs are sufficient to reconstruct system behavior

---

# END OF FILE
