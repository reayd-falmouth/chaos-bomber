# Plan F — Standings trophies match wins

## Goal

**Standings** UI shows the correct **trophy count** per player from `SessionManager` (or authoritative wins store). No silent failures from missing child names or null `SessionManager`.

## Primary files

- [Assets/Scripts/MasterBlaster/Runtime/Scenes/Standings/StandingsController.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Scenes/Standings/StandingsController.cs)
- [Assets/Scripts/MasterBlaster/Runtime/Core/SessionManager.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Core/SessionManager.cs)
- Relevant prefabs under `Assets/Prefabs/MasterBlaster/` (row layout: `Avatar`, `TrophyContainer`)

## Tests (write first)

- **Assembly:** `Scenes.Tests`.
- **Approach:** Unit-test trophy row builder: given wins N, expect N active trophy icons (pure function from data → `List<bool>` or count). Avoid instantiating full UI if possible.
- **Case:** 0 wins, 3 wins, max wins; missing child logs assert (optional).

## Implementation notes

- Defensive `Find` → clear error or fallback.
- Confirm Standings scene has bootstrap with `SessionManager`.

## Verify

- Unity MCP: compile, run tests, play flow to standings.

## Commit

- Example: `fix(ui): standings trophies reflect session wins`
