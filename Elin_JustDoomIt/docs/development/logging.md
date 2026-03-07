# Logging

Last checked: 2026-03-07

## Purpose
- Keep `Player.log` useful for post-release support without spamming it.
- Record state transitions that explain why a DOOM mod entry can or cannot be launched.

## Required Events
- Log mod discovery start and finish.
- Log each discovered entry's resolved state.
- Log multi-WAD setup confirmation.
- Log `CONTINUE` mismatch decisions.
- Log startup-time config deletion when cleanup occurs.

## Required Fields
- Include these fields when they exist:
  - `entry_id`
  - resolved `state`
  - mismatch reason or GC action

## Style
- Use one concise line per event.
- Prefer stable machine-readable keys over prose-only messages.
- Do not log per-frame UI hover or selection noise.

## Example Shapes

```text
[JustDoomIt] mod_discovery_start root=wad/mods
[JustDoomIt] mod_entry_state entry_id=alien_vendetta state=ready-multi
[JustDoomIt] mod_setup_confirm entry_id=alien_vendetta wad_count=2
[JustDoomIt] continue_unavailable reason=selection_mismatch saved_iwad=freedoom2.wad saved_entry_id=alien_vendetta
[JustDoomIt] config_delete reason=startup_gc entry_id=alien_vendetta
```

## Scope
- This is feature-specific operational logging for the DOOM arcade flow.
- It does not define a global logging framework for unrelated systems.
