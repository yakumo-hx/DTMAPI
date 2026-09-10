# 20260531-0013 OneAction Vegetation Exception Smoke

## Source Request / Goal

- Continue the DTMAPI 0.1.13 follow-up without redoing completed official-local packaging, base localization, fish roe display, animal bell display, title-button lifecycle, F6 input, instant-save, or ActionSpeed interaction work.
- Close the remaining OneAction vegetation/dandelion requirement by recording the real game path and verifying tool rules.
- Do not copy or imitate DLKsmapi source.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `testmods/OneActionCompleteMod/ModEntry.cs`
- `testmods/OneActionCompleteMod/i18n/english.json`
- `testmods/OneActionCompleteMod/i18n/schinese.json`
- `testmods/OneActionCompleteMod/README.md`
- `tools/scripts/run-game-smoke.ps1`
- `docs/debug/INDEX.md`
- `docs/debug/lessons.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260531-0013-oneaction-vegetation-exception-smoke.md`

## Known Facts And Rejected Hypotheses

- Known: decompiled build 23465763 shows `VegetationDandelion.OnFell(ItemTool, Vector2)` calls `Vegetation.CheckToolConstraints(ItemTool)`, then removes through the vegetation host.
- Known: `VegetationRenderer` implements `IFellable`, so `ToolCollider.HandleTools(Collider2D)` reaches vegetation through `VegetationRenderer.OnFell`.
- Rejected: treating dandelion/vegetation as a `DungeonResourceRenderer` one-action resource. It has no `ResourceFellData` path and should not receive DTMAPI forced damage.
- Rejected: hard-coding dandelion as sickle-only without evidence. The smoke reads `VegetationInfo.ToolConstraints` and records the expected tool type/min level in the log.

## Implementation

- Added a focused `AutoExerciseOneActionVegetation` smoke path.
- The smoke transitions from the third-save indoor farm room to the official main farm when needed.
- It creates/renders a transient dandelion vegetation sample through the game's vegetation host/renderer path.
- It exercises the same private `ToolCollider.HandleTools` path with:
  - wrong tool from a different tool type,
  - expected tool derived from `VegetationInfo.ToolConstraints`.
- It verifies wrong tools do not remove the vegetation and do not increment `OneActionApplicationCount`.
- It verifies the expected tool removes the dandelion through native `VegetationRenderer.OnFell` and still does not increment `OneActionApplicationCount`.
- OneAction menu/status text now describes vegetation/dandelion as a recorded native exception path rather than a pending resource-completion feature.

## Validation

- Build:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`, 0 warnings, 0 errors.
- Vegetation exception smoke:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -SaveSlot 3 -TimeoutSeconds 220 -AutoExerciseOneActionVegetation -SkipBuild`
  - Passed: `docs/debug/evidence/GAME-SMOKE/20260531-160900`, collected logs `docs/debug/evidence/GAME-SMOKE/20260531-160943`.
- Title-menu visual recheck:
  - `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -SaveSlot 0 -TimeoutSeconds 120 -AutoOpenTitleSettingsMenu -SkipBuild`
  - Passed: `docs/debug/evidence/GAME-SMOKE/20260531-161357`, collected logs/screenshots `docs/debug/evidence/GAME-SMOKE/20260531-161545`.

## Evidence

- `result.json` in `GAME-SMOKE/20260531-160900` has `OneActionVegetation=true`, `SaveLoaded=true`, `ProcessExited=true`, `NoFatalInstanceWindow=true`, and `ForcedClose=false`.
- `process-check.txt` in `GAME-SMOKE/20260531-160943` says `No DolocTown.exe process found.`
- Logs show the third save loaded slot/index 2 and transitioned from `farm_大型集装箱...` to `farm_type1-平地`.
- Logs show `target=dandelion/DolocTown.VegetationDandelion`.
- Logs show `path=ToolCollider.HandleTools->VegetationRenderer.OnFell->VegetationDandelion.OnFell->Vegetation.CheckToolConstraints`.
- Logs show expected tool `old_sickle`, expected type `SICKLE`, `expectedMinLevel=0`, wrong tool `old_pickaxe`, wrong type `PICKAXE`.
- Logs show `wrongRemoved=False`, `correctRemoved=True`, `oneActionDeltaWrong=0`, `oneActionDeltaCorrect=0`, and `resourcePath=DungeonResourceRenderer:none`.
- `docs/debug/evidence/GAME-SMOKE/20260531-161545/DTMAPI-evidence/UI-004/20260531-161433/title-settings-menu.png` shows the updated OneAction row as `一键完成（资源/加料已验证）`.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `CONFIG-005`, `ONEACTION-002`, `ONEACTION-003`
- `docs/hook-map/README.md`: `Actions.OneActionComplete`
- `docs/debug/lessons.md`: `2026-05-31: Dandelion Is Not A DungeonResource OneAction Target`
- Previous broader record: `docs/updates/2026/20260531-0011-runtime-behavior-013.md`

## Rollback Notes

- If a future Doloc Town build changes vegetation host creation, disable only `AutoExerciseOneActionVegetation` and keep the runtime OneAction resource/fuel/feed behavior intact.
- Do not move dandelion into the `ResourceFellData` completion path unless a future game build changes it to a real `DungeonResourceRenderer` resource.

## Follow-Up

- Startup 30-second abnormal comparison remains pending; current successful runs are normal startup samples, not an abnormal 30-second DTMAPI segment sample.
