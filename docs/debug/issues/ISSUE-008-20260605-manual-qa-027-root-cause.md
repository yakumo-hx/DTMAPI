# ISSUE-008: 2026-06-05 0.2.7 Manual QA Root-Cause Fixes

- State: `verified`
- Current boundary: Current record is smoke-verified.

## Current Status

- Opened: 2026-06-05 +08:00
- Target: DTMAPI 0.2.7
- Source: `readme.md` 0.2.7 implementation ledger, based on review records `20260605-0001` and `20260605-0002`.
- Boundary: Mine/Machine, AnimalHusbandryProgress, Y-console, and ConfigMenuExample packaging. The continuation explicitly did not execute motor smoke.

## Known Facts Before Changes

- 0.2.6 Mine tech/scale/Y-console cleanup was smoke-verified, but manual/code review still found root-cause gaps: production could initialize after a native time skip, Animal progress had overlapping post-render strategies, Y-console right-click could use a stale target, and console-open input suppression only covered DTMAPI hotkeys.
- `ITimeDebugApi.SkipToNextWeatherPeriod` already used native `ArchiveDataHandle.PassTimeNoControl`; the unsupported part was DTMAPI Machine API catch-up and placed Mine discovery.
- The ConfigMenuExample package is a dev test mod and should not appear in formal player-facing installs unless test mods are explicitly requested.

## Rejected Hypotheses And Shortcuts

- Do not accept `ForceMachineProductionDueForSmoke` as proof. 0.2.7 Mine evidence must use native pass-time plus catch-up.
- Do not treat a final Animal screenshot alone as flicker proof. The render path must be single-pass before native viewer display.
- Do not use the last hovered/left-clicked item as a global right-click fallback. The current pointer target must be hit-tested.
- Do not claim input isolation if only DTMAPI mod hotkeys are suppressed. Native backpack/tool/item entry points must also be swallowed while the modal is open.
- Do not run or cite motor smoke for this continuation.

## Manual QA Items

### 1. Machine API pass-time and Mine production

- Implementation: Machine runtime now enumerates current/root/archive/farm candidate rooms, schedules observed machines before native pass-time, processes bounded due cycles during catch-up, records due/nextDue, and retains due time on failed production. Smoke can force only the poll/observation path, not production due.
- Passing evidence: `GAME-SMOKE/20260605-231924` records native time skip `2-1-24 06:30 -> 18:00`, five `MachineProduction cycle OK` entries, `afterCycles=5`, `outputTarget=equipment-storage`, `storage=9/16`, `storageLineCapacity=4`, `ProcessExited=true`, and no fatal popup.
- Retained failure: `GAME-SMOKE/20260605-231408` failed with `nextDueTUs=39120` because the first observation was throttled until after native pass-time; this led to the poll-only smoke fix.

### 2. AnimalHusbandryProgress single-pass render

- Implementation: `AnimalFullInfoData` decoration now writes `moodInfo` and `moodProgress` for the selected special-produce row before the native viewer uses its mood progress bar; post-render cloned overlay rows are cleared instead of added.
- Passing evidence: `GAME-SMOKE/20260605-232019` logs `Smoke.AnimalViewerProgressUi = verified`, `single-pass native moodBar`, `moodInfo=羊毛脂 0/100`, `clonedOverlay=False`, `AnimalViewerUi=true`, `ProcessExited=true`, and no fatal popup.

### 3. Y-console right-click localization and native input isolation

- Implementation: the stale recent-target fallback was removed in favor of visible-cell mouse-coordinate hit testing; right-click status text has Chinese/English localization; `DebugConsoleModalOpen` drives GameBridge prefixes for native `AgentControllerState.EnterUICheck`, `UseTool`, and `UseItem`.
- Passing evidence: `GAME-SMOKE/20260605-230442` logs `UI.DebugConsoleInputIsolation = verified`, `AgentControllerState.EnterUICheck suppressed`, `Debug console right-click give hit-test item=suspicious_drink_4`, `source=mouse1-hit-test`, native backpack placement `requested=10 placed=10 before=1 after=11 success=True`, `DebugConsoleMouseGive=true`, `ProcessExited=true`, and no fatal popup.
- Retained failure: `GAME-SMOKE/20260605-225731` proved the pre-fix right-click path was still failing while left click and input isolation were already working.

### 4. ConfigMenuExample dev-only packaging

- Implementation/evidence: normal smoke installs were run without `-IncludeTestMods`. A read-only post-install directory check showed no `DTMAPI.ConfigMenuExample`; game `Mods` had no sample mod, official local `MODS` listed DTMAPI runtime packages only, and `BepInEx/plugins` contained only `DTMAPI`.

## Required Evidence

- Release build/unit with 0 errors.
- Third-save smokes for Y-console, Mine, and Animal.
- Process and fatal-popup checks for every passing smoke.
- Documentation updates in update record, debug index, smoke matrix, hook map, and public API matrix.

## Attempts

### 2026-06-05 / DTMAPI 0.2.7 implementation and validation

- Change: bumped controlled version sources to 0.2.7; added debug-console native input isolation; replaced stale right-click fallback with hit-tested pointer targeting; added Animal single-pass moodBar data path; expanded Machine production catch-up and forced poll-only smoke observation; confirmed ConfigMenuExample dev-only packaging.
- Build evidence: `tools/scripts/build.ps1` passed with 0 errors and UnitTests OK. `NU1900` warnings were from restricted NuGet vulnerability-index lookup.
- Y-console smoke: `docs/debug/evidence/GAME-SMOKE/20260605-230442`.
  - `summary.txt`: `AutoExerciseDebugConsoleMouseGive=True`, `AutoExerciseDebugInventory=True`, `AutoExerciseDebugWeather=True`, `AutoExerciseDebugTeleport=True`, `AutoExerciseDebugTime=True`, `AutoExerciseVehicle=False`.
  - `result.json`: `DebugConsoleMouseGive=true`, `DebugInventory=true`, `DebugWeather=true`, `DebugTeleport=true`, `DebugTime=true`, `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`.
- Mine smoke: `docs/debug/evidence/GAME-SMOKE/20260605-231924`.
  - `summary.txt`: `AutoExerciseMineContentApis=True`, `AutoExerciseVehicle=False`, `AutoExerciseNewContentApis=False`.
  - `result.json`: `NewContentMineApis=true`, `NewContentMineOfficialJson=true`, `NewContentMineOfficialTechTreeUi=true`, `NewContentMineProduction=true`, `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`.
- Animal smoke: `docs/debug/evidence/GAME-SMOKE/20260605-232019`.
  - `summary.txt`: `AutoOpenAnimalPanel=True`, `AutoExerciseVehicle=False`.
  - `result.json`: `AnimalViewerUi=true`, `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`.
- Exit evidence: all three passing smoke `process-check.txt` files say no `DolocTown.exe`.

## Result

Smoke-verified for the 0.2.7 implementation ledger. Manual gameplay can still add broader long-session confidence, but no current blocker remains for the automated acceptance slice.
