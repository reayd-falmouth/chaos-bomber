# Plan J — Death sprite ends on blank / idle-none

## Goal

After the **death** animation for the hybrid/billboard player, the renderer shows **blank** or **idle-none** (no looping death clip forever).

## Primary files

- [Assets/Scripts/MasterBlaster/Runtime/Player/PlayerDualModeController.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Player/PlayerDualModeController.cs)
- [Assets/Scripts/MasterBlaster/Runtime/Player/AnimatedSpriteRenderer.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Player/AnimatedSpriteRenderer.cs)

## Tests (write first)

- **Assembly:** `Scenes.Tests`.
- **Approach:** Unit-test `AnimatedSpriteRenderer` (or extracted runner): `StartAnimation(loop:false)` → after last frame callback sets `sprite=null` or `idle` sentinel; no `NextFrame` after complete.
- **Case:** `loop=true` unchanged for walk cycles; death path forces one-shot end state.

## Implementation notes

- On complete: disable renderer, clear sprite, or switch to explicit empty frame.
- Align with respawn / destroy lifecycle in `GameManager`.

## Verify

- Unity MCP: compile, run tests, manual death.

## Commit

- Example: `fix(masterblaster): death sprite ends on blank frame`
