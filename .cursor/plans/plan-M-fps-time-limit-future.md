# Plan M (optional) — FPS mode time limit (future)

## Goal

**FPS gameplay** becomes **time-limited** (match timer, Sudden Death, or return to flow) consistent with arena rules — only if product wants parity with shrinking match.

## Primary files (TBD after design)

- [Assets/Scripts/MasterBlaster/Runtime/GameModeManager.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/GameModeManager.cs)
- [Assets/Scripts/MasterBlaster/Runtime/Scenes/Arena/Map/ArenaShrinker.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Scenes/Arena/Map/ArenaShrinker.cs) or new `FpsMatchTimer`
- UI: pause/HUD if countdown shown in FPS

## Tests (write first)

- **Assembly:** `Scenes.Tests`.
- **Approach:** Unit-test timer transitions (start, pause, expire) without full FPS scene.

## Implementation notes

- Clarify interaction with **Plan A** (one global match clock vs per-mode clocks).

## Verify

- Unity MCP: compile, run tests, play FPS until limit.

## Commit

- Example: `feat(masterblaster): time limit for fps mode`
