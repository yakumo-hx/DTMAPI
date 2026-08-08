# 20260706-0010 - Phase 8.19 YConsole / Zoom Pair Decomposition

Date: 2026-07-06 +08:00
Status: runtime-evidence-captured / singles passed / pair reproduced / issue-010-open

## Source Request

External review requested Phase 8.19:

- do not enter content/native-heavy root isolation yet;
- keep the Phase 8.18 light diagnostic route;
- run Zoom-only, then YConsole-only, then pair confirmation if both singles passed;
- stop on a reproduced pair fatal and move the next phase to input/event root-type isolation.

## Changed Files

- `docs/reviews/code/2026/20260706-0009-phase819-yconsole-zoom-pair-decomposition.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/debug/evidence/GAME-SMOKE/20260706-201209/ui-pair-decomposition-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260706-201209/managed-root-isolation-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260706-201209/validation-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260706-211430/ui-pair-decomposition-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260706-211430/managed-root-isolation-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260706-211430/validation-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260706-221651/ui-pair-decomposition-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260706-221651/managed-root-isolation-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260706-221651/validation-summary.txt`

No source files were changed in this phase.

## Runtime Evidence

- Zoom-only `docs/debug/evidence/GAME-SMOKE/20260706-201209`: passed.
- YConsole-only `docs/debug/evidence/GAME-SMOKE/20260706-211430`: passed.
- YConsole + Zoom pair confirmation `docs/debug/evidence/GAME-SMOKE/20260706-221651`: reproduced Fatal GC before SaveLoaded.

All runs used no HookProbe, no PreLoad GC, `SaveLoadObjectSnapshotMode=Lite`, `SmokeRootIsolationProfile=UiRuntime`, `SmokeNativeLoadContinuationProbe=VersionPatcher`, `CoreCustomAnimals`, the five non-UI base extras, and disabled AutoFishing, MoreSaves, and MoreEquipmentSlots.

## Result

Owner/root counters:

| Profile | Fatal | `ModOwner.records` | `EventHandler` | `InputButton` | `ConfigMenuPage` | `LoadedCodeMod` |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Zoom-only | no | 77 | 14 | 7 | 6 | 7 |
| YConsole-only | no | 75 | 15 | 4 | 6 | 7 |
| YConsole + Zoom pair | yes | 87 | 18 | 9 | 7 | 8 |

The pair confirmation reproduced on the first post-idle LoadGame with `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, `duplicateRequests=0`, and final breadcrumb `NativeContinuation.Step=DolocAPI.LoadGame.Enter`.

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings.
- `git diff --check`: passed with line-ending normalization warnings only.
- Runtime lock was acquired and released for each run.
- No leftover `DolocTown.exe`.
- Profiles were restored.
- `SaveLoadObjectSnapshotMode=Lite`, `SmokeRootIsolationProfile=UiRuntime`, and `SmokeNativeLoadContinuationProbe=VersionPatcher` were verified for each run.

## Classification

Zoom-only and YConsole-only each passed once, so neither single owner was sufficient in those samples. The pair reproduced twice under comparable conditions (`20260706-185612` and `20260706-221651`), making combined `YConsole + Zoom` input/event/config/code-owner pressure the current strongest suspect.

Next phase should isolate root type inside that pair instead of running UI triples or service hard-disable.

## Rollback

No runtime rollback is needed because this phase made no source or public API changes. The only new files are documentation and evidence summaries.

## Non-Changes

This phase did not:

- change public API;
- change ordinary player runtime behavior;
- run HookProbe;
- run PreLoad GC;
- run `SaveLoadObjectSnapshotMode=Off`;
- run UI triples;
- run service hard-disable;
- run content/native-heavy isolation;
- destroy unknown native Unity objects;
- mark ISSUE-010 solved.
