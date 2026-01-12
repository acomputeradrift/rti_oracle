# RTI Smart Home Processor (SHP)
## Diagnostics Interfaces — Proven Knowledge Reference (HTTP + WebSocket)

This document contains ONLY behavior that has been directly observed, probed,
or verified against live RTI Smart Home Processors.

It is structured for AI ingestion and automation.

---

## 1. Diagnostics Architecture (Confirmed)

Two independent planes:

- HTTP (static metadata)
- WebSocket (real-time stream)

---

## 2. Network Ports

| Port | Protocol | Purpose |
|-----:|----------|---------|
| 80 | HTTP | Diagnostics UI (HTML only) |
| 5000 | HTTP | /diagnostics/data REST |
| 1234 | WebSocket | Live diagnostics stream |
| 50001 | TCP | Internal service (unknown) |
| 1900 | UDP | UPnP SSDP |

---

## 3. HTTP API (Port 5000)

Base:
http://<IP>:5000/diagnostics/data/

### Working Endpoints

- /drivers
- /flags
- /zigbee
- /rtipanel

### Non-existent

- /variables
- /systemlog
- /dashboard

---

## 4. WebSocket API (Port 1234)

URL:
ws://<IP>:1234/diagnosticswss

Resources:

- MessageLog
- LogLevel
- Sysvar
- Flags
- UPnP

---

## 5. Sysvars = Project Variables

Variables are exposed ONLY as Sysvar messages via WebSocket.

Example:
{
  "messageType": "Sysvar",
  "sysvarid": 327,
  "sysvarval": "true"
}

Names are NOT exposed via diagnostics.

---

## 6. Binary Sysvar Store

Endpoint:
/diagnostics/data/sysvars

Returns binary sysvars.bin containing variable metadata.

---

## 7. Driver Mapping

HTTP IDs map directly to WS identifiers:

DRIVER//0
DRIVER//1
DRIVER//2

---

## 8. What Is Not Available

- Variable names
- Variable list via HTTP
- Driver metadata via WebSocket

---

END OF FILE
