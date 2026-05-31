# 20260531-0008 ActionSpeed Tool Animation Smoke

## Source Request / Goal

- Active goal: review and fix DTMAPI 0.1.12 player-visible issues without redoing completed official-local packaging, base localization, or fish roe display work.
- Target slice: make one real ActionSpeed gameplay path effective, or keep it clearly marked as experimental. This update verifies only the tool-animation path.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `testmods/ActionSpeedMod/ModEntry.cs`
- `testmods/ActionSpeedMod/i18n/schinese.json`
- `testmods/ActionSpeedMod/i18n/english.json`
- `tools/scripts/run-game-smoke.ps1`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/INDEX.md`

## Known Facts And Rejected Hypotheses

- Known: ActionSpeed config pages and hotkeys existed before this update, but that did not prove gameplay animation speed changed.
- Known: the migrated ActionSpeed mod should not own fragile Harmony/reflection logic directly; game-specific animator access belongs in `DTMAPI.GameBridge.DolocTown`.
- Known: ordinary DTMAPI mods remain outside `BepInEx/plugins`; formal install still packages migrated mods through the official local `MODS/Yuuka_DTMAPI_*` path.
- Rejected: treating config save or title-menu visibility as ActionSpeed gameplay evidence.
- Rejected: marking bottle fill, eat/drink, machine add, harvest, auto-fill, or continuous drink verified without matching in-game evidence.
- Rejected: copying or porting old DLKsmapi runtime behavior. The old ActionSpeed source was used only as compatibility context to identify intended player-facing features; the hook implementation is a DTMAPI GameBridge slice.

## Implementation

- Added experimental `IActionSpeedApi` and `ActionSpeedOptions` so ActionSpeed can register a policy with GameBridge instead of patching game internals itself.
- Patched `DolocTown.AgentStateTool.OnEnter` and `OnExit` through GameBridge callbacks.
- On tool-state enter, GameBridge checks the registered ActionSpeed policy, accepts axe/pickaxe/sickle tools, records original animator speeds, applies the configured multiplier to body/tool/tool-collider animators, and records hook status evidence.
- On tool-state exit and smoke cleanup, GameBridge restores captured animator speeds.
- Added `run-game-smoke.ps1 -AutoExerciseActionSpeedTool` to enter the third save, generate a smoke pickaxe, exercise the real `AgentStateTool` path, verify speed writes, and require clean exit checks.
- Updated ActionSpeed UI text to `动作加速（工具已验证/部分实验）` and kept non-tool features labeled experimental/pending.
- Updated runtime hook-status text so the DTMAPI Hooks page reports existing evidence accurately instead of saying verified hooks still need smoke proof.

## Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -TimeoutSeconds 240 -AutoExerciseActionSpeedTool -SkipBuild`
  - Passed.
  - Result: `docs/debug/evidence/GAME-SMOKE/20260531-044611/result.json`
  - Collected logs: `docs/debug/evidence/GAME-SMOKE/20260531-044654`
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\install-to-game.ps1 -SkipBuild`
  - Reinstalled formal player package layout.
  - Game `Mods` directory query returned no sample/test mods.
  - `Get-Process -Name DolocTown` returned no running process.

## Evidence

- Third-save load: `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`
- ActionSpeed policy: `ActionSpeed bridge policy OK reason=runtime-apply status=configured-verified-tool-hook`
- Tool speed write: `ActionSpeed tool animation speed applied by Yuuka.DTMAPI.ActionSpeed tool=old_pickaxe multiplier=3 animators=3.`
- Hook status: `Smoke.ActionSpeedTool = verified. owner=Yuuka.DTMAPI.ActionSpeed, tool=old_pickaxe, multiplier=3, animators=3, samples=body:1->3;tool-renderer:1->3;tool-collider:1->3`
- Restore: `ActionSpeed animator speeds restored reason=AgentStateTool.OnExit restored=3.`
- Hook status page source: `ActionSpeed.ToolAnimation = verified`, while non-tool ActionSpeed paths remain pending.
- Exit check: `docs/debug/evidence/GAME-SMOKE/20260531-044611/process-check.txt` says no `DolocTown.exe`.
- Fatal popup check: `docs/debug/evidence/GAME-SMOKE/20260531-044611/fatal-window-check.txt` says no fatal instance popup.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `ACTIONSPEED-001`, `CONFIG-004`, `CONFIG-005`
- `docs/hook-map/README.md`: `ActionSpeed.ToolAnimation`
- `docs/api/public-api-matrix.md`: `IActionSpeedApi.Configure/GetStatus`
- Earlier pending warning record: `docs/updates/2026/20260531-0003-pending-migrated-mod-ui-warnings.md`

## Rollback Notes

- Remove the `IActionSpeedApi` public surface and unregister it from GameBridge.
- Remove the `AgentStateTool.OnEnter/OnExit` Harmony patches and smoke exercise flag.
- Revert ActionSpeed UI labels to experimental/unsupported if gameplay evidence is invalidated.

## Follow-Up

- Bottle fill, eat/drink, machine add, harvest, auto-fill bottle, and continuous drink remain pending.
- A future slice should add separate evidence rows before any of those paths are described as verified.
