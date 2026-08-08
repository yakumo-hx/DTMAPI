# 20260707-0007 - Hotkey Edge Follow-Up

Status: source-console-manual-runtime-verified / 50-key-pressure-passed / original-route-passed / no-virtual-long-passed / issue-010-main-menu-pressure-mitigated

## Source Request

After the AutoFishing regression fix, the user supplied a second review of the hotkey edge-sampling layer. The review accepted the general direction but found that chord short taps, `KeybindReleased`, owner-bound helper queries, and remaining hot-path allocations still needed tightening before the input layer could feel close to native Unity Input System behavior.

## Changed Files

- `src/DTMAPI.Abstractions/Input.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/DebugConsoleSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `testmods/AutoFishingMod/ModEntry.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/reviews/manual-qa/2026/20260707-0002-hotkey-edge-followup-review.md`
- `docs/updates/2026/20260707-0007-hotkey-edge-followup.md`
- `docs/updates/INDEX.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`

## Implementation

- `DtmKeybind.IsPressed` gained a three-state overload that accepts `wasReleased` in addition to `wasPressed` and `isDown`. Chords now dispatch when at least one member has a pressed edge and all other members are either down, pressed, or released in the same frame. This covers a held modifier plus a very short main-key tap that is already up by the sampled frame.
- `DtmKeybind.IsReleased` and `DtmKeybindList.IsReleased` now consume released edges directly. Core can dispatch `KeybindPressed` and then `KeybindReleased` in the same frame for a short tap without leaving aggregate keybind state stuck down.
- Core records release edges into the frame state even when the old compatibility `ButtonReleased` event is not dispatched because DTMAPI had not previously tracked that physical key as down. This preserves the existing conservative legacy button event behavior while letting typed keybind release state consume native `ReleasedEdge`.
- Owner-bound helpers now query exact owner registration keys for `IsKeybindDown` and `WasKeybindPressed`, so two mods using the same keybind id no longer read each other's transient keybind state through the helper.
- Remaining hot-path LINQ/temporary arrays in active registration iteration, inactive keybind cleanup, suppression checks, trigger-button lookup, and keybind list state checks were replaced with cached buffers and manual loops. Dirty-path canonical button list rebuilds still allocate the cached array by design.
- Win32 cached sampling keeps the prior held-key guard and is treated as a degraded fallback. Native-feel guarantees depend on Unity Input System `wasPressedThisFrame` / `wasReleasedThisFrame` when that backend is available.
- Smoke-only synthetic input now dispatches the same three-state keybind frame as the rebuilt hotkey layer. DebugConsole smoke no longer proves only legacy `ButtonPressed`, and AutoFishing movement-cancel smoke validates the snapshot path by holding A down for one runtime update.
- AutoFishing movement cancel keys were narrowed to the fishing-state movement set A/D/Space/Shift. W/S are not treated as current fishing-state movement-cancel inputs.

## Validation

- `dotnet build DTMAPI.sln -c Release` passed with 0 warnings and 0 errors.
- Console unit runner passed with `DOTNET_ROLL_FORWARD=Major`.
- Added coverage for chord short tap pressed+released ordering, release-edge cleanup, owner-bound same-id isolation, rapid retap behavior, and the legacy tracked-button path.
- User manual retest on 2026-07-08 passed AutoFishing, Y console, Zoom, AutoFishing F6 behavior, and return-to-title behavior.
- Short functional smoke `docs/debug/evidence/GAME-SMOKE/20260708-021606` passed `DebugConsole*`, `Zoom`, `AutoFishingHotkey`, and `AutoFishingMovementCancel`. The AutoFishing movement-cancel gate uses A.
- 50-key 20-minute pressure smoke `docs/debug/evidence/GAME-SMOKE/20260708-030543` passed with `SmokeVirtualInputPressure=Passed`, `SmokeVirtualInputKeyCount=50`, `SaveLoadCycle=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`; diagnostics kept `legacyRegisteredStringPath=false`, `getKeyDownCalls=0`, `getKeyCalls=0`, and `titleButtonsPolled=0`.
- Excluded runtime attempt `docs/debug/evidence/GAME-SMOKE/20260708-021859` is invalid for conclusions because Steam did not reach a fresh DTMAPI startup and the requested YConsole/Zoom profile was not active.
- Original unsuppressed YConsole+Zoom long route `docs/debug/evidence/GAME-SMOKE/20260708-032723` passed after 3600 seconds title idle and two post-idle save loads with no virtual keys and no input-root suppression.
- No-virtual long route with input diagnostics `docs/debug/evidence/GAME-SMOKE/20260708-042909` passed after 3600 seconds title idle and two post-idle save loads; diagnostics again showed `legacyRegisteredStringPath=false`, `getKeyDownCalls=0`, `getKeyCalls=0`, and `titleButtonsPolled=0`.
- Parser check, `git diff --check`, and optional `dotnet test` are recorded in the final task validation.

## Documentation

- Added manual review record `docs/reviews/manual-qa/2026/20260707-0002-hotkey-edge-followup-review.md`.
- Updated ISSUE-010 to classify the main-menu long-idle input-pressure path as mitigated while keeping the broader long-term gameplay/native GC class open.
- Added regression matrix row `INPUT-HOTKEY-EDGE-FOLLOWUP-20260707` and runtime closure row `INPUT-HOTKEY-EDGE-FOLLOWUP-RUNTIME-20260708`.

## Rollback Notes

- Reverting the `wasReleased`-aware keybind logic would reintroduce missed `Ctrl+F6` short taps and same-frame pressed/released keybinds.
- Reverting exact owner helper queries would allow same-id keybind state to leak between owner-bound helpers.
- Reverting the release-edge frame-state split would keep legacy `ButtonReleased` behavior but break typed `KeybindReleased` for short taps.

## Follow-Up

- AutoFishing movement cancel remains a snapshot consumer and was not redesigned to read native movement state in this update.
- ISSUE-010 should remain open for long-term gameplay/native GC crashes. Future crash packages should continue to collect Unity crash evidence and title-return/load breadcrumbs.
