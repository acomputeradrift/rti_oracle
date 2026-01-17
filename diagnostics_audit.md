# Diagnostics Audit

## ✅ Kept

- Manual IP entry with explicit connect and disconnect controls.
- SSDP discovery flow with optional subnet scan fallback.
- UI-driven driver log level toggles and level buttons.
- Raw and processed log windows remain side by side without UI changes.
- Diagnostics transport now accessed through `SHPDiagnosticsViewer/DiagnosticsTransport/IDiagnosticsTransport.cs` with legacy behavior preserved.

## ⚠️ Reusable but requires modification

- SSDP discovery and subnet scan logic can remain, but verification currently depends on `http://{ip}:5000/diagnostics/data/drivers`, which is not defined in the traceviewer transport specification.
- Driver list parsing assumes a web diagnostics JSON payload and must be replaced with a traceviewer-aligned source when defined.

## ❌ Invalid under the current diagnostics model

- WebSocket connection to `ws://{ip}:1234/diagnosticswss` is incompatible with TCP port 2113 framed UTF-16LE transport.
- WebSocket subscriptions to `MessageLog`, `Sysvar`, and `LogLevel` assume unsupported resources.
- JSON `messageType` parsing assumes web diagnostics payloads; traceviewer transport delivers framed UTF-16LE text lines.
- Raw log fallthrough can emit raw JSON, violating the rule that raw logs never show JSON.
