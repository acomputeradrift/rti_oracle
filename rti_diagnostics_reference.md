# RTI Smart Home Processor — Diagnostics API Reference
Comprehensive reference for the HTTP + WebSocket diagnostics interfaces.  
Generated from live probe analysis.

---

## 1. Device Overview
The RTI Smart Home Processor (SHP) exposes two separate diagnostic interfaces:

### A. HTTP REST Interface (Port 5000)
Used for:
- Driver metadata
- Zigbee network info
- Flags
- RTiPanel channels

### B. WebSocket Diagnostics Interface (Port 1234)
Used for:
- Real-time event logs
- Log level control
- UPnP events
- Flag updates

The WebSocket interface is the same mechanism used by RTI TraceViewer.

---

# 2. Network Ports (Confirmed)

| Port | Protocol | Description |
|------|----------|-------------|
| **80** | HTTP | Diagnostics web interface (UI only) |
| **5000** | HTTP | All REST `/diagnostics/data/*` endpoints |
| **1234** | WebSocket | Main diagnostics stream (`/diagnosticswss`) |
| **50001** | TCP | Auxiliary system service (used internally) |
| **1900** | UDP | UPnP SSDP multicast |

---

# 3. HTTP Diagnostics API (Port 5000)

These endpoints follow:
```
http://<IP>:5000/diagnostics/data/<endpoint>
```

### ✅ Existing REST Endpoints (working)

| Endpoint | Status | Description |
|----------|--------|-------------|
| `/drivers` | 200 | Driver metadata, threads, CPU, memory, GUIDs |
| `/rtipanel` | 200 | Channels connected from RTiPanel apps |
| `/zigbee` | 200 | Zigbee network stats + device list |
| `/flags` | 200 | Full list of all user/system flags |

### ❌ Non-existent REST endpoints (404)

| Endpoint | Why Missing |
|----------|-------------|
| `/dashboard` | No implementation on SHP |
| `/variables` | NOT available via HTTP nor WS |
| `/systemlog` | Not implemented |

---

# 4. WebSocket Diagnostics API (Port 1234)

WebSocket URL:
```
ws://<IP>:1234/diagnosticswss
Origin: http://<IP>
```

### WebSocket supports message types in JSON format:
```
{
  "type": "<Command>",
  "resource": "<Resource>",
  "value": <Value>
}
```

---

## 4.1 Confirmed WebSocket Resources (exist)

| Resource | Purpose |
|----------|---------|
| **MessageLog** | Real-time logs (button presses, driver commands, macros) |
| **LogLevel** | Read/change subsystem logging levels |
| **UPnP** | UPnP event stream |
| **Flags** | Live updates to flag values |

### Valid subscription command:
```
{
  "type": "Subscribe",
  "resource": "MessageLog",
  "value": "true"
}
```

---

## 4.2 Non-existent WebSocket Resources

These return:
```
"error": "Attempted to Subscribe to a resource that does not exist"
```

| Resource | Notes |
|----------|-------|
| Variables | No WS + no HTTP = Currently inaccessible |
| Drivers | WS does not expose driver metadata (HTTP only) |
| RTiPanel | WS does not expose panel info (HTTP only) |
| Zigbee | WS does not expose Zigbee info (HTTP only) |
| SystemLog | Not implemented |

---

# 5. LogLevel Subsystem Identifiers

From probe results:

| Subsystem | Meaning |
|-----------|---------|
| `DRIVER//0` | Driver engine 0 |
| `DRIVER//1` | Driver engine 1 |
| `DRIVER//2` | Driver engine 2 |
| `EVENTS_DRIVER` | Driver event handling |
| `EVENTS_INPUT` | Button/input events |
| `EVENTS_SCHEDULED` | Scheduled events |
| `EVENTS_PERIODIC` | Periodic events |
| `EVENTS_SENSE` | Sense inputs |
| `DEVICES_RTIPANEL` | RTiPanel subsystem |
| `DEVICES_EXPANSION` | Expansion modules |
| `USER_GENERAL` | General user-level events |

These correspond to log filters shown in the web UI.

---

# 6. Real-Time Log Format (MessageLog)

Example:
```json
{
  "messageType": "MessageLog",
  "id": 243,
  "category": 29,
  "priority": 7,
  "depth": 2,
  "groupId": 228,
  "flags": 0,
  "text": "Driver - Command:'Layer Switch v2.x\\Ex. Set selected Layer\\Ex. Group: DISPLAY - Popups SOURCES(Viewport [SHOW])' Sustain:NO",
  "time": "09:27:10.484"
}
```

This structure is stable across all probes.

---

# 7. Driver Metadata (HTTP `/drivers`)

Fields include:
- name  
- guid  
- id  
- ticks  
- cpu_percent  
- cpu_average  
- thread_id  
- memory_peak  
- memory_allocated  
- port_list  

Example port list:
```
"MCAST:1900 TCPSRV:50001 HTTPSVR:80 HTTPSVR:5000 HTTPSVR:1234"
```

---

# 8. UPnP (WebSocket)

UPnP does not appear over HTTP.  
Subscription succeeds:

```json
{ "type": "Subscribe", "resource": "UPnP", "value": "true" }
```

Meaning:
- UPnP exists
- It may only produce events when devices join/leave
- The processor listens on port 1900 (confirmed)

---

# 9. Variables (Missing)

### Current state:
- Not available via HTTP
- Not available via WebSocket
- May require:
  - A hidden endpoint
  - A different namespace
  - A device setting
  - An Integration Designer RPC call

### NEXT STEPS:
We will discover variables via:
1. **Full traffic capture of ID11 while accessing variable editor**  
2. **WS opcode inspection**  
3. **Brute-force probing of `/diagnostics/data/*`**  
4. **Reverse-engineering UPnP state requests**

---

# 10. Complete API Overview (Final)

### HTTP:
- drivers
- rtipanel
- zigbee
- flags

### WebSocket:
- MessageLog
- LogLevel
- UPnP
- Flags

### Unknown / Undocumented:
- Variables (most important missing piece)

---

# 11. Next Action Items

1. Capture traffic while ID11 queries variables  
2. Build WebSocket schema extractor  
3. Attempt brute-force discovery of hidden HTTP endpoints  
4. Attempt probe with alternative WebSocket namespaces:
   - `"Vars"`
   - `"Variable"`
   - `"UserVariables"`
   - `"SystemVariables"`
   - `"ProjectVariables"`
   - `"VarState"`
   - `"STATE"`
   - `"DATA"`

5. Map driver IDs to driver names (via `/drivers`)

---

# END OF DOCUMENT
