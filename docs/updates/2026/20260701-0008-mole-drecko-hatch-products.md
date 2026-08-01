# 20260701-0008 Mole Drecko Hatch Products

## Source

User asked to use the remaining two local assets through subagents following the standalone custom-animal author guide:

- `mole` as a `marsh_pangolin` template animal, producing one native `meat` and one native `soil`;
- `drecko` as a `goat` template animal, producing one native `wool` and one native `meat`;
- Hatch product should return to native `meat`.

## Changed Files

- `E:\DolocTownUnity\DolocTownMeta\prototypes\mole\DTMAPI_MoleAssets\info.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\mole\DTMAPI_MoleAssets\Content\animal_tbanimal.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\mole\DTMAPI_MoleAssets\Content\animal_tbanimaldocument.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\mole\DTMAPI_MoleAssets\Content\item_tbitem.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\mole\DTMAPI_MoleAssets\Content\item_tbitemspawn.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\mole\DTMAPI_MoleAssets\Content\mod_tbmodstoreextension.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\mole\DTMAPI_MoleAssets\Content\DTMAPI\manifest.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\mole\DTMAPI_MoleAssets\Content\DTMAPI\dtmapi-package.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\mole\DTMAPI_MoleAssets\Content\DTMAPI\custom-animals.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\mole\DTMAPI_MoleAssets\Content\DTMAPI\audio-replacements.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\drecko\DTMAPI_DreckoAssets\info.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\drecko\DTMAPI_DreckoAssets\Content\animal_tbanimal.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\drecko\DTMAPI_DreckoAssets\Content\animal_tbanimaldocument.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\drecko\DTMAPI_DreckoAssets\Content\item_tbitem.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\drecko\DTMAPI_DreckoAssets\Content\item_tbitemspawn.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\drecko\DTMAPI_DreckoAssets\Content\mod_tbmodstoreextension.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\drecko\DTMAPI_DreckoAssets\Content\DTMAPI\manifest.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\drecko\DTMAPI_DreckoAssets\Content\DTMAPI\dtmapi-package.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\drecko\DTMAPI_DreckoAssets\Content\DTMAPI\custom-animals.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\drecko\DTMAPI_DreckoAssets\Content\DTMAPI\audio-replacements.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content\item_tbitemspawn.json`
- `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content\item_tbitem.json`
- `docs/updates/2026/20260701-0008-mole-drecko-hatch-products.md`
- `docs/updates/INDEX.md`

## Summary

- Added/filled `DTMAPI_MoleAssets` from existing Mole PNG/WAV assets.
- Mole uses `speciesId=mole`, `schedule_id/templateSpeciesId/aiTemplate=marsh_pangolin`, `pngSpriteOverride`, `templateSpritePrefix=anim_animal_marsh_pangolin`, and `customSpritePrefix=anim_animal_mole`.
- Mole product `mole_produce` uses `count_range 2..2` and two guaranteed rows: native `meat x1` plus native `soil x1`.
- Main review aligned Mole's first-pass body/economy values back to the native marsh-pangolin template values: `size=3`, `space=4`, `grow_interval=12`, `fertility_interval=72`, `breed_duration=1152`, `metabolism_interval=3`, `manual_metabolism=true`.
- Added/filled `DTMAPI_DreckoAssets` from existing Drecko PNG/WAV assets.
- Drecko uses `speciesId=drecko`, `schedule_id/templateSpeciesId/aiTemplate=goat`, `pngSpriteOverride`, `templateSpritePrefix=anim_animal_goat`, and `customSpritePrefix=anim_animal_drecko`.
- Drecko product `drecko_produce` uses `count_range 2..2` and two guaranteed rows: native `wool x1` plus native `meat x1`.
- Main review aligned Drecko's first-pass body/economy values back to the native goat template values: `size=2`, `space=3`, child/adult `sprite_size=20x30/34x38`, `grow_interval=12`, `fertility_interval=72`, `breed_duration=1152`, `metabolism_interval=9`, `manual_metabolism=true`.
- Hatch product `hatch_produce` now uses native `meat x1`; the no-longer-used custom `hatch_meat` item row was removed from Hatch `item_tbitem.json`.
- Synced Hatch, Oilfloater, Mole, and Drecko packages to the local game `MODS` directory for manual testing.
- Enabled local package entries `Local.DTMAPI_HatchAssets`, `Local.DTMAPI_OilfloaterAssets`, `Local.DTMAPI_MoleAssets`, and `Local.DTMAPI_DreckoAssets` in `SAVE\mod_infos.json`.

## Validation

Passed:

- Parsed 11 JSON files each under Hatch, Mole, Drecko, and Oilfloater package roots with PowerShell `ConvertFrom-Json`.
- Verified Mole and Drecko frame manifests reference existing PNG files:
  - Mole: 45 PNG files, `mole_frame_manifest.json` OK.
  - Drecko: 36 PNG files, `drecko_frame_manifest.json` OK.
- Confirmed native item IDs exist in the current reverse content configs: `meat`, `soil`, `wool`, and `coal`.
- Inspected product tables:
  - Hatch: `meat x1`.
  - Mole: `meat x1` + `soil x1`.
  - Drecko: `wool x1` + `meat x1`.
  - Oilfloater: `coal x1`.
- Ran `git diff --check`; it reported only LF/CRLF normalization warnings for edited Markdown files and no diff-check failures.
- Installed DTMAPI Release runtime to `D:\steam\steamapps\common\Doloc Town` with `tools\scripts\install-to-game.ps1 -Configuration Release -InstallAllDevOfficialMods`.
- Synced local content packages under `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS`:
  - `DTMAPI_HatchAssets`: 11 JSON, 44 PNG, 2 WAV.
  - `DTMAPI_OilfloaterAssets`: 11 JSON, 48 PNG, 2 WAV.
  - `DTMAPI_MoleAssets`: 11 JSON, 45 PNG, 2 WAV.
  - `DTMAPI_DreckoAssets`: 11 JSON, 36 PNG, 2 WAV.
- Ran `tools\scripts\check-dtmapi-status.ps1`; DTMAPI runtime and all four local content packs were detected.
- Ran slot-7 runtime smoke:
  - Command: `tools\scripts\run-game-smoke.ps1 -DirectExe -IncludeHookProbe -SaveSlot 7 -AutoReloadMods -AutoExerciseHatchAnimalVoice -TimeoutSeconds 300 -AutoExitAfterSecondsOverride 160 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260701-141821`
  - Result: `RunStatus=Passed`, `GameLaunched=Passed`, `StartupLog=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `HatchAnimalVoice=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
  - Logs indexed `DTMAPI.DreckoAssets`, `DTMAPI.HatchAssets`, `DTMAPI.MoleAssets`, and `DTMAPI.OilfloaterAssets` as `ContentPack`.
  - Logs verified custom animal hooks for AnimatorBridge, AiTemplateBridge, PngSpriteBridge, SleepWakeDiagnostics, and SleepTaskBoundary.
  - Logs loaded WAV replacements for Drecko, Hatch, Mole, Oilfloater, and ShellCrab.
  - Hatch smoke triggered child/adult native animal sounds and logged `played=True suppressed=True` for both `hatch_pet_young.wav` and `hatch_pet_adult.wav`.
  - Runtime negative chicken check was not applicable because the slot-7 smoke fixture did not find a vanilla chicken.
- Confirmed no remaining `DolocTown.exe` process and no fatal instance popup after smoke.

## Rollback

Remove `DTMAPI_MoleAssets` and `DTMAPI_DreckoAssets` from their prototype directories, restore Hatch `item_tbitemspawn.json` / `item_tbitem.json` from the previous Hatch prototype state, remove synced local copies from `MODS`, remove their local enablement entries from `SAVE\mod_infos.json`, then delete this update record and remove its row from `docs/updates/INDEX.md`.

## Follow-Up

- Manual-test slot 7 for Mole, Drecko, and Oilfloater spawn, pet/hit sound, sleep/wake, production equipment routing, and original-template isolation.
- Revisit Drecko and Mole `sprite_size` only after in-game interaction/selection feel is tested.
