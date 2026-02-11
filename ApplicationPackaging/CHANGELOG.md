# Changelog

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
