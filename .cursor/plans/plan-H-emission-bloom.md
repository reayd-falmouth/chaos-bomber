# Plan H — Explosions / items emission readable (URP)

## Goal

**Explosion** and **item** materials that use emission read as glowing in-game (bloom or strong enough emission + tonemapping), in both **top-down** and **FPS** views.

## Primary files

- [Assets/Settings/Rendering/DefaultVolumeProfile.asset](mdc:Assets/Settings/Rendering/DefaultVolumeProfile.asset) (bloom intensity)
- Materials: `Assets/Art/MasterBlaster/Materials/Explosion.mat`, `Assets/Art/MasterBlaster/Materials/Items/*.mat`
- Spawners if keywords must be enabled at runtime: bomb/explosion scripts

## Tests (write first)

- **Approach:** Asset-only changes often have **no code test**; add a minimal **EditMode** test only if you introduce a `MaterialEmissionBootstrap` script. Otherwise document **manual screenshot checklist** in PR and rely on Unity MCP compile.
- **Optional:** test that a helper sets `_EMISSION` keyword when missing (if you add such helper).

## Implementation notes

- Prefer **volume profile** bloom > 0 over cranking every material if art intent is “glow”.
- Use **HDR** emission colors where appropriate.

## Verify

- Unity MCP: compile; visual pass in editor play mode.

## Commit

- Example: `fix(vfx): enable bloom for masterblaster emissive pickups`
