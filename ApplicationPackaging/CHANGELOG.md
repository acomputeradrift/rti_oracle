# Changelog

## [1.1.5] - 2026-02-19

### Changed
- Mapping start separator now writes as a plain line (`----- MAPPING START ... -----`) without structured log prefix fields.
- Apex mapping success message now prefixes source transitions (`Source <from> -> <to>`).

### Fixed
- Driver mapping transition extraction now handles nested command arguments correctly, preventing malformed mapped transition details.
- Log level ack resolution now handles `Diagnostics: Primary Processor` and `DRIVER//N` alias pairing to avoid false baseline ack timeouts.
- Reconnect/status logging is now UI-thread safe, preventing cross-thread dispatcher exceptions during reconnect attempts.

## [1.1.4] - 2026-02-16

### Added
- Status area now reports initial driver baseline reception and updated status counts.
- Missing driver name warnings are surfaced in the status area.

### Changed
- Connection flow now waits for the first LogLevels baseline before forcing protected log levels.
- Driver log level presets never touch the protected diagnostics drivers.
- Restricted diagnostics drivers are removed from the driver log level UI list.

### Fixed
- LogLevels baseline parsing handles prefixed payloads from the websocket feed.
- Websocket echo and LogLevels summary noise suppressed from raw log display.
- Blocking error popups replaced with status area reporting.

## [1.1.3] - 2026-02-13

### Added
- On connect, force the Diagnostics driver log level to 3 to ensure MessageLog acks are emitted.

### Fixed
- Driver log level status now updates from MessageLog ack lines in addition to LogLevels snapshots.

## [1.1.2] - 2026-02-13

### Fixed
- Improved startup rendering compatibility for remote desktop sessions by forcing software rendering in-app.

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
- Toggle wording normalized to action form (`toggled`) rather than state.
- System Manager formatting expanded:
  - `Set Layer Visibility(...)`
  - `[Hide]\\System Off`
- `RTI Internal` hidden from Driver Profiles UI list.

### Fixed
- Preserved `[No Format!]` behavior for System Manager route/room-off patterns.
- Unresolved mapped driver commands now use `[No Map!]`.
- Command parser now handles nested parentheses correctly (e.g. `CEC (Hex)(...)`).
- System Manager source index resolution now supports source-number offset behavior from project data.
