# Plan L — FPS pickups match top-down / hybrid items

## Goal

Collecting items in **FPS mode** applies the **same gameplay effects** as **top-down / hybrid** `ItemPickup3D` (or explicitly **disables** hybrid pickups in FPS with clear UX — product default should be parity).

## Primary files

- [Assets/Scripts/MasterBlaster/Runtime/Arena/ItemPickup3D.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Arena/ItemPickup3D.cs)
- [Assets/Scripts/FPS/Runtime/Gameplay/Pickup.cs](mdc:Assets/Scripts/FPS/Runtime/Gameplay/Pickup.cs) and derived pickups
- [Assets/Scripts/MasterBlaster/Runtime/GameModeManager.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/GameModeManager.cs)
- [Assets/Scripts/MasterBlaster/Runtime/Player/PlayerDualModeController.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Player/PlayerDualModeController.cs) (colliders / tags per mode)

## Tests (write first)

- **Assembly:** `Scenes.Tests` + possibly `fps.Tests.EditMode`.
- **Approach:** Unit-test **gating**: `ItemPickup3D.ShouldApply(mode)`; unit-test effect routing mocks (e.g. Ghost activates on right component).
- **Cases:** FPS mode picks powerup → BombController3D / Ghost / health matches hybrid path; hybrid pickups ignored when FPS authoritative if that is design.

## Implementation notes

- Single **authority** per item type avoids double pickup.

## Verify

- Unity MCP: compile, run tests, manual: same item both modes.

## Commit

- Example: `fix(masterblaster): align fps pickup effects with item pickup 3d`
