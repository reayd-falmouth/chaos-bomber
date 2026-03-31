# Plan B — Game resets after Quit to main menu

## Goal

After **MasterBlaster pause → Quit to Menu** (`SceneFlowManager` / `GlobalPauseMenuController`), the next **Start game** session does **not** inherit prior round/session state (wins, coins, upgrades, stale singletons — scope per product: full session vs “match only”).

## Primary files

- [Assets/Scripts/MasterBlaster/Runtime/Core/GlobalPauseMenuController.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Core/GlobalPauseMenuController.cs)
- [Assets/Scripts/MasterBlaster/Runtime/Core/SceneFlowManager.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Core/SceneFlowManager.cs)
- [Assets/Scripts/MasterBlaster/Runtime/Core/SessionManager.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Core/SessionManager.cs)
- [Assets/Scripts/MasterBlaster/Runtime/Scenes/MainMenu/MainMenuController.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Scenes/MainMenu/MainMenuController.cs) (compare with quit path)

## Tests (write first)

- **Assembly:** `Scenes.Tests`.
- **Approach:** Unit-test `SessionManager` (or a small `ISessionReset` façade) **reset API** called from quit vs start; assert cleared fields / re-init. Where `MonoBehaviour`/singleton makes unit tests hard, extract **reset logic** to a plain class testable in EditMode.
- **Cases:** quit-to-menu calls reset OR next `Initialize` overwrites; no double-reset crashes.

## Implementation notes

- Single-scene mode toggles roots without domain reload — **explicit reset** is required.
- Align behavior with design: “quit = abandon run” vs “keep progress”.

## Verify

- Unity MCP: compile, run tests, smoke: play round → quit → start.

## Commit

- Example: `fix(masterblaster): reset session when quitting to menu`
