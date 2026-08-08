# 20260707-0001 - Hotkey AutoFishing Regression Review

## Source Feedback

1. YConsole/Zoom hand testing passed.

Analysis: this keeps the edge-sampling hotkey direction intact. The regression should be scoped to AutoFishing's consumer behavior or to a backend edge path that toggle-style consumers expose more sharply than YConsole/Zoom.

2. AutoFishing repeatedly toggles after F6.

Analysis: `GAME-SMOKE/20260707-190231` already showed repeated `auto-fishing.toggle` pressed lines every roughly 250 ms after one external F6 send. The earlier classification as harness retry noise was wrong after user reproduction. The code-level culprit is twofold: Win32 cached sampling allowed `GetAsyncKeyState` transition low-bit to become `PressedEdge` even while local state still had F6 down, and AutoFishing accepted every toggle event without waiting for F6 release.

Fix direction: gate Win32 cached pressed edges with `!wasDown`, while preserving short-tap recovery via `transitioned` only when local state was not down. Add an AutoFishing toggle-release guard so repeated backend edge glitches cannot flip the automation state every frame.

3. Movement does not cancel AutoFishing.

Analysis: AutoFishing's manual-cancel keys were converted into temporary typed keybind registrations during the first hotkey rebuild. That made movement-cancel keys act like hotkey event consumers, even though the desired behavior is only to sample whether movement is currently down while automation is enabled. The current fishing-state cancel set is A/D/Space/Shift; W/S are not valid movement cancel keys for this state. In the repeated-F6 state, the automation may also be disabled again before movement snapshot checks matter.

Fix direction: register movement cancel keys as temporary legacy tracked buttons only while automation is enabled, then read `helper.Input.IsDown(...)` from the input snapshot. Do not publish `KeybindPressed` events for movement cancel keys.

## Verification Notes

- Source validation passed after the fix: Release build, Release unit tests after rebuild, console unit runner, and `git diff --check`.
- Runtime attempts `GAME-SMOKE/20260707-195714`, `GAME-SMOKE/20260707-195938`, and `GAME-SMOKE/20260707-200227` did not verify F6 because `Send-DolocTownNamedKey(F6)` failed to target the game window. They did verify clean exits/no fatal windows; `20260707-195938` also showed local AutoFishing idle input registration count reduced to one.
- Final acceptance still needs user manual retest for AutoFishing F6 and movement cancel.
