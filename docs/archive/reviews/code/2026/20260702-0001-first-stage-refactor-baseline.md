# 20260702 First-Stage Refactor Baseline

## Purpose

Freeze the behavior that the first guarded refactor scaffold must preserve. This is a baseline record, not a migration design and not evidence that the long-idle crash is fixed.

## Refactor Guardrail

The first stage is observation-only and shadow-only:

- Do not replace custom animal runtime behavior.
- Do not replace `AnimalVoice` audio replacement behavior.
- Do not move or rewrite Hook patch installation.
- Do not change JSON field semantics.
- Do not change content-pack paths.
- Do not change player-visible mod behavior.

## Currently Working Content Packs

The current content-pack baseline includes:

- `DTMAPI_HatchAssets`
- `DTMAPI_ShellCrab`
- `DTMAPI_Mole`
- `DTMAPI_Drecko`
- `DTMAPI_Oilfloater`
- ordinary official/local content pack roots discovered through the existing DTMAPI scanner and Doloc Town `MODS` paths

Recent supporting records:

- `docs/updates/2026/20260701-0008-mole-drecko-hatch-products.md`
- `docs/updates/2026/20260701-0015-animalvoice-cooldown-native-suppression.md`
- `docs/debug/regressions/smoke-matrix.md`

## CustomAnimals Success Path To Preserve

The known-good custom animal route is:

1. Existing scanner discovers the content pack.
2. Runtime reads `Content/DTMAPI/custom-animals.json`.
3. Runtime reads official animal JSON files from the same content root.
4. Runtime reuses a native animal AI template such as chicken, goat, sheep, pangolin, or honey amoeba.
5. Runtime resolves either PNG sprite overrides or AssetBundle animator metadata.
6. Runtime registers native table rows and production rows.
7. Native animal bag/release/growth/AI/renderer paths continue to own live animal behavior.

Fields and files to preserve as an external contract:

- `Content/DTMAPI/custom-animals.json`
- official animal JSON files such as `animal_tbanimal.json` and related native table extension JSON
- PNG sprite override paths
- AssetBundle animator metadata paths
- `speciesId`, `templateSpeciesId`, `aiTemplate`, `animatorMode`, production row references, and existing tolerated optional fields

## AnimalVoice Success Path To Preserve

The known-good `AnimalVoice` route is:

1. Existing scanner discovers the content pack.
2. Runtime reads `Content/DTMAPI/audio-replacements.json`.
3. Runtime registers `AnimalVoice` replacement entries scoped by custom animal, life stage, and native sound event.
4. Runtime loads WAV files from the existing content path.
5. `Animal.PlayAnimalSound` context scopes the later Wwise event.
6. `nativeSoundEvent`, `stage`, `suppressNativeWhenReady`, and cooldown suppression preserve current behavior.

Fields and files to preserve as an external contract:

- `Content/DTMAPI/audio-replacements.json`
- `AnimalVoice`
- `stage`
- `nativeSoundEvent`
- WAV path fields
- `suppressNativeWhenReady`
- `cooldownMilliseconds`

Recent supporting records:

- `docs/updates/2026/20260701-0001-hatch-animal-voice-audio-replacement.md`
- `docs/updates/2026/20260701-0002-shell-crab-animalvoice-json.md`
- `docs/updates/2026/20260701-0015-animalvoice-cooldown-native-suppression.md`

## Smoke Commands To Keep Stable

Automatic source/build validation:

```powershell
tools/scripts/test.ps1 -Configuration Release
```

Runtime smoke baselines, run only under the shared runtime lock:

```powershell
tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -TimeoutSeconds 220
tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoExerciseHatchAnimalVoice -TimeoutSeconds 260
tools/scripts/run-game-smoke.ps1 -SaveSlot 10 -AutoExerciseAudioReplacement -TimeoutSeconds 260
```

Long title-idle validation is explicit only and must not become part of the default per-change smoke matrix:

```powershell
tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -TitleIdleBeforeSaveSeconds 3000 -TimeoutSeconds 3300
tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -TitleIdleBeforeSaveSeconds 3600 -TimeoutSeconds 3900
```

## Baseline Result Format

`run-game-smoke.ps1` keeps `result.json` `SchemaVersion=2` and now records the first-stage scaffold fields without changing old result fields:

- `LifecycleObservation`
- `ShadowContentRegistry`
- `RefactorScaffoldFlags`
- `RefactorScaffoldFlagsSummary`
- `LongTitleIdleBeforeSave`
- `TitleIdleBeforeSaveSeconds`

## Long-Idle Crash Acceptance Item

The "main menu idle for about 50-60 minutes, then load a save" crash remains a release-blocking acceptance item for the layered refactor. The first-stage scaffold does not claim a fix; it adds a controlled smoke switch and result fields so the issue can be verified with:

- shared runtime lock acquired;
- third save loaded after title idle;
- clean exit;
- no `Fatal error in GC`;
- no `Unexpected mark stack overflow`;
- no duplicate Hook diagnostics;
- no duplicate content load diagnostics;
- no resource-growth diagnostics;
- no leftover `DolocTown.exe`.

