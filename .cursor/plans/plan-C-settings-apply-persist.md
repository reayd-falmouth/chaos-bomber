# Plan C — Settings menu honored in gameplay

## Goal

Values set in the **settings UI** (volumes, scanlines, etc.) **persist** (PlayerPrefs or project standard) and **apply** to the audio/render path actually used during **MasterBlaster + hybrid FPS** gameplay. No conflicting stack overwriting prefs (e.g. menu close forcing master volume to 1).

## Primary files

- [Assets/Scripts/MasterBlaster/Runtime/Core/GlobalPauseMenuController.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Core/GlobalPauseMenuController.cs)
- [Assets/Scripts/FPS/Runtime/UI/InGameMenuManager.cs](mdc:Assets/Scripts/FPS/Runtime/UI/InGameMenuManager.cs)
- [Assets/Scripts/FPS/Runtime/Game/AudioUtility.cs](mdc:Assets/Scripts/FPS/Runtime/Game/AudioUtility.cs)
- [Assets/Scripts/FPS/Runtime/Game/Managers/AudioManager.cs](mdc:Assets/Scripts/FPS/Runtime/Game/Managers/AudioManager.cs) if used

## Tests (write first)

- **Assembly:** Prefer **EditMode** in `fps.Tests.EditMode` for FPS menu math; `Scenes.Tests` for MasterBlaster prefs application if logic is extracted.
- **Approach:** Extract **ApplySettingsFromPrefs** / **SaveSettings** to static or service; test: saved float → applied target (mock mixer or spy on helper that sets a `TestOnly` delegate).
- **Case:** closing FPS menu does not stomp saved master volume.

## Implementation notes

- Pick **one** authority for master volume (Mixer vs `AudioListener`) and route both menus through it.
- Document PlayerPrefs keys in one place.

## Verify

- Unity MCP: compile, run tests, manual: change setting → play → quit → verify.

## Commit

- Example: `fix(audio): unify settings apply for hybrid gameplay`
