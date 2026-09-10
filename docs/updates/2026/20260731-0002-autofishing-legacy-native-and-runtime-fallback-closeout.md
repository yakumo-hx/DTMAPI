# 20260731-0002 AutoFishing, Legacy Native Compatibility, And Runtime Fallback Closeout

## Metadata

- Update ID: `20260731-0002`
- Date: 2026-07-31
- Lifecycle Status: `implemented`
- Validation Level: `source,unit`
- Runtime Validation: `not-run`
- Related Issue State: `open`
- Area: AutoFishing/ProductNative/legacy-CodeMod/Core/classifier/Manager/Doctor/Bootstrap/GameBridge/Hook/lifecycle
- Source: user selected AutoFishing option C, legacy third-party native compatibility, and obsolete fallback retirement option U1 after the 2026-07-31 manual retest and code review

## Scope

This Update owns three independently reviewable implementation phases:

1. replace AutoFishing's composite bonus-note hash key with a bounded
   current-minigame tracker and close the input-provider fault half-state;
2. keep explicitly declared Strict CodeMods under the Strict reference
   boundary while routing omitted-kind legacy third-party DtmMods through a
   clearly labelled author-managed, restart-required compatibility lane;
3. remove six obsolete generic UI Hook attempts and the obsolete coroutine
   frame driver while retaining the exact menu repairs, active layout repair,
   PlayerLoop, InputSystem/MonoBehaviour fallback and health pump.

Player-facing Manager information architecture is explicitly out of scope.
Legacy third-party hot unload, automatic Harmony reversal and removal of
already copied external BepInEx payloads are deferred to a later DTMAPI
version.

## Source Authorities

- [`PROJECT.md`](../../../PROJECT.md)
- [`20260731-0001 manual QA review`](../../archive/reviews/manual-qa/2026/20260731-0001-autofishing-manager-player-ui-and-loop-stall.md)
- [`20260719-0008 DLL entry audit`](../../archive/reviews/code/2026/20260719-0008-dll-mod-entry-and-migration-boundary-audit.md)
- [`20260719-0001 audit Update`](../../archive/updates/2026/20260719-0001-dll-mod-entry-model-audit.md)
- [`20260728-0005 retained external consumer review`](../../archive/reviews/code/2026/20260728-0005-retained-external-consumer-native-admission-review.md)
- [`Batch 6 managed identity contract`](../../architecture/batch6-managed-mod-identity-contract.md)
- [`ISSUE-010`](../../debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md)
- [`Hook Map`](../../hook-map/README.md)

## Selected Behavior

### AutoFishing

- Bonus-note de-duplication is scoped to the current native minigame handle.
- A handle change clears the previous note-index set; minigame stop, pull,
  session release and lifecycle cleanup clear the tracker.
- No composite identity-hash multiplication remains.
- Any failure in frame construction, de-duplication, provider invocation or
  result publication records one bounded session fault. The product consumes
  that fault on its update path and performs the same complete disable/session
  cleanup as a normal product stop.

### Legacy Third-Party Native Compatibility

- Explicit `CodeModKind=Strict` remains Strict and continues to reject direct
  Unity, Harmony, BepInEx and game references.
- SDK-produced Advanced products retain receipt, game-build, owner and
  lifecycle admission.
- A historical CodeMod that omits `CodeModKind` is not silently called Strict.
  It is classified as a legacy native compatibility CodeMod: DTMAPI owns
  discovery, dependency/version ordering, cold-start admission, log
  attribution and Entry exception isolation, while the third-party author owns
  native references, Harmony/static state, save side effects and cleanup.
- Once such code is loaded, disable/update/unsubscribe is restart-required.
  DTMAPI does not claim CLR unload or automatic reversal of unknown Hooks.
- Exact Catalog admissions may add stronger provenance, but are no longer the
  only way for an omitted-kind legacy DtmMod to load.
- External `BaseUnityPlugin` payloads remain outside DTMAPI management. Their
  future receipt-bound removal/quarantine workflow is deferred.

### Obsolete Runtime Fallbacks

- Remove open/closed generic `DolocGridUI<T>` Hook attempts and their
  dedicated callbacks/status fields/helpers.
- Keep `HomePageUiState.RenderTextMenu`,
  `MainMenuPanel.OnStartShow`, `MenuUI.SetCapacity`,
  `GameDataPanel.SetCapacity` and active menu layout repair.
- Remove the coroutine driver and startup attempt.
- Keep PlayerLoop as the primary frame source, InputSystem and MonoBehaviour
  Update-family fallback paths, lifecycle reinstall and the 250 ms health
  pump.

## Changed Files

- AutoFishing ProductNative:
  - `products/first-party/AutoFishing/src/ModEntry.cs`
  - `products/first-party/AutoFishing/src/Native/FishingMiniGameInputState.cs`
  - `products/first-party/AutoFishing/src/Native/FishingMiniGameNativeCache.cs`
  - `products/first-party/AutoFishing/src/Native/FishingNativeAdapter.cs`
  - `products/first-party/AutoFishing/src/Native/FishingPrimitivesService.cs`
- Legacy native compatibility policy and Runtime:
  - `PROJECT.md`
  - `src/DTMAPI.Core/AssemblyInfo.cs`
  - `src/DTMAPI.Core/Manifesting/ManagedModClassification.cs`
  - `src/DTMAPI.Core/Runtime/ContentManifestRegistry.cs`
  - `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
  - `src/DTMAPI.Core/Runtime/ModOwnerLifecycleCoordinator.cs`
  - `src/DTMAPI.Core/Manager/DtmManagerViewModels.cs`
  - `src/DTMAPI.Core/Manager/ManagerPageRowFormatter.cs`
  - `src/DTMAPI.InstallDoctor/DoctorEngine.cs`
  - `src/DTMAPI.InstallDoctor/DoctorModels.cs`
  - `src/DTMAPI.InstallDoctor/DoctorReportFormatter.cs`
  - `author-sdk/schemas/doctor-report.schema.json`
  - `author-sdk/templates/codemod/dtmapi.author.json.template`
  - `author-sdk/templates/codemod/manifest.json.template`
  - `docs/architecture/batch6-managed-mod-identity-contract.md`
  - `tools/release/contracts/batch6-g0-mod-identity-contract.json`
  - `tools/release/contracts/batch6-g2-advanced-synthetic-contract.json`
  - `tools/scripts/test-batch6-g2-advanced-synthetic.ps1`
  - `tools/scripts/test-batch6-phase0-contract.ps1`
- Legacy native fixtures:
  - `tests/DTMAPI.UnitTests/Fixtures/LegacyNativeHelperFixture/LegacyNativeHelper.cs`
  - `tests/DTMAPI.UnitTests/Fixtures/LegacyNativeHelperFixture/LegacyNativeHelperFixture.csproj`
  - `tests/DTMAPI.UnitTests/Fixtures/LegacyNativeCodeModFixture/LegacyNativeCodeModFixture.csproj`
  - `tests/DTMAPI.UnitTests/Fixtures/LegacyNativeCodeModFixture/LegacyNativeProbeMod.cs`
  - `tests/DTMAPI.UnitTests/Fixtures/LegacyNativeThrowingCodeModFixture/LegacyNativeThrowingCodeModFixture.csproj`
  - `tests/DTMAPI.UnitTests/Fixtures/LegacyNativeThrowingCodeModFixture/ThrowingLegacyNativeProbeMod.cs`
  - `tests/DTMAPI.UnitTests/Fixtures/LegacyCollisionA/CollisionProbeMod.cs`
  - `tests/DTMAPI.UnitTests/Fixtures/LegacyCollisionA/LegacyCollisionA.csproj`
  - `tests/DTMAPI.UnitTests/Fixtures/LegacyCollisionB/CollisionProbeMod.cs`
  - `tests/DTMAPI.UnitTests/Fixtures/LegacyCollisionB/LegacyCollisionB.csproj`
- Fallback/Hook retirement:
  - `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
  - `src/DTMAPI.GameBridge.DolocTown/Features/UiDiagnostics/NativeUiLayoutDiagnosticsFeature.cs`
  - `src/DTMAPI.GameBridge.DolocTown/Features/UiDiagnostics/NativeUiLayoutRepairService.cs`
  - `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
  - `src/DTMAPI.GameBridge.DolocTown/Hooking/HarmonyReflectionPatcher.cs`
  - `src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/DolocTownGameBridge.G4Fixtures.cs`
  - `docs/hook-map/README.md`
  - `docs/hook-map/focused/NativeUiLayout.md`
  - `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- Tests and task records:
  - `tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj`
  - `tests/DTMAPI.UnitTests/Program.cs`
  - `tests/DTMAPI.UnitTests/Batch6AdvancedRuntimeTests.cs`
  - `tests/DTMAPI.QaUnitTests/Program.cs`
  - `tests/DTMAPI.InstallDoctor.Tests/Program.cs`
  - `tests/DTMAPI.AuthorSdk.Tests/DTMAPI.AuthorSdk.Tests.csproj`
  - `tests/DTMAPI.AuthorSdk.Tests/Program.cs`
  - `docs/reviews/manual-qa/2026/20260731-0001-autofishing-manager-player-ui-and-loop-stall.md`
  - `docs/updates/2026/20260731-0002-autofishing-legacy-native-and-runtime-fallback-closeout.md`
  - `docs/updates/INDEX-2026-07.md`
  - `tools/release/dtmapi-product-catalog.json`

The already-present AutoFishing three-language Workshop-copy edits remain
owned by Update `20260731-0001`; this Update does not re-own them. Other dirty
MoreEquipmentSlots/release-route documentation in the shared worktree is also
outside this inventory.

## Validation

- `tools/scripts/build.ps1 -Configuration Release -SkipTests`: PASS on the
  final source state.
- `DTMAPI.UnitTests`: PASS on the final source state. Coverage includes
  first/repeated bonus notes, native-handle changes, lifecycle clears,
  frame/provider fault closure, explicit Strict rejection, omitted-kind legacy
  cold load, helper closure, Entry isolation, source drift, same-name assembly
  collision, restart-required behavior, exact UI repairs, exact four-Hook
  tuples, retired-source absence and Bootstrap driver/shutdown wiring.
- `DTMAPI.QaUnitTests`: PASS on the final source state, including exact native
  UI readiness and read-only title/pause observations.
- `DTMAPI.InstallDoctor.Tests`: PASS on the final source state.
- `DTMAPI.AuthorSdk.Tests`: PASS on the final source state.
- `tools/scripts/test-batch6-phase0-contract.ps1`: PASS; historical Phase 0
  receipt reproduced and the current 0.5.5 amendment remained present.
- The first current-HEAD Runtime package projection correctly rejected the
  stale Batch 4 semantic count. Adding the new AutoFishing input-state source
  raises the live production inventory from 278 to 279 files; the Catalog
  structured metric and its matching `currentDebt` claim were updated
  together, after which `tools/scripts/check-product-catalog.ps1` passed.
- AutoFishing was built through the real Author SDK into
  `temp/autofishing-input-fix-build-r4/DTMAPI-AutoFishing-advanced-pilot.zip`.
  Package SHA-256:
  `DC116D4FCA493982727AEB3B0BCE8F9DCF56FE504F4912DC426A384E172713BC`;
  entry SHA-256:
  `1BBB0D1C26EB5154BA1EFE73F431B623E1B9987A6B230C35DA7AC463E9C65772`.
- No complete Release suite and no game launch were run for this Update.

## Evidence

- AutoFishing's one authorized multi-agent review found no remaining
  blocker after the bounded handle tracker, NotReady/Faulted split and pending
  cleanup ledger were corrected.
- Legacy compatibility's one authorized multi-agent review reopened
  code-closure drift, same-identity collision, copied platform assembly,
  Doctor schema/report and historical-contract issues; all were corrected
  before the final Unit/Doctor/Author/Phase-0 passes.
- U1's one authorized three-way review found no P1. It identified a dormant
  global GridLayoutGroup setter lane, weak retained-path assertions, incorrect
  per-frame wording, Error-level health-pump fallback and a queued-callback
  shutdown race. The dead lane was removed, exact source/behavior gates were
  added, cadence/lifecycle wording was corrected, fallback failure became a
  Warning, and shutdown now closes queued recovery before driver cleanup.
- The current Runtime/game evidence gap is deliberate: the next fifth-save
  AutoFishing manual/game run must prove the player-visible 30-second stall is
  gone; the next bounded Runtime run may also confirm the seven obsolete
  startup Errors disappeared and frame continuity survives save/title
  boundaries.

## Rollback

Each phase must remain separately revertible. Rollback restores the prior
AutoFishing tracker/fault behavior, omitted-kind Strict classification, or
fallback registrations without changing player saves, Workshop subscriptions
or frozen third-party bytes. The legacy lane can be disabled again only by an
explicit compatibility-policy change; doing so would intentionally make
already-published omitted-kind native DtmMods unloadable.

## Follow-Up

- A fifth-save AutoFishing `NoNativeSave` manual/game acceptance remains
  necessary before the player-visible bonus-note failure can be marked closed.
- A later Runtime version may add receipt-bound removal/quarantine for external
  payloads copied into `BepInEx/plugins`; it must not claim in-process unload.
- Manager player-information simplification remains deferred by explicit user
  decision.
