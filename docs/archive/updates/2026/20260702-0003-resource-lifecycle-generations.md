# 20260702-0003 Resource Lifecycle Generations

## Status

source-and-short-smoke-verified / long-idle-fatal-reproduced

## Source Request

Implement the third-stage guarded refactor: add an internal resource lifecycle ledger with owner, generation, lifetime, ownership, acquisition/release phase, and cleanup policy; clean only low-risk DTMAPI-owned `SaveLifetime` state; keep `RegistryTakesOver=false`; do not replace CustomAnimals behavior, AnimalVoice behavior, Hook patch installation, JSON semantics, content-pack paths, or stable public APIs.

## Changed Files

- `docs/goals/2026/20260702-0003-resource-lifecycle-generations.md`
- `docs/goals/2026/20260702-0003-resource-lifecycle-generations.goal.txt`
- `src/DTMAPI.Core/Runtime/ResourceLifecycleLedgerService.cs`
- `src/DTMAPI.Core/Runtime/RefactorScaffoldOptions.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Implementation Notes

- Added internal-only resource lifecycle fields and enums for `resourceKind`, `resourceId`, `ownerId`, `sourcePath`, `lifetime`, `ownership`, `generation`, `status`, `acquiredAtPhase`, `releasedAtPhase`, and `releasePolicy`.
- Added default-on local scaffold flags `ResourceLifecycleLedger=true` and `ResourceLifecycleCleanup=true`, plus default-off `ResourceLifecycleTitleAssetRelease=false`. Environment variables can still override for smoke/CI; these remain internal runtime switches.
- Added process/content/save generation tracking. `ProcessGeneration` stays `1`; content generation advances only when the loaded content signature changes; save generation opens at `SaveLoaded`/`SecondSaveLoaded` and closes at `ReturnedToTitle`.
- AudioReplacement now records content definitions, WAV requests, ready clips, platform players, and `AnimalSoundContext` stack entries in the ledger. `AnimalSoundContext` is cleared on save/title boundaries when cleanup is enabled. Existing `CleanupEntry` remains the only AudioClip/player release path and now writes ledger release records.
- CustomAnimals now records definitions, animator registrations, AI template registrations, bundle/controller managed-cache refs, PNG sprite override contexts, and sleep follow-up contexts. Save-scoped PNG/sleep/diagnostic contexts are cleared on save/title boundaries. Title-level Unity assets are not unloaded or destroyed in this stage.
- Content refresh is signature-aware. Save/title/environment boundaries may check state, but unchanged CustomAnimals and AudioReplacement definitions no longer rebuild from those boundaries.
- Smoke `result.json` now exposes `ResourceLifecycleLedger`, `ResourceLifecycleCleanup`, `ResourceLifecycleSummary`, and `TitleIdleResourceGrowth`.

## Validation

- `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`; NuGet vulnerability metadata warnings `NU1900` remain network/feed warnings.
- `git diff --check` passed with line-ending warnings only.
- PowerShell parser check passed for changed smoke/common scripts.
- Slot 3 short lifecycle smoke passed: `docs/debug/evidence/GAME-SMOKE/20260702-183915`.
  - Passed `RunStatus`, `StartupLog`, `GameLaunched`, `SaveLoaded`, `TitleButtonLifecycle`, `LifecycleObservation`, `LifecycleBoundaryContract`, `ShadowContentRegistry`, `RefactorScaffoldFlags`, `ResourceLifecycleLedger`, `ResourceLifecycleCleanup`, `TitleIdleResourceGrowth`, `NoFatalInstanceWindow`, `ProcessExited`, and `ForcedClose`.
  - Resource summary ended `status=ok`, `contentGeneration=1`, `saveGeneration=1`, `records=56`, `titleIdleGrowthWarnings=0`, `warnings=0`, `errors=0`.
- Slot 7 Hatch AnimalVoice smoke passed: `docs/debug/evidence/GAME-SMOKE/20260702-184039`.
  - Passed `HatchAnimalVoice` and the resource lifecycle fields.
  - Logs show Hatch child/adult replacements `played=True suppressed=True`; no native voice leak was detected in the automated path.
  - Resource summary ended `status=ok`, `records=89`, `byOwnership=BorrowedNative:24,DtmapiOwned:65`, `titleIdleGrowthWarnings=0`, `warnings=0`, `errors=0`.
- Long title-idle gate reproduced the known native GC failure: `docs/debug/evidence/GAME-SMOKE/20260702-184205`.
  - Held title for 3600 seconds, loaded slot 3, and logged `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`.
  - `result.json` has `RunStatus=Aborted`, `LongTitleIdleBeforeSave=Failed`, and `NoFatalInstanceWindow=Failed`.
  - `fatal-window-check.txt` captured `Fatal error in GC` and `Unexpected mark stack overflow`.
  - The new resource gates still passed: `ResourceLifecycleLedger=Passed`, `ResourceLifecycleCleanup=Passed`, `TitleIdleResourceGrowth=Passed`.
  - Ledger/log counts during the idle window were bounded: `CustomAnimalsRefresh=1`, `AudioRefresh=1`, `LocalWavReady=11`, `HookInstallRepeated=0`, `ResourceDiagnostics=0`.

## Rollback

- Runtime rollback without code revert: set `ResourceLifecycleLedger=false` and `ResourceLifecycleCleanup=false` in `DTMAPI/config/refactor-scaffold.json`, or use the matching environment overrides for a single smoke run.
- `ResourceLifecycleTitleAssetRelease` defaults to `false`; leave it off. This stage did not add any active `AssetBundle.Unload`, `RuntimeAnimatorController` destroy, native sprite release, or unknown Unity object release path.
- Source rollback is local to the new ledger service, its DtmApiRuntime wiring, the AudioReplacement/CustomAnimals ledger calls and save-scope cleanup, smoke result fields, and unit tests.

## Follow-Up

- ISSUE-010 remains open. The third-stage evidence narrows the long-idle crash away from repeated shadow registry refresh, repeated Hook install, repeated WAV ready growth, repeated CustomAnimals binding growth, and save-scoped DTMAPI context accumulation.
- Do not run a broad matrix next. Use the ledger to run one focused long-idle isolation at a time: AudioReplacement disabled, CustomAnimals disabled, or Core/lifecycle/native save-load boundary instrumentation.
