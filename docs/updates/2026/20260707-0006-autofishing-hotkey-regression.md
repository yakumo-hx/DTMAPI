# 20260707-0006 - AutoFishing Hotkey Regression

Status: source-verified / superseded-by-0007-runtime-manual-pass / issue-010-open

## Source Request

After the edge-sampling hotkey rebuild, the user manually verified YConsole and Zoom but reported two AutoFishing regressions: pressing F6 made AutoFishing repeatedly toggle, and movement no longer cancelled automation.

## Changed Files

- `src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs`
- `testmods/AutoFishingMod/ModEntry.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/reviews/manual-qa/2026/20260707-0001-hotkey-autofishing-regression-review.md`
- `docs/updates/2026/20260707-0005-hotkey-edge-sampling.md`
- `docs/updates/2026/20260707-0006-autofishing-hotkey-regression.md`
- `docs/updates/INDEX.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`

## Root Cause

- `ReflectedUnityInput.TrySampleWin32KeyCached` treated the Win32 low-bit `GetAsyncKeyState(vk) & 0x0001` transition as `PressedEdge` even when DTMAPI's local state still considered the key down. For a toggle consumer, that can produce repeated F6 pressed events while the key is effectively held.
- AutoFishing accepted every `auto-fishing.toggle` press as a state flip and did not require the toggle key to be released before accepting the next toggle.
- AutoFishing's manual-cancel keys had been registered as temporary typed keybinds. The intended model was snapshot observation while automation is enabled, not publishing movement-cancel keys as AutoFishing hotkey events.

## Implementation

- Win32 cached sampling now reports `PressedEdge` only when the local previous state was not down: `!wasDown && (isDown || transitioned)`. This keeps short tap recovery from the low bit but prevents held-state repeated edges.
- AutoFishing now has a `waitingForToggleRelease` guard, so one physical F6 press can toggle only once until the configured keybind is no longer down.
- AutoFishing disable paths, including ReturnedToTitle and already-disabled cleanup, clear the toggle-release guard immediately instead of waiting for a later update tick.
- AutoFishing manual cancel keys now use `helper.Input.RegisterButton` / `UnregisterButton` while automation is enabled. They feed `helper.Input.IsDown(...)` snapshot checks but do not dispatch typed `KeybindPressed` events.
- Added unit coverage that legacy tracked buttons can feed snapshot state without publishing typed keybind events.

## Validation

- `dotnet build DTMAPI.sln -c Release` passed with 0 warnings and 0 errors.
- `dotnet test tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj -c Release --no-build` exited successfully after the rebuild.
- Console unit runner passed with `DOTNET_ROLL_FORWARD=Major`.
- `git diff --check` exited successfully with line-ending normalization warnings only.

## Runtime Evidence

Runtime lock was acquired for each local game attempt and released afterward. No `DolocTown.exe` process remained after the attempts.

- `docs/debug/evidence/GAME-SMOKE/20260707-195714`: invalid for F6 verification. `CoreAutoFishing` selected the Workshop AutoFishing id, and all three external F6 sends failed before any AutoFishing toggle log appeared. Process/fatal checks were clean.
- `docs/debug/evidence/GAME-SMOKE/20260707-195938`: invalid for F6 verification but useful for owner state. It enabled local `Local.Yuuka_DTMAPI_AutoFishing`, loaded the third save, and showed AutoFishing idle owner input reduced to one registration instead of eight, confirming manual-cancel keys are no longer registered while disabled. External F6 sends still failed at the smoke input/focus layer.
- `docs/debug/evidence/GAME-SMOKE/20260707-200227`: invalid for F6 verification. Current-profile Steam launch loaded cleanly and had no fatal window, but external F6 sends again failed at the smoke input/focus layer.

The original available smoke path could not deliver F6 to the game window. Final verification was completed in `20260707-0007`: user manual retest passed AutoFishing F6 and movement cancel, and short smoke `docs/debug/evidence/GAME-SMOKE/20260708-021606` passed `AutoFishingHotkey` plus `AutoFishingMovementCancel` using typed-frame F6 and A snapshot-cancel evidence.

## Documentation

- Added manual QA/root-cause record `docs/reviews/manual-qa/2026/20260707-0001-hotkey-autofishing-regression-review.md`.
- Corrected `20260707-0005-hotkey-edge-sampling.md`: the repeated F6 lines in `GAME-SMOKE/20260707-190231` are now treated as confirmed AutoFishing regression evidence, not harmless harness retry noise.
- Updated ISSUE-010 with the AutoFishing regression classification while keeping the long-run native GC issue open.
- Added regression matrix row `INPUT-HOTKEY-AUTOFISHING-REGRESSION-20260707`.

## Rollback Notes

- Reverting only the AutoFishing guard would re-expose toggle consumers to repeated edge events from any backend glitch.
- Reverting manual cancel keys back to typed keybinds would make A/D/Space/Shift hotkey events again instead of snapshot-only movement observation.
- If a future backend needs stronger Win32 transition handling, preserve `!wasDown` gating for held keys and add a targeted short-tap test route rather than restoring repeated held transitions.

## Follow-Up

- Keep the `20260708-021606` typed-frame smoke route for future F6/A regression checks.
