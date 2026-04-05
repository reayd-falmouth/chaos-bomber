# MasterBlaster — Game prototype

![Unity 6](https://img.shields.io/badge/Unity-6000.3.9f1%20(URP%2017.3.0)-blue)

Single-player–focused prototype: **Bomberman-style arena combat** and **first-person combat** in one session, built on the **Unity FPS Microgame** template with a hybrid mode switch. Primary scene for play in-editor: [`Assets/Scenes/MasterBlaster/MasterBlaster_FPS.unity`](Assets/Scenes/MasterBlaster/MasterBlaster_FPS.unity).

---

## Deliverables (coursework)

| Item | Link |
|------|------|
| **Windows (or similar) build** | [Add your uploaded build URL here](https://REPLACE-WITH-YOUR-WINDOWS-BUILD-LINK.example.com) |
| **Project source (backup)** | [Add your uploaded source archive URL here](https://REPLACE-WITH-YOUR-SOURCE-BACKUP-LINK.example.com) |

Replace the placeholder URLs after you publish your build and a zip of the project.

---

## Control scheme

Bindings are defined in [`Assets/App/MasterBlaster/Input/PlayerControls.inputactions`](Assets/App/MasterBlaster/Input/PlayerControls.inputactions) (Input System) for menus and arena/Bomberman, and in [`ProjectSettings/InputManager.asset`](ProjectSettings/InputManager.asset) for **FPS mode** (legacy axes used by the FPS Microgame scripts).

### Menus (e.g. main menu)

The main menu uses the **Player** action map: **Move** to change selection, **PlaceBomb** to confirm (the menu reuses this action as “submit”).

| Action | Keyboard | Gamepad |
|--------|----------|---------|
| Navigate | Arrow keys or **WASD** | Left stick or D-pad |
| Confirm / advance | **Space** | South face button (A / Cross) |

### Arena — Bomberman / top-down (Input System)

| Action | Keyboard | Gamepad |
|--------|----------|---------|
| Move | Arrow keys or **WASD** | Left stick or D-pad |
| Place bomb | **Space** | South face button |
| Switch Bomberman ↔ FPS | **Tab** | North face button |
| Pause | **Esc** or **P** | **Start** |

The **GameUI** map (same asset) includes **Pause** on Esc, P, and Start.

### FPS mode (legacy Input Manager)

Used when the hybrid player is in FPS mode (`PlayerDualModeController` enables the FPS Microgame controller).

| Action | Keyboard / mouse | Notes |
|--------|------------------|--------|
| Move | **WASD** | Axes Horizontal / Vertical |
| Look | **Mouse** | Mouse X / Mouse Y |
| Fire | **Left mouse** | |
| Aim | **Right mouse** (hold) | |
| Sprint | **Left Shift** | Also gamepad sprint where configured |
| Jump | **Space** | In FPS only; in Bomberman mode **Space** places a bomb instead |
| Crouch | **C** | |
| Reload | **R** | |
| Next / previous weapon | **Q** / **E** (see project Input Manager) | Plus mouse wheel |
| Pause / menu | **Tab**, **P** | Gamepad: see Input Manager |
| UI Submit | **Enter** | |
| UI Cancel | **Esc** | |

---

## Known bugs / issues

- **Alternate “normal level” layouts** — When “Normal Level” is disabled in the menu, alternate map settings are not fully applied. `LoadAlternateLevelSettings()` in [`GameManager.cs`](Assets/Scripts/MasterBlaster/Runtime/Scenes/Arena/GameManager.cs) is still a stub (spawn offsets and related layout tweaks are TODO).

- **Credits / “continue” flow** — If more than one [`ContinueOnAnyInput`](Assets/Scripts/MasterBlaster/Runtime/Core/ContinueOnAnyInput.cs) instance is active, a single keypress could advance the flow twice on screens that use “any input to continue.”

- **Optional tooling** — Packages such as **Netcode**, **ML-Agents**, **Multiplayer Services**, and **Unity MCP** (editor tooling) are present for development or experiments; they are **not** required for a local single-player prototype build.

---

## Third-party assets and packages

### Unity Package Manager ([`Packages/manifest.json`](Packages/manifest.json))

| Package | Version |
|---------|---------|
| com.coplaydev.unity-mcp | Git `main` (MCPForUnity) |
| com.unity.2d.sprite | 1.0.0 |
| com.unity.ai.navigation | 2.0.12 |
| com.unity.cinemachine | 3.1.6 |
| com.unity.collab-proxy | 2.11.3 |
| com.unity.connect.share | 4.2.4 |
| com.unity.ide.rider | 3.0.39 |
| com.unity.ide.visualstudio | 2.0.26 |
| com.unity.ide.vscode | 1.2.4 |
| com.unity.inputsystem | 1.18.0 |
| com.unity.learn.iet-framework.authoring | 1.5.3 |
| com.unity.ml-agents | 4.0.2 |
| com.unity.multiplayer.center | 1.0.1 |
| com.unity.multiplayer.tools | 2.2.8 |
| com.unity.netcode.gameobjects | 2.10.0 |
| com.unity.probuilder | 6.0.8 |
| com.unity.progrids | 3.0.3-preview.6 |
| com.unity.render-pipelines.universal | 17.3.0 |
| com.unity.services.multiplayer | 2.1.3 |
| com.unity.test-framework | 1.6.0 |
| com.unity.timeline | 1.8.10 |
| com.unity.ugui | 2.0.0 |

Also **com.unity.modules.*** entries (engine modules) — see `manifest.json` for the full list.

### Embedded / third-party content in this repo

| Asset / library | Location / note |
|-----------------|-----------------|
| **Unity FPS Microgame** | Base project and gameplay template ([Unity Learn / FPS Microgame](https://learn.unity.com/project/fps-microgame)); see also `Assets/App/FPS/FPSMicrogame_README.txt`. |
| **Feel** (More Mountains) | `Assets/Feel/` — feedback and juice utilities (e.g. MMFeedbacks). |
| **TextMesh Pro** | `Assets/TextMesh Pro/` — TMP resources shipped with the project. |
| **NavMesh Components** | `Assets/App/NavMeshComponents/` — see `LICENSE` in that folder. |

---

*Coursework-oriented copy also lives in the Unity asset **Project Architecture Readme** (`Assets/Editor/ProjectReadme/ProjectArchitectureReadme.asset`), which can auto-open once per machine when the project loads (or via **Tools → MasterBlaster → Open Architecture Readme**).*
