# Master index — MasterBlaster bug fixes (one plan per task)

This folder splits the bug list into **separate executable plans**. Follow **[unity-skill](../skills/unity-skill/SKILL.md)** for every task: **test first → implement → Unity MCP (compile/console) → run tests → lint/diagnostics → one commit**.

## Workflow (repeat per task)

1. **Branch** (optional): `fix/<short-slug>` from a clean baseline; otherwise use **atomic commits** on your working branch.
2. **Write failing tests** under [Assets/Scripts/Tests](mdc:Assets/Scripts/Tests):
   - MasterBlaster (`HybridGame`): **[Scenes.Tests](mdc:Assets/Scripts/Tests/Runtime/Scenes/Tests/Tests.asmdef)** (already references `HybridGame`).
   - FPS-only logic: **[fps.Tests.EditMode](mdc:Assets/Scripts/Tests/EditMode/fps.Tests.EditMode.asmdef)** or extend an existing FPS test asmdef.
   - Prefer **EditMode** for pure logic (timers, session reset rules, wheel index math). Use **PlayMode** only when you must step the engine (camera, spawning).
3. **Implement** the smallest change that satisfies the task; avoid unrelated `.unity` / `.prefab` churn.
4. **Unity MCP**: read console (Errors); wait for compile/domain reload; **fix all compiler errors** before running tests.
5. **Run tests** via Unity MCP — target the **new/edited** fixture first, then broader suite if needed.
6. **Lint**: resolve warnings/diagnostics on touched files (Unity + Cursor). Add `.editorconfig`/analyzers only if the team agrees (out of scope unless requested).
7. **One commit per task**, Conventional Commits, **stage only this task’s files**:

   `fix(<scope>): <imperative description>`

8. **Rollback**: one task ↔ one revert; never combine unrelated fixes.

**Commit milestone (per [unity-skill](../skills/unity-skill/SKILL.md)):** commit when (a) new production code + its tests are complete, (b) compiler errors are cleared for that task, and (c) the relevant Unity tests pass — message like `feat: [Task ID] - all tests passed` or `fix: [Task ID] - all tests passed`.

## Suggested execution order (low coupling)

| Order | ID | Plan |
| ----- | -- | ---- |
| 1 | B | [plan-B-quit-menu-session-reset.md](plan-B-quit-menu-session-reset.md) |
| 2 | C | [plan-C-settings-apply-persist.md](plan-C-settings-apply-persist.md) |
| 3 | A | [plan-A-arena-alarm-shrink-delay.md](plan-A-arena-alarm-shrink-delay.md) |
| 4 | I | [plan-I-bombs-blocking.md](plan-I-bombs-blocking.md) |
| 5 | L | [plan-L-pickups-parity.md](plan-L-pickups-parity.md) |
| 6 | D | [plan-D-billboards-fps-camera.md](plan-D-billboards-fps-camera.md) |
| 7 | H | [plan-H-emission-bloom.md](plan-H-emission-bloom.md) |
| 8 | J | [plan-J-death-sprite-final.md](plan-J-death-sprite-final.md) |
| 9 | F | [plan-F-standings-trophies.md](plan-F-standings-trophies.md) |
| 10 | G | [plan-G-wheel-pointer-winner.md](plan-G-wheel-pointer-winner.md) |
| 11 | E | [plan-E-shop-to-game-reset-policy.md](plan-E-shop-to-game-reset-policy.md) |
| 12 | K | [plan-K-fps-gun-side.md](plan-K-fps-gun-side.md) |
| — | M | [plan-M-fps-time-limit-future.md](plan-M-fps-time-limit-future.md) (optional, after A/L) |

</think>


<｜tool▁calls▁begin｜><｜tool▁call▁begin｜>
StrReplace