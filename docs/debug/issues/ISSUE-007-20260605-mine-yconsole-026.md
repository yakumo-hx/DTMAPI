# ISSUE-007: 2026-06-05 0.2.6 Mine scale/research and Y-console reload/UI regressions

## Current Status

- Status: smoke-verified
- Opened: 2026-06-05 +08:00
- Target: DTMAPI 0.2.6
- Source: user manual QA feedback summarized in `readme.md`
- Boundary: only MineMod and the Y-key debug console were in scope. Do not use this record as permission to rework SecondMotor, Oil, MoreEquipmentSlots, ActionSpeed, AutoFishing, or AnimalHusbandryProgress.

## Known Facts Before Changes

- Mine 0.2.5 used a 2x visual path that could leak scale into unrelated equipment/chests/pots, and the placement preview path was not covered.
- Mine official research showed duplicate Mine unlock entries and still cost 30 research points.
- Y console exposed a reload action that saved and immediately loaded in the active save. Manual QA showed severe scene residue after using it.
- Y-console weather buttons were multi-row; language choices showed `schinese`; weather/teleport/category labels could expose hard-coded Chinese or raw internal ids; hover and first-open empty-search behavior still needed player-visible evidence.

## Rejected Hypotheses And Shortcuts

- Do not fix Mine by proving telemetry only. Player-visible renderer scale and non-Mine containment must be evidenced.
- Do not keep Y-console reload hidden under another label or as a player-facing experimental option. Save-only is acceptable; save-then-load is not.
- Do not run the broader `-AutoExerciseNewContentApis` as the focused Mine proof because it also executes Oil and EquipmentSlots smoke. Use `-AutoExerciseMineContentApis`.
- Do not run `-AutoExerciseVehicle` for this issue. The user explicitly said not to execute motor work after compaction.

## Manual QA Items

### 1. Mine scale containment and official research payload

- User-confirmed facts: Mine preview was not enlarged, unrelated equipment could enlarge, the tech UI listed Mine twice, and the unlock cost should be 1 point.
- Implementation: renderer scale is applied only when the actual equipment id is `dtmapi_mine`; pooled equipment renderers reset scale on reuse; placement preview hooks set Mine-only preview scale; native tech payload is recipe-only with no equipment unlock entry; research cost is 1.
- Acceptance evidence: `GAME-SMOKE/20260605-181224` logs `unlockEntries=recipe-only`, `equipmentEntries=0`, `recipeEntries=1`, `costValues=SCIENCE:1`, official TechTree UI screenshot evidence, `rendererScale=2x2`, `scaleContainment={containment=True, contamination=False, total=7, nonMineScaled=0}`, and production into Mine-owned storage.

### 2. Y-console reload removal, localization, hover, and search lifecycle

- User-confirmed facts: the reload button caused scene residue, weather buttons should be compact, `schinese` should not be player-facing, labels should localize, hover should work, and first save entry after restart should not restore stale search such as `石油`.
- Implementation: UI only exposes `存这里`; `IInstantSaveDebugApi.Save(... reloadAfterSave: true)` returns `reload-disabled`; weather buttons are one compact row; language display names use `中文`/`English`; weather/teleport/category labels are formatted by current language; hover tooltip records `Smoke.DebugConsoleHoverTooltip`; screenshot hover preparation no longer mutates search/source filters.
- Acceptance evidence: `GAME-SMOKE/20260605-181224` logs `reloadDisabled=True`, first open `searchText=<empty> category=<empty> sourceFilter=__base`, `Smoke.DebugConsoleHoverTooltip=verified`, and screenshot summary `HoverDetailTooltip=True` / `SearchText=`. The screenshot shows no reload button and one-row weather buttons.

## Required Evidence

- Release build/unit with 0 errors.
- Third-save smoke through Steam with DTMAPI startup, HookProbe `GameLaunched`/`SaveLoaded`, focused Mine-only content smoke, Y console open/close/mouse/hover/search evidence, instant save save-only evidence, teleport/weather evidence, and clean exit.
- Process and fatal popup checks.

## Attempts

### 2026-06-05 / DTMAPI 0.2.6 implementation and validation

- Change: bumped controlled version sources to 0.2.6; removed Y-console reload UI; disabled reload execution; added localization/display formatting; added Mine-only smoke harness; made Mine tech payload recipe-only with 1-point cost; added Mine renderer reuse and placement-preview containment hooks.
- Build evidence: `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- Third-save smoke: `docs/debug/evidence/GAME-SMOKE/20260605-181224`.
  - `summary.txt`: `AutoExerciseVehicle=False`, `AutoExerciseNewContentApis=False`, `AutoExerciseMineContentApis=True`.
  - `result.json`: `NewContentMineApis=true`, `NewContentMineOfficialJson=true`, `NewContentMineOfficialTechTreeUi=true`, `NewContentMineOfficialTechTreeUiScreenshotFile=true`, `NewContentMineProduction=true`, `DebugConsoleOpenY1=true`, `DebugConsoleCloseEscape=true`, `DebugConsoleCloseY=true`, `DebugConsoleMouseGive=true`, `DebugConsoleTenYShortTaps=true`, `DebugConsoleHoldYNoFlicker=true`, `InstantSave=true`, `DebugWeather=true`, `DebugTeleport=true`, `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`.
  - Mine log: `techPoint=1`, `unlockEntries=recipe-only`, `equipmentEntries=0`, `recipeEntries=1`, `costValues=SCIENCE:1`, `mode=mine-only`, `oilItemMetadata={skipped-mine-only}`, `equipmentSlots={skipped-mine-only}`, `rendererScale=2x2`, `scaleContainment={containment=True, contamination=False, total=7, nonMineScaled=0}`.
  - Y-console log: reset for `ReturnedToTitle` and `SaveLoaded slot=2 isNewGame=False`, first open `searchText=<empty>`, save-only instant save `reloadDisabled=True`, hover hook `Smoke.DebugConsoleHoverTooltip=verified`, Esc/Y close, ten short taps, and hold-no-flicker.
  - Visual evidence: `DTMAPI-evidence/DEBUG-CONSOLE-UI/20260605-181315/debug-console.png`; `DTMAPI-evidence/NEWCONTENT-025/20260605-181312/mine-official-tech-tree-dtmapi-mine.png`; `DTMAPI-evidence/NEWCONTENT-025/20260605-181312/mine-placed-dtmapi-mine.png`.
  - Exit evidence: `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.
- Result: smoke-verified. Official TechTreeUiState screenshot evidence is captured in the final smoke, alongside the native table payload proof for duplicate/cost fields.
