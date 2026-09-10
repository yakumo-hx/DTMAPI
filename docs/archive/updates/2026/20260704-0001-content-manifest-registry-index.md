# 20260704-0001 Content Manifest Registry Index

## Summary

Implemented DTMAPI phase 8A as an internal-only content/manifest registry authoritative diagnostic index. The registry records discovered mod/content-pack identity, source, enablement, owner, capabilities, manifest diagnostics, dependency compatibility, and legacy/new registry diffs without changing the legacy loader or any content application result.

`RegistryTakesOver` remains false. CustomAnimals, AnimalVoice, AutoFishing, AnimalViewer behavior, public APIs, JSON semantics, Hook targets, and content-pack paths are unchanged.

## Source Request / Goal

- User request: enter "DTMAPI 第 8 阶段：Content / Manifest Registry 收口，先做 8A authoritative index".
- Goal record: `docs/goals/2026/20260704-0001-content-manifest-registry-index.md`.
- Short prompt backup: `docs/goals/2026/20260704-0001-content-manifest-registry-index.goal.txt`.

## Changed Files

- `src/DTMAPI.Core/Runtime/ContentManifestRegistry.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/RefactorScaffoldOptions.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/goals/2026/20260704-0001-content-manifest-registry-index.md`
- `docs/goals/2026/20260704-0001-content-manifest-registry-index.goal.txt`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`

## Behavior

- Added internal scaffold flag `ContentManifestRegistry=true` by default, with config/env override support.
- Added `ContentManifestRegistry` snapshots and rows for:
  - `UniqueID`, name, version, type, entry DLL/type.
  - source, official enablement state, toggle ownership, manifest/root paths.
  - owner id and capabilities such as `CodeMod`, `ContentPack`, `CustomAnimals`, `AudioReplacement`, `AnimalVoice`, `OfficialJson`, `Workshop`, `OfficialLocal`, and `Local`.
  - dependency rows and compatibility diagnostics.
  - registry diffs between legacy loaded/discovered results and the new index.
- Dependency compatibility now resolves against runtime `ModRegistry.GetAll()` as well as legacy loaded mods, so runtime-owned API manifests like `DTMAPI.GameBridge.DolocTown` are not falsely reported missing.
- The registry is diagnostic-only. It does not reorder mods, decide loading, apply content, change CustomAnimals/AnimalVoice readers, or replace the old scanner/loader.
- Added runtime report context:
  - `ContentRegistry`
  - `ManifestRegistry`
  - `DependencyCompatibility`
  - `ContentPackOwnership`
  - `RegistryDiffs`
- Added smoke `result.json` fields with the same names.

## Diagnostics

- `RegistryDiffs=0` is the hard 8A compatibility gate.
- Manifest/dependency warnings are diagnostic and do not imply runtime takeover or loader failure.
- Current local smoke diagnostics intentionally report:
  - duplicate UniqueID warnings where enabled Workshop packages shadow disabled OfficialLocal packages, or one enabled OfficialLocal package shadows disabled Workshop.
  - compatibility warnings for locally present `0.5.3-alpha` mods while the runtime baseline remains `0.5.2-alpha`.
- These warnings are visible in `ManifestRegistry` and `DependencyCompatibility`, while short smokes pass because old runtime behavior remains authoritative and `RegistryDiffs=0`.

## Validation

- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; restore/build emitted restricted-network `NU1900` package-vulnerability feed warnings only.
- `git diff --check`: passed; Git reported line-ending normalization warnings only.
- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- Initial slot 3 smoke exposed a phase-8A false-positive dependency resolver bug:
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260704-004551`
  - Result: `RunStatus=Failed` only because `ManifestRegistry` and `DependencyCompatibility` were treated as hard-fail warnings.
  - Runtime behavior was clean: `SaveLoaded=Passed`, `TitleButtonLifecycle=Passed`, `ContentRegistry=Passed`, `RegistryDiffs=Passed`, `RegistryTakesOver=false`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
  - Root cause: the new registry looked only at legacy discovered/loaded content and missed runtime-owned registered manifests such as `DTMAPI.GameBridge.DolocTown`.
  - Fix: pass `ModRegistry.GetAll()` into the dependency resolver and treat manifest/dependency warnings as diagnostic in smoke unless they become `error`/`failed`.
- Slot 3 lifecycle smoke passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseTitleButtonLifecycle -TimeoutSeconds 320 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260704-005016`
  - Key fields: `RunStatus=Passed`, `SaveLoaded=Passed`, `TitleButtonLifecycle=Passed`, `ContentRegistry=Passed`, `ManifestRegistry=Passed`, `DependencyCompatibility=Passed`, `ContentPackOwnership=Passed`, `RegistryDiffs=Passed`, `RegistryTakesOver=false`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
  - Registry summary: `rows=28`, `loadedRows=19`, `contentPacks=7`, `loadedContentPacks=5`, `customAnimalPacks=6`, `animalVoicePacks=5`, `diffs=0`.
- Slot 7 Hatch AnimalVoice smoke passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoExerciseHatchAnimalVoice -TimeoutSeconds 260 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260704-005121`
  - Key fields: `RunStatus=Passed`, `HatchAnimalVoice=Passed`, all phase-8A registry fields passed, `RegistryTakesOver=false`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
- Slot 4 AnimalViewer UI smoke passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 4 -AutoOpenAnimalPanel -TimeoutSeconds 260 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260704-005217`
  - Key fields: `RunStatus=Passed`, `AnimalViewerUi=Passed`, all phase-8A registry fields passed, `RegistryTakesOver=false`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
- Slot 5 AutoFishing short soak passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 5 -AutoExerciseAutoFishingPhase -AutoFishingSoakLoops 3 -TimeoutSeconds 420 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260704-005317`
  - Key fields: `RunStatus=Passed`, `AutoFishingPhase=Passed`, `AutoFishingSoak=Passed`, all phase-8A registry fields passed, `RegistryTakesOver=false`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.

## Rollback Notes

- Set `ContentManifestRegistry=false` in `DTMAPI/config/refactor-scaffold.json` to disable the phase-8A index.
- Disabling the flag leaves the legacy scanner, loader, shadow registry, content application, CustomAnimals, AnimalVoice, AutoFishing, AnimalViewer, and public APIs unchanged.
- This phase does not remove old fallback systems and does not enable Registry takeover.

## Follow-Up

- Phase 8B can use this index as the diagnostic source for manifest/content ownership, but should still keep `RegistryTakesOver=false` until a separate cutover plan and fallback are verified.
- Keep `RegistryDiffs` as the hard compatibility signal; manifest/dependency warnings should remain visible but not break ordinary smoke unless they represent a new unexplained runtime failure.
- ISSUE-010 remains open. Phase 8A adds better content ownership evidence and passed the requested short smoke gate, but it did not run a new long-title gate and does not by itself prove long-run Fatal GC is fully solved.
