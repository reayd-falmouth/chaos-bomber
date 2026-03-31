# Plan K — FPS weapon on correct side (handedness)

## Goal

First-person **weapon socket** appears on the intended side (right/left). Stable across mode switches if applicable.

## Primary files

- [Assets/Scripts/FPS/Runtime/Gameplay/Managers/PlayerWeaponsManager.cs](mdc:Assets/Scripts/FPS/Runtime/Gameplay/Managers/PlayerWeaponsManager.cs)
- [Assets/Prefabs/FPS/Player.prefab](mdc:Assets/Prefabs/FPS/Player.prefab) (`WeaponParentSocket`, default/aim local positions)

## Tests (write first)

- **Assembly:** `fps.Tests.EditMode` if you add **handedness sign** to local position math; otherwise **no automated test** for authored transforms — add a **tiny test** only when code computes mirror: e.g. `ApplyHandedness(right, localPos)`.
- Document manual: weapon grip visible on correct side in play.

## Implementation notes

- Prefer **prefab authoring** fix if only X sign wrong; add optional `[SerializeField] bool useLeftHand` if product wants toggle.

## Verify

- Unity MCP: compile, in-editor check FPS view.

## Commit

- Example: `fix(fps): correct weapon socket handedness`
