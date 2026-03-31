# Plan I — Bombs block players after leaving placed cell

## Goal

**Classic Bomberman rule:** player can leave the cell they **placed** the bomb in while it is a trigger; once they **leave that cell**, bomb collider becomes solid and **blocks** movement (unless Ghost / intended exception). Fix cases where bombs never block, block too early, or wrong layer.

## Primary files

- [Assets/Scripts/MasterBlaster/Runtime/Bomb/BombPassThroughGrid3D.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Bomb/BombPassThroughGrid3D.cs)
- [Assets/Scripts/MasterBlaster/Runtime/Arena/ArenaGrid3D.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Arena/ArenaGrid3D.cs) (WorldToCell / Y handling)
- Bomb prefab: [Assets/Prefabs/MasterBlaster/Bomb3D.prefab](mdc:Assets/Prefabs/MasterBlaster/Bomb3D.prefab) (collider on tree, layer `Bomb3D`)

## Tests (write first)

- **Assembly:** `Scenes.Tests`.
- **Approach:** Unit-test grid math: `ShouldBeTrigger(placerCell, bombCell, playerCell)` without full physics if extracted; or PlayMode with stub grid.
- **Cases:** same cell → trigger; adjacent after leave → solid; y-offset does not false-trigger.

## Implementation notes

- Ensure `BoxCollider` reference matches prefab hierarchy (root vs child).
- Align placer position (feet) vs bomb transform with `bombYOffset`.

## Verify

- Unity MCP: compile, run tests, manual: drop bomb, walk off, verify blockage.

## Commit

- Example: `fix(masterblaster): bomb pass-through arms when leaving cell`
