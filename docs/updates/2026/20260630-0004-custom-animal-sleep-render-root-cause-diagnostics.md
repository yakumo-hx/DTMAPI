# 20260630-0004 Custom Animal Sleep Render Root-Cause Diagnostics

## Summary

Added diagnostic-only follow-up sampling for the Hatch/Shell Crab nighttime stand-up investigation. This does not force sleep animation or change animal behavior; it records whether `AnimalRenderer.PlayAnimation("sleep")` and the next `AnimalRenderer.FixedUpdate` leave a sleeping custom animal on `idle`/pooled sprite state or settle into the `sleep` state.

## Source Request

User rejected a narrow behavior patch and asked to keep researching root cause. Recent slot 7 logs showed Hatch and multiple Shell Crabs entering the barn at midnight with `sleep=true`, template free-time AI states, and renderer state still at `idle@0` with a pooled `grassslime_eat_0` sprite, without `WakeUp`, `OnFell`, or `CallToRoom`.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `docs/reviews/manual-qa/2026/20260630-0002-hatch-shellcrab-eat-sleep-review.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Implementation Notes

- Added `AnimalRenderer.PlayAnimation` postfix diagnostics scoped to registered custom animals and `animName=sleep`, logging the renderer state immediately after native `PlayAnimation`.
- Added `AnimalRenderer.FixedUpdate` postfix diagnostics that only log renderers previously marked by a suspicious sleeping `Animal.OnRender`; the follow-up entry is cleared after the first fixed update.
- Suspicious follow-up tracking is limited to `sleep=true` custom animals where the renderer is not already in `animState=sleep` or the AI state is not `Normal_SleepState`.
- `AnimalRenderer.OnRecycle` clears pending follow-up state to avoid pooled renderer contamination in diagnostics.
- A previous narrow stabilizer idea was not kept in source and was not installed to the game runtime.

## Root-Cause Notes

Native review points at a timing/lifecycle mismatch rather than a confirmed wake call:

- `Animal.OnRender()` assigns the renderer controller, calls `Renderer.PlayAnimation(_GetCurrentAnimation())`, and `_GetCurrentAnimation()` returns `sleep` when `isSleep` is true.
- `AnimalRenderer.OnRecycle()` clears movement/eating/jump flags and sleep effects, but does not clear the current sprite or runtime animator controller.
- `Animal.__OnDayChanged()` refreshes AI, which explains the observed template free-time AI state at midnight while `isSleep` remains true.
- The new diagnostics should distinguish whether the visible stand-up is only the first render frame holding pooled `idle`/sprite residue, or whether the renderer remains on `idle` after the next fixed update.

## Validation

- Passed: `tools/scripts/test.ps1 -Configuration Release` with `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/build.ps1 -Configuration Release`.
- Passed: `git diff --check`; only existing CRLF normalization warnings were reported.
- Installed current Release runtime to the local Doloc Town directory under runtime lock with `tools/scripts/install-to-game.ps1 -Configuration Release -SkipBuild -SkipOfficialLocalMods`, then released the lock.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -SaveSlot 7 -TimeoutSeconds 240` under runtime lock. Evidence: `docs/debug/evidence/GAME-SMOKE/20260630-205912`.
- Smoke evidence verified `StartupLog`, `GameLaunched`, `SaveLoaded`, `HookProbe`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose`; `process-check.txt` reports no `DolocTown.exe`.
- Startup log verified `animalRendererPlayAnimation=True`, `animalRendererFixedUpdate=True`, and `diagnosticSpecies=hatch,shell_crab`.
- After the smoke, removed the smoke-installed legacy test mod directories `DTMAPI.HookProbeMod`, `DTMAPI.HelloDtmMod`, and `DTMAPI.ConfigMenuExample` under runtime lock. `tools/scripts/check-dtmapi-status.ps1` then reported required install files present, `DTMAPI_HatchAssets` and `DTMAPI_ShellCrab` present, and no legacy DLK/SMAPI items detected.
- Warnings: restricted-network `NU1900` package vulnerability index warnings while querying NuGet metadata.

## Manual Follow-Up Evidence

- Slot 7 manual logs from 2026-06-30 21:12 show the last two reproductions no longer point to a gameplay wake source. The affected custom animals remain `sleep=true`; no same-window `Animal.WakeUp`, `AnimalRenderer.OnFell`, or `Animal.CallToRoom` is logged.
- After a debug skip from 2-2-15 18:10 to 2-2-16 00:00, all three Shell Crabs plus Hatch entered `Animal.OnRender` with a stale renderer snapshot (`animState=idle@0`, `sprite=grassslime_eat_0`) while still `sleep=true`. The next `AnimalRenderer.FixedUpdateAfterRender` 24-28 ms later shows all four settled to `animState=sleep`.
- A later re-entry records `AnimalRenderer.PlayAnimation("sleep")` for Shell Crab 3 with `afterPlayRendererState=idle@0`; 12-13 ms later the same renderer is `sleep@0`. This confirms native `PlayAnimation("sleep")` does not make the immediately sampled renderer state reliable in the same tick.
- A second debug skip from 2-2-16 18:00 to 2-2-17 00:00 shows the same pattern: normal `Animal.Sleep` at 19:10/19:20, then midnight/re-entry follow-up samples already settled to `sleep@0` even though their stored on-render snapshot was `idle@0`.
- Correction after user clarification: the observed animal can remain visibly awake and may move a short distance. The previous one-frame follow-up only proves the first sampled fixed update settled to `sleep`; it does not prove the animal stayed asleep afterward. Current evidence still excludes explicit `WakeUp`, `OnFell`, and `CallToRoom` as the logged source, but it does not exclude a later `sleep=true` + free-time task mismatch.

## Extended Diagnostics

- Changed `AnimalRenderer.FixedUpdate` diagnostics from a one-shot follow-up into a bounded 180-frame follow-up for suspicious custom animals. It logs on state signature changes and at sample frames 1/10/30/60/120/180.
- Added logging for non-`sleep` `AnimalRenderer.PlayAnimation` requests while a suspicious sleeping custom animal is being followed, so a later `idle` or `move` animation can be tied to the same `animal=ref/index/dataIdx/cell`.
- Each follow-up sample still includes `sleep`, `aiState`, `task`, `cell/ws`, `rendererPos`, renderer moving/eating/jump flags, `animState`, and sprite name.
- New working hypothesis: the remaining issue may be sleep flag vs AI state/task desynchronization after midnight/day-change/room entry, not just a renderer first-frame artifact.
- Extended diagnostics validation: `tools/scripts/test.ps1 -Configuration Release` and `tools/scripts/build.ps1 -Configuration Release` passed with only existing `NU1900` package vulnerability index warnings. `git diff --check` passed with CRLF normalization warnings only.
- Runtime validation: installed the Release build under runtime lock and ran `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -SaveSlot 7 -TimeoutSeconds 240`; evidence `docs/debug/evidence/GAME-SMOKE/20260630-212844` passed `StartupLog`, `GameLaunched`, `SaveLoaded`, `HookProbe`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose`.
- Startup log for `GAME-SMOKE/20260630-212844` verifies `CustomAnimals.SleepWakeDiagnostics=verified`, `animalRendererPlayAnimation=True`, `animalRendererFixedUpdate=True`, and `diagnosticSpecies=hatch,shell_crab`. The smoke did not manually reproduce the midnight barn stand-up; it only validates the extended diagnostics install/startup path.
- After smoke, removed smoke-installed legacy test mod directories `DTMAPI.HookProbeMod`, `DTMAPI.HelloDtmMod`, and `DTMAPI.ConfigMenuExample` under runtime lock. Follow-up status reports no legacy DLK/SMAPI items and no leftover `DolocTown.exe`.

## Root-Cause Recheck

- User clarification: the visible animal is not only a one-frame flash; a specific custom animal can stay awake/standing and may move a short distance.
- The earlier first-frame renderer-artifact conclusion is therefore too narrow. Same-tick renderer sampling still explains stale `idle`/`grassslime_eat_0` snapshots, but not the sustained/move symptom.
- Re-reading `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\Player-prev.log` found a concrete task owner: 2026-06-30 21:12:48.886 logs Hatch at 00:05 with `sleep=true`, `period=Night`, `aiState=Normal_SleepState`, and `task=DolocTown.AnimalMove`. The three Shell Crabs in the same sample have `task=none`.
- Native code review explains the path: `Animal.__OnDayChanged()` recreates `AnimalController`; `RedSaw.AI.StateMachine.StateMachine.Update()` sets the default FreeTime state on the first null-state tick and returns before evaluating the night/sleep guard; `DecisionMaker.Update()` can then call FreeTime `MakeDecision()` while `CurrentTask == null`; `MakeDecision_FreeTime()` has no `isSleep` guard and can return random `WanderEx()` movement; later transition to `Normal_SleepState` does not automatically break the existing current task.
- `Animal.Move()` has no `isSleep` guard and plays `"move"` through the renderer, so an asleep animal with an `AnimalMove` current task can visibly stand/move without any `Animal.WakeUp`, `AnimalRenderer.OnFell`, or `Animal.CallToRoom` event.
- Current classification: sleep flag vs AI/current-task lifecycle desynchronization exposed by custom template animals after midnight/day-change/room-entry. Renderer state residue is a secondary symptom, not the sustained-move root cause.

## Rollback

Remove the `AnimalRenderer.PlayAnimation` and `AnimalRenderer.FixedUpdate` diagnostic hooks and the `sleepRenderFollowUps` state if the next manual logs prove enough root cause evidence. Do not replace this with a force-play sleep workaround unless logs prove native render sampling is the true owner and the behavior impact is reviewed separately.

## Follow-Up

Next implementation pass should review a custom-species-only sleep/task gate rather than a renderer stabilizer. Candidate gates must prevent FreeTime `WanderEx()` / `AnimalMove` from running while a registered custom animal is `sleep=true` during Night, or replace/break such movement tasks without touching vanilla animals or the normal morning `Normal_SleepState.OnExit -> WakeUp` path. Runtime validation should still include a no-tool midnight barn-entry retest and prove no `sleep=true task=DolocTown.AnimalMove` persists for Hatch/Shell Crab.
