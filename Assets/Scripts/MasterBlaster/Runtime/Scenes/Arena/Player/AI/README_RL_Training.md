# Training the Bomberman AI with Reinforcement Learning

The game uses **Unity ML-Agents** so AI players can be controlled by a trained neural network instead of the scripted brain. This document explains how to train the agent.

## Prerequisites

1. **Unity ML-Agents package** (already in the project): `com.unity.ml-agents` 4.0.0.
2. **Python 3.10** (e.g. 3.10.12) with the ML-Agents Python package:
   ```bash
   pip install mlagents==1.1.0
   ```

## Enabling RL in the Game

1. In the **Game** scene, select the GameObject that has the **GameManager** component.
2. In the Inspector, under **AI**, enable **Use Reinforcement Learning**.
3. With this on, any player slot that gets AI (no controller assigned) will use the **BombermanAgent** + **MLAgentsBrain** instead of the scripted AI.
4. Without a trained model, the agent falls back to its **Heuristic** (built-in rule-based behavior). After training, assign the `.onnx` model to the agent’s **Behavior Parameters** for inference.

## Agent Setup (Automatic)

When **Use Reinforcement Learning** is enabled, the GameManager adds these components to AI players at runtime:

- **BombermanAgent** – observations, actions, rewards, heuristic.
- **BehaviorParameters** – created in code with:
  - Vector Observation Size: **15**
  - Discrete branches: **5, 2, 2** (move, place bomb, detonate).
- **MLAgentsBrain** – adapts the agent to the existing `IAIBrain` / `AIPlayerInput` flow.

No manual setup is required for training or inference beyond enabling **Use Reinforcement Learning** and (after training) assigning the model.

## Observations (15 floats)

| Index | Description |
|-------|-------------|
| 0–1   | My position (x, y) normalized by arena scale |
| 2–4   | Nearest opponent: relative (dx, dy), distance (normalized) |
| 5–7   | Nearest bomb: relative (dx, dy), radius (normalized) |
| 8     | Is my current cell in explosion danger (0 or 1) |
| 9–10  | Bombs remaining and explosion radius (normalized) |
| 11–12 | Nearest item relative (dx, dy) if any |
| 13–14 | Reserved / padding |

## Actions (discrete)

- **Branch 0 – Move:** 0 = none, 1 = up, 2 = down, 3 = left, 4 = right.
- **Branch 1 – Place bomb:** 0 = no, 1 = yes.
- **Branch 2 – Detonate (remote):** 0 = hold (don’t detonate), 1 = release (detonate).

## Rewards (tunable on BombermanAgent)

- **rewardPerStep** (default 0.001) – small reward each fixed step to encourage surviving.
- **rewardKillOpponent** (default 1) – when the number of active opponents decreases.
- **rewardCollectItem** (default 0.3) – when the agent collects a power-up (if hooked up).
- **rewardDeath** (default -1) – when the agent dies; also ends the episode.

## Training with the ML-Agents Python API

1. **Build a training build** (or use the Editor with the ML-Agents training flag).
2. Create a YAML config (see `bomberman_config.yaml` in this folder or below).
3. Run training, e.g.:
   ```bash
   mlagents-learn bomberman_config.yaml --run-id=bomberman_v1
   ```
4. When prompted, **open the Game scene (or Train scene) in the Editor, then press Play**. The scene that loads when you press Play **must** be Game or Train (the one with the agents), or the trainer will timeout ("The Unity environment took too long to respond").
5. Trained artifacts are written to `results/bomberman_v1/`. Use the generated `.onnx` file as the model in the agent’s **Behavior Parameters** (Model) for inference in the game.

## Example YAML config (save as `bomberman_config.yaml`)

```yaml
behaviors:
  Bomberman:
    trainer_type: ppo
    hyperparameters:
      batch_size: 1024
      buffer_size: 10240
      learning_rate: 3.0e-4
      learning_rate_schedule: linear
      beta: 5.0e-3
      epsilon: 0.2
      lambd: 0.95
      num_epoch: 3
    network_settings:
      normalize: true
      hidden_units: 256
      num_layers: 2
      vis_encoder: null
    reward_signals:
      extrinsic:
        gamma: 0.99
        strength: 1.0
    max_steps: 500000
    time_horizon: 512
    summary_freq: 5000
    keep_checkpoints: 3
    checkpoint_interval: 20000
```

Adjust `max_steps`, `time_horizon`, and `hyperparameters` as needed. The **Behavior Name** in your Unity **Behavior Parameters** must match the key under `behaviors` (e.g. `Bomberman`).

## Train scene (recommended for training)

A dedicated **Train** scene keeps training logic separate from the main game. When the active scene name **contains** "Train" (e.g. "Train", "Train 1"), `TrainingMode.IsActive` is true automatically (no `-training` flag needed).

### Creating the Train scene

1. In Unity, duplicate the **Game** scene: in the Project window right‑click **Game** → **Duplicate**, then rename the copy to **Train**.
2. In the **Train** scene, ensure **TrainingAcademyHelper** is present (e.g. on the GameManager GameObject or an empty GameObject). Add it if you only had it in Game.
3. For training builds, add **Train** to **File → Build Settings** and set it as the **first (index 0)** scene so the executable starts in the Train scene.

When you open the **Train** scene and press Play (or run a build that starts with Train), the game runs in training mode: 2 RL agents, no device assignment, and the scene reloads on round end. You can still use **Game** with the `-training` flag if you prefer.

## Autonomous training (no human play)

To train rapidly without playing manually, use **training mode**: all players are RL agents, and the arena reloads automatically when a round ends so episodes repeat.

### 1. Training mode

Training mode is active when either:

- The active scene is **Train** (see above), or  
- The game is run with the **`-training`** command-line argument (e.g. when using the Game scene as the first scene).

When active, the scene uses a fixed 2 players, assigns no devices (all slots are AI). If an **Academy** is in the scene (e.g. for ML-Agents training), players use `BombermanAgent` + `MLAgentsBrain`; otherwise they use scripted AI so they still move and place bombs. When a round ends (or the timer/shrink triggers), the scene reloads instead of going to Standings or Shop.

### 2. Scene setup

In the scene you use for training (**Train** or **Game**), add the **TrainingAcademyHelper** component (e.g. on the GameManager GameObject or an empty GameObject). When in training mode it subscribes to the ML-Agents Academy `OnEnvironmentReset`; on reset (e.g. after max steps) it reloads the current scene. The Academy is auto-initialized by ML-Agents when agents run. Optionally add an **Academy** component in the Editor to configure **Max Step** (e.g. 2000–5000).

### 3. Training build and entry

For the **training** build, set **Train** (or **Game**) as the **first (index 0)** scene in **File → Build Settings** so the executable starts directly in the arena. Use **Server Build** for headless runs (no rendering, faster; suitable for multiple instances).

### 4. Run autonomous training

1. Start the Python trainer: `mlagents-learn bomberman_config.yaml --run-id=bomberman_v1`
2. Run the training build:
   - If the first scene is **Train**: run the exe with no arguments (e.g. `MasterBlaster.exe`).
   - If the first scene is **Game**: run with `-training` (e.g. `MasterBlaster.exe -training`).
3. When the trainer prompts, press Play (or the build connects automatically). Episodes run autonomously: two RL agents play, the round ends, the scene reloads, and the next episode starts. No human input required.

For **multiple parallel environments**, build headless, run multiple instances of the executable (with **Train** as first scene, or with `-training` if using Game). Use different `--worker-id` / `--base-port` per process if required by your ML-Agents version. The trainer connects to all and aggregates experience.

### 5. Summary

| Step | Action |
|------|--------|
| Scene | **Train**: duplicate Game, save as Train; add TrainingAcademyHelper. Or use **Game** with `-training`. |
| Build | Train (or Game) = first scene; optional Server Build for headless. |
| Run | `YourGame.exe` (Train as first scene) or `YourGame.exe -training` (Game as first scene). |
| Train | `mlagents-learn bomberman_config.yaml --run-id=bomberman_v1` then connect. |

## Tips

- Train with **2 players** so episodes are short. In autonomous training both are RL and the scene reloads each round; otherwise use one human and one AI.
- Increase **Max Step** in the Academy or let rounds end naturally so episodes don’t run forever.
- If the agent never explores bombing, try increasing the reward for placing a bomb when an opponent is nearby (e.g. a small bonus in the agent code when `LastPlaceBomb` and an opponent is in range), or train longer.
- Use the **Heuristic** in the Editor (no model assigned) to verify observations and actions; the agent will behave like the scripted AI.
