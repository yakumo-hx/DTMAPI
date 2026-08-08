# 20260702-0003 Resource Lifecycle Generations

## Metadata

- Date: 2026-07-02
- Status: implementation
- Source: User request to implement DTMAPI third-stage resource ownership and generation cleanup.
- Owner: DTMAPI

## Goal

Add an internal resource lifecycle ledger so DTMAPI-owned, native-owned, borrowed, external-mod-owned, and unknown resources have explicit owner, generation, lifetime, ownership, status, and release-boundary diagnostics. This stage starts low-risk cleanup for DTMAPI-owned `SaveLifetime` state while keeping title-level Unity/native assets in report-only mode.

## Scope

- Add an internal-only resource lifecycle ledger with `ProcessLifetime`, `TitleLifetime`, and `SaveLifetime` generations.
- Track resource fields: `resourceKind`, `resourceId`, `ownerId`, `sourcePath`, `lifetime`, `ownership`, `generation`, `status`, `acquiredAtPhase`, `releasedAtPhase`, and `releasePolicy`.
- Extend local scaffold flags with `ResourceLifecycleLedger`, `ResourceLifecycleCleanup`, and `ResourceLifecycleTitleAssetRelease`.
- Mark AudioReplacement WAV/request/player resources as DTMAPI-owned title-lifetime content-generation resources.
- Mark AnimalVoice runtime context stacks as DTMAPI-owned save-lifetime state and clear them at save/title boundaries.
- Mark CustomAnimals definitions, animator registrations, AI template registrations, bundle/cache references, borrowed native template controllers, borrowed native sprites, PNG override contexts, and sleep/render follow-up contexts with lifecycle metadata.
- Drop only stale managed cache references when content generation changes. Do not unload or destroy title-level Unity/native assets in this stage.
- Add diagnostics and smoke result fields for ledger status, cleanup status, resource summary, returned-to-title cleanup plan, and title-idle resource growth.

## Guardrails

- Keep `RegistryTakesOver=false`.
- Do not replace CustomAnimals gameplay logic.
- Do not replace AnimalVoice replacement logic.
- Do not change manifest fields, content-pack JSON fields, CustomAnimals fields, AnimalVoice fields, or content-pack paths.
- Do not move Hook patch installation order.
- Do not add stable public APIs.
- Do not call `AssetBundle.Unload`, destroy `RuntimeAnimatorController`, destroy native sprites, or release unknown Unity objects in this stage.
- Cleanup is limited to DTMAPI-owned `SaveLifetime` state and existing content-generation replacement cleanup paths.
- If scaffold flags are disabled, the new ledger/cleanup layer must not execute.

## Known Inputs

- ISSUE-010 now has one stable 3600-second title-idle Fatal GC reproduction sample from `docs/debug/evidence/GAME-SMOKE/20260702-152855`.
- Second-stage lifecycle diagnostics showed no obvious continuing registry refresh, Hook reinstall, or audio/resource load loop before the long-idle crash.
- Slot 7 Hatch AnimalVoice smoke and user manual custom-animal QA passed before this phase.
- Slot 8 paper-box behavior was user-manual verified; automatic fixture repair is out of scope.

## Required Validation

- `tools/scripts/test.ps1 -Configuration Release`
- `git diff --check`
- PowerShell parse check for changed smoke/common scripts.
- Short smoke gate:
  - `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseTitleButtonLifecycle -TimeoutSeconds 320 -SkipBuild`
  - `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoExerciseHatchAnimalVoice -TimeoutSeconds 260 -SkipBuild`
- Stage-end long idle gate once short smoke passes:
  - `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -TitleIdleBeforeSaveSeconds 3600 -TimeoutSeconds 3900 -SkipBuild`

## Acceptance

- Existing code mods and content packs load the same way as before.
- `RegistryTakesOver=false` remains visible in logs and smoke JSON.
- `ResourceLifecycleLedger` and `ResourceLifecycleCleanup` pass in short smoke.
- `TitleIdleResourceGrowth` shows no generation growth, WAV ready growth, custom animal binding growth, Hook install growth, or repeated rebuild during title idle.
- Slot 7 Hatch AnimalVoice remains clean: JSON + PNG + WAV custom animal path still works, and native animal sound does not leak.
- Long title idle either passes without Fatal GC or leaves ledger evidence narrowing the next isolation target to Audio, CustomAnimals, or Core lifecycle/registry.
- Update/debug records explicitly state which resources were actually cleaned, which were ledger-only, and which native/title assets remain untouched.
