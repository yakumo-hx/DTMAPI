# Phase 8.8 Full Profile Fatal Window Capture

Date: 2026-07-05 +08:00
Status: source-and-runtime-evidence-captured / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 full known-failing profile fatal-window/root-set capture after Phase 8.7 UI single/pair split entered situation C.

## Summary

Phase 8.8 stopped UI owner bisection and ran only the full known-failing profile:

- `CoreCustomAnimals`
- action/utility owners: ActionSpeed, OneActionComplete, AnimalHusbandryProgress, FishBreedingAssistant
- Manbo audio
- all UI owners: YConsole, MoreEquipmentSlots, MoreSaves, Zoom
- AutoFishing disabled

The original failing sample `docs/debug/evidence/GAME-SMOKE/20260705-055655` used `IncludeHookProbe=True`, so Phase 8.8 retained `-IncludeHookProbe` for baseline parity.

The first no-probe full run passed, so the same no-probe profile was rerun after a clean process restart. The rerun reproduced `Fatal error in GC / Unexpected mark stack overflow` before `SaveLoaded`. The same profile with `-AutoExercisePreLoadGcProbe` then reproduced again, but the forced GC completed and the run reached `SaveLoaded` before the fatal popup.

Current classification:

- Continuous one-hour title idle plus full stable root-set can still reproduce the fatal class.
- A forced managed GC on the title screen did not crash in the PreLoad probe run.
- The fatal therefore was not proven to be "title stable root-set alone is sufficient" in this run.
- Because the PreLoad run still crashed after LoadGame/SaveLoaded, the stronger suspect is native LoadGame/save activation plus the full stable root graph, not UI owner growth and not duplicate LoadGame.

## Smoke Harness Change

Added one smoke-only safety knob:

- `tools/scripts/run-game-smoke.ps1 -FatalWindowCrashDumpGraceSeconds <seconds>`

Default is `0`, preserving ordinary smoke behavior.

When a fatal window is detected and the grace is nonzero, the harness now:

1. Immediately records fatal window state before closing the process:
   - `fatal-window-check.txt`
   - `fatal-window-live-check.txt`
   - `fatal-window-live-summary.txt`
   - `fatal-window-live-process.txt`
   - `fatal-window-dtmapi-log-position.txt`
2. Waits the configured grace period.
3. Runs live `collect-logs.ps1` while `DolocTown.exe` is still alive if possible.
4. Closes or kills the process.
5. Waits 20 seconds.
6. Runs a second post-close collect pass.
7. Appends `UnityCrashEvidencePhase=...;finalSource=...` to `summary.txt`.

Phase 8.8 used `-FatalWindowCrashDumpGraceSeconds 30` and `-TimeoutSeconds 6000`.

No ordinary player runtime behavior, owner cleanup, GameBridge service, or Unity object lifetime behavior changed.

## Profile Validation

All Phase 8.8 runs had exact enabled IDs:

- `Workshop.3742763309`
- `Workshop.3742763843`
- `Workshop.3742763540`
- `Workshop.3742763706`
- `Local.Yuuka_DTMAPI_ManboCardboardAudio`
- `Workshop.3742714442`
- `Workshop.3744059735`
- `Workshop.3742763050`
- `Workshop.3742717440`
- `Local.DTMAPI_ShellCrab`
- `Local.DTMAPI_HatchAssets`
- `Local.DTMAPI_OilfloaterAssets`
- `Local.DTMAPI_MoleAssets`
- `Local.DTMAPI_DreckoAssets`

`official-mod-profile-summary.json` validation:

| Evidence | Applied | Enabled count | Missing expected IDs | Unexpected enabled IDs | AutoFishing disabled |
| --- | ---: | ---: | --- | --- | --- |
| `docs/debug/evidence/GAME-SMOKE/20260705-184626` | true | 14 | none | none | true |
| `docs/debug/evidence/GAME-SMOKE/20260705-194759` | true | 14 | none | none | true |
| `docs/debug/evidence/GAME-SMOKE/20260705-205236` | true | 14 | none | none | true |

## Runtime Evidence

| Run | Evidence | Probe | Result | SaveLoad summary | Fatal/crash evidence |
| --- | --- | --- | --- | --- | --- |
| full no-probe attempt 1 | `docs/debug/evidence/GAME-SMOKE/20260705-184626` | skipped | Passed | `requests=2`, `nativeEnter=2`, `nativeReturn=2`, `saveLoaded=2`, `duplicateRequests=0`, `fatalWindows=0` | no fatal window |
| full no-probe attempt 2 | `docs/debug/evidence/GAME-SMOKE/20260705-194759` | skipped | Aborted/fatal | `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, `duplicateRequests=0`; active `SL-0001` at native enter | live fatal process `50648`, title `Fatal error in GC`; Unity crash `Crash_2026-07-05_124847467`; final source `live-collect` |
| full PreLoad GC probe | `docs/debug/evidence/GAME-SMOKE/20260705-205236` | passed | Aborted/fatal | `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=1`, `duplicateRequests=0`; forced GC completed before LoadGame | live fatal process `35116`, title `Fatal error in GC`; Unity crash `Crash_2026-07-05_135327389`; final source `live-collect` |

No-probe fatal details:

- `fatal-window-live-summary.txt`: detected at `2026-07-05T20:48:13.5594201+08:00`, grace `30`.
- `fatal-window-live-process.txt`: `ProcessId=50648; MainWindowTitle=Fatal error in GC; HasExited=False`.
- `fatal-window-dtmapi-log-position.txt`: last DTMAPI line was `LoadGame requested for slot/index 2. requestId=SL-0001.`
- `Unity-Crashes/summary.txt`: latest relevant crash directory `Crash_2026-07-05_124847467`, with `crash.dmp` and `Player.log` copied.

PreLoad fatal details:

- `PreLoadForcedGCProbe=Passed`.
- `BeforePreLoadForcedGC`, `PreLoadForcedGC`, `BeforeNextLoadGame`, `LoadGameNativeEnter`, and `SaveLoaded` snapshots were captured.
- `fatal-window-live-summary.txt`: detected at `2026-07-05T21:52:52.2277152+08:00`, grace `30`.
- `fatal-window-live-process.txt`: `ProcessId=35116; MainWindowTitle=Fatal error in GC; HasExited=False`.
- `fatal-window-dtmapi-log-position.txt`: last DTMAPI line was `TitleReturn object graph snapshot 17:SaveLoaded...`.
- `Unity-Crashes/summary.txt`: latest relevant crash directory `Crash_2026-07-05_135327389`, with `crash.dmp` and `Player.log` copied.
- Unity `Player.log` stack included `mono-2.0-bdwgc` frames such as `mono_gc_register_root`, `mono_runtime_invoke`, DTMAPI `DtmApiRuntime.NotifySaveLoaded(bool)`, and dynamic `DolocAPI::LoadGame`.

## Full Versus Phase 8.7 Threshold Table

| Profile | `ModOwner.records` | `EventHandler` | `InputButton` | `ConfigPage` | `LoadedCodeMod` | `GameBridge.featureById` | `AudioReplacement.platformPlayers` | `CustomAnimals.registrations` | Bootstrap roots | `UI.byOwner` root total | Fatal |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: | --- |
| 8.7 B + YConsole | 98 | 24 | 4 | 7 | 10 | 14 | 11 | 10 | `1/1` | 2 | no |
| 8.7 B + MoreEquipmentSlots | 93 | 21 | 2 | 7 | 10 | 14 | 11 | 10 | `1/1` | 2 | no |
| 8.7 B + MoreSaves | 92 | 20 | 2 | 7 | 10 | 14 | 11 | 10 | `1/1` | 2 | no |
| 8.7 B + Zoom | 100 | 23 | 7 | 7 | 10 | 14 | 11 | 10 | `1/1` | 2 | no |
| 8.7 B + YConsole + MoreEquipmentSlots | 103 | 25 | 4 | 8 | 11 | 14 | 11 | 10 | `1/1` | 2 | no |
| 8.7 B + YConsole + MoreSaves | 102 | 24 | 4 | 8 | 11 | 14 | 11 | 10 | `1/1` | 2 | no |
| 8.7 B + MoreSaves + MoreEquipmentSlots | 97 | 21 | 2 | 8 | 11 | 14 | 11 | 10 | `1/1` | 2 | no |
| 8.8 full no-probe pass | 119 | 28 | 9 | 10 | 13 | 14 | 11 | 10 | `1/1` | 2 | no |
| 8.8 full no-probe fatal | 119 | 28 | 9 | 10 | 13 | 14 | 11 | 10 | `1/1` | 2 | yes |
| 8.8 full PreLoad fatal | 119 | 28 | 9 | 10 | 13 | 14 | 11 | 10 | `1/1` | 2 | yes |

The full pass and full fatal rows have the same DTMAPI stable owner counts. This does not support a simple monotonic DTMAPI owner leak between those runs. It supports the current hypothesis that the full stable root graph crosses a pressure threshold where native LoadGame/save activation can intermittently trigger Mono GC mark-stack overflow.

## Interpretation

Ruled out or weakened by Phase 8.8:

- UI single-owner and high-value pair bisection as the next useful path.
- Duplicate DTMAPI/smoke LoadGame requests for the captured fatal windows.
- AutoFishing participation in the full profile, because `Local.Yuuka_DTMAPI_AutoFishing` was disabled in all three profile summaries.
- Title-stable forced managed GC alone as a sufficient trigger in the PreLoad run, because the probe completed.
- A single DTMAPI counter growing between full pass and full fatal, because the stable counts matched.

Still suspect:

- Native LoadGame/save activation over the larger full stable root-set.
- Managed Unity shell/native wrapper growth or activation during `DolocAPI.LoadGame`.
- Post-SaveLoaded DTMAPI/GameBridge callbacks as a trigger point for the PreLoad run, because the fatal appeared after `SaveLoaded` and before native `LoadGame` return.
- Idle heap/allocation/GC-threshold pressure as a contributing factor, because the same full no-probe profile was intermittent.

Next verification should focus on native/root-set analysis and service-level hard-disable only after comparable full-profile fatals are stable enough. Do not resume UI owner single/pair splits.

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings.
- `git diff --check`: pending final docs pass.
- Runtime lock was acquired and released for all Phase 8.8 runtime operations.

## Safety Notes

No unknown native `GameObject`, `Component`, `AudioClip`, `AssetBundle`, `RuntimeAnimatorController`, or Unity shell object was destroyed in this phase.

No GameBridge service hard-disable experiment was run. The full no-probe baseline reproduced once after one comparable pass, and the PreLoad repro is intentionally not the same baseline condition.
