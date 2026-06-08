# 20260608-0010 GameBridge AnimalViewer Feature Split

## Metadata

- Update ID: 20260608-0010
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the AnimalViewer feature implementation from the large `DolocTownExperimentalBridgeApi.cs` file into `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/`.
- Kept hook installation, hook callback methods, public API shape, hook IDs, status strings, and log text unchanged.
- Moved only AnimalViewer-owned API methods, hook-installed flag setter, animal data decorators, progress overlay/render helpers, UI evidence helpers, husbandry/progress lookup helpers, AnimalViewer-only text helpers, and AnimalViewer-only nested runtime DTOs.
- Left shared Unity/reflection helpers, Hooking callbacks, smoke entry points, and unrelated runtime update calls in their existing files for this slice.
- Did not change AnimalHusbandryProgress behavior or AnimalViewer UI rendering semantics.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/DolocTownExperimentalBridgeApi.AnimalViewer.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0010-gamebridge-animalviewer-feature-split.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `git diff --check -- src/DTMAPI.GameBridge.DolocTown`
  - Only existing CRLF warning output was reported.
- Passed: `tools/scripts/build.ps1`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1`
  - Release build/test pass completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoOpenAnimalPanel -SkipBuild -TimeoutSeconds 240`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-073404`
- Startup evidence: `StartupLog=true` in `result.json`.
- HookProbe evidence: `HookProbe=true`, `SaveLoaded=true`, and log line `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
- Hook install evidence: logs show `Animals.ViewerRendering = verified. Patched animal viewer data construction plus prefilled independent progress rows before native Show settles, with real UI refresh evidence in ANIMAL-001.`
- Feature evidence: logs show `Animal viewer progress hook applied by Yuuka.DTMAPI.AnimalHusbandryProgress for goat/wool_grease current=0 threshold=100.`
- UI evidence: logs show `Animal viewer progress independent row active independent cloned ProgressBar prefilled rows=1, primary=羊毛脂 0/100, moodOverride=False, stateDescriptionOverride=False.`
- Real UI evidence: logs show `Animal viewer UI evidence OK owner=Yuuka.DTMAPI.AnimalHusbandryProgress title=角羊驼2 marker=羊毛脂 moodInfo=96/100 renderPath=independent-cloned-progressbar overlay=independent cloned ProgressBar prefilled rows=1, primary=羊毛脂 0/100, moodOverride=False, stateDescriptionOverride=False ... stateDescription=开心的动物能够提供产出。`
- Smoke evidence: logs show `Smoke.AnimalPanelUi = verified. Opened official AnimalPanel UI and observed progress text.` and `Smoke.AnimalViewerUi = verified. Observed animal progress text in the real animal viewer UI.`
- Evidence folder: `D:\Steam\steamapps\common\Doloc Town\DTMAPI\evidence\ANIMAL-001\20260608-073441`
  - `summary.txt` records `RenderPath=AnimalFullInfoData ctor -> AnimalViewer.Show independent cloned ProgressBar`, `MoodOverride=False`, `StateDescriptionOverride=False`, and the same overlay summary.
  - `animal-viewer-ui.png` and `animal-viewer-ui-delayed.png` exist in the folder, while the hook status for delayed screenshot remained `pending/unavailable`; the pass criterion for this refactor slice is the verified real UI status, not screenshot status.
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0009-gamebridge-fishroe-feature-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-FEATURE-ANIMALVIEWER-001`
- Existing regression case: `docs/debug/regressions/smoke-matrix.md` row `ANIMAL-001`
- Hook map: `docs/hook-map/README.md` row `Animals.ViewerRendering`

## Rollback Notes

- Move `DolocTownExperimentalBridgeApi.AnimalViewer.cs` contents back into `DolocTownExperimentalBridgeApi.cs`.
- No hook IDs, callback paths, status text, log text, public API semantics, or AnimalViewer behavior require rollback because this was a mechanical feature split.

## Follow-Up

- Continue moving one feature at a time into `Features/`.
- Keep AnimalViewer behavior or screenshot-capture changes separate from this mechanical split.
- Split Smoke and Diagnostics only after feature slices remain build- and smoke-clean.
