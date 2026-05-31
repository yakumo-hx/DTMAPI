# 20260531-0004: OneAction Resource-Hit Smoke Evidence

## Source Request

- Goal: continue DTMAPI 0.1.12 player-visible fixes without redoing completed official-local packaging, base localization, or fish roe display.
- Focus for this update: verify `Yuuka.DTMAPI.OneActionComplete` on a real resource/tool-hit path instead of treating config visibility as gameplay proof.

## Known Facts And Rejected Hypotheses

- Previous OneAction smoke attempts proved launch, save load, hook installation, fatal-window guard, and clean exit, but failed to find active `DungeonResourceRenderer` instances.
- Logs showed the third and fifth local saves loaded into `farm_大型集装箱...`, an indoor room with no dungeon resources.
- The failure was not evidence that `ToolCollider.HandleTools` or the OneAction postfix was broken; it was a smoke-world readiness problem.
- Direct `DolocTown.exe` launch remains rejected for smoke because it can produce the known fatal instance popup. This evidence uses the Steam launch path.

## Changes

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
  - Changed the OneAction smoke exercise from a single early attempt into a pending/succeeded/failed state machine.
  - Waits until the loaded game reaches normal gameplay state and the current room is rendered.
  - If the loaded room has no rendered resource, requests the official main-farm transition through `DolocAPI.EnterFarm`.
  - Re-renders existing room resources when needed.
  - Adds a smoke-only fallback that can create a transient game resource through `IDungeonResourceHost.CreateDungeonResource` if a rendered resource is still unavailable.
  - Keeps the evidence path on `ToolCollider.HandleTools(Collider2D)` and the Harmony postfix; it does not call the OneAction completion hook directly.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
  - Records OneAction application count and summary so smoke can prove the postfix ran after the normal tool hit.
  - Updates status text so the resource/tool-hit path is no longer shown as lacking gameplay evidence while fuel/feeder paths remain pending.
- `tools/scripts/run-game-smoke.ps1`
  - Supports `-AutoExerciseOneActionResourceHit`, temporary OneAction config enablement, and `OneActionResourceHit` pass/fail output.
- `testmods/OneActionCompleteMod/ModEntry.cs`
  - Updates the config page warning to say resource/tool-hit completion has third-save smoke evidence while fuel/feeder paths remain experimental.
- `testmods/OneActionCompleteMod/i18n/schinese.json`
  - Renames the page to `一键完成（资源已验证/部分实验）`.
- `testmods/OneActionCompleteMod/i18n/english.json`
  - Adds the matching English fallback text.
- Docs updated:
  - `docs/api/public-api-matrix.md`
  - `docs/debug/regressions/smoke-matrix.md`
  - `docs/hook-map/README.md`
  - `docs/updates/INDEX.md`

## Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/build.ps1`
  - Passed 2026-05-31.
  - `DTMAPI.UnitTests: OK`.
- OneAction resource-hit smoke:
  - Command: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/scripts/run-game-smoke.ps1 -TimeoutSeconds 240 -AutoExerciseOneActionResourceHit -SkipBuild`
  - Result: `docs/debug/evidence/GAME-SMOKE/20260531-032318/result.json`
  - Collected logs: `docs/debug/evidence/GAME-SMOKE/20260531-032401/DTMAPI-latest.log`
  - `StartupLog`, `GameLaunched`, `SaveLoaded`, `OneActionResourceHit`, `NoFatalInstanceWindow`, and `ProcessExited` are all true.
  - `ForcedClose` is false.
  - `process-check.txt` says no `DolocTown.exe` process found.
  - `fatal-window-check.txt` says no fatal instance popup found.

## Evidence

- Log lines in `docs/debug/evidence/GAME-SMOKE/20260531-032401/DTMAPI-latest.log`:
  - `Hook status: Actions.OneActionComplete = verified.`
  - `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`
  - `Current room has no rendered one-action resource; requested official main farm transition for smoke`
  - `from=type=TemplateRoomInHouse, roomId=farm_大型集装箱.d0136ec0-c673-4d80-be1d-ce21411937e2, ... resources=0`
  - `to=type=TemplateRoomOutdoor, roomId=farm_type1-平地, ... resources=46`
  - `One-action tool hook completed resource stone for Yuuka.DTMAPI.OneActionComplete damage=11.`
  - `Smoke exercise OneActionResourceHit OK owner=Yuuka.DTMAPI.OneActionComplete, resource=stone, tool=old_pickaxe`
- Smoke result:
  - `docs/debug/evidence/GAME-SMOKE/20260531-032318/result.json`
- Clean exit/fatal guards:
  - `docs/debug/evidence/GAME-SMOKE/20260531-032318/process-check.txt`
  - `docs/debug/evidence/GAME-SMOKE/20260531-032318/fatal-window-check.txt`

## Related Records

- Smoke matrix: `ONEACTION-001`
- Hook map: `Actions.OneActionComplete`
- Related failed evidence kept for regression context:
  - `docs/debug/evidence/GAME-SMOKE/20260531-030539`
  - `docs/debug/evidence/GAME-SMOKE/20260531-030908`
- First passing smoke before the final status-text cleanup:
  - `docs/debug/evidence/GAME-SMOKE/20260531-031809`
  - `docs/debug/evidence/GAME-SMOKE/20260531-031851`

## Not Fully Verified

- This verifies the resource/tool-hit OneAction path only.
- Machine fuel and feeder completion paths remain pending.
- ActionSpeed real animation impact remains pending.
- AutoFishing real fishing phase automation remains pending.

## Rollback

- Revert the OneAction smoke-readiness helpers in `DolocTownGameBridge.cs` to remove the main-farm transition and transient-resource fallback.
- Keep `ONEACTION-001` at least partial/pending if rolling back this evidence path.
- Do not move ordinary DTMAPI mods into `BepInEx/plugins`; this change stays in the DTMAPI runtime and smoke harness.

## Follow-up

- Build an AutoFishing smoke that verifies hotkey reachability, visible state, and at least one real fishing phase behavior.
- Build or downgrade ActionSpeed based on a real animation/timing test.
