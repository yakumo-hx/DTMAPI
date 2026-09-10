# 20260707-0002 - Phase 8.21 YConsole vs Zoom Input Owner Isolation

Date: 2026-07-07 +08:00
Status: runtime-evidence-captured / ZoomNoInput passed twice / YConsoleNoInput passed twice / issue-010-open

## Source Request

External review requested Phase 8.21:

- keep the Phase 8.20 valid route unchanged;
- split the input-root suspect by owner with `ZoomNoInput` and `YConsoleNoInput`;
- keep both YConsole and Zoom enabled in the feature profile;
- remove only the selected owner's `InputButton` roots while retaining event handlers, config pages, and loaded code owners;
- repeat a passing profile once after clean restart before drawing conclusions;
- do not run PreLoad GC, `Off`, HookProbe, service hard-disable, UI triples, public API changes, GameBridge rewrite, or unknown native object destruction.

## Changed Files

- `docs/reviews/code/2026/20260707-0002-phase821-yconsole-vs-zoom-input-owner-isolation.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260707-0002-phase821-yconsole-vs-zoom-input-owner-isolation.md`
- `docs/debug/evidence/GAME-SMOKE/20260707-054818/validation-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260707-065046/validation-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260707-075645/validation-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260707-085818/validation-summary.txt`

No source/public API file changed in this phase.

## Runtime Evidence

Valid evidence:

- `docs/debug/evidence/GAME-SMOKE/20260707-054818`: `ZoomNoInput`, passed.
- `docs/debug/evidence/GAME-SMOKE/20260707-065046`: `ZoomNoInput` clean-process repeat, passed.
- `docs/debug/evidence/GAME-SMOKE/20260707-075645`: `YConsoleNoInput`, passed.
- `docs/debug/evidence/GAME-SMOKE/20260707-085818`: `YConsoleNoInput` clean-process repeat, passed.

All valid runs used DirectExe fallback, no HookProbe, no PreLoad GC, `SaveLoadObjectSnapshotMode=Lite`, `SmokeRootIsolationProfile=UiRuntime`, `SmokeNativeLoadContinuationProbe=VersionPatcher`, `3600s` continuous title idle, max two LoadGames, and the same `CoreCustomAnimals + base non-UI extras + YConsole + Zoom` profile.

## Result

`ZoomNoInput`:

```text
targetOwners=DTMAPI.ZoomMod
targetRootTypes=InputButton
removed={InputButton=5; EventHandler=0; ConfigPage=0}
DTMAPI.ZoomMod: InputButton 5 -> 0, EventHandler 3 -> 3, ConfigPage 1 -> 1, LoadedCodeMod 1 -> 1
```

`YConsoleNoInput`:

```text
targetOwners=DTMAPI.DebugConsoleMod
targetRootTypes=InputButton
removed={InputButton=2; EventHandler=0; ConfigPage=0}
DTMAPI.DebugConsoleMod: InputButton 2 -> 0, EventHandler 4 -> 4, ConfigPage 1 -> 1, LoadedCodeMod 1 -> 1
```

All four valid runs passed:

```text
requests=2
nativeEnter=2
nativeReturn=2
saveLoaded=2
duplicateRequests=0
fatalWindows=0
```

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: direct run built successfully but could not launch the `net8.0` test exe because the host lacks `Microsoft.NETCore.App 8.0.0`; rerun with `DOTNET_ROLL_FORWARD=Major` passed with `DTMAPI.UnitTests: OK`.
- `git diff --check`: passed with line-ending normalization warnings only.
- Runtime lock was acquired and released.
- No leftover `DolocTown.exe` after the run set.
- Profiles were restored in every run.
- AutoFishing, MoreSaves, and MoreEquipmentSlots were disabled.
- Owner-root suppression succeeded in every run with no unexpected remaining suppressed roots and no disabled code owners.
- `managed-root-isolation-summary.txt` and `ui-pair-decomposition-summary.txt` were not expected for this owner-root-type isolation phase.

## Classification

Both single-owner input suppressions passed twice. The current best classification is:

```text
YConsole and Zoom input roots both contribute to the observed pressure island; removing either side lowers this diagnostic route below the observed fatal threshold.
```

This is not a player-runtime fix and not proof that either owner is individually defective. DirectExe fallback and baseline intermittency remain caveats.

## Rollback

Docs/evidence-only update for Phase 8.21. No player runtime rollback is needed.

## Follow-Up

Inspect or instrument the input-root lifetime and callback captures for both `DTMAPI.DebugConsoleMod` and `DTMAPI.ZoomMod`. A future fix should preserve behavior while reducing long-lived input roots or stale callback captures, then rerun the original `YConsole + Zoom` pair without smoke suppression.

## Non-Changes

This phase did not:

- change source/public API;
- change ordinary player runtime behavior;
- run PreLoad GC;
- run `SaveLoadObjectSnapshotMode=Off`;
- run HookProbe;
- run service hard-disable;
- run UI triples;
- run content/native-heavy isolation;
- destroy unknown native Unity objects;
- mark ISSUE-010 solved.
