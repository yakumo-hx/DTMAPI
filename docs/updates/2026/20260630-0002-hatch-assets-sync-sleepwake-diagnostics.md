# 20260630-0002 Hatch Asset Sync And Sleep Wake Diagnostics

## Summary

Synced the updated Hatch prototype package into the local Doloc Town `MODS` folder and added custom-animal-only sleep/wake diagnostics for Hatch and Shell Crab. This records the likely fixed eat-frame asset mismatch separately from the still-open nighttime wake root cause.

## Source Request

User reported that Hatch resources were updated with additional frames and asked to sync the mod and add diagnostics. Previous manual QA showed Hatch could fall back to chicken during eating because the local runtime package was stale, and Hatch/Shell Crab could sometimes stand up shortly after entering the barn at night.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/reviews/manual-qa/2026/20260630-0002-hatch-shellcrab-eat-sleep-review.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- Runtime content sync only: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_HatchAssets`

## Implementation Notes

- Synced `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets` to the local runtime `MODS\DTMAPI_HatchAssets` path under the shared runtime lock.
- Confirmed the runtime Hatch package now has 44 `anim_animal_hatch_*.png` files instead of the stale 38-file package.
- Confirmed runtime `animal_tbanimal.json` now has Hatch young `sprite_size` `32x26` and adult `36x34`.
- Added `CustomAnimals.SleepWakeDiagnostics` hook status and diagnostics scoped only to registered custom animal species.
- Patched `Animal.OnRender`, `Animal.Sleep`, `Animal.WakeUp`, `Animal.CallToRoom`, and `AnimalRenderer.OnFell` prefix/postfix for diagnostics. These callbacks log custom species, sleep/render/passing-time flags, stage, room/home, AI state/task, game time, and event-specific details.
- Left native behavior unchanged: the new hooks only record state and do not block wake, sleep, call-to-room, renderer recycle, or tool collision paths.

## Validation

- Passed: Hatch source content JSON `ConvertFrom-Json` for all JSON files under `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content`.
- Passed: `tools/scripts/test.ps1 -Configuration Release` with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings were emitted while querying NuGet vulnerability metadata.
- Passed: current DTMAPI runtime installed to local Doloc Town under the shared runtime lock.
- Passed: slot 7 DirectExe smoke `GAME-SMOKE/20260630-195052` with `RunStatus`, `StartupLog`, `GameLaunched`, `HookProbe`, `SaveLoaded`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose`.
- Evidence: `DTMAPI-latest.log` shows `CustomAnimals.SleepWakeDiagnostics = verified` and hook install flags `animalSleep=True`, `animalWakeUp=True`, `animalCallToRoom=True`, `animalRendererOnFellPrefix=True`, and `animalRendererOnFellPostfix=True`.
- Evidence: the same smoke has no `missing mapped sprite`, no `CustomAnimals.PngSpriteBridge.hatch = degraded`, and no leftover `DolocTown.exe`.

## Evidence Links

- Manual QA review/root cause: `docs/reviews/manual-qa/2026/20260630-0002-hatch-shellcrab-eat-sleep-review.md`
- Runtime smoke: `docs/debug/evidence/GAME-SMOKE/20260630-195052`
- Hook map: `docs/hook-map/README.md`
- Smoke matrix rows: `HATCH-PNG-CUSTOM-ANIMAL-20260630` and `CUSTOM-ANIMAL-SLEEPWAKE-DIAGNOSTICS-20260630`

## Rollback

Remove the diagnostic hook registrations and callback methods if they introduce runtime noise or Harmony patch instability. The Hatch content sync can be rolled back by recopying the prior runtime package, but that would reintroduce the known missing `eat_4..6` frame risk.

## Follow-Up

- Follow-up manual run confirmed Hatch direction and eating are fixed. Logs verify adult `eat_4`, `eat_5`, and `eat_6` map from chicken to Hatch sprites with no degraded Hatch sprite bridge status.
- Tool-caused wakeups are now classified: the log shows `AnimalRenderer.OnFell.Prefix ... tool=ItemTool:steel_sickle`, then `Animal.WakeUp` for Hatch and Shell Crab.
- Remaining open issue: the first-entry Shell Crab stand-up did not log `Animal.WakeUp`, `AnimalRenderer.OnFell`, or `Animal.CallToRoom`; it logged `sleep=true` but `aiState=Goat_FreeTimeState` at night. Next work should inspect why entering/render refresh can leave a sleeping custom animal in the template free-time AI state.
