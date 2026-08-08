# 20260710-0004: AutoFishing Input, Charge, and Smoke Polish

## Metadata

- Update ID: `20260710-0004`
- Date: 2026-07-10
- Status: source-unit-and-targeted-runtime-verified / manual-visual-and-config-retest-pending / issue-010-open
- Source: user manual QA after the first-party AutoFishing cutover
- Goal: `docs/goals/2026/20260710-0004-autofishing-input-charge-polish.md`
- Review: `docs/reviews/manual-qa/2026/20260710-0001-autofishing-input-charge-polish-review.md`
- Owner: DTMAPI Core, Bootstrap, Config UI, GameBridge Fishing Primitives, and first-party AutoFishing

## Summary

- Replaced AutoFishing's demand-local toggle query with one owner-bound `Gameplay` keybind registration and event. The binding is armed before input sampling, consumes same-frame short-tap edges, updates in place after config save, and is absent from title-scope sampling.
- Removed the registered-keybind steady-state allocation found during the migration: `InputRegistrationState.Key` is now cached instead of concatenated three times per frame, and Core evaluates registered keybind state directly without temporary delegates. A warmed 10,000-frame Core route allocates zero bytes on the test thread.
- Added the optional `IDtmConfigMenuKeybindDefaultsApi` capability without adding a member to the existing config-menu interface. AutoFishing uses it to show `Reset` to F6; older providers retain the existing keybind row, and Escape, Backspace, or Delete during capture still clears the binding.
- Primed Win32 transition state at the Capture click, skipped that Unity click frame, then scans all input backends beginning on the next frame. A key pressed before Capture and the Capture mouse click cannot become the new binding.
- Restored first-party `CastChargeRatio` as an independent 0–1 option. Each cast passes a scalar `FishingPrimitiveCastRequest` before native `UseFishRod`; charge is not part of the animation lease or bite/minigame decision state.
- Zero charge releases synthetic use input immediately so `SetPower` remains exactly at the minimum. Native `AgentStateFishingReady.NextState` still waits for `_isAnimationDone` on `fishing_ready`, preserving the backswing through the game's own animation gate. FastAnimations never changes the target or accelerates that backswing animator; for positive targets it may accelerate timer progress after the animation completes.
- Removed blocking sleeps from the fifth-save AutoFishing smoke. First-cast checks now advance once per Unity frame, custom toggle keys are used for both open and cleanup, and the movement-cancel gate waits for real physical movement when native `HorizontalMoveFactor` is available.
- Stopped FastAnimations from adding Ready timer progress after target release, reconciled a native Ready/Cast/WaitEntered exit back to an item-usable non-fishing state as `Interrupted` so a missed cast cannot leave the product stuck, and fixed external hotkey smoke to match from a pre-send log offset instead of sending a second toggle after missing an already-written line.

## User-Visible Impact

- Fast short presses should toggle AutoFishing once without requiring a long press or a second press.
- The title screen no longer polls AutoFishing's gameplay toggle.
- Key rebinding follows `click Capture -> press next key`; Reset restores F6.
- Cast charge is configurable from 0 to 1 in 0.05 steps, independent of InstantBite, SkipMiniGame, and FastAnimations.
- The automated fifth-save smoke no longer creates an artificial multi-second visual freeze on its first cast.

## Changed Files

- `first-party-mods/AutoFishingMod/`
- `src/DTMAPI.Abstractions/ConfigMenu.cs`, `Input.cs`, `FirstPartyFishingPrimitives.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuItems.cs`, `ConfigMenuRegistry.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs`, `ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/reviews/manual-qa/2026/20260710-0001-autofishing-input-charge-polish-review.md`
- `docs/goals/2026/20260710-0004-autofishing-input-charge-polish.md` and sibling `.goal.txt`

## Validation

- `DOTNET_ROLL_FORWARD=Major dotnet run -c Release --project tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj` passed with `DTMAPI.UnitTests: OK`, including post-release FastAnimations target preservation and native pre-Wait interruption reconciliation.
- Final `DOTNET_ROLL_FORWARD=Major tools/scripts/build.ps1 -Configuration Release` passed with zero build warnings/errors and `DTMAPI.UnitTests: OK`.
- Fifth-save Steam smoke `GAME-SMOKE/20260710-135136` passed with F7 external input, `DefaultLoop`, charge 0 exact release, visible native minigame completion, one extra soak loop, report export, cleanup, fatal-window check, and process exit. The loop recorded four applied casts, three Wait/Bite/MiniGame/Pull completions, then zero owner options/states, primitive sessions, input/animation leases, native transient handles, animators, and hook physics after F7 close.
- Partial fifth-save attempt `GAME-SMOKE/20260710-134805` passed F7 input/hotkey, positive target `0.5` release at progress `0.52`, FastAnimations, lifecycle, and clean process/fatal checks, but the configured endpoint did not enter the current pond fixture, so it is not cited as full-loop proof. It exposed the need for pre-Wait native-exit reconciliation. Earlier `134449` exposed post-release Fast timer drift; `134005` is excluded because the smoke script missed the first already-written F7 line and sent a second F7.
- PowerShell 5.1 parse, AutoFishing i18n JSON parse, and `git diff --check` passed.
- Manual retest remains required for rapid F6/F7 taps, Capture ordering, Reset/clear, charge 0/0.5/1 player-visible distance, and backswing visibility. The targeted runtime evidence does not replace those UI/visual gates.

## Evidence and Related Records

- Regression row: `AUTOFISHING-INPUT-CHARGE-POLISH-20260710`
- Supersedes the product-specific no-charge/no-registered-root decisions in `20260710-0003`; the compatibility fishing API and shared Harmony owner remain unchanged. The config-menu reset capability is a separate optional experimental interface, so existing `IDtmConfigMenuApi` implementers do not gain a new abstract member.
- ISSUE-010 stays open. This removes one measured Core allocation and title polling path; it is not an end-to-end Bootstrap/Unity zero-allocation or long-gameplay GC claim.
- Runtime evidence: `docs/debug/evidence/GAME-SMOKE/20260710-135136`; retained partial/failed diagnostic attempts: `134805`, `134449`, `134005`.

## Rollback

- Revert the first-party cast request and product config together; do not route the product back through `IFishingAutomationApi`.
- Revert the Gameplay registration only if short-tap/manual regression requires it; keep the cached registration key and direct state evaluator because they are general Core allocation fixes.
- Do not restore smoke `Thread.Sleep` loops on the Unity main thread.

## Follow-Up

- Run focused manual QA and fifth-save scenarios for charge 0/0.5/1, FastAnimations on/off, the independent and combined option matrix, and Capture/Reset.
- Run current-build 10/100/500-loop soak and broader Bootstrap/backend allocation diagnostics before changing ISSUE-010 status.
