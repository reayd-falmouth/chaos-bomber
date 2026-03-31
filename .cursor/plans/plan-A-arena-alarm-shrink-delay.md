# Plan A — Arena alarm at 3:00, then delayed shrink

## Goal

After the round clock reaches **3:00 elapsed** (or remaining hits zero from a 3:00 match, per design), **alarm starts first**; **arena shrink begins after a configured delay**. Timer must start **once** (no duplicate coroutines).

## Primary files

- [Assets/Scripts/MasterBlaster/Runtime/Scenes/Arena/Map/ArenaShrinker.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Scenes/Arena/Map/ArenaShrinker.cs)
- [Assets/Scripts/MasterBlaster/Runtime/Scenes/Arena/Map/MapSelector.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Scenes/Arena/Map/MapSelector.cs)
- [Assets/Scripts/MasterBlaster/Runtime/Scenes/Arena/GameManager.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Scenes/Arena/GameManager.cs) (re-apply / timer start)

## Tests (write first)

- **Assembly:** `Scenes.Tests` (reference `HybridGame`).
- **Approach:** Extract **pure time math** into a small static helper or testable component API, e.g. `ArenaShrinkSchedule.ComputeAlarmAndShrinkTimes(matchDuration, alarmLead, shrinkDelayAfterAlarm)` **or** drive a stripped-down test double that does not require networking.
- **Cases:** alarm fires at t=180s from round start; shrink starts at t=180+delay; `StartTimer` idempotent (second call no-op while running).

## Implementation notes

- Replace or augment fraction-based `alarmThresholdFraction` / `shrinkThresholdFraction` if product wants **wall-clock 3:00 + delay** (confirm `matchDuration` vs “elapsed” semantics in `ArenaShrinker`).
- Document serialized fields (delay seconds after alarm).

## Verify

- Unity MCP: compile clean, no console errors.
- Run new + related tests.

## Commit

- Example: `fix(masterblaster): arena alarm at 3min then delayed shrink`
- Stage only files touched for Plan A.
