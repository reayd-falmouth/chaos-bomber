# Plan D — Billboard / animated sprites correct in FPS mode

## Goal

In **FPS mode**, character billboards / sprites **face the active gameplay camera** reliably (no `Camera.main` null/wrong during Cinemachine + `HybridCameraManager` transitions). Top-down mode remains correct.

## Primary files

- [Assets/Scripts/MasterBlaster/Runtime/Player/BillboardSprite.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Player/BillboardSprite.cs)
- [Assets/Scripts/MasterBlaster/Runtime/Camera/HybridCameraManager.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Camera/HybridCameraManager.cs)
- [Assets/Scripts/MasterBlaster/Runtime/Camera/CineMachineModeSwitcher.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Camera/CineMachineModeSwitcher.cs)
- [Assets/Scripts/MasterBlaster/Runtime/GameModeManager.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/GameModeManager.cs) (mode broadcast hook)

## Tests (write first)

- **Assembly:** `Scenes.Tests`.
- **Approach:** Prefer **PlayMode** minimal scene or **EditMode** tests on a helper `BillboardFacing.ResolveLookRotation(mode, cameraForward)` if sign/orientation logic is extracted.
- **Case:** when assigned camera reference is X, quaternion matches expected (regression for `-dir` vs `dir`).

## Implementation notes

- Inject camera from `HybridCameraManager` / mode switch event instead of `Camera.main` only.
- Ensure order: camera activated before billboard `LateUpdate`.

## Verify

- Unity MCP: compile, run tests, manual: toggle FPS ↔ top-down repeatedly.

## Commit

- Example: `fix(masterblaster): stabilize billboard facing in fps mode`
