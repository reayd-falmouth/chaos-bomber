# Plan G — Wheel of fortune pointer matches winner

## Goal

The wheel **visual stop segment** matches the **rewarded** `stopIndex` (no off-by-one or “spin ends elsewhere”).

## Primary files

- [Assets/Scripts/MasterBlaster/Runtime/Scenes/WheelOFortune/WheelController.cs](mdc:Assets/Scripts/MasterBlaster/Runtime/Scenes/WheelOFortune/WheelController.cs)

## Tests (write first)

- **Assembly:** `Scenes.Tests`.
- **Approach:** Extract **spin completion** math: input `stopIndex`, segment count, duration → final rotation bucket; unit-test several indices and edge wrap.
- **Case:** award index == pointed index after spin.

## Implementation notes

- After time-based spin, **snap** or **tween** to exact rotation for `stopIndex`.
- Keep reward grant and visual in one code path to avoid drift.

## Verify

- Unity MCP: compile, run tests, manual spin sample.

## Commit

- Example: `fix(minigame): wheel stops on selected prize`
