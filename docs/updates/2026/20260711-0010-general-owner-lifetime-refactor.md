# 20260711-0010 General Owner Lifetime Refactor

## Metadata

- Update ID: `20260711-0010`
- Date: 2026-07-11
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `open`
- Source: user approved implementation of the recorded P1/P2 common owner-lifetime audit.

## Scope

- make code-Mod activation and deactivation atomic and idempotent;
- deactivate DTMAPI-owned roots immediately after official disable, removal, or required-dependency invalidation while requiring restart before re-entry;
- bind API, Config, ConfigPage, Input, and Event registration to the helper owner;
- publish Content assets and enabled item rows only from authoritative active owners after atomic activation;
- return canonical owner-bound facades for every manifest-bearing GameBridge, CustomEntity, fishing, ConfigMenu, Camera, and DebugConsole contract, and count their real provider-side roots;
- reject duplicate API/page/migration registration;
- isolate every Core participant and Experimental movement/time-scale/creative/machine/equipment cleanup failure domain while retaining failed roots for retry;
- release quarantined delegates and retain bounded scalar diagnostics only;
- preserve committed process-lifetime Mod services across SaveLoaded and ReturnedToTitle.

## Source Review

- `docs/reviews/code/2026/20260711-0001-general-owner-lifetime-boundary-audit.md`

## Safety Boundary

先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

This slice is Core-owned. It does not change native Hook signatures, native game state, content schemas, public API signatures, or the `0.5.3-alpha` version line.

## Changed Files

- `src/DTMAPI.Abstractions/ConfigMenu.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`
- `src/DTMAPI.Core/Logging/FileMonitor.cs`
- `src/DTMAPI.Core/Runtime/ModOwnerLifecycleCoordinator.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/ModOwnerCleanup.cs`
- `src/DTMAPI.Core/Runtime/ModOwnerLedgerService.cs`
- `src/DTMAPI.Core/Runtime/RuntimeSnapshotFactory.cs`
- `src/DTMAPI.Core/Services/ConfigService.cs`
- `src/DTMAPI.Core/Services/EventManager.cs`
- `src/DTMAPI.Core/Services/OwnerBoundCustomEntityApis.cs`
- `src/DTMAPI.Core/Services/RegistryAndHelpers.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/AssemblyInfo.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/OwnerBoundGameBridgeApis.cs`
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/LegacyFishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/OwnerBoundFishingAutomationApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Diagnostics/DolocTownExperimentalBridgeApi.Diagnostics.cs`
- owner cleanup/count implementations in `src/DTMAPI.GameBridge.DolocTown/Features/ActionCompletion`, `ActionSpeed`, `AnimalViewer`, `AudioReplacement`, `Camera`, `ChestLocatorEnhancer`, `CropHarvesting`, `CustomAnimals`, `FishingAutomation`, `FishRoeTooltip`, `SaveSlots`, and `StrongPlantingGun`;
- `src/DTMAPI.GameBridge.DolocTown/Features/OwnerCleanup/DolocTownExperimentalBridgeApi.OwnerCleanup.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/OwnerCleanup/GameBridgeModOwnerCleanupParticipant.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuItems.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuPage.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/design/mod-owner-lifetime-contract.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/reviews/code/2026/20260711-0001-general-owner-lifetime-boundary-audit.md`
- this Update and `docs/updates/INDEX-2026-07.md`

## Validation

- `tools/scripts/test.ps1 -Configuration Release`: passed; all runtime and ordinary-Mod projects stayed `netstandard2.0`, and `DTMAPI.UnitTests: OK`.
- Unit coverage includes all eight load checkpoints; begin/commit/completion diagnostic isolation; API/page/migration duplicates; structured owner-key collision resistance; canonical owner spoof rejection across Config/Core/GameBridge/CustomEntity/fishing; facade consumer/provider invalidation and stale ConfigMenu graph deactivation; DebugConsole host-graph cleanup; active-owner-only Content publication for Entry/dependency/disable/restart paths; source removal; refreshed provider-version downgrade and required-dependency cascade; optional-dependency warning; disable/re-enable restart behavior; participant and Experimental substep failure isolation/retry; authoritative Camera/GameBridge counts, failed native-restore tombstone retry, and lease release; SaveLoaded/ReturnedToTitle Camera-request preservation; quarantine/config callback `WeakReference`; and diagnostic cap stress.
- `tools/scripts/check-doc-governance.ps1`: passed.
- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `git diff --check`: passed.

## Runtime Evidence

- Shared runtime lock was acquired for each attempt and released after the process exited.
- `GAME-SMOKE/20260711-150532` exposed a smoke-only ordering error: the synthetic owner was finalized at a pre-load ReturnedToTitle. It is preserved as superseded evidence.
- `GAME-SMOKE/20260711-150855` proved OwnerLifetime itself but exposed smoke-only restart classification for a synthetic no-assembly owner. It is preserved as superseded evidence.
- Third-save `GAME-SMOKE/20260711-151205` passed the original Core-only owner boundary:
  - startup, HookProbe, SaveLoaded, title lifecycle, OwnerLifetime, and GameBridge final health;
  - control roots `7 -> 7` across SaveLoaded and `7 -> 0` at final ReturnedToTitle cleanup;
  - cleanup owner `remaining=0` across Event/Input/API/ConfigPage/migration/Core/GameBridge participants;
  - no fatal instance popup, normal process exit, and no residual `DolocTown.exe`.
- Enhanced third-save `GAME-SMOKE/20260711-162335` proved the real Camera participant boundary (`8 -> 8 -> 0`) and zero Core/GameBridge remaining roots, but the run was classified failed because the diagnostic ledger inferred `failedSteps=none` as a cleanup failure. The process exited normally with no residual `DolocTown.exe`; it is superseded by the corrected run below.
- Final third-save `GAME-SMOKE/20260711-230125` passed the corrected cross-domain acceptance command:
  - startup, HookProbe, SaveLoaded, title lifecycle, OwnerLifetime, GameBridge scheduler/final health, no-fatal window, and process exit all passed;
  - the process-lifetime control owner retained all eight Core + real Camera roots across SaveLoaded, then unified deactivation reported `eventsRemoved=1`, `inputRemoved=1`, `configPagesRemoved=1`, `migrationsRemoved=1`, `registryRemoved=4`, `participantResourcesRemoved=1`, `participantCleanupFailures=0`, and `remaining=0`;
  - the real Camera lease remained live before cleanup and was released only by owner deactivation;
  - the cleanup owner and control owner both reached zero authoritative Core/GameBridge roots;
  - the game exited normally, no fresh fatal/crash evidence was created, no `DolocTown.exe` remained, and the shared runtime lock was released.

## Rollback

Revert this implementation Update and its source/document changes. Do not remove the preceding review record. Restart the game after rollback: already-loaded Mono assemblies, third-party statics/Harmony patches, and native/Unity state cannot be rolled back safely in-process.

## Follow-Up

No public API or version promotion. ISSUE-010 remains open; no additional inactive platform baseline is warranted by this change. ZoomMod productization and ordinary-Mod/Camera same-process disable validation are owned by a subsequent Update and commit.

2026-07-12 follow-up: player-visible Config-page loss exposed that the original refresh coverage did not exercise an *active* ordinary owner whose dependency was a registry-only process-lifetime provider. The explicit deactivation/zero-root evidence above remains valid, but the no-op refresh path falsely treated ConfigMenu, GameBridge, and DebugConsoleHost as missing ordinary sources. Review, correction, and replacement runtime validation are owned by `20260712-0001`; this historical Update is not rewritten as the fix owner.
