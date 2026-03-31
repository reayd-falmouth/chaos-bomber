# Plan E — Shop → Game arena reset (product policy)

## Goal

**Decide explicitly:** leaving **Shop** and returning to **Game** should either **reset** the arena (current `GameManager` behavior) or **preserve** in-run state. Implement the chosen behavior and make flow consistent with player expectations.

## Primary files

- [Assets/Scripts/MasterBlaster/Runtime/Core/SceneFlowManager.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Core/SceneFlowManager.cs)
- [Assets/Scripts/MasterBlaster/Runtime/Scenes/Arena/GameManager.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Scenes/Arena/GameManager.cs)
- Shop flow: [Assets/Scripts/MasterBlaster/Runtime/Scenes/Shop/ShopController.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Scenes/Shop/ShopController.cs)

## Tests (write first)

- **Assembly:** `Scenes.Tests`.
- **Approach:** Test a small **policy flag** or flow hook: “on Shop exit, call `ResetArenaForNewRound` vs skip”. If too scene-coupled, test `GameManager` public method guard with a test double lifecycle.
- **Case:** preserve path does not clear destructibles; reset path does.

## Implementation notes

- Document in tooltip / dev comment so future changes do not “fix” intentional resets.
- If preserving: ensure bombs/items/session timers align with state.

## Verify

- Unity MCP: compile, run tests, manual shop loop.

## Commit

- Example: `fix(masterblaster): shop return preserves arena state` or `docs/gameplay: clarify shop arena reset`
