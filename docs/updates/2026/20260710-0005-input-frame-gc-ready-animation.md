# 20260710-0005 Input Frame, GC, and Ready Animation

## Status

- Source/unit verified.
- Native Gameplay frame cadence and manual-assisted full loop verified on fifth save.
- Player manual functional/config/charge/visual scope passed; automated external-key sender unverified.
- `ISSUE-010` remains open.

## Source Request

The user asked to retain the newly found input/GC/Ready problems first, then implement a real per-frame input driver and edge latch, remove unbounded config audit growth and AutoFishing hot-path strings, and finally include the native Ready/charge stage in the first-party animation lease.

- Goal: `docs/goals/2026/20260710-0005-input-frame-gc-ready-animation.md`
- Review: `docs/reviews/manual-qa/2026/20260710-0002-autofishing-input-gc-ready-review.md`
- Previous handoff: `docs/updates/2026/20260710-0004-autofishing-input-charge-polish.md`

## Changes

### Per-frame input and edge latch

- `src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs`
  - Added stable watched-button latch state with frame generations and exact-once edge consumption.
  - Retains a press/release that occurs before Core drains the frame and prunes stale watches without rotating per-frame collections.
  - Compiles Input System bool property getters once, removing `PropertyInfo.GetValue` bool boxing from ordinary samples.
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
  - Added Input System after-update edge capture and a DTMAPI PlayerLoop node for title/non-normal fallback.
  - Timer fallback is health-check only and no longer dispatches ordinary Mod Update.
  - Added lifecycle refresh diagnostics and native Gameplay drain arbitration; PlayerLoop does not duplicate sampling while native Gameplay callbacks are current.
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`, `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge*.cs`, and `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
  - Added an internal-only `NativeGameFrame` notification from `DolocTown.NormalGameState.OnUpdate(float)` Postfix.
  - GameBridge owns the fragile hook; Bootstrap remains the single owner of input/Core/UI dispatch. No public API or raw game type was added.

### Bounded config/owner diagnostics

- `src/DTMAPI.ModConfigMenu/ConfigMenuPage.cs`
  - Applies/restores only changed preview values, audits once per apply/restore scope, and continues rollback after individual setter failures.
- `src/DTMAPI.Core/Runtime/ModOwnerLedgerService.cs` and `DtmApiRuntime.cs`
  - Aggregate successful preview observations by owner/item/kind/operation with total/first/latest counters.
  - Cap recent preview failure detail at 64 and the general ledger at 2048, retaining trimmed counts.
  - Publish full lifecycle summaries only on failure; the first success publishes one lightweight status.

### AutoFishing hot paths and Ready animation

- `first-party-mods/AutoFishingMod/ModEntry.cs`
  - Stores phase as an enum, builds movement reasons only after native movement crosses the threshold, and requests one Ready/Cast/Pull multiplier.
- `src/DTMAPI.Abstractions/FirstPartyFishingPrimitives.cs` and `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingPrimitivesService.cs`
  - Extended the internal first-party lease with an independently clamped Ready multiplier while retaining the internal two-value compatibility constructor.
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
  - Removed per-Ready-tick list/join/summary work, publishes pending/apply diagnostics once per state, and keeps the last summary reference stable on warm ticks.
  - Ready snapshots and scales body/rod animators, advances native charge timing at the same multiplier, and restores exact prior speeds at state/lifecycle cleanup.
  - Fast success status is once per flow rather than once per frame.
- First-party English/Chinese config text and README now describe the option as full fishing animation acceleration.

## Validation

- Release solution build: passed, 0 warnings, 0 errors.
- Console unit runner: `DTMAPI.UnitTests: OK`.
- Added focused tests for:
  - press+release latch retention and exact-once consumption;
  - 200 successful preview scopes aggregating into two stable rows without retained success entries;
  - 200 failed scopes retaining full counts but only 64 recent failures;
  - Ready warm ticks preserving the same diagnostic string reference;
  - independent Ready/Cast/Pull clamp and Ready animator `1.25 -> 5 -> 1.25` restoration.
- `git diff --check`: passed; repository line-ending warnings remain informational.

## Runtime Evidence

- `docs/debug/evidence/GAME-SMOKE/20260710-171352`
  - Retained partial run. It passed external AutoFishing input/hotkey, Ready/Cast animation speed, target `0.5`, process exit, and fatal-window gates. The scripted movement produced native `HorizontalMoveFactor=-1`, correctly cancelling automation before the complete loop. Cleanup returned Ready states, animators, and hook physics to zero.
- `docs/debug/evidence/GAME-SMOKE/20260710-172128`, `172756`, and `173218`
  - Retained failed implementation probes that respectively exposed InputSystem callback loss across load, PlayerLoop replacement by native LoadGame, and a non-running PlayerLoop after in-stack reinstall. They are root-cause evidence, not pass evidence.
- `docs/debug/evidence/GAME-SMOKE/20260710-173812`
  - `GameLoop.NativeFrameDrain = experimental`; 2394 Gameplay frames and 11968 button samples in 6.8 seconds, `nativeGameFrameCallbackSeen=True`, clean exit, no fatal window.
  - All three external F6 attempts were `SentExternal...Failed` before the game observed input, so this run proves frame cadence only and is not classified as a product F6 failure or short-tap pass.

## Related Records

- Debug issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- Debug index: `docs/debug/INDEX.md`
- Regression matrix: `INPUT-FRAME-GC-READY-20260710`
- Hook map: `Runtime.InputFrameDrain` and `Fishing.Automation`
- API matrix: Input, Config UI, and internal Fishing Primitives rows

## Rollback

- Remove the `NormalGameState.OnUpdate` Postfix and native frame notification to fall back to the retained PlayerLoop path; do not restore 250ms Gameplay polling.
- Ready can fall back to Cast/Pull-only by supplying Ready multiplier `1`, but charge target and exact speed restoration must remain intact.
- Do not roll diagnostics back to per-preview retained success entries; aggregation/caps are the safe floor.

## Follow-up

- Manually repeat ten approximately 40ms F6 and custom F7 taps, then one held-key toggle, in the fifth save.
- Visually compare Fast off/2/3/4 for Ready backswing plus charge timing, and recheck charge distances at 0/0.5/1.
- Run a current-build 100/500-loop soak before making any broader ISSUE-010 claim.

## Manual Retest Regression Fix

The first post-implementation player retest looked like AutoFishing never enabled. The current log proved the package and every early native option were active, but one physical F6 produced two `KeybindPressed` events 17ms apart: the first acquired a primitive session and the second immediately released it before the real key release.

- `ReflectedUnityInput` now accepts only one registered-button latch per Unity frame, reports duplicate-source attempts, and refuses a repeated backend pressed flag while the same physical cycle remains down without a release.
- First-party AutoFishing restores its release guard so even a future platform duplicate cannot toggle the product twice before `KeybindReleased`.
- Focused unit coverage verifies same-frame source rejection, held-repeat suppression, release, and a later valid press.
- `GAME-SMOKE/20260710-185323` is a manual-assisted F6 runtime pass: the player, not the smoke sender, pressed F6 during the run. After that input it passes complete DefaultLoop, three visible minigame/result cycles, one extra soak loop, second-F6 close, report export, fatal-window, and process-exit gates. The opening F6 has exactly one press and one release, but this sample does not verify automated key injection.
- `GAME-SMOKE/20260710-185537` is retained as partial combined evidence: full charge target 1 and Ready/Cast Fast pass, but the fixture's full-distance endpoint repeatedly misses water before Wait.
- `GAME-SMOKE/20260710-185857` is retained as behavior-pass/external-sender-gate-fail evidence: in-game `AutoFishingLoop OK` verifies InstantBite, Skip, Fast, two native skip reels, PullExit, and next cast at charge 0, while the script's separate external-input result remains Failed.
- The current development AutoFishing package and Bootstrap/GameBridge binaries were installed to the shared local runtime under the lock. Original user config (`charge=1`, Instant/Skip/Fast enabled, multiplier 3, F6) was restored after smoke.
- Final player retest passed the requested functional scope: AutoFishing is present and loops, individual settings work, charge adjustment works, and the overall manual matrix passes. This closes the immediate player-facing regression and the Ready/config/charge manual gates. Automated external-key sender reliability and ISSUE-010 100/500-loop or arbitrary long-gameplay gates remain separate.
