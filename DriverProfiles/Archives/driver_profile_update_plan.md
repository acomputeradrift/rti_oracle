# Driver Profile Update Plan - C-Bus and Vaux Mapping

## Scope
This plan covers:
- Driver profile parsing and mapping rules for C-Bus and Vaux logs.
- Output formatting rules for resolved and unresolved mappings.

This plan does not modify any code. It documents intended behavior only.

## Inputs
- Project Data module lookup maps from Additional Info upload.
- Raw log lines containing C-Bus and Vaux commands/events.
- DriverProfile schema definitions that specify Additional Info extraction per driver.

## Driver Device Name Match Rules
- Sheet tab name must match the driver device name from the .apex file (not display name).
- Match is exact; no aliases or partial matches.
- If no match, the sheet is ignored and an error is recorded.

## C-Bus Log Handling
Each section below is based on a distinct C-Bus command or event example found in the logs.

### General - Immediate Switch
Example:
- Driver - Command:'Clipsal C-Bus\General\Immediate Switch(255, 22, 48)' Sustain:NO  Sent to 'WorkShop Slave'

Parsing rule:
- Tuple is (State, GroupID, AppId).

Mapping rule:
- Lookup (AppId, GroupID) in the Clipsal C-Bus sheet.
- Always include GroupRoom and GroupName in output.
- State mapping:
  - 1 -> Off
  - Other states -> render as "set to {state} (Unknown State)" until defined

Output format example:
- Driver Command [Clipsal C-Bus]: 'Garage Motion Sensor set to 121 (Unknown State)' Sustain:NO Sent to 'WorkShop Slave'

Needs decision:
- Define the meaning of states 121 and 255.

### General - Ramp to level
Example:
- Driver - Command:'Clipsal C-Bus\General\Ramp to level(2, 4, 6, 202)' Sustain:YES Rate:400

Parsing rule:
- Parameters are not yet defined.

Mapping rule:
- Requires analysis to determine how AppId, GroupID, and level are encoded.

Output format:
- Placeholder until parameter meaning is confirmed.

Needs decision:
- Provide parameter definitions for Ramp to level to map into AppId/GroupID and level.

### HVAC - HVAC Zone Setpoint Up
Example:
- Driver - Command:'Clipsal C-Bus\HVAC\HVAC Zone Setpoint Up(1, Unswitched (0))' Sustain:NO  Sent to 'WorkShop Slave'

Parsing rule:
- Tuple is (GroupID, ZoneId).

Mapping rule:
- Lookup (GroupID, ZoneId) in the Clipsal C-Bus HVAC sheet.
- Always include GroupName and ZoneName in output.

Output format example:
- Driver Command [Clipsal C-Bus HVAC]: 'GroupName ZoneName setpoint up' Sustain:NO Sent to 'WorkShop Slave'

Needs decision:
- Confirm desired phrasing for HVAC command outputs.

### Driver Event - App/Group Off
Example:
- Driver event 'When 'App 56, Group 4 Off' happens on 'Clipsal C-Bus\App 56, Group Off''

Parsing rule:
- Extract AppId, GroupID, and state from the event text.

Mapping rule:
- Lookup (AppId, GroupID) in the Clipsal C-Bus sheet.
- Always include GroupRoom and GroupName in output.
- State mapping:
  - Off -> Off
  - On -> On

Output format example:
- Driver Event [Clipsal C-Bus]: 'Garage Motion Sensor set to Off'

### Driver Event - App/Group On
Example:
- Driver event 'When 'App 56, Group 32 On' happens on 'Clipsal C-Bus\App 56 Group On''

Parsing rule:
- Extract AppId, GroupID, and state from the event text.

Mapping rule:
- Lookup (AppId, GroupID) in the Clipsal C-Bus sheet.
- Always include GroupRoom and GroupName in output.
- State mapping:
  - On -> On
  - Off -> Off

Output format example:
- Driver Event [Clipsal C-Bus]: 'Kitchen Recessed set to On'

## Vaux Lattice Matrix Log Handling
Examples found in logs (spelled as "Vaux Lattis Matrix" in the raw text):
- Driver - Command:'Vaux Lattis Matrix\Output Settings\Source Select(Route All, 13, 1)' Sustain:NO
- Driver - Command:'Vaux Lattis Matrix\Output Settings\Volume Up(13)' Sustain:YES Rate:200
- Driver - Command:'Vaux Lattis Matrix\Output Settings\Output Mute(Toggle, 13)' Sustain:NO
- Driver - Command:'Vaux Lattis Matrix\Output Settings\Output Off(13)' Sustain:NO

Mapping plan:
- Sheet tab name MUST match the driver device name from the .apex file or it is ignored.
- The DriverProfile defines the schema for Additional Info extraction.
- Input Index and Output Index should resolve to names where available.
- Example inference from current sheet:
  - Output 13 -> "Gym"
  - Input 1 -> "Shaw 1"

Placeholders (needs analysis):
- Source Select: determine how parameters map to input index, output index, and routing scope.
- Volume Up: confirm whether the parameter is an output index or another identifier.
- Output Mute: determine how Toggle and output index should be rendered.
- Output Off: determine how output index should map to output name.

## Open Items
- Define meanings for Immediate Switch state values 121 and 255.
- Define parameter semantics for Ramp to level.
- Confirm preferred output phrasing for HVAC commands.
- Define parameter meanings for Vaux Lattice Matrix commands (Source Select, Volume Up, Output Mute, Output Off).

## Scope Confirmation
- No code changes made.
- No source documents modified.
- Plan limited to driver profile parsing and mapping behavior.
