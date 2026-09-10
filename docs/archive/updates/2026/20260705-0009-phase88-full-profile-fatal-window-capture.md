# 20260705-0009 - Phase 8.8 Full Profile Fatal Window Capture

Date: 2026-07-05
Status: source-and-runtime-evidence-captured / issue-010-open
Area: smoke/saveload/issue-010/native-root-set/fatal-window

## Trigger

User requested Phase 8.8 after Phase 8.7 completed UI single/pair split without isolating a sufficient UI owner. The next step was to run only the full known-failing profile, preserve crash/root-set evidence, and classify the PreLoad GC probe result.

## Summary

Added `-FatalWindowCrashDumpGraceSeconds` to the smoke harness, defaulting to `0`, so fatal-window evidence can be collected in two stages without changing ordinary smoke behavior.

Ran the full known-failing profile with `-TimeoutSeconds 6000` and `-FatalWindowCrashDumpGraceSeconds 30`. The first no-probe full run passed, so the same no-probe profile was rerun and reproduced `Fatal error in GC / Unexpected mark stack overflow` before `SaveLoaded`. The same profile with `-AutoExercisePreLoadGcProbe` then reproduced again after the probe completed and after `SaveLoaded`.

## Changed Files

- `tools/scripts/run-game-smoke.ps1`
- `docs/reviews/code/2026/20260705-0004-phase88-full-profile-fatal-window-capture.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260705-0009-phase88-full-profile-fatal-window-capture.md`
- `docs/updates/INDEX.md`

## Runtime Evidence

Full profile, no PreLoad GC:

- `GAME-SMOKE/20260705-184626`: passed, `requests=2`, `nativeEnter=2`, `nativeReturn=2`, `saveLoaded=2`, `duplicateRequests=0`, `fatalWindows=0`.
- `GAME-SMOKE/20260705-194759`: reproduced fatal, `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, `duplicateRequests=0`.

Full profile with PreLoad GC:

- `GAME-SMOKE/20260705-205236`: reproduced fatal, `PreLoadForcedGCProbe=Passed`, `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=1`, `duplicateRequests=0`.

All three profile summaries were valid: all full-profile IDs plus the expected `CoreCustomAnimals` local animal packs were enabled, AutoFishing was disabled, and no unexpected enabled IDs were present.

Crash evidence:

- No-probe fatal: live fatal process `50648`; Unity crash directory `Crash_2026-07-05_124847467`; `UnityCrashEvidencePhase` final source `live-collect`.
- PreLoad fatal: live fatal process `35116`; Unity crash directory `Crash_2026-07-05_135327389`; `UnityCrashEvidencePhase` final source `live-collect`.

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings.
- Runtime lock was acquired and released for all game runs.
- `git diff --check`: passed with line-ending normalization warnings only.

## Decision

PreLoad forced GC did not crash on the title screen. The PreLoad run still crashed during the following LoadGame/save path, after `SaveLoaded` and before native `LoadGame` return. This classifies the current evidence as native LoadGame/save activation over the full stable root-set, not title-stable forced GC alone and not a sufficient single UI owner.

The full profile is intermittent under the no-probe baseline: one pass and one fatal under comparable conditions. Do not start service-level hard-disable experiments until comparable full-profile fatal reproduction is stable enough. Do not continue UI owner single/pair splits.

## Rollback

Revert this update to remove the smoke-only fatal grace parameter and docs. Ordinary runtime behavior is unchanged because the new parameter defaults to `0` and is only used by smoke commands.

## Follow-Up

- Analyze the new Unity crash dumps and `Player.log` stacks against the final `BeforeNextLoadGame`, `LoadGameNativeEnter`, `SaveLoaded`, and optional `PreLoadForcedGC` snapshots.
- Add native/root-set style counters if crash dump analysis still cannot identify a DTMAPI-owned root island.
- Only after at least two comparable no-probe full-profile fatals, run service-level hard-disable experiments.
- Do not destroy unknown native Unity objects; keep CustomAnimals, AnimalVoice, AudioReplacement, AssetBundle/controller, and Unity shell objects on count-first diagnostics until ownership is proven.
