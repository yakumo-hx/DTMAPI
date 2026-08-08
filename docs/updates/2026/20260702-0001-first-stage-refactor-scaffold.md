# 20260702-0001 First-Stage Refactor Scaffold

## Status

source-smoke-user-manual-long-idle-verified / paper-box-automation-follow-up

## Source Request

User requested implementation of the guarded first-stage DTMAPI refactor scaffold: establish baseline records, add lifecycle observation, add a shadow content registry, add feature flags, and extend smoke result fields without changing runtime behavior for existing mods/content packs.

## Changed Files

- `docs/goals/2026/20260702-0001-first-stage-refactor-scaffold.md`
- `docs/goals/2026/20260702-0001-first-stage-refactor-scaffold.goal.txt`
- `docs/reviews/code/2026/20260702-0001-first-stage-refactor-baseline.md`
- `docs/debug/regressions/smoke-matrix.md`
- `src/DTMAPI.Core/Runtime/RefactorScaffoldOptions.cs`
- `src/DTMAPI.Core/Runtime/LifecycleObservationService.cs`
- `src/DTMAPI.Core/Runtime/ShadowContentRegistry.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/updates/INDEX.md`

## Implementation Notes

- Added an internal `RefactorScaffoldOptions` loader with defaults:
  - `LifecycleObservation=true`
  - `ShadowContentRegistry=true`
  - `ShadowResourceLoader=false`
  - `RegistryTakesOver=false`
- Options are read from `DTMAPI/config/refactor-scaffold.json`. Environment variables can temporarily override individual flags for smoke/CI without adding a public mod API.
- Added lifecycle observation counters for startup, title baseline, save load, second save load, return to title, log export, and shutdown.
- Lifecycle observation records phase, count, timestamp, managed thread id, save slot, new-game flag, loaded/discovered mod counts, and hook status count.
- Added a shadow content registry that reads only mods/content packs already discovered by the old scanner.
- The shadow registry summarizes manifest/source/enabled/type/entry DLL information, parses `Content/DTMAPI/custom-animals.json`, parses `Content/DTMAPI/audio-replacements.json`, checks key official JSON file shape, and formats diagnostics/diffs.
- Shadow diagnostics are published through existing logs, warnings, feature statuses, and runtime report context only. They do not alter mod loading, resource loading, hooks, custom animals, or audio behavior.
- Added smoke support for `-TitleIdleBeforeSaveSeconds` and new `result.json` fields while keeping schema version 2.
- The legacy paper-box audio smoke guard now allows either `-SaveSlot 8` or `-SaveSlot 10` because slot 10 no longer reliably positions the player on a paper-box interaction fixture.
- Added a smoke-only auto-load fallback after `ModChangeListUiState` confirmation: if the official mod-change confirmation is invoked but `SaveLoaded` is not observed within 15 seconds, the harness records diagnostics and falls back to the existing direct `DolocAPI.LoadGame` path. This does not affect player runtime behavior or public APIs.

## Validation

- Passed: `tools/scripts/test.ps1 -Configuration Release` on 2026-07-02.
  - `DTMAPI.UnitTests: OK`.
  - Warnings were restricted-network `NU1900` package vulnerability index warnings.
- New unit coverage verifies:
  - feature flag defaults and environment overrides;
  - all-scaffold-flags-off disables lifecycle and shadow execution;
  - lifecycle counter pure logic records phase counts;
  - shadow content registry parses good content-pack JSON and records bad JSON diagnostics.
- Existing diagnostics snapshot test was updated to assert feature-status presence without assuming an exact total feature count, since the scaffold intentionally adds internal diagnostic statuses.
- Short runtime smoke gate was attempted under the shared runtime lock on 2026-07-02, then stopped before long-idle/manual validation because slot 10 failed:
  - Passed: slot 3 lifecycle smoke `docs/debug/evidence/GAME-SMOKE/20260702-080518` with `RunStatus`, `StartupLog`, `GameLaunched`, `SaveLoaded`, `TitleButtonLifecycle`, `LifecycleObservation`, `ShadowContentRegistry`, `RefactorScaffoldFlags`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose` all `Passed`.
  - Passed: slot 7 Hatch AnimalVoice smoke `docs/debug/evidence/GAME-SMOKE/20260702-081159` with `RunStatus`, `StartupLog`, `GameLaunched`, `SaveLoaded`, `HatchAnimalVoice`, `LifecycleObservation`, `ShadowContentRegistry`, `RefactorScaffoldFlags`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose` all `Passed`.
  - Blocked: slot 10 legacy audio replacement smoke `docs/debug/evidence/GAME-SMOKE/20260702-081400` had `RunStatus=Failed` and `AudioReplacement=Failed`, while startup, launch, save load, lifecycle observation, shadow registry, scaffold flags, process exit, and fatal-window checks all passed. The log skipped `Yuuka.DTMAPI.ManboCardboardAudio` because the duplicate official/local and Workshop entries were both disabled by the existing official enablement state, so the smoke timed out waiting for the Manbo audio replacement ready line.
  - All three short-smoke evidence logs contained no `[Error]`, `[Fatal]`, `Fatal error in GC`, `Unexpected mark stack overflow`, duplicate-hook diagnostics, duplicate content-load diagnostics, or resource-growth diagnostics.
  - The scaffold stayed observation-only: `RefactorScaffoldFlagsSummary` recorded `LifecycleObservation=true; ShadowContentRegistry=true; ShadowResourceLoader=false; RegistryTakesOver=false`, and the shadow registry reported `diffs=0`.
- Follow-up on 2026-07-02 enabled the local official Manbo fixture and reran slot 10:
  - Refreshed the current Release runtime and developer official-local packages with `tools/scripts/install-to-game.ps1 -Configuration Release -SkipBuild`.
  - Updated `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\SAVE\mod_infos.json` entry `Local.Yuuka_DTMAPI_ManboCardboardAudio` from `enabled=false` to `enabled=true`, with backup `mod_infos.before-enable-manbo-smoke-20260702-083247.json`.
  - Steam slot 10 rerun `docs/debug/evidence/GAME-SMOKE/20260702-083259` loaded `Yuuka.DTMAPI.ManboCardboardAudio` from OfficialLocal, reached `AudioReplacement local WAV ready owner=Yuuka.DTMAPI.ManboCardboardAudio`, and still failed `AudioReplacement` because six real `E` attempts produced no `AudioReplacement paper-box OnInteract` and no replacement event.
  - DirectExe slot 10 rerun `docs/debug/evidence/GAME-SMOKE/20260702-083843` reproduced the same result: Manbo enabled/ready, save loaded, lifecycle/shadow/flags passed, but external `E` did not hit the paper-box native owner path.
  - The short-smoke gate remains blocked by the slot 10 paper-box interaction fixture, not by scaffold takeover or Manbo loading.
- User requested testing the paper-box path through the eighth save slot on 2026-07-02:
  - Automated slot 8 smoke `docs/debug/evidence/GAME-SMOKE/20260702-105351` loaded save slot/index `7`, passed startup/save/lifecycle/shadow/flags/process/fatal-window checks, reached Manbo WAV ready, and failed `AudioReplacement` because the smoke's six standing `E` attempts still did not trigger paper-box `OnInteract`.
  - A longer slot 8 observation run `docs/debug/evidence/GAME-SMOKE/20260702-105859` kept the game open for Computer Use. The automatic result still ended `RunStatus=Failed`/`AudioReplacement=Failed`, but manual positioning reached a visible `E 打开` prompt on the paper-box fixture and the runtime log recorded successful native/replacement evidence at 11:02:35, 11:02:46, and 11:02:48.
  - The successful manual evidence includes `AudioReplacement event owner=Yuuka.DTMAPI.ManboCardboardAudio replacement=manbo-paper-box event=PLAY_RESOURCE_PAPER_BOX played=True suppressed=True`, `AudioReplacement paper-box OnInteract owner=DungeonResourceModelPaperBox event=PLAY_RESOURCE_PAPER_BOX`, and `Audio.PaperBoxNativeOwner = verified`.
  - This preserves the conclusion that the Manbo audio replacement runtime path works; the remaining blocker is automating the paper-box fixture interaction reliably enough for a green `result.json`.
- User manual validation reported on 2026-07-02:
  - Slot 8 paper-box manual test is normal.
  - Slot 7 animal manual test is normal.
  - Screenshot-transcribed animal checks all passed: JSON + PNG + WAV animal generation; child/adult animations; movement, sleeping, and waking; AnimalVoice replacement; no native sound leak; return-to-title and re-enter-save behavior.
  - This user-visible evidence closes the player-behavior concern for slot 7/slot 8 first-stage validation. The remaining slot 8 issue is smoke-harness automation only: the automatic `AudioReplacement` field still needs a reliable fixture interaction before it can be green without manual input.
- Long title-idle validation on 2026-07-02:
  - First long-idle run `docs/debug/evidence/GAME-SMOKE/20260702-111813` used `-TitleIdleBeforeSaveSeconds 3600`. It kept the game on the title path past the 50-60 minute window without `Fatal error in GC`, `Unexpected mark stack overflow`, `[Error]`, or `[Fatal]`, and without repeated shadow registry scans or repeated audio ready loads. It failed `RunStatus`, `SaveLoaded`, and `LongTitleIdleBeforeSave` because the smoke selected slot 3 and confirmed `ModChangeListUiState`, but no `SaveLoaded` followed before the run was manually closed to collect evidence.
  - The smoke-only ModChange fallback was added after that failure, then `tools/scripts/test.ps1 -Configuration Release` passed again with `DTMAPI.UnitTests: OK` and only restricted-network `NU1900` warnings.
  - Final long-idle run `docs/debug/evidence/GAME-SMOKE/20260702-122940` passed with `RunStatus`, `StartupLog`, `GameLaunched`, `SaveLoaded`, `HookProbe`, `LifecycleObservation`, `ShadowContentRegistry`, `RefactorScaffoldFlags`, `LongTitleIdleBeforeSave`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose` all `Passed`.
  - The final run held title idle for 3600 seconds, selected slot 3 at 13:29:47, reached `SaveLoaded hook dispatched. slot/index=2 isNewGame=False` at 13:29:50, and recorded `Refactor lifecycle observation phase=SaveLoaded`.
  - Final long-idle log checks found no `Fatal error in GC`, no `Unexpected mark stack overflow`, no `[Error]`, no `[Fatal]`, `ShadowRegistryLines=4`, `AudioReadyLines=21`, `SaveLoadedLines=1`, and `RegistryTakesOver=false`.

## Game Smoke

Short-smoke validation was started under the shared runtime lock. The slot 3 and slot 7 gates passed. Slot 10 first failed because the required legacy Manbo audio fixture was disabled in the existing official/Workshop enablement state; after enabling the local official fixture, slot 10 still failed because the smoke's external `E` input did not trigger the expected nearby paper-box native interaction:

```powershell
tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseTitleButtonLifecycle -TimeoutSeconds 320 -SkipBuild
tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoExerciseHatchAnimalVoice -TimeoutSeconds 260 -SkipBuild
tools/scripts/run-game-smoke.ps1 -SaveSlot 8 -AutoExerciseAudioReplacement -TimeoutSeconds 260 -SkipBuild
tools/scripts/run-game-smoke.ps1 -SaveSlot 10 -AutoExerciseAudioReplacement -TimeoutSeconds 260 -SkipBuild
```

Per the verification plan, long-idle was initially held after the slot 10 short-smoke gate failure. After the user-directed slot 8 attempt, targeted Computer Use verified the paper-box audio path manually, and the user later confirmed both slot 8 paper-box and slot 7 custom-animal manual paths were normal. The automatic paper-box smoke still needs harness repair, but the next first-stage acceptance step is now the explicit long-title-idle gate.

The long-title-idle acceptance gate is explicit only:

```powershell
tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -TitleIdleBeforeSaveSeconds 3000 -TimeoutSeconds 3300
tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -TitleIdleBeforeSaveSeconds 3600 -TimeoutSeconds 3900
```

The 3600-second gate passed in `docs/debug/evidence/GAME-SMOKE/20260702-122940`.

## Evidence

- Baseline record: `docs/reviews/code/2026/20260702-0001-first-stage-refactor-baseline.md`.
- Smoke matrix pending gate: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-SCAFFOLD-LONG-TITLE-IDLE-20260702`.
- Goal handoff: `docs/goals/2026/20260702-0001-first-stage-refactor-scaffold.md`.
- Source validation: `tools/scripts/test.ps1 -Configuration Release`, 2026-07-02, `DTMAPI.UnitTests: OK`.
- Passed slot 3 lifecycle smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260702-080518`.
- Passed slot 7 Hatch AnimalVoice smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260702-081159`.
- Blocked slot 10 legacy audio replacement smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260702-081400`.
- Manbo enablement backup: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\SAVE\mod_infos.before-enable-manbo-smoke-20260702-083247.json`.
- Blocked slot 10 after enabling Manbo, Steam launch: `docs/debug/evidence/GAME-SMOKE/20260702-083259`.
- Blocked slot 10 after enabling Manbo, DirectExe launch: `docs/debug/evidence/GAME-SMOKE/20260702-083843`.
- Blocked slot 8 automatic paper-box smoke: `docs/debug/evidence/GAME-SMOKE/20260702-105351`.
- Slot 8 targeted paper-box observation with manual success in logs but failed automatic result: `docs/debug/evidence/GAME-SMOKE/20260702-105859`.
- Long title-idle first attempt, no GC/fatal but no SaveLoaded after mod-change confirmation: `docs/debug/evidence/GAME-SMOKE/20260702-111813`.
- Passed long title-idle gate after smoke-only ModChange fallback: `docs/debug/evidence/GAME-SMOKE/20260702-122940`.

## Rollback

Disable the scaffold without reverting old behavior by setting all values in `DTMAPI/config/refactor-scaffold.json` to `false`:

```json
{
  "LifecycleObservation": false,
  "ShadowContentRegistry": false,
  "ShadowResourceLoader": false,
  "RegistryTakesOver": false
}
```

For a source rollback, revert the added internal runtime files and the `DtmApiRuntime`/bootstrap/smoke-script wiring. No player-facing JSON fields or public APIs were introduced.

## Follow-Up

- Repair the paper-box interaction automation so slot 8 produces a green automatic `AudioReplacement=Passed` result without manual input. Slot 8 has confirmed manual native/replacement evidence, so it is the better fixture candidate.
- Preserve the user-manual slot 7 custom-animal and slot 8 paper-box pass as first-stage acceptance evidence unless a later smoke or long-idle run shows a regression.
- Keep watching the existing smoke quit behavior: long-idle requested game quit after save-loaded evidence, but the process still needed a normal `CloseMainWindow` to finish evidence collection. This did not leave `DolocTown.exe` running and did not affect the long-idle acceptance result.
