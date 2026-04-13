# MasterBlaster — IGO721 indie prototype

![Unity 6](https://img.shields.io/badge/Unity-6000.3.9f1%20(URP%2017.3.0)-blue)

**MasterBlaster** is a single-player prototype that combines **Bomberman-style arena combat** (top-down) and **first-person combat** in one session. It extends the [Unity FPS Microgame](https://learn.unity.com/project/fps-microgame) with a hybrid mode switch so you can move between grid-based bombing and FPS shooting without leaving the run.

**Primary scene (play in Editor):** [`Assets/Scenes/MasterBlaster/MasterBlaster_FPS.unity`](Assets/Scenes/MasterBlaster/MasterBlaster_FPS.unity)

**Unity version:** `6000.3.9f1` — see [`ProjectSettings/ProjectVersion.txt`](ProjectSettings/ProjectVersion.txt).

---

## How to run

1. Open this folder as a Unity 6 project (version above).
2. Open **`MasterBlaster_FPS`** (path above) and press **Play**.
3. **Vendor-only assets:** `Assets/3rdParty/` is not committed to git (see [Third-party assets](#third-party-assets)). For a full local copy, extract the same archive used in CI (e.g. `3rdparty-ci.zip` at the repo root) so that `Assets/3rdParty/` exists, or run the project’s third-party setup step if you use one.

---

## Coursework deliverables (IGO721)

| Item | Link |
|------|------|
| **Pitch video (Panopto)** | [DavidReay_IGO721_2026](https://falmouth.cloud.panopto.eu/Panopto/Pages/Viewer.aspx?id=a3713fcb-2db8-4d8d-931e-b42a00eb0952) |
| **Google Drive folder (pitch video + prototype)** | [DavidReay_IGO721 — Google Drive](https://drive.google.com/drive/folders/11xjCfhYJwO3qSf5NiCT2jXfPoi4jaVCt?usp=drive_link) |
| **Project source** | [https://github.com/reayd-falmouth/MasterBlaster_FPS](https://github.com/reayd-falmouth/MasterBlaster_FPS) |

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

> Used when the hybrid player is in FPS mode (`PlayerDualModeController` enables the FPS Microgame controller).

| Action | Keyboard / mouse | Notes |
|--------|------------------|--------|
| Move | **WASD** | Horizontal and Vertical axes |
| Look | **Mouse** | Mouse X and Mouse Y |
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

- **Multiplayer** — Multiplayer was not completed; Netcode / multiplayer-related packages in the project do not translate to a working networked mode in this build.

- **Title screen — asteroid particle** — The particle effect for the asteroid reads too large on the title screen.

- **Load time after countdown** — The game takes too long to load or transition into play after the countdown.

- **Optional tooling** — Packages such as **Netcode**, **ML-Agents**, **Multiplayer Services**, and **Unity MCP** (editor tooling) are present for development or experiments; they are **not** required for a local single-player prototype build.

---

## Third-party assets

### Unity Package Manager

Authoritative list: [`Packages/manifest.json`](Packages/manifest.json). The table below lists **package dependencies** (excluding `com.unity.modules.*` engine modules — those remain in the manifest).

| Package | Version / source |
|---------|------------------|
| com.coplaydev.unity-mcp | Git `main` ([MCPForUnity](https://github.com/CoplayDev/unity-mcp)) |
| com.rmc.rmc-readme | 1.2.2 (npm scoped registry) |
| com.unity.2d.animation | 13.0.4 |
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
| com.unity.postprocessing | 3.5.4 |
| com.unity.probuilder | 6.0.8 |
| com.unity.progrids | 3.0.3-preview.6 |
| com.unity.recorder | 5.1.6 |
| com.unity.render-pipelines.universal | 17.3.0 |
| com.unity.services.multiplayer | 2.1.3 |
| com.unity.test-framework | 1.6.0 |
| com.unity.timeline | 1.8.10 |
| com.unity.ugui | 2.0.0 |

**Notable stack:** URP (`com.unity.render-pipelines.universal`), Input System, Cinemachine, AI Navigation, ugui, Post Processing, Recorder — plus optional multiplayer/ML packages as above.

### Embedded in the repository (tracked assets)

| Asset / library | Location / note |
|-----------------|-----------------|
| **Unity FPS Microgame** | Base template and FPS content under [`Assets/App/FPS/`](Assets/App/FPS); see [`Assets/App/FPS/FPSMicrogame_README.txt`](Assets/App/FPS/FPSMicrogame_README.txt) and [`Assets/App/FPS/Third-PartyNotice.txt`](Assets/App/FPS/Third-PartyNotice.txt) (fonts: Roboto Apache 2.0, EmojiOne, LiberationSans, etc.). |
| **NavMesh Components** | [`Assets/App/NavMeshComponents/`](Assets/App/NavMeshComponents) (license/README) and runtime/editor scripts under [`Assets/Scripts/NavMeshComponents/`](Assets/Scripts/NavMeshComponents). |

### `Assets/3rdParty/` (vendor plugins — local / CI only)

The folder [`Assets/3rdParty/`](Assets/3rdParty) is **gitignored** so licensed Asset Store or third-party packs are not committed. CI downloads an archive (see [`.github/actions/setup-thirdparty-assets/action.yml`](.github/actions/setup-thirdparty-assets/action.yml)) and extracts it under `Assets/`.

After extraction (e.g. from `3rdparty-ci.zip`), vendor content typically lives under **`Assets/3rdParty/Vendor/`**, including (folder names from the current archive layout):

| Vendor folder | Description (from package structure) |
|---------------|--------------------------------------|
| **DAVFX** | *Realistic 6D Lighting Explosions* (VFX assets) |
| **Feel** | More Mountains Feel (feedback / juice) |
| **ithappy** | Third-party art/content pack |
| **Nebula Skyboxes** | Skybox assets |
| **ParallelCascades** | Third-party assets |
| **PicaVoxel** | Voxel-related tools/content |
| **PlayFabSDK** | PlayFab SDK |
| **SpriteExporter** | Editor/tooling |
| **TextMesh Pro** | TMP resources (may duplicate or supplement Unity’s TMP package usage) |
| **Universal Sound FX** | Audio library |
| **VolFx** | Volume/post-style effects pack |

If you submit a **source zip** for assessment, include `Assets/3rdParty/` if your build depends on these assets, or document that tutors must run the same extraction step.

---

## Generative AI disclosure

The IGO721 brief requires you to **declare** any generative AI used (text, code, images, audio, ideation, etc.): tools, purpose, and what was incorporated. Undisclosed use may be treated as academic misconduct.

| Tool | Purpose | What was incorporated |
|------|---------|------------------------|
| **Cursor** | Coding assistance in the editor | AI-assisted suggestions for C#/Unity code (generation, edits, refactors, debugging); outputs were reviewed and integrated where appropriate. |
| **Google Gemini** | Concept art | Generative concept imagery for visual development; selected or adapted pieces used as reference or basis for project art direction. |

---


## In-editor documentation

[`Assets/Documentation/ReadMe.asset`](Assets/Documentation/ReadMe.asset) uses **com.rmc.rmc-readme**; extended architecture notes are on [`Assets/Documentation/ProjectArchitectureReadme.asset`](Assets/Documentation/ProjectArchitectureReadme.asset). Open via **Window → MasterBlaster → Documentation → Open ReadMe** or **Tools → MasterBlaster → Open Project Readme (Documentation)**.
