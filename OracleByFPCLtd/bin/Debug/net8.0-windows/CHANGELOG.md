# Changelog

## [1.1.6] - 2026-02-20

### Changed
- Hard Diagnostics startup flow is now more reliable: project diagnostics is established first, then system diagnostics is applied.
- If project diagnostics cannot be confirmed, system hard-level application is safely skipped.
- Status reporting during hard diagnostics startup is clearer and more actionable.

### Fixed
- Reduced false hard-diagnostics failures caused by delayed or inconsistent acknowledgement timing.

## [1.1.5] - 2026-02-19

### Changed
- Mapping start markers are now easier to read and scan in exported output.
- Mapping success messages now show clearer source-to-source transitions.

### Fixed
- Driver mapping now handles complex nested command patterns more reliably.
- Hard diagnostics confirmation is more stable when acknowledgements use alternate target naming.
- Reconnect status reporting is more reliable during repeated reconnect attempts.

## [1.1.4] - 2026-02-16

### Added
- Status area now reports initial driver snapshot reception and updated status counts.
- Missing driver name warnings are surfaced in the status area.

### Changed
- Connection flow now waits for the first log-level snapshot before applying protected diagnostics levels.
- Driver log level presets never touch the protected diagnostics drivers.
- Restricted diagnostics drivers are removed from the driver log level UI list.

### Fixed
- Initial log-level snapshot handling is more tolerant of message prefix variations.
- WebSocket echo and log-level summary noise are suppressed from raw log display.
- Blocking error popups replaced with status area reporting.

## [1.1.3] - 2026-02-13

### Added
- On connect, diagnostics output is automatically set to ensure acknowledgement events are emitted.

### Fixed
- Driver log level status tracking now relies on live acknowledgement events for better runtime accuracy.

## [1.1.2] - 2026-02-13

### Fixed
- Improved startup rendering compatibility for remote desktop sessions.

## [1.1.1] - 2026-02-13

### Changed
- Maintenance release with no new end-user features.

## [1.1] - 2026-02-11

### Added
- Unified human-readable driver command formatter with a shared template.
- New driver profiles:
  - `Layer Switch v2.x`
  - `Lutron Caseta / RA2 Select`
  - `Samsung Ex-Link`
  - `QMotion QzHub3`
  - `RTI VIP-UHD-CTRL`
  - `VHDx`
  - `Jandy iAquaLink`
- `Help -> Driver Profiles` list showing registered profiles.
- Driver profile "last updated" timestamps in Driver Profiles dialog.

### Changed
- Standardized command output format:
  - `<<log line>> [<<datetime>>] Driver Command (<<driver name>>): '<<resolved mapping>> <<action text>> <<state>.' <<extra info>>`
- Toggle wording is normalized to clearer action phrasing.
- System Manager output formatting expanded for better readability in common actions.
- `RTI Internal` hidden from Driver Profiles UI list.

### Fixed
- Preserved fallback formatting behavior for System Manager route and room-off patterns.
- Unresolved mapped driver commands now use a clear unresolved marker.
- Command parsing now handles nested parentheses correctly.
- System Manager source index resolution now handles source-number offset behavior more reliably.
