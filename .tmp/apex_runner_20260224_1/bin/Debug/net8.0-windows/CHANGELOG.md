# Changelog

## [1.1.9] - 2026-02-23

### Added
- Reprocessing progress overlay now appears when applying new Additional Info to large log sets, with live progress updates.
- Tagged driver report export now groups unresolved output by tag type (for example, profile/mapping/state/format issues) and includes known-driver incomplete-profile cases.
- Driver profile coverage was expanded with new profile support and broader parsing for additional devices.

### Changed
- System Manager source-name resolution now uses a dedicated indexed source catalog, improving source naming accuracy in mapped output.
- Driver and event sentence wording was refined for clearer readability across additional device events and state transitions.
- Unresolved mapping markers are now consistently shown as `[Unresolved!]`.
- Existing driver profiles were updated with additional command/event mapping and driver-update handling.

### Fixed
- Project Data preview now accepts newly discovered drivers more reliably.
- Additional Info loading for `RTI RCM-12 Relay Module` was corrected.
- Project selection UI no longer shifts layout with long file names.
- Unnecessary loading behavior was removed when opening project data that is already available in memory.

## [1.1.8] - 2026-02-22

### Added
- New driver profiles for `RTI Diagnostics` and `Vantage InFusion`.
- No-profile driver report export now supports fallback save locations and can open the saved file location after export.
- RTI Internal mapping now supports relay-name lookups from Additional Info sheet data for `RTI RCM-12 Relay Module`.

### Changed
- RTI Internal mapping coverage was expanded for page-change logs, internal IR/relay commands, and internal lifecycle events.
- System Manager update lines are now normalized into `Driver Update (System Manager)` output.
- Driver event sentence formatting now uses consistent `When ...` phrasing, with clearer Vantage InFusion ON/OFF wording.

### Fixed
- Lines without a matched profile now retain the `[No Profile!]` marker while still allowing system-level mapping to proceed.

## [1.1.7] - 2026-02-20

### Changed
- Log level setting commands were updated to mirror web server behavior, making command handling significantly more robust.

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
