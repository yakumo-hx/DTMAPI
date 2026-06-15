# Hook Map

Every DTMAPI hook must be registered here before it becomes a stable API event/helper.

Use this template:

```md
## Hook: PublicApi.EventOrHelperName

- Status: proposed / experimental / verified / stable / disabled
- Public surface:
- Game build:
- Game method/type:
- Patch type: Postfix / Prefix / Finalizer / Transpiler / Unity callback / reflection
- Why this point:
- Failure behavior:
- Mods/tests depending on it:
- Evidence:
  - Build:
  - Save:
  - Log line:
  - Screenshot/report:
- Regression cases:
```

Default preference: Postfix or read-only reflection first, Prefix only when needed, Transpiler only with explicit review and regression evidence.

## Diagnostic: Smoke.DiagnosticsReportExport

- Status: verified
- Public surface: none; `run-game-smoke.ps1` result-field evidence only.
- Game build: 23465763 workshop
- Game method/type: smoke harness log/result validation around `Smoke.DiagnosticsSnapshot = verified` entries produced by `IDtmDiagnosticsApi.GetSnapshot` after `runtime.ExportLogs()`.
- Patch type: smoke script result aggregation; no Harmony hook, public API, hook/status ID, or gameplay behavior change.
- Why this point: report export proof should be comparable across Camera, ActionSpeed, AutoFishing, and future diagnostics snapshot smokes instead of being locked to a per-feature result field.
- Failure behavior: if any requested diagnostics-export scenario does not log `Smoke.DiagnosticsSnapshot = verified`, the new `DiagnosticsReportExport` result field is `Failed` and the overall smoke run fails; existing behavior fields and `AutoFishingReportExport` stay separate for compatibility.
- Mods/tests depending on it: Camera/ActionSpeed/AutoFishing smoke harness routes and final web audit package evidence selection.
- Evidence:
  - Build: 2026-06-11 `git diff --check`, PowerShell script syntax parsing, Release build, and Release unit tests passed.
  - Save: local slot 3 / index 2.
  - Log line: final `Refactor` evidence `GAME-SMOKE/20260611-031502` logs `Smoke.DiagnosticsSnapshot = verified. scenario=Camera` and result `DiagnosticsReportExport=Passed`; `GAME-SMOKE/20260611-031721` logs `scenario=ActionSpeed` and result `DiagnosticsReportExport=Passed`; `GAME-SMOKE/20260611-031838` logs `scenario=AutoFishing AutoFishingMiniGameComplete report export` with result `DiagnosticsReportExport=Passed` and compatibility `AutoFishingReportExport=Passed`.
  - Screenshot/report: smoke evidence under `docs/debug/evidence/GAME-SMOKE/20260611-031502`, `20260611-031721`, and `20260611-031838`; report zips `dtmapi-report-20260611-031644.zip`, `dtmapi-report-20260611-031801.zip`, and `dtmapi-report-20260611-031919.zip`.
- Regression cases: DIAGNOSTICS-REPORT-EXPORT-FIELD-20260611

## Diagnostic: HookCallbackSafeFallbacks

- Status: verified
- Public surface: none; internal GameBridge/Harmony callback safety policy.
- Game build: 23465763 workshop
- Game method/type: non-lifecycle Harmony callback paths owned by `DolocTownHookCallbacks`, including ChestLocator, FishRoe, ActionSpeed enter/continuous-use, ActionCompletion/Oil tool hit, Fishing phase/minigame, Motor, Equipment, StrongPlantingGun, input isolation, creative/debug, and animal viewer callbacks.
- Patch type: internal callback wrapper around existing Prefix/Postfix bodies; no hook target, hook ID, public API, or smoke schema change.
- Why this point: ordinary hook callbacks should fail toward native behavior instead of leaking exceptions through Harmony into Doloc Town control flow. Lifecycle cleanup/restore ordering is tracked separately under `LIFECYCLE-CALLBACK-ISOLATION-20260610`.
- Failure behavior: `SafeResult<T>` returns the original result/fallback on failure, `SafePrefix` returns `true` by default so native logic continues, and `SafePostfix` records diagnostics without throwing back into native code. Failures are recorded under `DTMAPI.GameBridge.HookCallback` with runtime-monitor log details.
- Mods/tests depending on it: `DTMAPI.UnitTests`, `DTMAPI.ChestLocatorEnhancerMod`, `Yuuka.DTMAPI.FishBreedingAssistant`, `Yuuka.DTMAPI.ActionSpeed`, `Yuuka.DTMAPI.OneActionComplete`, `Yuuka.DTMAPI.AutoFishing`, plus the affected feature smoke harnesses.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors; unit helper coverage verified fallback values and diagnostics recording.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260610-035216`, `GAME-SMOKE/20260610-035326`, `GAME-SMOKE/20260610-035434`, `GAME-SMOKE/20260610-035639`, and `GAME-SMOKE/20260610-035752` all record their focused smoke cases as passed with clean process/fatal checks after non-lifecycle callbacks were wrapped.
  - Screenshot/report: smoke evidence under `docs/debug/evidence/GAME-SMOKE/20260610-035216`, `20260610-035326`, `20260610-035434`, `20260610-035639`, and `20260610-035752`; latest runtime report generated during the branch was `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-035514.zip`.
- Regression cases: HOOK-CALLBACK-SAFE-FALLBACKS-20260610

## Diagnostic: HookCallbackFailureThrottle

- Status: verified
- Public surface: none; internal GameBridge/Harmony callback failure diagnostics policy.
- Game build: 23465763 workshop
- Game method/type: `DolocTownHookCallbacks.RecordHookCallbackFailure` for non-lifecycle callback failures recorded by `SafeResult<T>`, `SafePrefix`, `SafePostfix`, and direct guarded callback bodies.
- Patch type: internal diagnostics throttling around existing callback wrappers; no hook target, hook ID, public API, or smoke schema change.
- Why this point: a repeated per-frame hook failure should not flood diagnostics and runtime logs after the first actionable error, but the callback must still fail toward native behavior.
- Failure behavior: first failure per operation records a full `DTMAPI.GameBridge.HookCallback` diagnostics error and error log; the next two repeats write short warning logs; later repeats are suppressed until a 30-second summary window. Prefix callbacks still return the native-pass fallback, result callbacks still return fallback/original values, and void callbacks still do not throw into Harmony/native code.
- Mods/tests depending on it: `DTMAPI.UnitTests`, plus ChestLocator, FishRoe, ActionSpeed, OneAction, and AutoFishing smoke paths that use the affected callback wrappers.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors; unit coverage verifies fallback values and one diagnostics error per repeated operation.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260610-092620`, `GAME-SMOKE/20260610-092819`, `GAME-SMOKE/20260610-092928`, `GAME-SMOKE/20260610-093037`, and `GAME-SMOKE/20260610-093148` all passed their focused cases with clean process/fatal checks and no hook-callback failure entries.
  - Screenshot/report: smoke evidence under `docs/debug/evidence/GAME-SMOKE/20260610-092620`, `20260610-092819`, `20260610-092928`, `20260610-093037`, and `20260610-093148`.
- Regression cases: HOOK-CALLBACK-FAILURE-THROTTLE-20260610

## Diagnostic: ToolColliderCallbackIsolation

- Status: verified
- Public surface: none; internal GameBridge/Harmony callback safety policy for the shared `ToolCollider.HandleTools` postfix route.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.ToolCollider.HandleTools` shared Prefix/Postfix installed by `ToolColliderHitHookBridge`; callback routing lives in `DolocTownHookCallbacks.ToolColliderHandleToolsPostfix`.
- Patch type: internal callback wrapper split around the existing shared Postfix route; no hook target, hook ID, public API, or smoke schema change.
- Why this point: ActionCompletion and OilCoalDrop share the same native `ToolCollider.HandleTools` route, but ActionCompletion failure should not prevent OilCoalDrop from applying or clearing captured coal-resource state.
- Failure behavior: `SafeResult("ToolCollider.HandleTools.ActionCompletion", false, ...)` falls back to the native-pass/Oil route on ActionCompletion exceptions; `SafePostfix("ToolCollider.HandleTools.OilCoalDrop.ApplyAfterHit", ...)` and `SafePostfix("ToolCollider.HandleTools.OilCoalDrop.ClearCaptured", ...)` record independent diagnostics failures without throwing into Harmony/native code.
- Mods/tests depending on it: `DTMAPI.UnitTests`, `DTMAPI.OneActionCompleteMod`, `DTMAPI.OilMod`, and NewContent/Oil smoke paths.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors; unit coverage verifies ActionCompletion failure does not block OilCoalDrop apply and that ActionCompletion/OilCoalDrop apply/clear failures use distinct diagnostics keys.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260610-163813` records `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`; `GAME-SMOKE/20260610-163928` records `NewContentOilItemMetadata=Passed`, `NewContentOilCoalDrop=Passed`, `NewContentEquipmentSlots=Passed`, `NewContentMineOfficialJson=Passed`, `NewContentMineProduction=Passed`, `NewContentApis=Passed`, and clean process/fatal checks.
  - Screenshot/report: smoke evidence under `docs/debug/evidence/GAME-SMOKE/20260610-163813` and `docs/debug/evidence/GAME-SMOKE/20260610-163928`; these smoke modes did not export fresh report zips, and their stale `latest-report.txt` files are not cited as report evidence.
- Regression cases: TOOLCOLLIDER-CALLBACK-ISOLATION-20260610

## Diagnostic: GameBridgeFeatureFailureThrottle

- Status: verified
- Public surface: none; internal GameBridge feature-host dispatch diagnostics policy.
- Game build: 23465763 workshop
- Game method/type: `DolocTownGameBridge.DispatchGameBridgeFeature(...)` catch path for feature-host operations such as `RegisterApis`, `PublishHookStatuses`, `InstallHooks`, `Update`, `SaveLoaded`, `ReturnedToTitle`, and environment reset.
- Patch type: internal diagnostics throttling around existing feature-host dispatch; no hook target, hook ID, public API, feature service behavior, or smoke schema change.
- Why this point: repeated high-frequency feature-host failures, especially `Update()`, should not flood diagnostics and runtime logs after the first actionable error, while structured `Feature.<Id>` state still needs current failure counts and latest error text.
- Failure behavior: first failure per `featureId + operation` records a full diagnostics error and error log; the next two repeats write short warning logs; later repeats are suppressed until a 30-second summary window. Internal `GameBridgeFeatureStatus` and diagnostics feature snapshots still increment cumulative `FailureCount` and update `LastError` on every failure, while `Feature.<Id>` hook-status publication follows the throttled publication decision instead of rewriting on every failure-count change. Feature status details also include `consecutiveFailureCount` and `lastRecoveredAt`; after three successful dispatches for the same feature operation, the failure episode is cleared so a later failure records a fresh diagnostics error.
- Mods/tests depending on it: `DTMAPI.UnitTests`, diagnostics snapshot/status UI, and all feature-hosted GameBridge features.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors; unit coverage verifies one diagnostics error for six repeated `UnitFeature/Update` failures while failure count reaches 6.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260610-113540`, `GAME-SMOKE/20260610-113752`, `GAME-SMOKE/20260610-113902`, and `GAME-SMOKE/20260610-114008` all record focused feature-host smokes as passed with clean process/fatal checks and no GameBridge feature failure entries.
  - Hook-status publication split: 2026-06-10 unit coverage verifies the diagnostics feature snapshot reaches `FailureCount=6` and latest error text while `Feature.UnitFeature` hook status stops at the third allowed failure publication; focused smokes `GAME-SMOKE/20260610-133933`, `20260610-134145`, `20260610-134259`, and `20260610-134406` pass Camera, ActionSpeed, SaveSlots/HookProbe, and AnimalViewer with no GameBridge feature failure entries.
  - Recovery episode policy: 2026-06-10 unit coverage verifies three stable successes after a repeated `UnitFeature/Update` failure episode reset `consecutiveFailureCount` to 0, preserve cumulative `FailureCount=6`, record `lastRecoveredAt`, and allow a later post-recovery failure to record a second diagnostics error with `consecutiveFailureCount=1`. Camera smoke `GAME-SMOKE/20260610-164830` and ActionSpeed smoke `GAME-SMOKE/20260610-165043` verify the existing ready/smoke routes still pass with fresh report zips `dtmapi-report-20260610-165010.zip` and `dtmapi-report-20260610-165123.zip`.
  - Screenshot/report: smoke evidence under `docs/debug/evidence/GAME-SMOKE/20260610-113540`, `20260610-113752`, `20260610-113902`, and `20260610-114008`; report zips `dtmapi-report-20260610-113722.zip`, `dtmapi-report-20260610-113832.zip`, `dtmapi-report-20260610-113939.zip`, and `dtmapi-report-20260610-114044.zip`.
- Regression cases: FEATURE-HOST-FAILURE-THROTTLE-20260610, FEATURE-FAILURE-STATUS-THROTTLE-20260610, FEATURE-FAILURE-RECOVERY-POLICY-20260610

## Diagnostic: UI.NativeLayoutDiagnostics

- Status: experimental/diagnostic
- Public surface: none; internal GameBridge hook status and runtime log diagnostics only.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.HomePageUiState.RenderTextMenu`, concrete `DolocTown.UI.MenuUI.SetCapacity`, `DolocTown.UI.MainMenuPanel.OnStartShow`, `DolocTown.UI.GameDataPanel.SetCapacity`, best-effort concrete `DolocTown.UI.HomePageTextMenu.ResetLayoutSize` / `DolocTown.UI.MenuUI.ResetLayoutSize`, and attempted generic `DolocTown.UI.DolocGridUI<T>.ResetLayoutSize/SetCapacity`.
- Patch type: Harmony Postfix diagnostics only. `HomePageUiState.RenderTextMenu`, `MenuUI.SetCapacity`, `MainMenuPanel.OnStartShow`, and `GameDataPanel.SetCapacity` record owner-event evidence; active `HomePageUiState.Update` and `MainMenuUiState.Update` sample only and do not repair. The broad 500 ms polling repair, later Update repair loop, and global `GridLayoutGroup.constraintCount` setter normalization were removed after root-cause proof showed SaveSlots was misapplying `ResetLayoutSize(2)` through an inherited Select hook.
- Why this point: manual QA showed the title homepage menu and in-save pause menu could switch to a two-column/vertical layout after DTMAPI install. Stack diagnostics in `GAME-SMOKE/20260613-142631` proved the delayed homepage write came from `SaveSlotsService.TryResetLayoutSize -> RestoreOfficialSavePanel -> EnsureOfficialSavePanelPageForSelection -> GameDataPanelSelectPrefix`, where the inherited `DolocGridUI<T>.Select` hook reached `HomePageTextMenu`.
- Failure behavior: native UI continues unchanged if diagnostics hooks are unavailable. When installed, the service logs source, target type, current UI state, DTMAPI input context, requested total/line counts, slot counts, visible slot counts, layout group constraint/count, and a truncated stack for interesting title/pause targets or observed two-column values. Update sampling logs `twoColumnObserved=True` without trying to repair it.
- Mods/tests depending on it: DTMAPI support/manual QA only.
- Evidence:
  - Build: 2026-06-13 `20260613-0001` package/icon build-test-install passed. Follow-up `20260613-0002` `git diff --check`, Release build/test, package staging/install, and status check passed; installed GameBridge DLL length is `970752`. Follow-up `20260613-0003` `git diff --check`, Release build/test, package staging/install, and status check passed after removing the polling repair and adding concrete writer diagnostics; installed GameBridge DLL length is `975360`. Revalidation `20260613-0006` passed `git diff --check`, Release build/test, install, and title smoke. Intermediate setter normalization `20260613-0008` passed Release tests and smokes but was superseded by SaveSlots root fix `20260613-0013`, which passed Release build, `git diff --check`, title, pause, and MoreSaves smokes without installing a setter normalization hook.
  - Save: title homepage smoke plus local slot 3 / index 2 pause-menu layout smoke.
  - Log line: root-cause diagnostic `GAME-SMOKE/20260613-142631` logged the inherited Select stack through `SaveSlotsService.TryResetLayoutSize`, `RestoreOfficialSavePanel`, and `GameDataPanelSelectPrefix`. Root-fix title smoke `GAME-SMOKE/20260613-143434` passed title button/menu screenshot checks and logged `Save.MoreSlotsUiPaging = not-required` with no `requestedCount=2` or `Native UI layout normalized`. Pause smoke `GAME-SMOKE/20260613-143621` observed 14 samples over 12 seconds with `layoutConstraintCount=8`, `rowCount=1`, and `twoColumnObserved=False`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260613-143434` has title button/menu screenshot evidence and clean process/fatal checks. `docs/debug/evidence/GAME-SMOKE/20260613-143621/DTMAPI-evidence/UI-007/20260613-143712` has pause-menu initial/final screenshots and samples.
- Regression cases: TITLE-PAUSE-AUTOFISHING-ROOTFIX-20260613, TITLE-PAUSE-AUTOFISHING-FOLLOWUP-20260613, INSTALL-AUTOFISHING-UI-DIAGNOSTICS-20260613, TITLE-HOMEPAGE-GUARD-REMOVAL-20260612

## UI: TitleSettingsPaging

- Status: experimental
- Public surface: none; internal first-party title Settings UI over `IConfigMenuRuntime`, `DtmManagerViewModel`, and diagnostics snapshots.
- Game build: 23465763 workshop
- Game method/type: reflected Unity title canvas hosted by `ReflectedTitleMenuSettingsUi`; no raw public mod API or native gameplay hook. Official title/pause layout evidence belongs separately to `UI.NativeLayoutDiagnostics`; the title settings entry itself does not mutate official menu layout groups.
- Patch type: Unity UI reflection and DTMAPI runtime UI state; deterministic pager buttons for title config and Manager pages. The earlier title-homepage `HomePageTextMenu` single-column guard was removed in `20260612-0017` after manual feedback that it affected pause-menu horizontal icons. Update `20260613-0002` restores only native-owner layout invariants outside the title settings UI host.
- Why this point: the compact title-page DTMAPI icon entry, config page list, long config item pages, and Manager Mods/Errors/Warnings/Hooks/Features lists must remain reachable without exposing internal config edit verbs to ordinary mods or disturbing the official homepage menu layout.
- Failure behavior: if Unity UI types are unavailable, the title UI records `title-ui-types-missing` and no gameplay hook is affected. Paging state is in-memory and rebuilt with the reflected panel. Title Settings itself does not mutate official title/pause menu layout groups; official menu repair evidence belongs to `UI.NativeLayoutDiagnostics`.
- Mods/tests depending on it: DTMAPI title Manager smoke, ConfigMenu smoke screenshots, user manual QA for Manager/config visibility.
- Evidence:
  - Build: 2026-06-12 bottom-layer Release build/test passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`; `git diff --check` passed with line-ending warnings only.
  - Save: title homepage for UI entry/config/Manager evidence; third local save slot / index 2 for runtime follow-up smokes.
  - Log line: `GAME-SMOKE/20260612-165456` verifies the bottom-layer title/config route before the icon restoration follow-up; `GAME-SMOKE/20260612-204350` verifies the compact icon button and title settings menu screenshots, but its `UI.TitleHomeMenuLayout` guard evidence is superseded by `20260612-0017` because that guard was removed. `GAME-SMOKE/20260612-170119` verifies Manager Status, Mods, Errors, Hooks, Features, Logs, summary copy/fallback, and logs export.
  - 2026-06-13 packaged install evidence: package staging/install passed and `check-dtmapi-status.ps1` reported `[OK] DTMAPI title icon asset` for `D:\steam\steamapps\common\Doloc Town\BepInEx\plugins\DTMAPI\assets\branding\dtmapi-icon.png` with length `8018`. Title revalidation `GAME-SMOKE/20260613-114113` passed after `UI.NativeLayoutDiagnostics` owner-state homepage repair.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260613-114113` has latest title button/menu screenshot evidence; `docs/debug/evidence/GAME-SMOKE/20260612-204350` has earlier `DTMAPI-evidence/UI-004/20260612-204434/title-settings-button.png` and `title-settings-menu.png`; `docs/debug/evidence/GAME-SMOKE/20260612-170119` has Manager `UI-004/20260612-170153`; all cited runs have clean process/fatal checks.
- Regression cases: INSTALL-AUTOFISHING-UI-DIAGNOSTICS-20260613, BOTTOM-LAYER-REFACTOR-20260612, UI-001, CONFIG-006

## Hook: CustomEntities.CoreRegistry

- Status: verified
- Public surface: `ICustomAnimalApi`, `ICustomMonsterApi`, `ICustomAttackApi`, `ICustomDroneApi`
- Game build: 23465763 workshop
- Game method/type: DTMAPI Core runtime registry and save-boundary lifecycle
- Patch type: runtime dispatch
- Why this point: custom entity definitions, snapshots, lifecycle events, owner cleanup, and failure reasons are DTMAPI-owned registry state and do not require raw game object access; the public registry contracts remain `StableCandidate`.
- Failure behavior: invalid IDs and duplicates return result DTO errors; provider/listener exceptions are recorded under the owner; native spawn/summon/execute requests return `runtime-creation-blocked` until GameBridge adapters are verified.
- Mods/tests depending on it: no player-facing mod in 0.4.0; `DTMAPI.UnitTests` and internal `DTMAPI.CustomEntityApiSmokeHarness`.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors; unit test `CustomEntityRegistriesValidateRegistrationDuplicateCleanupAndSnapshots` verifies invalid ID, duplicate ID, four-family registration, snapshots/status, blocked requests, save-boundary cleanup, and owner cleanup.
  - Save: local slot 3 / index 2 in `GAME-SMOKE/20260610-045414`.
  - Log line: `GAME-SMOKE/20260610-045414` logs `CustomEntities.CoreRegistry = verified. StableCandidate 0.4.0 custom entity registry contracts are registered...`, `Smoke.CustomEntityApis = verified`, and blocked request summary `requests=runtime-creation-blocked`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-045414`; result has `CustomEntityApis=Passed`, `SaveLoaded=Passed`, `HookProbe=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Regression cases: CUSTOMENTITY-STATUS-NAMING-20260610, CUSTOM-ENTITY-040-STABLE-API

## Hook: CustomAnimals.RegistryContract

- Status: configured-blocked
- Public surface: `ICustomAnimalApi`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.AnimalManager.CreateAnimal`, `DolocTown.Animal`, animal work classes, `AnimalViewer`, and animal save data paths.
- Patch type: reflection research/status path; no native creation patch installed in 0.4.0.
- Why this point: native animals depend on `AnimalInfo` proto data, home/current rooms, feed/excrement/breeding work, produce rules, UI viewer rows, and save data. StableCandidate registry DTOs must stay separated from these fragile runtime details.
- Failure behavior: registration/query/snapshot works; `RequestSpawn` returns `runtime-creation-blocked` with adapter details instead of creating an unsafe animal.
- Mods/tests depending on it: internal custom entity smoke harness only.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2 in `GAME-SMOKE/20260610-045414`.
  - Latest naming: `GAME-SMOKE/20260610-045414` logs `CustomAnimals.RegistryContract = configured-blocked` with details `StableCandidate registry contract; runtime creation remains blocked`.
  - Historical smoke summary: `Smoke exercise CustomEntityApis OK registered=animal,monster,attack,drone; invalidAnimal=invalid-definition; duplicateAnimal=duplicate-definition-id; requests=runtime-creation-blocked; cleanupRemoved=5; lifecycleEvents=2/3/2/2.`
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-045414`.
- Regression cases: CUSTOMENTITY-STATUS-NAMING-20260610, CUSTOM-ENTITY-040-STABLE-API, ANIMAL-001

## Hook: CustomMonsters.RegistryContract

- Status: configured-blocked
- Public surface: `ICustomMonsterApi`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.MonsterController`, `MonsterGroupManager`, `MonsterAI_Target`, `MonsterStateManager`, `MonsterAttackBehaviour`, and `MonsterAttackBehaviourManager`.
- Patch type: reflection research/status path; no native creation patch installed in 0.4.0.
- Why this point: native monsters require verified spawn group, AI, movement, attack, damage, death/drop, and despawn adapters. StableCandidate registry DTOs define the author contract without exposing raw update loops.
- Failure behavior: registration/query/spawn-table/snapshot works; `RequestSpawn` returns `runtime-creation-blocked` until native adapters are verified.
- Mods/tests depending on it: internal custom entity smoke harness only.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2 in `GAME-SMOKE/20260610-045414`.
  - Latest naming: `GAME-SMOKE/20260610-045414` logs `CustomMonsters.RegistryContract = configured-blocked` with details `StableCandidate registry contract; runtime creation remains blocked`.
  - Historical smoke summary: `Smoke exercise CustomEntityApis OK ... cleanupRemoved=5`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-045414`.
- Regression cases: CUSTOMENTITY-STATUS-NAMING-20260610, CUSTOM-ENTITY-040-STABLE-API

## Hook: CustomAttacks.RegistryContract

- Status: configured-blocked
- Public surface: `ICustomAttackApi`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.BulletFactory`, `BulletManager`, `Bullet`, `BulletEntity`, `PhysicalDamageBox`, `AttackInfo`, and `AttackHitInfo`.
- Patch type: reflection research/status path; no native projectile/damage patch installed in 0.4.0.
- Why this point: projectile creation and barrage behavior must preserve native collision and damage ownership without exposing physics/collider/decompiled types to public API consumers.
- Failure behavior: registration/query/snapshot works; `SpawnProjectile` and `ExecuteAttack` return `runtime-creation-blocked` until BulletManager/collision/damage adapters are verified.
- Mods/tests depending on it: internal custom entity smoke harness only; future monster and drone APIs reference attack IDs.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2 in `GAME-SMOKE/20260610-045414`.
  - Latest naming: `GAME-SMOKE/20260610-045414` logs `CustomAttacks.RegistryContract = configured-blocked` with details `StableCandidate registry contract; runtime creation remains blocked`.
  - Historical smoke summary: `Smoke exercise CustomEntityApis OK ... requests=runtime-creation-blocked`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-045414`.
- Regression cases: CUSTOMENTITY-STATUS-NAMING-20260610, CUSTOM-ENTITY-040-STABLE-API

## Hook: CustomDrones.RegistryContract

- Status: configured-blocked
- Public surface: `ICustomDroneApi`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.Drone`, `DroneController`, `DroneWeapon`, `DroneWeaponGun`, `DroneWeaponSword`, `DronePanel`, and `DolocAPI.EquipDrone`.
- Patch type: reflection research/status path; no native drone creation/equipment patch installed in 0.4.0.
- Why this point: native drones combine controller, weapon, equipment, movement, owner binding, UI panel, and save/persistence behavior. StableCandidate registry definitions keep future mods away from fragile raw types.
- Failure behavior: registration/query/snapshot works; `RequestSummon`, `Equip`, and `SetMode` return `runtime-creation-blocked` until drone controller/weapon/equipment adapters are verified.
- Mods/tests depending on it: internal custom entity smoke harness only.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2 in `GAME-SMOKE/20260610-045414`.
  - Latest naming: `GAME-SMOKE/20260610-045414` logs `CustomDrones.RegistryContract = configured-blocked` with details `StableCandidate registry contract; runtime creation remains blocked`.
  - Historical smoke summary: `Smoke exercise CustomEntityApis OK ... requests=runtime-creation-blocked`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-045414`.
- Regression cases: CUSTOMENTITY-STATUS-NAMING-20260610, CUSTOM-ENTITY-040-STABLE-API

## Diagnostic: Startup.SegmentTiming

- Status: experimental
- Public surface: DTMAPI startup logs and smoke evidence only.
- Game build: 23465763 workshop
- Game method/type: Steam launch wall-clock checkpoints plus BepInEx `Awake`, DTMAPI runtime start, manifest/official MODS/workshop scans, content query index, Harmony initialization, icon loading, and mod loading.
- Patch type: timing instrumentation around existing bootstrap/runtime paths plus smoke-harness timeline capture.
- Why this point: the occasional 30s launch must be compared through segment logs instead of guessed optimizations.
- Failure behavior: if Steam never creates `DolocTown.exe`, no DTMAPI segment log exists; that is tracked as a launch-blocking smoke issue rather than DTMAPI startup slowness.
- Mods/tests depending on it: smoke harness, repeated startup sampler, startup regression matrix.
- Evidence:
  - Build: DTMAPI 0.2.1 local build passed 2026-06-01 with 0 errors; earlier 0.1.13 build/unit passed 2026-05-31
  - Save: n/a
  - Log line: startup segment logs for `Bootstrap.Awake`, `Bootstrap.HarmonyInitialize`, `Core.Start`, `ManifestScan`, `OfficialModsScan`, `WorkshopScan`, `ContentQueryIndex`, `ModLoad`, and `IconLoad`; smoke timeline fields `LaunchToProcessMs`, `LaunchToDtmapiLogFileMs`, and `LaunchToStartupPatternMs`.
  - Screenshot/report: normal collected logs `docs/debug/evidence/GAME-SMOKE/20260531-114255`, `docs/debug/evidence/GAME-SMOKE/20260531-115235`, `docs/debug/evidence/GAME-SMOKE/20260531-124429`, `docs/debug/evidence/GAME-SMOKE/20260531-125453`, `docs/debug/evidence/GAME-SMOKE/20260531-125613`, `docs/debug/evidence/GAME-SMOKE/20260531-160943`, and `docs/debug/evidence/GAME-SMOKE/20260531-161545`; launch-blocked evidence `docs/debug/evidence/GAME-SMOKE/20260531-120507` and `docs/debug/evidence/GAME-SMOKE/20260531-123258`; analyzer report `docs/debug/evidence/STARTUP-COMPARE/20260531-162725/startup-analysis.md`; integrated title smoke evidence `docs/debug/evidence/GAME-SMOKE/20260531-163330/startup-analysis.md`; integrated timeline smoke evidence `docs/debug/evidence/GAME-SMOKE/20260531-164214/startup-timeline.json` and `docs/debug/evidence/GAME-SMOKE/20260531-164214/startup-analysis.md`; repeated startup aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-165252/startup-analysis.md` with primary timelines `docs/debug/evidence/GAME-SMOKE/20260531-165253/startup-timeline.json` and `docs/debug/evidence/GAME-SMOKE/20260531-165447/startup-timeline.json`; extended baseline aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-170717/startup-analysis.md` with five normal primary samples from `docs/debug/evidence/GAME-SMOKE/20260531-170718` through `docs/debug/evidence/GAME-SMOKE/20260531-171105`; fast pure-startup aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-171630/startup-analysis.md` with five normal primary samples from `docs/debug/evidence/GAME-SMOKE/20260531-171631` through `docs/debug/evidence/GAME-SMOKE/20260531-171859`; threshold-summary aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-172605/startup-sample-summary.md` with two normal primary samples `docs/debug/evidence/GAME-SMOKE/20260531-172606` and `docs/debug/evidence/GAME-SMOKE/20260531-172640`; stop-on-slow aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-173318/startup-samples.md` proved `-StopOnSlowSample` using artificial `SlowLaunchThresholdMs=1`, stopped after primary sample `docs/debug/evidence/GAME-SMOKE/20260531-173318`, and did not indicate DTMAPI runtime slowness; real-threshold stop-on-slow baseline `docs/debug/evidence/STARTUP-SAMPLES/20260531-174136/startup-sample-summary.md` captured six normal samples from `docs/debug/evidence/GAME-SMOKE/20260531-174136` through `docs/debug/evidence/GAME-SMOKE/20260531-174425` with `SlowLaunchCount=0`, `SlowRuntimeCount=0`, `LaunchToStartupPatternMs=6136-6165`, and `Bootstrap.Awake totalMs=587-615`; monitor normal validation `docs/debug/evidence/STARTUP-MONITOR/20260531-175403/startup-monitor.md` captured two normal samples and `Triggered=False`; monitor artificial trigger validation `docs/debug/evidence/STARTUP-MONITOR/20260531-175538/startup-monitor.md` stopped on `slow-launch-threshold` with artificial `SlowLaunchThresholdMs=1`; real-threshold monitor run `docs/debug/evidence/STARTUP-MONITOR/20260531-180222/startup-monitor.md` captured two batches / six normal samples in `docs/debug/evidence/STARTUP-SAMPLES/20260531-180222` and `docs/debug/evidence/STARTUP-SAMPLES/20260531-180406`, with `Triggered=False`, `LaunchToStartupPatternMs=6142-7172`, and `Bootstrap.Awake totalMs=589-608`; follow-up real-threshold monitor run `docs/debug/evidence/STARTUP-MONITOR/20260531-181048/startup-monitor.md` captured three batches / nine normal samples in `docs/debug/evidence/STARTUP-SAMPLES/20260531-181048`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-181230`, and `docs/debug/evidence/STARTUP-SAMPLES/20260531-181413`, with `Triggered=False`, `LaunchToStartupPatternMs=6139-7170`, and `Bootstrap.Awake totalMs=586-612`.
  - Failure-capture validation: title-settings monitor `docs/debug/evidence/STARTUP-MONITOR/20260531-182957/startup-monitor.md` stopped with `sample-failure-count-1`, while linked `docs/debug/evidence/STARTUP-SAMPLES/20260531-182958/startup-analysis.md` classified the primary smoke `docs/debug/evidence/GAME-SMOKE/20260531-182958` as `NormalDtmapiStartup` with `LaunchToStartupPatternMs=6153` and `Bootstrap.Awake totalMs=657`; normal no-title recheck `docs/debug/evidence/STARTUP-SAMPLES/20260531-183210/startup-sample-summary.md` recorded `LaunchToStartupPatternMs=6159`, `Bootstrap.Awake totalMs=610`, and no leftover process.
  - Longer real-threshold monitor: `docs/debug/evidence/STARTUP-MONITOR/20260531-183958/startup-monitor.md` captured four batches / twelve normal startup samples in `docs/debug/evidence/STARTUP-SAMPLES/20260531-183958`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-184141`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-184325`, and `docs/debug/evidence/STARTUP-SAMPLES/20260531-184508`, with `Triggered=False`, `LaunchToStartupPatternMs=6132-6172`, `Bootstrap.Awake totalMs=584-638`, and no leftover process.
  - Follow-up longer real-threshold monitor: `docs/debug/evidence/STARTUP-MONITOR/20260531-185341/startup-monitor.md` captured five batches / twenty normal startup samples in `docs/debug/evidence/STARTUP-SAMPLES/20260531-185341`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-185551`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-185759`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-190010`, and `docs/debug/evidence/STARTUP-SAMPLES/20260531-190217`, with `Triggered=False`, `LaunchToStartupPatternMs=6133-7181`, `Bootstrap.Awake totalMs=586-617`, and no leftover process.
  - External launch observer: `docs/debug/evidence/STARTUP-OBSERVE/20260531-191241/startup-analysis.md` verified that no-launch timeout does not reuse stale logs; `docs/debug/evidence/STARTUP-OBSERVE/20260531-191352/startup-analysis.md` captured an external Steam URL launch as `NormalDtmapiStartup` with `LaunchToStartupPatternMs=7236`, `Bootstrap.Awake totalMs=230`, and cleanup evidence `process-check-after-cleanup.txt` showing no leftover process.
  - Startup comparison gate: `docs/debug/evidence/STARTUP-COMPARE/20260531-192226/startup-comparison.md` compared normal external-launch evidence with blocked/no-fresh-log evidence and reported `OnlyPreRuntimeOrBlockedAbnormalEvidence`, so the true 30-second DTMAPI runtime comparison remains pending.
- Regression cases: STARTUP-001, SMOKE-002

## Hook: GameLoop.GameLaunched

- Status: verified
- Public surface: `helper.Events.GameLoop.GameLaunched`
- Game build: 23465763 workshop
- Game method/type: DTMAPI runtime lifecycle after mod `Entry`
- Patch type: runtime dispatch
- Why this point: lets mods subscribe during `Entry` and receive a first ready signal.
- Failure behavior: if a mod handler throws, DTMAPI records the owning mod error.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`, `DTMAPI.HelloDtmMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: n/a
  - Log line: `HookProbe GameLaunched OK`
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260530-071017`
- Regression cases: HOOK-001

## Hook: GameLoop.UpdateTicked

- Status: verified
- Public surface: `helper.Events.GameLoop.UpdateTicked`
- Game build: 23465763 workshop
- Game method/type: BepInEx plugin frame callback with fallback pump
- Patch type: Unity callback / SynchronizationContext fallback / coroutine fallback
- Why this point: no game internals exposed; enough for first QoL mods and smoke probes.
- Failure behavior: if Unity `Update` does not fire, DTMAPI uses the fallback pump and logs the source.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`, official-local `Yuuka.DTMAPI.ActionSpeed` hotload smoke
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 3 / index 2
  - Log line: `HookProbe UpdateTicked OK tick=1`
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-080759`
- Regression cases: LOOP-001

## Hook: GameLoop.OneSecondUpdateTicked

- Status: verified
- Public surface: `helper.Events.GameLoop.OneSecondUpdateTicked`
- Game build: 23465763 workshop
- Game method/type: DTMAPI timer derived from frame loop
- Patch type: runtime dispatch
- Why this point: throttled periodic work without mod-side timers.
- Failure behavior: no event if frame loop stops.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.11 local
  - Save: local slot 3 / index 2
  - Log line: `HookProbe OneSecondUpdateTicked OK second=1`
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-202404`
- Regression cases: LOOP-002

## Hook: Save.SaveLoaded

- Status: verified
- Public surface: `helper.Events.Save.SaveLoaded`
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.OnAfterLoadArchiveData` event, fallback candidate `DolocAPI.AfterLoadArchiveData(bool isNewGame)`
- Patch type: UnityEvent subscription, Harmony Postfix fallback
- Why this point: Save_Load map identifies it as a medium/risky post-load candidate with no public raw game type exposure.
- Failure behavior: status remains pending; no save event is exposed as verified.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 3 / index 2
  - Log line: `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`
  - 2026-06-10 lifecycle isolation log line: `GAME-SMOKE/20260610-012052`, `GAME-SMOKE/20260610-012242`, `GAME-SMOKE/20260610-012400`, `GAME-SMOKE/20260610-012750`, and `GAME-SMOKE/20260610-012907` all record `SaveLoaded=Passed` after `DolocTownHookCallbacks` wrapped each SaveLoaded cleanup/restore/runtime notify/smoke-mark callback in `SafeCallback`; the same logs contain no `Lifecycle callback failed` entries.
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-080759`
- Regression cases: SAVE-001

## Hook: Save.LoadGameRequested

- Status: verified
- Public surface: diagnostics/internal save-load evidence
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.LoadGame(int index)` / fallback `DolocTown.GameData.DataPersistenceManager.LoadGame`
- Patch type: Harmony Prefix
- Why this point: records the requested save slot before `SaveLoaded` so public save events can carry a stable slot/index without raw game types.
- Failure behavior: `SaveLoaded` still dispatches with `SaveSlot=null` if the load request cannot be observed.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 3 / index 2
  - Log line: `LoadGame requested for slot/index 2.`
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-080759`
- Regression cases: SAVE-001

## Hook: Save.SaveSaving

- Status: verified
- Public surface: `helper.Events.Save.SaveSaving`
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.SaveGame` / fallback `DolocTown.GameData.DataPersistenceManager.SaveGame`
- Patch type: Harmony Prefix
- Why this point: exposes a pre-save event without raw save handles.
- Failure behavior: no save-saving event; diagnostics status remains pending/experimental.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 5 / index 4
  - Log line: `HookProbe SaveSaving OK slot=4`
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-081411`
- Regression cases: SAVE-002

## Hook: Save.SaveSaved

- Status: verified
- Public surface: `helper.Events.Save.SaveSaved`
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.SaveGame` / fallback `DolocTown.GameData.DataPersistenceManager.SaveGame`
- Patch type: Harmony Postfix
- Why this point: exposes a post-save event without raw save handles.
- Failure behavior: no save-saved event; diagnostics status remains pending/experimental.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 5 / index 4
  - Log line: `HookProbe SaveSaved OK slot=4`
  - 2026-06-10 lifecycle isolation log line: `GAME-SMOKE/20260610-012750` records `InstantSave=Passed`, `SaveSaving hook dispatched. slot/index=2`, `SaveSaved hook dispatched. slot/index=2`, clean exit, no fatal popup, and no `Lifecycle callback failed` entries after save prefix/postfix callbacks were wrapped independently.
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-081411`
- Regression cases: SAVE-002

## Diagnostic: Debug.InstantSave

- Status: experimental
- Public surface: smoke/debug testing feature only; not a stable public API.
- Game build: 23465763 workshop
- Game method/type: native `DolocAPI.SaveGame(int index)` with reflected room/position/time snapshots before and after the save call. Historical debug-only reload evidence used `DolocAPI.LoadGame(int index)`, but the 0.2.6 player-facing Y console path is save-only.
- Patch type: smoke/debug reflection call over the native save method; public APIs do not expose raw save data.
- Why this point: lets hook/API validation create a checkpoint in field, fishing, or machine-test scenes while using the game's own save path instead of writing save files directly.
- Failure behavior: `reloadAfterSave=true` now returns `reload-disabled`; the player-facing console does not perform save-then-immediate-load because manual QA showed active-scene residue.
- Mods/tests depending on it: smoke harness only.
- Evidence:
  - Build: DTMAPI 0.1.13 local build/unit passed 2026-05-31
  - Save: local slot 3 / index 2
  - Log line: 0.2.6 focused smoke `GAME-SMOKE/20260605-181224` logs `Smoke exercise InstantSave OK ... sameRoom=True, distance=0, reloadDisabled=True`; historical 0.1.13 debug-only evidence logged `LoadGame requested for slot/index 2` and `limitation=none`.
  - Screenshot/report: smoke result `docs/debug/evidence/GAME-SMOKE/20260531-115149`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-115235`; first early-sample limitation attempt `docs/debug/evidence/GAME-SMOKE/20260531-114928`.
- Regression cases: SAVE-003

## Hook: Workshop.ReloadMods

- Status: verified
- Public surface: `helper.Events.Workshop.ModListChanged`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.Config.ModManager.ReloadMods`
- Patch type: Harmony Postfix
- Why this point: respects official ModManager and uses DTMAPI UI only for status/diagnostics. As of 2026-05-31, the notification also lets DTMAPI hot-load newly enabled, not-yet-loaded code mods without taking over official enable/disable ownership.
- Failure behavior: DTMAPI startup scan still works; if a mod is already loaded and later officially disabled, DTMAPI does not attempt DLL unload and marks config as restart-required.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.12 local build/unit passed 2026-05-31
  - Save: title homepage smoke
  - Log line: `Workshop ModListChanged hook dispatched. discoveredMods=5 hotLoaded=1`, followed by later `hotLoaded=0`
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260531-011745`, state backup/restored in `docs/debug/evidence/OFFICIAL-HOTLOAD/20260531-011650`
- Regression cases: WORKSHOP-001, WORKSHOP-002

## Hook: UI.TitleSettingsEntry

- Status: verified
- Public surface: title-page DTMAPI Settings button; config pages reached through `IUiHelper.OpenConfigPage`.
- Game build: 23465763 workshop
- Game method/type: `HomePageUiState` active-context detection with a reflected Unity UI Canvas. Blocking title-page states such as `ModUiState`, `GameDataUiState`, and confirmation/menu panels are detected first so the compact DTMAPI icon button only appears on the unobstructed title homepage.
- Patch type: reflection-created Unity UI Canvas plus EventSystem fallback, no official ModManager enable/disable override.
- Why this point: gives players a compact visible DTMAPI entry on the title homepage while leaving official mod enable/disable/order controls in the official path.
- Failure behavior: if Unity UI creation fails or the active page is not the unobstructed `HomePageUiState`, DTMAPI hides/recreates the canvas and keeps runtime ticks isolated from UI failures.
- Mods/tests depending on it: migrated config pages for `Yuuka.DTMAPI.ActionSpeed`, `Yuuka.DTMAPI.AutoFishing`, `Yuuka.DTMAPI.OneActionComplete`.
- Evidence:
  - Build: DTMAPI 0.1.13 local build/unit passed 2026-05-31
  - Save: title homepage and local slot 3 / index 2 return-to-title lifecycle
  - Log line: `DTMAPI title settings button visible on HomePageUiState.`, `Startup segment IconLoad ... result=loaded`, `Title settings button screenshot OK`, and `Smoke exercise TitleButtonLifecycle OK startupOpen=true, closed=true, saveLoaded=True, returnedContext=HomePageUiState, reopened=true`. The former `UI.TitleHomeMenuLayout` guard log is historical only and no longer emitted after `20260612-0017`.
  - 2026-06-10 lifecycle isolation log line: `GAME-SMOKE/20260610-012907` records `TitleButtonLifecycle=Passed`, `ReturnedToTitle hook dispatched.`, clean exit, no fatal popup, and no `Lifecycle callback failed` entries after returned-to-title cleanup/restore/runtime notify callbacks were wrapped independently.
  - Screenshot/report: current compact icon and menu smoke `docs/debug/evidence/GAME-SMOKE/20260612-204350/DTMAPI-evidence/UI-004/20260612-204434/title-settings-button.png` and `title-settings-menu.png`; older position/localization screenshot `docs/debug/evidence/GAME-SMOKE/20260531-042239/DTMAPI-evidence/UI-004/20260531-042057/title-settings-button.png`; lifecycle smoke result `docs/debug/evidence/GAME-SMOKE/20260531-111831`; returned-title screenshot `docs/debug/evidence/GAME-SMOKE/20260531-111940/DTMAPI-evidence/UI-006/20260531-111940/title-settings-after-return.png`.
- Regression cases: UI-003, UI-004, UI-006

## Hook: UI.TitleSettingsMenu

- Status: verified
- Public surface: title-page DTMAPI Settings menu with Config, Mods, Status, Errors, Hooks, and Logs pages.
- Game build: 23465763 workshop
- Game method/type: DTMAPI reflected Unity UI Canvas opened from the title settings entry.
- Patch type: reflection-created Unity UI Canvas plus EventSystem fallback.
- Why this point: replaces the temporary F8/F10 overlay route with a title-screen menu that can edit DTMAPI mod config without taking over official mod management.
- Failure behavior: menu is closed when leaving `HomePageUiState`; ordinary mod updates continue to tick if UI rendering has a recoverable failure.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`, `DTMAPI.ConfigMenuExample`, `Yuuka.DTMAPI.ActionSpeed`, `Yuuka.DTMAPI.AutoFishing`, `Yuuka.DTMAPI.OneActionComplete`.
- Evidence:
  - Build: DTMAPI 0.1.13 local build/unit passed 2026-05-31
  - Save: title homepage and local slot 3 / index 2 return-to-title lifecycle
  - Log line: `DTMAPI title settings button clicked.`, `Smoke automation opened DTMAPI title settings menu.`, `DTMAPI title settings menu opened.`, `Title settings menu screenshot OK`, and lifecycle reopen after returning to `HomePageUiState`.
  - Screenshot/report: title UI visual evidence `docs/debug/evidence/GAME-SMOKE/20260531-042239/DTMAPI-evidence/UI-004/20260531-042057/title-settings-menu.png`; lifecycle smoke result `docs/debug/evidence/GAME-SMOKE/20260531-111831`; logs/screenshots `docs/debug/evidence/GAME-SMOKE/20260531-111940`.
- Regression cases: UI-003, UI-004, UI-006, CONFIG-003

## Diagnostic: Smoke.ManagerStatusPage

- Status: verified
- Public surface: none; `run-game-smoke.ps1` Status page result-field evidence only.
- Game build: 23465763 workshop
- Game method/type: DTMAPI title settings UI opened from `HomePageUiState`, then `UiRuntimeService.OpenDtmApiStatusPage()` refreshes the internal Manager view model.
- Patch type: smoke harness automation and reflected Unity screenshot capture; no Harmony hook, public API, or ConfigMenu contract change.
- Why this point: validates that the first real Manager UI consumer is visible on the title page and that support-facing summary text can be captured independently from the older Config screenshot rotation.
- Failure behavior: if the Status page cannot open, the Manager model is unavailable, summary text is missing, or screenshot capture does not produce a file, `ManagerStatusPage`, `ManagerStatusSummaryText`, `ManagerStatusPageScreenshot`, or `ManagerStatusPageScreenshotFile` fails and the smoke run fails.
- Mods/tests depending on it: compact web audit package Manager Status evidence and future Manager UI page work.
- Evidence:
  - Build: 2026-06-11 `git diff --check`, PowerShell AST parse, Release build, and Release unit tests passed.
  - Save: local slot 3 / index 2.
  - Log line: DirectExe smoke `GAME-SMOKE/20260611-100827` logs `Smoke automation opened DTMAPI Manager Status page.`, `Manager Status summary text OK overall=ready; mods=loaded:16,blocked:0,disabled:0; diagnostics=errors:0,warnings:0; hooks=failed:0,missing:0; features=failed:0,degraded:0; report=ready; ...`, and `Manager Status page screenshot OK screenshot=...manager-status-page.png`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260611-100827`; copied title/menu/status screenshots exist under the smoke evidence package, while the source screenshot folder contains `title-settings-button.png`, `title-settings-menu.png`, `manager-status-page.png`, and `summary.txt`.
  - Severity model: branch `codex/refactor-manager-status-severity-model` keeps the same smoke status IDs while moving missing hook and degraded feature counts into first-class internal `ManagerSummary` fields; DirectExe Status smoke `GAME-SMOKE/20260611-101818` verified `ManagerStatusPage`, `ManagerStatusSummaryText`, `ManagerStatusPageScreenshot`, screenshot file existence, clean exit, and summary text with `hooks=failed:0,missing:0` and `features=failed:0,degraded:0`.
  - Final hardening: `GAME-SMOKE/20260611-103601` on final `Refactor` verifies `ManagerStatusPage=Passed`, `ManagerStatusSummaryText=Passed`, `ManagerStatusPageScreenshot=Passed`, `ManagerStatusPageScreenshotFile=Passed`, title/menu screenshot checks, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed` after export/refresh safety, dedicated Status smoke, severity model, and Logs report-state branches merged.
- Regression cases: MANAGER-STATUS-PAGE-SMOKE-20260611, MANAGER-STATUS-SEVERITY-MODEL-20260611, MANAGER-STATUS-HARDENING-FINAL-20260611, UI-003, UI-004

## Diagnostic: Smoke.ManagerMvpPages

- Status: verified
- Public surface: none; `run-game-smoke.ps1 -AutoOpenTitleSettingsManagerMvp` title Settings result-field evidence only.
- Game build: 23465763 workshop
- Game method/type: DTMAPI title settings UI opened from `HomePageUiState`, then `UiRuntimeService` refreshes the internal Manager model while smoke automation visits Status, Mods, Errors, Hooks, Features, and Logs.
- Patch type: smoke harness automation, internal Manager view-model UI rendering, report export, and reflected Unity screenshot capture; no Harmony hook, public API, gameplay feature, or ConfigMenu contract change.
- Why this point: validates the Manager MVP support loop as a real UI consumer instead of a design-only view model: summary, row pages, and Logs export can be checked from the title page without raw log parsing.
- Failure behavior: if any page cannot open, Status summary copy/fallback cannot produce text, Logs export does not reach `exported`, the exported report path does not match the refreshed snapshot, or Status/Logs screenshots are missing, the corresponding `Manager*` result field fails and the smoke run fails.
- Mods/tests depending on it: compact web audit package Manager MVP evidence and future Manager UI page slices.
- Evidence:
  - Build: 2026-06-11 Release build/test and PowerShell AST parse passed.
  - Save: title homepage for Manager MVP; local slot 3 / index 2 for HookProbe regression.
  - Log line: DirectExe smoke `GAME-SMOKE/20260611-112148` logs `Smoke automation opened DTMAPI Manager Mods page.`, `Errors page.`, `Hooks page.`, `Features page.`, `Logs page.`, `Manager Logs export button OK status=exported pathMatch=matched path=D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260611-112232.zip`, `Manager Logs export state OK export=exported; pathMatch=matched; snapshotReport=ready`, and `Manager Logs page screenshot OK screenshot=...manager-logs-page.png`.
  - Result fields: `GAME-SMOKE/20260611-112148` records `ManagerStatusPage=Passed`, `ManagerStatusSummaryText=Passed`, `ManagerStatusPageScreenshotFile=Passed`, `ManagerModsPage=Passed`, `ManagerErrorsPage=Passed`, `ManagerHooksPage=Passed`, `ManagerFeaturesPage=Passed`, `ManagerLogsPage=Passed`, `ManagerLogsExportButton=Passed`, `ManagerLogsExportStateText=Passed`, `ManagerLogsPageScreenshotFile=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
  - Runtime regression: `GAME-SMOKE/20260611-112402` records `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
  - Final `Refactor` evidence: `GAME-SMOKE/20260611-113018` records all Manager MVP page/export/screenshot result fields as `Passed`, including `ManagerLogsExportButton=Passed` and `ManagerLogsExportStateText=Passed`; logs show `Manager Logs export button OK status=exported pathMatch=matched path=D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260611-113103.zip`.
  - Final runtime regression: `GAME-SMOKE/20260611-113156` records `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
  - Developer Preview polish: `GAME-SMOKE/20260611-123852` records `ManagerStatusSummaryCopy=Passed`, all Manager Status/Mods/Errors/Hooks/Features/Logs page fields as `Passed`, Logs export/path-match fields as `Passed`, and clean process/fatal checks. The DTMAPI log records `Manager Status summary copy OK status=copy-unavailable`, proving the runtime-log fallback path without depending on OS clipboard availability. HookProbe regression `GAME-SMOKE/20260611-124008` records `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Regression cases: MANAGER-UI-MVP-PHASE1-20260611, MANAGER-UI-MVP-PHASE1-FINAL-20260611, MANAGER-UI-DEV-PREVIEW-POLISH-20260611, UI-003, UI-004

## Hook: UI.ConfigMenuAdvancedControls

- Status: experimental
- Public surface: `IDtmConfigMenuApi.AddInlineBoolNumberOption`, `AddInlineBoolBoolOption`, `AddColorPresetOption`, bool/text-option `isVisible`/`canEdit`, `IConfigMenuItem.IsVisible`, and `DtmColorPreset`.
- Game build: 23465763 workshop
- Game method/type: DTMAPI title settings reflected Unity UI menu only; no raw game type exposure.
- Patch type: config registry plus reflected Unity UI rendering.
- Why this point: migrated mods need native-feeling compact controls without hard-coding mod-specific UI in each mod.
- Failure behavior: unsupported controls stay inside the config menu page and do not affect game runtime hooks; save/cancel/reset still use the normal config transaction model.
- Mods/tests depending on it: `Yuuka.DTMAPI.ActionSpeed`, `Yuuka.DTMAPI.AutoFishing`, `Yuuka.DTMAPI.AnimalHusbandryProgress`, `DTMAPI.SecondMotorMod`.
- Evidence:
  - Build: DTMAPI 0.2.3 local build passed 2026-06-03 with 0 errors.
  - Save: title homepage.
  - Log line: `GAME-SMOKE/20260603-052444/DTMAPI-latest.log` records `Smoke.TitleSettingsConfigPageScreenshot.action-speed = verified`, `auto-fishing = verified`, `animal-husbandry-progress = verified`, and `second-motor = verified`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260603-052444/DTMAPI-evidence/UI-004/20260603-052523/title-settings-config-action-speed.png` shows ActionSpeed inline bool+number rows plus same-row `自动装水`/`强化自动装水`; `title-settings-config-auto-fishing.png` shows same-row `自动完成小游戏`/`跳过小游戏`; `title-settings-config-animal-husbandry-progress.png` shows swatches without the right-side `Orange` label or non-Custom hex input; `title-settings-config-second-motor.png` shows the `异色飞行摩托` status page.
- Regression cases: CONFIG-008, CONFIG-009

## Hook: UI.DebugConsoleHost

- Status: experimental
- Public surface: `IDebugConsoleApi`, ordinary mod `DTMAPI.DebugConsoleMod`, and the in-save Unity Canvas debug console with source/category item browser plus time, movement, weather, and teleport controls.
- Game build: 23465763 workshop
- Game method/type: DTMAPI bootstrap Unity `Update` plus reflected Unity UI `Canvas`, `Button`, `Text`, `InputField`, and `EventTrigger`. Ordinary Y/Escape binding is registered by `DTMAPI.DebugConsoleMod`; the host consumes Y/Escape while open so gameplay hotkeys do not receive duplicate toggles. Item give uses Unity `Button` for left-click and current `Mouse1` visible-cell hit testing for right-click when reflected right-button events are not delivered. 0.2.9 keeps movement at `1x/2x/3x/4x`, expands the item page to 35 cells, extends source/category lists, and centers icons in the item cells. 0.3.1 adds an in-game smoke path that dispatches Y through the DTMAPI input event service and drives Escape/Y close through the same DebugConsole host state machine, avoiding flaky external key injection.
- Patch type: reflection-created native Unity UI host; no IMGUI/F8/F10 overlay route.
- Why this point: keeps the debug console out of the title screen and out of ordinary `BepInEx/plugins` mod placement, while letting a normal DTMAPI mod own the player-facing hotkey.
- Failure behavior: if UI construction fails, the host logs a runtime error and the ordinary mod keeps the game playable; when the menu is open, DTMAPI blocks normal mod updates/hotkeys through the UI boundary and publishes modal state for native input-isolation prefixes.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseDebugConsole` and `-AutoExerciseDebugConsoleMouseGive`.
- Evidence:
  - Build: DTMAPI 0.3.1 Release build/unit passed 2026-06-06 with 0 errors; only restricted-network NU1900 vulnerability-index warnings occurred.
  - Save: local slot 3 / index 2.
  - Log line: 0.3.1 Y-console smoke `GAME-SMOKE/20260606-150210` logs `Smoke exercise DebugConsoleHotkey OK openCount=8, escapeCloseCount=1, yCloseCount=6, shortTaps=10, holdNoFlicker=True`, `InstantSave=true`, `DebugTeleportCsv=true` with 80 rows, `DebugTeleport=true`, `DebugInventory=true`, `DebugWeather=true`, `DebugTime=true`, and `DebugMovement=true`. 0.2.9 `GAME-SMOKE/20260606-051958` remains the current mouse-give/35-cell layout proof, and 0.2.7 `GAME-SMOKE/20260605-230442` remains the native input-isolation and detailed `Mouse1` hit-test proof.
  - Official UI: `GAME-SMOKE/20260601-135332` shows `selected=Local.DTMAPI_YKeyConsole, title=Y键控制台`; disabled-state evidence `GAME-SMOKE/20260601-135531` logs `Skipping DTMAPI.DebugConsoleMod` when `Local.DTMAPI_YKeyConsole.enabled=false`, with state restored from `OFFICIAL-ENABLE/20260601-135520`.
  - Screenshot/report: current 0.3.1 hotkey/save/teleport evidence `docs/debug/evidence/GAME-SMOKE/20260606-150210`; title/config UI evidence `docs/debug/evidence/GAME-SMOKE/20260606-150928`; 0.2.9 screenshot `D:\steam\steamapps\common\Doloc Town\DTMAPI\evidence\DEBUG-CONSOLE-UI\20260606-052037\debug-console.png` remains the 35-cell item page, extended filters, and centered-icons visual proof.
- Regression cases: DEBUGCONSOLE-001, OFFICIAL-001, OFFICIAL-004, INPUT-001, MANUALQA-025-Y-CONSOLE, MANUALQA-026-MINE-Y-CONSOLE, MANUALQA-028-README, MANUALQA-031-REGRESSION-NEWCONTENT

## Hook: Debug.AdvancedYConsoleApis

- Status: verified
- Public surface: `IDebugConsoleApi.BindAdvanced`, `IAdvancedDebugApi`, `AdvancedTimeAdvanceKind`, `TimeScaleDebugResult`, `DebugValueResult`, `DebugCommandResult`, `CropMaturityResult`, `CreativeModeState`, `CreativeModeResult`, `TechPointDebugOption`, `SpawnDebugOption`, and `SpawnDebugResult`.
- Game build: 23465763 workshop
- Game method/type: GameBridge reflection over native safe wrappers and tables, including `ArchiveDataHandle.PassTimeNoControl`, `DolocAPI.OnWakeUp`, `DolocAPI.SetTimeScale`, `DolocAPI.RevertTimeScale`, `DolocAPI.AddTechPoint`, the official money command/fallback current-money path, archive tech-tree collections, crop `DEBUG_SetLevel`, runtime `DolocConfig.Tables.TbItem`, official-local `dtmapi_creative_generator` JSON content, official `DolocAPI.Command_GenerateMonster`, and current-room resource/monster host probes.
- Patch type: GameBridge-owned reflection, Harmony Prefix/Postfix for creative cost/time hooks, official command wrapper for monster spawn, and reflected Unity UI buttons; no raw Unity objects or decompiled Doloc Town types are exposed through the public API.
- Why this point: the official console contains powerful commands, so DTMAPI exposes only explicit whitelisted actions with typed DTO results and keeps fragile native access inside `DTMAPI.GameBridge.DolocTown` / bootstrap UI host.
- Failure behavior: missing native methods, unavailable spawn hosts, unavailable generator item, or incomplete creative hooks return failed result DTOs and hook-status lines instead of executing arbitrary console/Lua commands. Creative mode applies/restores only bounded GameInitConfig flags and Harmony hooks while enabled.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, reflected Y-console advanced panel, smoke harness `-AutoExerciseAdvancedDebug`.
- Evidence:
  - Build: DTMAPI 0.3.1 Release build/unit passed 2026-06-06 with 0 errors; only restricted-network NU1900 vulnerability-index warnings occurred.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260606-172855` logs `Debug.CreativeMode = verified` with `ignoreMaterialCost=True`, `skipMoneyVerifyInShop=True`, `ignoreSpiritCost=True`, `canAffordMoneyIntMax=True`, `costEnergyNoChange=True`, `noTimeHookInstalled=True`, and `generatorAvailable=True`; `Debug.CreativeGeneratorGive = verified`; `Debug.SpawnMonster = verified` through official `Command_GenerateMonster`; `Debug.SpawnResource = verified`; `Smoke exercise AdvancedDebug OK`; and `Smoke.AdvancedDebug = verified`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260606-172855`; result has `AdvancedDebug=true`, `SaveLoaded=true`, `ProcessExited=true`, `NoFatalInstanceWindow=true`, and `ForcedClose=false`. Historical blocker baseline: `docs/debug/evidence/GAME-SMOKE/20260606-155802`.
- Regression cases: YCONSOLE-030-ADVANCED
- Pending related paths: none for the active 0.3.0 advanced Y-console closure. Chest Locator Enhancer and Strong Planting Gun are covered by their own hook records.

## Hook: UI.DebugConsoleInputIsolation

- Status: experimental
- Public surface: no new public API; this is GameBridge-owned native input isolation for the modal Y-console host.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.AgentControllerState.EnterUICheck`, `DolocTown.AgentControllerState.UseTool`, and `DolocTown.AgentControllerState.UseItem`, gated by `DolocTownHookCallbacks.DebugConsoleModalOpen`.
- Patch type: Harmony Prefix.
- Why this point: the debug console's reflected UI can consume DTMAPI hotkeys, but native gameplay input still reaches backpack/menu/tool/item state unless the bridge swallows these native entry points while the modal is open.
- Failure behavior: prefixes return normal native behavior when the console is closed; if a target is missing, hook status remains pending/failed and the goal cannot claim native input isolation.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseDebugConsoleMouseGive`.
- Evidence:
  - Build: DTMAPI 0.2.8 Release build/unit passed 2026-06-06 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260605-230442` logs `Hook status: UI.DebugConsoleInputIsolation = experimental. Patched native UI toggles and tool/item entry points`, then `Debug console native input isolation active: AgentControllerState.EnterUICheck suppressed while DTMAPI console is open.` and `UI.DebugConsoleInputIsolation = verified. Native backpack/menu/tool/item input is swallowed while the DTMAPI Y console is open.` 0.2.8 smoke `GAME-SMOKE/20260606-031919` rechecks the same console/mouse/debug API flow with clean exit.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260606-031919` and retained isolation baseline `docs/debug/evidence/GAME-SMOKE/20260605-230442`; both have `DebugConsoleMouseGive=true`, `ProcessExited=true`, and no fatal popup.
- Regression cases: MANUALQA-027-ROOT-CAUSE, MANUALQA-028-README, DEBUGCONSOLE-001, DEBUGITEMS-001

## Hook: Debug.InventoryWeatherTeleportApis

- Status: experimental
- Public surface: `IInventoryDebugApi`, `IWeatherDebugApi`, `ITeleportDebugApi`, and their DTOs in `DTMAPI.Abstractions`, including 0.2.3 `InventoryDebugQuery.SourceId` and `InventoryDebugPage.Sources`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.Config.DolocConfig.Tables.TbItem`, `DolocAPI.QueryItemProto`, `DolocAPI.CanPlaceItem`, `DolocAPI.TryPlaceInBackpack`, `DolocAPI.CountItem`, `TbWeather`, `TimeArchiveData.GetWeatherInfoOfDay`, `ArchiveDataHandle.SetWeather`, `ArchiveDataHandle.PatchWeather`, `TbStation`, `StationInfo.MarkPointId/Title`, `TbMarkPoint`, `MarkPointInfo.RoomId/Position`, and `DolocAPI.DoTransport`.
- Patch type: GameBridge-owned reflection over native tables/methods. Public APIs expose stable DTOs, not raw decompiled game types.
- Why this point: debug menus need powerful game actions, but fragile Unity/Harmony/reflection details must stay inside `DTMAPI.GameBridge.DolocTown`.
- Failure behavior: inventory full, illegal items, missing native methods, weather parse failures, and rejected transport requests return result DTOs with failure reasons and write hook-status/log evidence; teleport exposes only a whitelist and records before/after snapshots.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseDebugInventory`, `-AutoExerciseDebugWeather`, `-AutoExerciseDebugTeleport`, and `-AutoExerciseDebugConsoleMouseGive`.
- Evidence:
  - Build: DTMAPI 0.2.8 Release build/unit passed 2026-06-06 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: 0.2.8 Y-console smoke `GAME-SMOKE/20260606-031919` logs `Smoke exercise DebugInventory OK` for base `wood` and official-local `crude_oil`, `Smoke exercise DebugWeather OK options=7 ... before=THUNDERSTORM, after=CLOUDY`, `Smoke exercise DebugTeleport OK destination=上游丘陵1-下端 ... changedRoom=True`, left mouse give OK, and right mouse give OK.
  - Screenshot/report: 0.2.8 evidence `docs/debug/evidence/GAME-SMOKE/20260606-031919`; 0.2.6 Y-console screenshot `docs/debug/evidence/GAME-SMOKE/20260605-181224/DTMAPI-evidence/DEBUG-CONSOLE-UI/20260605-181315/debug-console.png` shows localized/compact weather buttons and teleport rows.
- Regression cases: DEBUGITEMS-001, DEBUGWEATHER-001, DEBUGTELEPORT-001, MANUALQA-026-MINE-Y-CONSOLE, MANUALQA-028-README

## Hook: Debug.TimeMovementApis

- Status: experimental
- Public surface: `ITimeDebugApi`, `IMovementDebugApi`, `TimeDebugState`, `TimeSkipResult`, `MovementDebugState`, and `MovementSpeedResult`.
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.GlobalParameter.Hour2Min/Day2Hour/GameMinutes2Secs`, `ArchiveDataHandle.PassTimeNoControl`, `DolocAPI.OnWakeUp(false,true,false)`, `DolocAPI.agent.MotionAbility`, and native `MotionAbility.SetMoveScaler(float)`.
- Patch type: GameBridge-owned reflection over native time and player motion APIs. Public APIs expose DTOs, not raw game types.
- Why this point: debug time skipping and movement speed are powerful save-state/gameplay changes, so the ordinary mod only asks GameBridge for explicit experimental actions.
- Failure behavior: missing native time/motion members return failed result DTOs and hook-status messages; movement reset applies 1x again on explicit reset and `ReturnedToTitle`.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseDebugTime`, `-AutoExerciseDebugMovement`.
- Evidence:
  - Build: DTMAPI 0.2.8 Release build/unit passed 2026-06-06 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: 0.2.8 Y-console smoke `GAME-SMOKE/20260606-031919` logs debug time period transitions and `Smoke.DebugMovement = verified. levels=1x:12,2x:24,3x:36,4x:48, restored=True, finalSpeed=12`.
  - Screenshot/report: current time/movement smoke `docs/debug/evidence/GAME-SMOKE/20260606-031919`; process check says no `DolocTown.exe`. Earlier `GAME-SMOKE/20260601-140915` remains historical native-method proof, including the now-removed `0.5x` UI option.
- Regression cases: DEBUGTIME-001, DEBUGMOVE-001, MANUALQA-028-README

## Hook: ActionSpeed.ToolAnimation

- Status: verified for tool animation, core interaction/eat animation, bottled-water continuous drink, bottle fill, no-key auto-fill including 0.2.3 strong cooldown evidence, planting, harvest, resin, and vegetation slices.
- Public surface: `IActionSpeedApi.Configure`, `IActionSpeedApi.GetStatus`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.AgentStateTool.OnEnter`, `DolocTown.AgentStateTool.OnExit`, `DolocTown.AgentStateInteract.OnEnter/OnExit`, `DolocTown.AgentStateEat.OnEnter`, `DolocTown.AgentControllerState.UseItemContinues(float dt)`, `AgentStateBase.OnExit`, and GameBridge reflection over the body/tool/tool-collider/shared interaction animators owned by active states.
- Patch type: Harmony Postfix/Prefix plus scoped reflection writes; public API does not expose raw decompiled game types.
- Feature host owner: `ActionSpeedFeature` registers `IActionSpeedApi` through `ActionSpeedService`; `ActionSpeedHookBridge` owns ActionSpeed enter/continuous-use hook installation while `AgentStateLifecycleHookBridge` owns the shared `AgentStateTool.OnExit`, `AgentStateInteract.OnExit`, and `AgentStateBase.OnExit` patch points consumed by ActionSpeed restore and ActionCompletion fuel/feed. The original `ActionSpeed.ToolAnimation` / `ActionSpeed.InteractionAnimation` hook IDs and status text are preserved. `DolocTownGameBridge` owns the internal feature-status model for `Feature.ActionSpeed`, recording feature id, last operation, success/failure, failure count, and last error without adding public-like members to `IGameBridgeFeature`. `Smoke/Cases/ActionSpeedSmokeCase.cs` owns the `AutoExerciseActionSpeedTool`, `AutoExerciseActionSpeedConfigApply`, and `AutoExerciseActionSpeedInteraction` smoke case implementation; `SmokeHarness.cs` keeps scheduling and result-field ownership.
- Why this point: keeps fragile animation-speed writes inside `DTMAPI.GameBridge.DolocTown` while the migrated ActionSpeed mod supplies only a policy and player config.
- Failure behavior: if the hook is not installed or no enabled tool policy exists, ActionSpeed remains configured/pending and no animator speed is changed. Speeds captured during `OnEnter` are restored on `OnExit`, smoke cleanup, `SaveLoaded`, and `ReturnedToTitle`; continuous-use scaling only changes the `dt` passed to the game's own use-item timer and does not call item logic directly. Auto-fill continues to call native `ItemBottle.UseAsItem`; 0.2.3 normal/strong modes differ only by bridge cooldown and log `strong/cooldownSeconds` for smoke comparison.
- Mods/tests depending on it: `Yuuka.DTMAPI.ActionSpeed`.
- Evidence:
  - Build: DTMAPI 0.1.13 local build/unit passed 2026-05-31
  - Save: local slot 3 / index 2
  - Log line: `ActionSpeed tool animation speed applied by Yuuka.DTMAPI.ActionSpeed tool=old_pickaxe multiplier=3 animators=3.`, `Hook status: Smoke.ActionSpeedTool = verified. owner=Yuuka.DTMAPI.ActionSpeed, tool=old_pickaxe, multiplier=3, animators=3, samples=body:1->3;tool-renderer:1->3;tool-collider:1->3`, `ActionSpeed animator speeds restored reason=AgentStateTool.OnExit restored=3.`
  - Config apply log line: `Smoke exercise ActionSpeedConfigApply OK before=... multiplier=2 ... samples=body:1->2;tool-renderer:1->2;tool-collider:1->2; after=... multiplier=4 ... samples=body:1->4;tool-renderer:1->4;tool-collider:1->4`
  - Interaction hook log line: `Hook status: ActionSpeed.InteractionAnimation = experimental. ... bottled-water right-click continuous drink ... no-key ItemBottle.UseAsItem auto-fill ...`
  - Interaction gameplay log line: `Smoke exercise ActionSpeedInteraction OK ... eatDrink={item=can ... continuous=none}; bottledWaterRightClick={item=bottle_of_water ... continuousDelta=1}; bottleFillInWater={branch=InteractiveWater.IsInWater ...}; autoFillBottle={... behavior=AutoFillBottle ... applications=1}; ... pending=none`.
  - 0.2.3 strong auto-fill log line: `GAME-SMOKE/20260603-041950` records native `ItemBottle.UseAsItem` with `strong=True`, `cooldownSeconds=0.08`, `normalCooldownSeconds=0.1`, `strongCooldownSeconds=0.08`, and `inventoryChanged=True`.
  - 2026-06-09 post-merge `Refactor` log line: `GAME-SMOKE/20260609-134246` records `Feature.ActionSpeed = ready`, `ActionSpeed.ToolAnimation = verified`, `ActionSpeed.InteractionAnimation = experimental`, `Smoke.ActionSpeedTool = verified`, `Smoke.ActionSpeedConfigApply = verified`, `Smoke.ActionSpeedAutoFillBottle = verified`, and `Smoke.ActionSpeedInteraction = verified`; earlier branch-package split evidence remains `GAME-SMOKE/20260609-120153`.
  - 2026-06-09 lifecycle restore log line: `GAME-SMOKE/20260609-135903` records `Feature.ActionSpeed = ready` dispatch through `ReturnedToTitle` and `SaveLoaded`, `ActionSpeed SaveLoaded restore boundary OK slot=2`, unchanged `AgentStateTool.OnExit` / `AgentStateInteract.OnExit` / `AgentStateBase.OnExit` restore logs, and unchanged `Smoke.ActionSpeedTool`, `Smoke.ActionSpeedConfigApply`, `Smoke.ActionSpeedAutoFillBottle`, and `Smoke.ActionSpeedInteraction` status meanings.
  - 2026-06-09 smoke case-file split log line: `GAME-SMOKE/20260609-140738` records unchanged `SchemaVersion=2`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `Feature.ActionSpeed = ready`, `Smoke.ActionSpeedTool = verified`, `Smoke.ActionSpeedConfigApply = verified`, `Smoke.ActionSpeedAutoFillBottle = verified`, and `Smoke.ActionSpeedInteraction = verified`; moved ActionSpeed smoke method hash stayed `492089ca05ff0faabff397f074a3d92a4af433c448042937409d69a14b31215e`.
  - 2026-06-09 feature-status model log line: `GAME-SMOKE/20260609-141440` records `Feature.ActionSpeed = ready` with `Feature status: id=ActionSpeed, lastOperation=PublishHookStatuses/InstallHooks/Update/ReturnedToTitle/SaveLoaded/EnvironmentReset, success=True, failureCount=0, lastError=none`, while `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, and all existing ActionSpeed smoke status meanings remain unchanged.
  - 2026-06-09 shared lifecycle owner log line: `GAME-SMOKE/20260609-172903` records `ActionSpeed.ToolAnimation = verified`, `ActionSpeed.InteractionAnimation = experimental`, `Feature.ActionSpeed = ready`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, and unchanged restore logs through `AgentStateTool.OnExit`, `AgentStateInteract.OnExit`, and `AgentStateBase.OnExit`; report zip `docs/debug/evidence/GAME-SMOKE/20260609-172903.zip`.
  - 2026-06-10 diagnostics mod-status log line: `GAME-SMOKE/20260610-022543` records `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `Feature.ActionSpeed = ready`, `Smoke.DiagnosticsSnapshot = verified`, and diagnostics summary `loadedMods=14, mods=14, errors=0, warnings=0, hooks=63, features=4`; report zip `docs/debug/evidence/GAME-SMOKE/20260610-022543.zip`.
  - 2026-06-10 lifecycle isolation log line: `GAME-SMOKE/20260610-012242` records `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, unchanged `ActionSpeed.ToolAnimation` / `ActionSpeed.InteractionAnimation` meanings, clean exit, no fatal popup, and no `Lifecycle callback failed` entries after `AgentStateTool.OnExit`, `AgentStateInteract.OnExit`, and `AgentStateBase.OnExit` callbacks were wrapped independently.
  - 2026-06-10 feature-status publish throttle log line: `GAME-SMOKE/20260610-041156` records `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `Feature.ActionSpeed = ready`, `Smoke.DiagnosticsSnapshot = verified`, diagnostics summary `loadedMods=14, mods=14, errors=0, warnings=0, hooks=64, features=5`, and only five successful `Feature.<Id>` `Update` status lines across five features during the short smoke, proving heartbeat publication instead of every-frame success publication.
  - 2026-06-10 diagnostics status-code log line: `GAME-SMOKE/20260610-043830` records `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `Feature.ActionSpeed = ready`, `Smoke.DiagnosticsSnapshot = verified`, diagnostics summary `loadedMods=14, mods=14, modStatusCodes=loaded=14, errors=0, warnings=0, hooks=64, features=5`, and report zip pointer `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-043910.zip`.
  - Config screenshot: `docs/debug/evidence/GAME-SMOKE/20260603-052444/DTMAPI-evidence/UI-004/20260603-052523/title-settings-config-action-speed.png` shows six inline bool+number rows plus same-row `自动装水` and `强化自动装水`.
  - Screenshot/report: tool smoke result `docs/debug/evidence/GAME-SMOKE/20260531-044611`; config-apply smoke result `docs/debug/evidence/GAME-SMOKE/20260531-045330`; 0.2.1 interaction smoke result `docs/debug/evidence/GAME-SMOKE/20260602-004823`; 0.2.3 strong interaction smoke result `docs/debug/evidence/GAME-SMOKE/20260603-041950`; final title config screenshot smoke result `docs/debug/evidence/GAME-SMOKE/20260603-052444`; branch feature-host ActionSpeed smoke result `docs/debug/evidence/GAME-SMOKE/20260609-120153`; post-merge ActionSpeed smoke result `docs/debug/evidence/GAME-SMOKE/20260609-134246`; lifecycle restore ActionSpeed smoke result `docs/debug/evidence/GAME-SMOKE/20260609-135903`; smoke case-file split ActionSpeed smoke result `docs/debug/evidence/GAME-SMOKE/20260609-140738`; feature-status ActionSpeed smoke result `docs/debug/evidence/GAME-SMOKE/20260609-141440`; report zip `docs/debug/evidence/GAME-SMOKE/20260609-141440.zip`.
- Regression cases: ACTIONSPEED-001, ACTIONSPEED-002, CONFIG-007, CONFIG-008
- Pending related paths: Recast/minigame fishing remains tracked separately under `Fishing.Automation`.

## Hook: Actions.OneActionComplete

- Status: verified for resource-hit path, tree/ore/garbage/weeds wrong-tool matrix, fuel/feeder native consume/fill path, and vegetation/dandelion exception classification
- Public surface: `IActionCompletionApi.Configure`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.ToolCollider.HandleTools(Collider2D)` for resource hits, with native `ResourceFellData(resource,currentTool,hitPoint)` validation before applying final damage and native `DolocAPI.HasEnoughEnergyForUsingTool` / `DolocAPI.CostToolEnergy` charged for each extra hit; `DolocTown.AgentStateInteract.OnExit` for post-native fuel/feed completion using the selected `PowerGeneratorFuel` or `Feeder`; `DolocTown.VegetationRenderer.OnFell(ItemTool,Vector2)` / `Vegetation.CheckToolConstraints(ItemTool)` recorded as a native exception path which DTMAPI must not force-complete through `DungeonResourceRenderer`.
- Patch type: Harmony Postfix plus GameBridge reflection, no public raw game type exposure.
- Feature host owner: `ActionCompletionFeature` registers `IActionCompletionApi` through `ActionCompletionService`; shared `ToolColliderHitHookBridge` owns the `ToolCollider.HandleTools` Prefix/Postfix route, while `ActionCompletionHookBridge` consumes the shared postfix ready state and publishes the unchanged `Actions.OneActionComplete` / `Actions.OneActionFuelFeed` statuses. The fuel/feed completion callback observes the `AgentStateInteract.OnExit` patch owned by `AgentStateLifecycleHookBridge`, so ActionCompletion no longer depends on ActionSpeed's hook installation state while status text and hook IDs stay unchanged. `Smoke/Cases/ActionCompletionSmokeCase.cs` owns the OneAction resource-hit, wrong-tool, fuel/feed, and vegetation smoke case implementation; `SmokeHarness.cs` keeps scheduling and result-field ownership.
- Why this point: migrates one-action resource completion into GameBridge instead of ordinary mods owning broad Harmony patches, while preserving the game's resource/tool matching rules.
- Failure behavior: if the ToolCollider hook is not installed, policy can still be registered but gameplay resource completion remains disabled; if native validation reports a tool-type/tool-level mismatch or native energy checks reject an extra hit, DTMAPI logs the skip/partial completion and does not apply unpaid extra damage. Fuel/feed completion runs only after the game's native interaction callback and consumes extra items through `CostSelf` before calling native fill helpers. Vegetation/dandelion hits are not `DungeonResourceRenderer` resources, so DTMAPI records the native path and leaves wrong/correct tool behavior to `Vegetation.CheckToolConstraints`.
- Mods/tests depending on it: `Yuuka.DTMAPI.OneActionComplete`
- Evidence:
  - Build: DTMAPI 0.2.1 local build passed 2026-06-01 with 0 errors; earlier 0.1.13 build/unit passed 2026-05-31
  - Save: local slot 3 / index 2
  - Log line: `OneActionComplete SaveLoaded restore boundary OK slot=2`, `Smoke one-action resource-hit waiting: Current room has no rendered one-action resource; requested official main farm transition for smoke`, `One-action tool hook completed resource stone for Yuuka.DTMAPI.OneActionComplete damage=11.`, `Smoke exercise OneActionResourceHit OK owner=Yuuka.DTMAPI.OneActionComplete, resource=stone, tool=old_pickaxe`
  - Guard behavior: 0.1.13 logs native-validation skip reasons such as `tool-type-mismatch` and `tool-level-mismatch`; wrong-tool matrix smoke verified `Tree`, `Ore`, `Garbage`, and `Weeds` in the third save with unchanged health, `removed=False`, and `oneActionDelta=0`. Fuel/feed smoke verified `PowerGeneratorFuel` with `wood` and `Feeder` with `roughage_feed` through `Equipment.DecoratedInteract -> AgentStateInteract.OnExit`, native `CostSelf`, and native `AddFuel`/`AddFeeds`. Vegetation smoke records dandelion as `VegetationDandelion` using `Vegetation.CheckToolConstraints(ItemTool)`, not the `DungeonResourceRenderer` one-action path: wrong `old_pickaxe` is rejected, expected `old_sickle` removes through native `OnFell`, and both sides keep `oneActionDelta=0`. 0.2.1 recheck `GAME-SMOKE/20260601-135239` records energy accounting for resource completion: `nativeDamage=4, paidExtraHits=2/2, damage=6`.
  - 2026-06-09 post-merge `Refactor` feature split log line: `GAME-SMOKE/20260609-144458` records `Feature.ActionCompletion = ready` with `Feature status: id=ActionCompletion, lastOperation=PublishHookStatuses/InstallHooks/Update/ReturnedToTitle/SaveLoaded/EnvironmentReset, success=True, failureCount=0, lastError=none`, unchanged `Actions.OneActionComplete = verified` and `Actions.OneActionFuelFeed = verified` status meanings, plus `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`; report zip `docs/debug/evidence/GAME-SMOKE/20260609-144458.zip`.
  - 2026-06-09 smoke case-file split log line: `GAME-SMOKE/20260609-171936` records unchanged `SchemaVersion=2`, `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `Feature.ActionCompletion = ready`, `Smoke.OneActionResourceHit = verified`, `Smoke.OneActionWrongTool = verified`, `Smoke.OneActionFuelFeed = verified`, and `Smoke.OneActionVegetation = verified`; moved ActionCompletion smoke method hash stayed `9bdd9ed007fbb05061025e3209002ac9c542fafedcf1e2d6e97e31c78f3628d3`.
  - 2026-06-09 shared lifecycle owner log line: `GAME-SMOKE/20260609-172748` records `Feature.ActionCompletion = ready`, `Actions.OneActionFuelFeed = verified`, unchanged OneAction resource/wrong-tool/fuel-feed/vegetation status meanings, `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, clean exit, and no fatal popup; report zip `docs/debug/evidence/GAME-SMOKE/20260609-172748.zip`.
  - 2026-06-09 native helper extraction log line: `GAME-SMOKE/20260609-174124` records `Feature.ActionCompletion = ready`, unchanged `Actions.OneActionComplete = verified` and `Actions.OneActionFuelFeed = verified`, `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, clean exit, and no fatal popup after `ActionCompletionService` moved from the experimental bridge static helper wrappers to `GameBridgeNativeHelpers`; latest report pointer is `docs/debug/evidence/GAME-SMOKE/20260609-174124/latest-report.txt`.
  - 2026-06-10 lifecycle isolation log line: `GAME-SMOKE/20260610-012400` records `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, unchanged `Actions.OneActionComplete` / `Actions.OneActionFuelFeed` meanings, clean exit, no fatal popup, and no `Lifecycle callback failed` entries after `AgentStateInteract.OnExit` fuel/feed callback isolation.
  - 2026-06-10 shared ToolCollider owner log line: `GAME-SMOKE/20260610-140419` records `Feature.ActionCompletion = ready`, `Feature.OilCoalDrop = ready`, unchanged `Actions.OneActionComplete = verified` and `Actions.OneActionFuelFeed = verified`, `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed` after `ToolCollider.HandleTools` Prefix/Postfix installation moved to `ToolColliderHitHookBridge`.
  - 2026-06-10 callback-isolation log line: `GAME-SMOKE/20260610-163813` records `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed` after the shared ToolCollider postfix split ActionCompletion and OilCoalDrop into independent safe wrappers.
  - Screenshot/report: verified positive smoke result `docs/debug/evidence/GAME-SMOKE/20260531-032318`; post-fuel/feed-hook positive smoke result `docs/debug/evidence/GAME-SMOKE/20260531-125529`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-125613`; first wrong-tool negative smoke result `docs/debug/evidence/GAME-SMOKE/20260531-130824`; full wrong-tool matrix result `docs/debug/evidence/GAME-SMOKE/20260531-133212`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-133256`; fuel/feed smoke result `docs/debug/evidence/GAME-SMOKE/20260531-140335`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-140416`; final status-text recheck `docs/debug/evidence/GAME-SMOKE/20260531-140944`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-141026`; vegetation exception smoke result `docs/debug/evidence/GAME-SMOKE/20260531-160900`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-160943`.
- Regression cases: ONEACTION-001, ONEACTION-002, ONEACTION-003

## Hook: Fishing.Automation

- Status: experimental
- Public surface: `IFishingAutomationApi.Configure`, `IFishingAutomationApi.SetEnabled`
- Game build: 23465763 workshop
- Game method/type: phase observation over `AgentStateFishingReady.OnEnter/OnPlay/NextState`, `AgentStateFishingCast.OnEnter`, `AgentStateFishingWait.OnEnter`, `AgentStateFishingWait.OnPlay`, `AgentStateFishingPull.OnEnter/OnExit`, `FishingGameScrollBar.StartGame/UpdateGame/StopGame`, native `BodyController.UseFishRod`, selected quick-slot rod placement, fishable pool lookup, `AgentStateFishingWait.RollFish`, native `InvokeFishOnHookTip`, bite-ready Pull/Battle routing, scoped `DolocUserInput` getter overrides during `FishingGameScrollBar.UpdateGame`, Ready charge `RedSaw.CastTimer` extra tick, Ready target hold/release through `DolocUserInput.NormalUseToolInProgress`, ready/cast/pull body and fish-rod animator speed writes, `FishRodRenderer.CastHook` hook physics scaling, and `FishRodRenderer.Pull` / `PullCancel` duration scaling.
- Patch type: Harmony Postfix/Prefix for phase evidence plus GameBridge-owned native auto-cast, bite timing, bite-ready routing, native minigame input override, and Cast/Pull animation-duration scaling.
- Hook/API owner: `FishingAutomationFeature` registers `IFishingAutomationApi` through `FishingAutomationService`; `FishingAutomationHookBridge` owns the existing Fishing hook targets and publishes `Fishing.Automation`.
- Why this point: AutoFishing must enter the game's native fishing state machine; GameBridge owns the fragile pool/rod/native-call reflection while the mod owns F6 policy and config.
- Failure behavior: policy/state can be registered; if no fishable water or selected rod is available, DTMAPI logs the state and does not fake fish/item rewards. Player toasts remain intentionally limited to F6 on/off and movement cancel; no-water/no-rod/auto-cast are not player toasts. F6 enables the default native loop: `BodyController.UseFishRod` casts, native `AgentStateFishingWait` waits for a bite, DTMAPI reels a bite-ready state through `AgentStateFishingWait.NextState`, the real `FishingGameScrollBar` is auto-played by scoped native input decisions while native `UpdateGame` owns scoring/result/panel animation, `AgentStateFishingPull` collects the result, and the loop recasts after the configured delay. `InstantBite` only changes the wait strategy by rolling a native bite immediately, invoking the native bite tip, refreshing rod visuals, and then using the same native post-bite route. Because native `AgentStateBase.Update` assigns the new Wait state after `AgentStateFishingWait.OnEnter` returns, the `Wait.OnEnter` hook prepares the bite only; reel advancement and the mirrored native `FishingEnergyCost` consume happen from `Wait.OnPlay` after the transition is complete. `SkipMiniGame` temporarily enables the native `skipFishingGame` route only while resolving `NextState`; it does not write Pull success or force `IsFailed=false`, so native failure/success is preserved. Fast-animation writes are applied only from `AgentStateFishingReady.OnEnter/OnPlay`, `AgentStateFishingCast.OnEnter`, `FishRodRenderer.CastHook`, and `AgentStateFishingPull.OnEnter`; Ready charge ticks native `_castTimer` with the extra multiplier delta and refreshes the progress circle, `CastChargeRatio` holds/releases native `NormalUseToolInProgress` until the configured `0..1` target progress and defaults to immediate release, visible cast hook flight scales hook `Velocity` and Rigidbody2D gravity, Pull duration is additionally scaled at `FishRodRenderer.Pull` / `PullCancel`, then animator speeds and hook physics are restored on Pull/base exit and save/title/environment reset. Save/title/environment boundaries also clear mini-game handles, minigame input overrides, phase log cooldowns, service failure throttle episodes, feedback/cast cooldowns, summaries, and smoke-only overrides so transient AutoFishing state cannot leak across saves or title transitions.
- Manual QA follow-up UI contract: the migrated AutoFishing config now exposes a `ToggleKey` keybind (default `F6`, explicit `None` disables the hotkey), `InstantBite`, `SkipMiniGame`, and `FastAnimations` as player switches plus bounded `AnimationMultiplier` (`1..4`) and `CastChargeRatio` (`0..1`, default no charge) number settings. The default loop is automatic cast, native wait, auto reel, visible minigame auto-complete, collect, and repeat.
- Owner policy: `FishingAutomationService` currently uses one effective enabled owner and does not merge options across multiple enabled mods. The experimental `FishingAutomationOptions` DTO now models native-stage strategy (`BiteWaitMode`, `ResultMode`, `AnimationMode`, recast delay, movement cancel, animation multiplier, cast charge target); future stable behavior needs an exclusive lease, priority, or explicit owner arbitration design before ordinary mods can rely on multi-owner semantics.
- Mods/tests depending on it: `Yuuka.DTMAPI.AutoFishing`
- Evidence:
  - Build: DTMAPI 0.2.3 local build passed 2026-06-03 with 0 errors.
  - Save: AutoFishing phase smokes now require explicit local slot 5 / index 4; other retained historical evidence used local slot 3 / index 2.
- Log line: `Input F6 pressed dispatched to DTMAPI mods`, `AutoFishing automation enabled reason=hotkey F6`, `Fishing automation auto-cast invoked native BodyController.UseFishRod`, `Smoke.AutoFishingInstantBite = verified`, `Smoke.AutoFishingMiniGameSkip = verified ... autoHook=AgentStateFishingPull`, `Smoke.AutoFishingMiniGameComplete = verified ... behavior=AutoPlayVisibleMiniGame, status=Success, stableHoldFrames=..., releaseFrames=...`, `Smoke.AutoFishingAnimationSpeed = verified ... behavior=FastPullDuration, source=FishRodRenderer.Pull, multiplier=...`, and `Smoke.AutoFishingCastCharge = verified ... target=0.5, progress=0.54, input=release`.
  - 0.2.3 toast-policy partial smoke: `docs/debug/evidence/GAME-SMOKE/20260603-030142` verified `toastPolicy=0.2.3-suppressed-no-water-no-rod-cast`, `ProcessExited=true`, and no fatal popup.
  - 0.2.4 direct skip=false minigame smoke: failed attempt `docs/debug/evidence/GAME-SMOKE/20260603-172124` rolled `waste_plastic_bottle` and correctly did not create the native minigame; passing attempt `docs/debug/evidence/GAME-SMOKE/20260603-173435` logged `FishingGameScrollBar`, `autoHook=AgentStateFishingBattle`, `fish=loach`, `isFish=True`, `forceFishForSmoke=True`, `currentGameStatus=Success`, `visibleSeconds=0.76`, clean exit, and no fatal popup.
  - 2026-06-09 animator restore smoke: `docs/debug/evidence/GAME-SMOKE/20260609-170646` records `AutoFishingHotkey=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `Fishing automation animation speed applied ... phase=Pull multiplier=3 animators=2`, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2`; report zip `docs/debug/evidence/GAME-SMOKE/20260609-170646.zip`.
  - 2026-06-10 lifecycle isolation smoke: `docs/debug/evidence/GAME-SMOKE/20260610-012052` records `AutoFishingHotkey=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean exit, no fatal popup, and no `Lifecycle callback failed` entries. `AgentStateFishingPull.OnExit` now keeps animator-speed restore in a finally-equivalent path after cooldown notification.
  - 2026-06-10 feature split smoke: `docs/debug/evidence/GAME-SMOKE/20260610-170839` records `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, `AutoFishingHotkey=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, clean process/fatal checks, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2` after API/service/hook ownership moved out of `DolocTownExperimentalBridgeApi`.
  - 2026-06-10 service failure throttle smoke: `docs/debug/evidence/GAME-SMOKE/20260610-202047` records `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, clean process/fatal checks, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and no `FishingAutomation service failed` entries on the passing path. Unit coverage verifies repeated `FishingAutomation.MiniGame.Update` service failures record one diagnostics error and suppress further hook-status rewrites after `failureCount=3`.
    - 2026-06-10 runtime state reset smoke: `docs/debug/evidence/GAME-SMOKE/20260610-203301` records `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, reset logs for `ReturnedToTitle`, `SaveLoaded`, and `DolocAPI.SetEnvCamera`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean process/fatal checks, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2`. Unit coverage verifies the reset helper clears mini-game handles, phase cooldowns, service failure episodes, smoke overrides, and animator snapshots.
  - 2026-06-10 native helper dependency smoke: `docs/debug/evidence/GAME-SMOKE/20260610-204134` records `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, reset logs for `ReturnedToTitle`, `SaveLoaded`, and `DolocAPI.SetEnvCamera`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean process/fatal checks, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2` after `FishingAutomationService` switched to `GameBridgeNativeHelpers`.
  - Options contract review: `docs/reviews/api/2026/20260610-fishing-options-contract-review.md` records that `AutoRecast` and `RequireSelectedFishingRod` are currently normalized to `true`; no runtime behavior changed in that docs-only branch.
  - 2026-06-12 independent-scenario follow-up: `GAME-SMOKE/20260612-204934` verifies `AutoCastOnly`; `GAME-SMOKE/20260612-205040` verifies `InstantBiteOnly`; `GAME-SMOKE/20260612-205147` verifies `SkipOnly`; `GAME-SMOKE/20260612-205248` verifies `AutoCompleteOnly`; `GAME-SMOKE/20260612-210544` verifies `FastAnimationsOnly` after auto-cast reachable animator paths were added; `GAME-SMOKE/20260612-210646` verifies `CombinedSkip`; and `GAME-SMOKE/20260612-210744` verifies `CombinedComplete`. Each third-save scenario records clean process/fatal checks and `AutoFishingReportExport=Passed`.
  - 2026-06-13 user-feedback fix: `20260613-0001` Release build/test passed after `AgentStateFishingWait.OnEnter` began applying wait automation and `InstantBite` began invoking native `InvokeFishOnHookTip`; expected status details include `nativeTipInvoked=True` and `source=AgentStateFishingWait.OnEnter Postfix`. `20260613-0002` Release build/test passed after forced bite duration switched from DTMAPI's unconditional `100f` timeout to native `PullTiming`; expected status details include `hookDuration=<native PullTiming>`. `20260613-0003` Release build/test passed after forced `InstantBite` began resolving to `action=ReelNativeBite`, with fish expected to route to `autoHook=AgentStateFishingBattle`; third-save visual confirmation remains pending.
  - 2026-06-13 native-loop rewrite: `20260613-0005` changes the experimental DTO to native-stage strategy enums, rewrites the migrated mod to F6 default full loop plus only three player switches, rewrites smoke scenarios to use the fifth-save real pond fixture, and adds unit coverage for default strategies, missing-input-edge native reel mirroring, skip result preservation, and Cast/Pull-only animation speed. Release build/test passed. Fifth-save `DefaultLoop` smoke `docs/debug/evidence/GAME-SMOKE/20260613-094928` first proved the fixture gate by failing with `selected=DolocTown.ItemTool`; after the fixture was saved with `carbon_fishrod`, fifth-save smokes passed for `DefaultLoop` (`GAME-SMOKE/20260613-102412`), `InstantBite` (`20260613-103216`), `SkipMiniGame` (`20260613-103328`), `FastAnimations` (`20260613-103427`), `CombinedInstantSkip` (`20260613-103531`), and `CombinedInstantComplete` (`20260613-103621`). Final HookProbe/exit smoke `docs/debug/evidence/GAME-SMOKE/20260613-095209` and all listed scenario smokes exited cleanly with no residual `DolocTown.exe`.
  - 2026-06-13 fifth-save revalidation: after the fifth save was confirmed to hold `carbon_fishrod`, Release build/test and `git diff --check` passed again, and the latest fifth-save smokes passed for `DefaultLoop` (`GAME-SMOKE/20260613-112900`), `InstantBite` (`20260613-113127`), `SkipMiniGame` (`20260613-113240`), `FastAnimations` (`20260613-113654`), `CombinedInstantSkip` (`20260613-113824`), and `CombinedInstantComplete` (`20260613-113937`). Logs record real pool `freshwater_forest`, `BodyController.UseFishRod`, native `NativeBiteReady`, `SkipMiniGameNativeResult -> AgentStateFishingPull` for skip routes, `FishingGameScrollBar.UpdateGame -> Success` for completion routes, Cast/Pull fast-animation samples, next AutoCast loop evidence, and clean process/fatal checks.
  - 2026-06-13 native minigame input and animation multiplier follow-up: `GAME-SMOKE/20260613-153632` verifies `AutoPlayVisibleMiniGame` with `stableHoldFrames=82`, `releaseFrames=49`, and no delayed direct-success evidence; user manual QA confirmed red/green/yellow handling succeeds. `GAME-SMOKE/20260613-153850` is retained as the rejected old-config path where missing `AnimationMultiplier` migrated to `1`; `GAME-SMOKE/20260613-154252` verifies Cast/Pull animator samples and `FishRodRenderer.Pull` duration, but is incomplete for visible cast speed because it did not observe hook physics. `GAME-SMOKE/20260613-160354` verifies `FastCastHookPhysics` with hook `Velocity` `18.55,29.68 -> 55.65,89.04`, hook `gravityScale` `12 -> 108`, Pull duration `0.147->0.049`, clean process/fatal checks, hook-physics restore, and report `dtmapi-report-20260613-160443.zip`. `GAME-SMOKE/20260613-163257` is retained as a smoke-harness false negative where Ready charge evidence existed but the old gate sampled a later Cast/Pull summary. Final Ready/Cast/Pull re-smoke `GAME-SMOKE/20260613-163812` verifies `FastReadyCharge` from `AgentStateFishingReady.OnPlay` (`castTimer.Progress:0->0.04;extraDt=0.04;powerBar.updated=2`), `FastCastHookPhysics` with hook `Velocity` `22.366,35.786 -> 67.098,107.357`, hook `gravityScale` `12 -> 108`, Pull duration `0.117->0.039`, `AutoFishingAnimationSpeed=Passed`, `AutoFishingMiniGameComplete=Passed`, final `readyCharge=True, fastAnimation=True`, clean process/fatal checks, and report `dtmapi-report-20260613-163900.zip`.
  - 2026-06-13 cast charge setting follow-up: `GAME-SMOKE/20260613-170514` is retained as a false positive because PowerShell integer `[Math]::Min/Max` overloads truncated the requested `0.5` ratio to `0`; `GAME-SMOKE/20260613-171104` is retained as the stricter-gate failure that exposed that clamp. Final fifth-save `FastAnimations` smoke `GAME-SMOKE/20260613-171405` verifies `AutoFishingCastCharge=Passed`, `target=0.5`, release at `progress=0.54`, Ready speed samples up to `castTimer.Progress:0.44->0.48`, `FastCastHookPhysics`, Pull duration scaling, final `readyCharge=True, fastAnimation=True`, clean process/fatal checks, and report `dtmapi-report-20260613-171455.zip`.
  - 2026-06-13 InstantBite energy-cost follow-up: unit `FishingAutomationInstantBiteDefersReelUntilWaitPlay` verifies `AgentStateFishingWait.OnEnter` prepares an instant bite without overwriting Battle/Pull and without `DolocAPI.CostEnergy`, then the following `Wait.OnPlay` advances to `AgentStateFishingBattle` and calls native fishing energy cost exactly once. Fifth-save in-game energy delta remains pending.
  - 2026-06-10 smoke case split: `docs/debug/evidence/GAME-SMOKE/20260610-205153` records `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, reset logs for `ReturnedToTitle`, `SaveLoaded`, and `DolocAPI.SetEnvCamera`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean process/fatal checks, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2` after only moving the smoke body into `Smoke/Cases/AutoFishingSmokeCase.cs`.
  - 2026-06-10 final hardening smoke: `docs/debug/evidence/GAME-SMOKE/20260610-205841` records `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, reset logs for `ReturnedToTitle`, `SaveLoaded`, and `DolocAPI.SetEnvCamera`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2` after all FishingAutomation hardening branches were merged back to `Refactor`. Its `latest-report.txt` still points to stale `dtmapi-report-20260610-171030.zip`, so that report is not cited as fresh evidence.
  - 2026-06-10 lifecycle ownership cleanup smoke: `docs/debug/evidence/GAME-SMOKE/20260610-220230` records `Feature.FishingAutomation = ready` through `ReturnedToTitle`, `SaveLoaded`, and `EnvironmentReset`, unchanged `Fishing.Automation = experimental`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean process/fatal checks, unchanged `Smoke.AutoFishingAutoCast/Phase/MiniGameComplete` verified statuses, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2`. The log has no `SaveLoaded.RestoreExperimentalAnimatorSpeeds` or `ReturnedToTitle.RestoreExperimentalAnimatorSpeeds` callback keys after save/title cleanup moved fully to `FishingAutomationFeature`.
  - 2026-06-10 service failure recovery smoke: `docs/debug/evidence/GAME-SMOKE/20260610-220835` records `Feature.FishingAutomation = ready`, unchanged `Fishing.Automation = experimental`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean process/fatal checks, unchanged AutoFishing smoke verified statuses, and no `Fishing automation service failed` / repeated / throttled service failure logs on the passing path. Unit coverage verifies a repeated `FishingAutomation.MiniGame.Update` failure episode clears after three stable successes and the next failure starts a fresh diagnostics episode.
  - 2026-06-10 fresh report export smoke: `docs/debug/evidence/GAME-SMOKE/20260610-221703` records `Feature.FishingAutomation = ready`, unchanged `Fishing.Automation = experimental`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`. The smoke now exports a fresh diagnostics report on success; `latest-report.txt` points to `dtmapi-report-20260610-221744.zip`, the file exists, and `Smoke.DiagnosticsSnapshot = verified` records matching `LatestReportPath`.
  - 2026-06-10 final Refactor fresh report smoke: `docs/debug/evidence/GAME-SMOKE/20260610-223354` records the same AutoFishing result after all follow-up branches merged to `Refactor`: `RunStatus=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`. `latest-report.txt` points to existing report `dtmapi-report-20260610-223436.zip`, and `Smoke.DiagnosticsSnapshot = verified` records `latestReport=D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-223436.zip`.
  - 2026-06-11 report-export result-field smoke: `docs/debug/evidence/GAME-SMOKE/20260611-000932` records the independent harness field `AutoFishingReportExport=Passed` alongside unchanged behavior fields `AutoFishingPhase=Passed` and `AutoFishingMiniGameComplete=Passed`; `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`. `latest-report.txt` points to existing report `dtmapi-report-20260611-001013.zip`, and `Smoke.DiagnosticsSnapshot = verified` records matching `latestReport`.
  - 2026-06-11 mid/long final Refactor smoke: `docs/debug/evidence/GAME-SMOKE/20260611-002930` records `RunStatus=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `AutoFishingReportExport=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed` after the Fishing docs/current-state, report-export field, API clarity, Manager view-model skeleton, and Camera gate refresh branches merged to `Refactor`. `latest-report.txt` points to existing report `dtmapi-report-20260611-003012.zip`, and `Smoke.DiagnosticsSnapshot = verified` records matching `latestReport`.
  - 2026-06-11 Manual QA Batch 1: config/UI wording was aligned with the current experimental service contract. Smoke `GAME-SMOKE/20260611-214536` records `RunStatus=Passed`, `AutoFishingMiniGameComplete=Passed`, `AutoFishingReportExport=Passed`, `DiagnosticsReportExport=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`; report `dtmapi-report-20260611-214620.zip`.
  - Native responsibility review: `docs/reviews/api/2026/20260610-fishing-native-responsibility.md` maps Ready/Cast/Wait/MiniGame/Pull/Cooldown owners and records that phase observation, native auto-cast, wait-phase intervention, minigame completion, movement cancel, fast-animation restore, and save/title cleanup have different ownership boundaries. `IFishingAutomationApi` remains Experimental, and a future feature split must preserve existing hook/status IDs while separating observation hooks from intervention callbacks.
  - Config screenshot: `docs/debug/evidence/GAME-SMOKE/20260603-052444/DTMAPI-evidence/UI-004/20260603-052523/title-settings-config-auto-fishing.png` shows same-row `自动完成小游戏` and `跳过小游戏`.
  - Screenshot/report: 0.2.3 movement/skip smoke `docs/debug/evidence/GAME-SMOKE/20260603-042437`; 0.2.4 skip=false minigame smoke `docs/debug/evidence/GAME-SMOKE/20260603-173435`; title config screenshot smoke `docs/debug/evidence/GAME-SMOKE/20260603-052444`; final old auto-cast/wait smoke `docs/debug/evidence/GAME-SMOKE/20260602-015720`; earlier wait-phase evidence retained under `GAME-SMOKE/20260531-035217` and external F6 evidence under `GAME-SMOKE/20260531-112959`.
- Regression cases: TITLE-PAUSE-AUTOFISHING-ROOTFIX-20260613, INSTALL-AUTOFISHING-UI-DIAGNOSTICS-20260613, FISHING-FOLLOWUP-WEB-AUDIT-20260610, AUTOFISHING-SMOKE-REPORT-EXPORT-20260610, FISHING-SERVICE-FAILURE-RECOVERY-20260610, FISHING-LIFECYCLE-OWNERSHIP-20260610, FISHING-HARDENING-FOLLOWUP-20260610, AUTOFISHING-SMOKE-CASE-SPLIT-20260610, FISHING-OPTIONS-CONTRACT-REVIEW-20260610, FISHING-NATIVE-HELPER-DEPENDENCY-20260610, FISHING-RUNTIME-STATE-RESET-20260610, FISHING-SERVICE-FAILURE-THROTTLE-20260610, FISHINGAUTOMATION-FEATURE-SPLIT-20260610, FISHING-NATIVE-RESPONSIBILITY-REVIEW-20260610, AUTOFISH-001, INPUT-004, SMOKE-002, FISHING-ANIMATOR-RESTORE-20260609

## Hook: Items.FishRoeTooltip

- Status: verified
- Public surface: `IItemTooltipApi.ConfigureFishRoeProvider`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.Item.get_title`, `DolocTown.Item.get_description`, `DolocTown.Item.GetDetailInfo`, `DolocTown.ItemFishRoe.fishName` identity reader, and native `DolocAPI.QueryItemProto(fishId).Title` fallback for fish display names.
- Patch type: Harmony Postfix plus GameBridge reflection.
- Hook owner: `FishRoeTooltipFeature` / `FishRoeTooltipHookBridge`; API/service owner: `FishRoeTooltipService`.
- Why this point: FishBreedingAssistant provides lookup data while GameBridge owns item identity and tooltip rendering fragility. The shareable public FishBreedingAssistant lookup source is a placeholder, so GameBridge must not depend on private generated tables to recover `鱼卵 (鱼名称)`. The old `DolocTownExperimentalBridgeApi` no longer implements `IItemTooltipApi`.
- Failure behavior: lookup provider can be registered; if a provider lookup misses, GameBridge falls back to native `DolocAPI.QueryItemProto(fishId).Title` and logs one native lookup result/miss per fish id. If item hooks do not install or native lookup also fails, no tooltip text is changed and diagnostics stay pending/failed.
- Mods/tests depending on it: `Yuuka.DTMAPI.FishBreedingAssistant`
- Evidence:
  - Build: DTMAPI 0.2.1 local build passed 2026-06-01 with 0 errors; earlier 0.1.10 build/unit passed 2026-05-30; feature split branch Release build/test passed 2026-06-09.
  - Save: local slot 3 / index 2
  - Log line: `HookProbe HookStatusChanged OK Items.FishRoeTooltip=experimental`, `Smoke exercise FishRoeTooltip OK item=fish_roe title=鱼卵 (鱼) detail=Hatches: 鱼; Incubate: 4 小时; Grow: 6 小时`
  - 0.2.1 player-facing change: `Yuuka.DTMAPI.FishBreedingAssistant` now registers title decoration only; the old details toggle is removed from config and default options set `LabelFishRoeDetails=false`.
  - 2026-06-09 feature split smoke: `docs/debug/evidence/GAME-SMOKE/20260609-181533` records `ExperimentalHooks=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `Items.FishRoeTooltip = verified`, `Feature.FishRoeTooltip = ready`, `Smoke.FishRoeTooltip = verified`, `Smoke.AnimalViewerRendering = verified`, and `Smoke.ExperimentalHookExercise = verified`; the smoke harness registered a smoke-only fallback provider because the public FishBreedingAssistant lookup source is a placeholder and the local `Yuuka.DTMAPI.FishBreedingAssistant` config was disabled.
  - 2026-06-10 smoke case-file split: `docs/debug/evidence/GAME-SMOKE/20260610-021014` records unchanged `ExperimentalHooks=Passed`, `Items.FishRoeTooltip = verified`, `Feature.FishRoeTooltip = ready`, `Smoke.FishRoeTooltip = verified`, `Smoke.AnimalViewerRendering = verified`, and `Smoke.ExperimentalHookExercise = verified. FishRoeTooltip=True, AnimalViewerRendering=True.` after moving only the FishRoe smoke case body to `Smoke/Cases/FishRoeTooltipSmokeCase.cs`; report/evidence zip `docs/debug/evidence/GAME-SMOKE/20260610-021014.zip`.
  - 2026-06-13 native title fallback: `docs/debug/evidence/GAME-SMOKE/20260613-201326` records `RunStatus=Passed`, `SaveLoaded=Passed`, `ExperimentalHooks=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed` after removing the smoke-only fallback provider. Logs show `Fish roe native title lookup resolved fish -> 鱼`, `Smoke exercise FishRoeTooltip OK item=fish_roe title=鱼卵 (鱼) detail=`, `Smoke.FishRoeTooltip = verified`, and `Smoke.ExperimentalHookExercise = verified. FishRoeTooltip=True, AnimalViewerRendering=True.` Retained `GAME-SMOKE/20260613-200911` is a too-short `AutoExitAfterSeconds=20` harness timing failure with clean process/fatal checks.
  - Retained rejected precondition smoke: `docs/debug/evidence/GAME-SMOKE/20260609-180405` reached `Items.FishRoeTooltip = verified` and `Feature.FishRoeTooltip = ready`, but failed `Smoke.FishRoeTooltip` because no enabled public provider lookup produced decoration before the smoke-only fallback was added.
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-150808`
- Regression cases: FISHROE-NATIVE-TITLE-FALLBACK-20260613, FISHROE-001, FISHROE-TOOLTIP-FEATURE-SPLIT-20260609, FISHROE-SMOKE-CASE-SPLIT-20260610

## Hook: Animals.ViewerRendering

- Status: verified
- Public surface: `IAnimalViewerApi.ConfigureSpecialProduceProgress`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.UI.AnimalFullInfoData(Animal)` constructor, `DolocTown.UI.AnimalViewer.Show`, `DolocTown.UI.AnimalPanel.RefreshViewer`, native `ProgressBar` cloning, `Animal.husbandryValues`, `Animal.protoName`, and `DolocTown.Config.DolocConfig.Tables.TbHusbandry` enumeration/threshold lookup.
- Patch type: `AnimalViewerFeature`/`AnimalViewerHookBridge` owned Harmony constructor/Viewer/Panel Postfix plus `AnimalViewer.Show` Prefix stale-row cleanup, cached GameBridge reflection, and smoke-only official `AnimalPanelUiState` open path. The current hidden-produce path clears old cloned rows before native show, creates new independent cloned native progress rows inactive, disables localization-style components on those clones before first activation, pre-fills text while inactive, activates, then refreshes cloned text again in the same callback to reduce the historical first-frame `心情` flicker risk while keeping native mood/state data intact.
- Why this point: AnimalHusbandryProgress stays an event/config mod while GameBridge owns private animal viewer data extraction and UI extension.
- Failure behavior: display policy can be registered; if viewer hooks do not install, no progress text is added and diagnostics stay pending/failed. The current path records whether hidden-produce rows were prefilled, independent, whether mood/state fields were overridden, how many localization components were disabled, and whether the same-callback first-frame text guard observed a mood-title hit. Manual repeated animal-switch confirmation remains required before treating the visual flicker issue as fully solved.
- Mods/tests depending on it: `Yuuka.DTMAPI.AnimalHusbandryProgress`
- Evidence:
  - Build: DTMAPI 0.3.1 Release build/unit passed 2026-06-06 with 0 errors; earlier 0.2.3 build passed 2026-06-03
  - Save: local slot 3 / index 2
  - Log line: 2026-06-10 feature split smoke `GAME-SMOKE/20260610-103119` logs `Feature.AnimalViewer = ready`, `Animals.ViewerRendering = verified`, `Smoke.AnimalViewerProgressUi = verified. independent cloned ProgressBar prefilled rows=1, primary=羊毛脂 0/100, moodOverride=False, stateDescriptionOverride=False`, `Smoke.AnimalPanelUi = verified`, and `Smoke.AnimalViewerUi = verified`.
  - Smoke case split: `GAME-SMOKE/20260610-123042` logs the same `Feature.AnimalViewer = ready`, `Animals.ViewerRendering = verified`, `Smoke.AnimalViewerProgressUi = verified`, `Smoke.AnimalPanelUi = verified`, and `Smoke.AnimalViewerUi = verified` statuses after moving only the AnimalPanel/AnimalViewer smoke helpers into `Smoke/Cases/AnimalViewerSmokeCase.cs`; report `dtmapi-report-20260610-123118.zip`.
  - 2026-06-11 Manual QA Batch 1: `Smoke.AnimalViewerFirstFrameFlickerGuard` was added as a smoke/debug status for same-callback cloned-text validation after localization components are disabled. Smoke `GAME-SMOKE/20260611-214845` records `AnimalViewerUi=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, clean process/fatal checks, report `dtmapi-report-20260611-214922.zip`, and Animal summary `localizationDisabled=1`, `firstFrameGuard=sameCallbackTextCheck ... moodTitleHits=0`; manual repeated-switch confirmation remained pending at that point.
  - 2026-06-12 bottom-layer implementation: the prefix now only clears stale cloned rows and the postfix owns inactive-prefill-activate. Smoke `GAME-SMOKE/20260612-170819` passes `AnimalViewerUi`, `HookProbe`, `SaveLoaded`, clean process/fatal checks, and logs repeated `AnimalPanel.RefreshViewer` / `Select` with `Smoke.AnimalViewerRepeatedSwitchFirstFrame=verified`, `Smoke.AnimalViewerFirstFrameFlickerGuard=verified`, and `moodTitleHits=0`; screenshots/summary are under `ANIMAL-001/20260612-170858`.
  - Earlier 0.3.1 Animal-only smoke `GAME-SMOKE/20260606-150721` logs the same independent cloned progress-row path and remains retained as pre-feature-host evidence.
  - Historical note: 0.2.7 `single-pass native moodBar` evidence is retained as the rejected mood-row strategy; 0.2.3 cloned-row evidence remains historical for the original UI extension path.
  - Screenshot/report: current feature-host evidence `docs/debug/evidence/GAME-SMOKE/20260610-103119`; report pointer `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-103155.zip`; config swatch/Custom screenshot evidence `docs/debug/evidence/GAME-SMOKE/20260606-150928/DTMAPI-evidence/UI-004/20260606-150959/title-settings-config-animal-husbandry-progress.png` and `title-settings-config-animal-husbandry-progress-custom.png`.
  - Exit check: `docs/debug/evidence/GAME-SMOKE/20260610-103119/process-check.txt` says no `DolocTown.exe`; fatal-window check says no fatal instance popup.
- Regression cases: ANIMAL-001, MANUALQA-027-ROOT-CAUSE, MANUALQA-028-README, MANUALQA-029-README, MANUALQA-031-REGRESSION-NEWCONTENT, CONFIG-008

## Diagnostic: Content.OfficialItemSourceIndex

- Status: experimental
- Public surface: `IContentQueryHelper.GetIndexedItems`, `IContentQueryHelper.GetIndexedItem`, `IContentItemInfo`, and source metadata on `InventoryDebugItem`.
- Game build: 23465763 workshop
- Game method/type: read-only filesystem scan of official local `MODS`, Steam Workshop `content/2285550`, `SAVE/mod_infos.json`, `info.json`, `Content/**/item_tbitem.json`, and related icon files; runtime give eligibility remains validated by `DolocTown.Config.DolocConfig.Tables.TbItem` and `DolocAPI.QueryItemProto`.
- Patch type: no Harmony patch; runtime/Core source indexing plus GameBridge runtime-table merge.
- Why this point: the Y console needs to show where official/Workshop items came from without modifying third-party files or assuming JSON-only rows are loaded by the game.
- Failure behavior: disabled source rows, JSON-only rows, missing runtime `TbItem`, illegal items, and full backpacks are displayed/logged as unavailable and are not given by DTMAPI.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseDebugInventory`.
- Evidence:
  - Build: DTMAPI 0.2.2 local build passed 2026-06-02 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: `Official content item source index = 108 item row(s) from 13 source mod(s)`, `Smoke exercise DebugInventory OK ... modItem=mod_butter ... sourceKind=Workshop ... sourceId=Workshop.3722791728 ... workshopRuntimeItem=verified`.
  - Screenshot/report: final butter Workshop item smoke `docs/debug/evidence/GAME-SMOKE/20260602-122848`; temporary enablement backup/restored in `docs/debug/evidence/WORKSHOP-MODINFO-BACKUP/20260602-122846`; earlier mineral-seed smoke `docs/debug/evidence/GAME-SMOKE/20260602-115735` remains retained.
- Regression cases: DEBUGITEMS-001

## Hook: Mail.ItemDelivery

- Status: experimental
- Public surface: `IMailDeliveryApi`, `MailItemDeliveryRequest`, and `MailItemDeliveryResult`.
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.SendItemAsEmail`, `DolocAPI.CountItem`, `DolocTown.EmailManager.emails`, `DolocTown.EmailAttachReward`, and `DolocTown.RewardItem`.
- Patch type: GameBridge reflection over native game objects. Public APIs expose DTOs and never raw decompiled game types.
- Why this point: migrated DTMAPI mods need safe item-mail delivery without directly editing saves or placing fragile `DolocAPI`/mail reflection inside ordinary mods.
- Failure behavior: invalid/missing items, disabled item sources, missing native methods, native item-generation failure, missing/unknown required content source, and native rejection return failed result DTOs and log reasons. Duplicate prevention first checks native backpack count, then scans unclaimed item-mail reward attachments; if a key already exists or is pending, delivery is skipped as a successful no-op. A request can require a specific enabled official content source so disabled mods cannot send attachment-less item mail.
- Mods/tests depending on it: `DTMAPI.SecondMotorMod`, smoke harness `-AutoExerciseVehicle`.
- Evidence:
  - Build: DTMAPI 0.2.5 Release build/unit passed 2026-06-04 with 0 errors; only NU1900 vulnerability metadata warnings occurred under restricted network access.
  - Save: local slot 3 / index 2.
  - Log line: enabled 0.2.5 smoke `GAME-SMOKE/20260604-111533` records `Mail item delivery owner=DTMAPI.SecondMotorMod item=dtmapi_second_motor_key requested=1 sent=False skipped=True backpack=0 pendingMail=1 template=send_item_template source=Local.DTMAPI_SecondMotor sourceEnabled=True sourceKnown=True success=True`, proving source-aware duplicate detection. Disabled 0.2.5 smoke `GAME-SMOKE/20260604-111901` audits `Skip=2 Registration=0 Mail=0`, proving the disabled official source never reaches native mail delivery.
  - Screenshot/report: enabled vehicle/new-content smoke `docs/debug/evidence/GAME-SMOKE/20260604-111533`; disabled-mail smoke `docs/debug/evidence/GAME-SMOKE/20260604-111901`; both process checks say no `DolocTown.exe`. Earlier first-send proof remains `docs/debug/evidence/GAME-SMOKE/20260603-051204`.
- Regression cases: VEHICLE-001, MANUALQA-025-SECOND-MOTOR

## Hook: Vehicle.MotorApi

- Status: experimental
- Public surface: `IMotorVehicleApi`, `CustomMotorDefinition`, compatibility `SecondMotorOptions`, `MotorVehicleState`, `MotorVehicleRegisterResult`, `MotorVehicleSummonResult`, `MotorVehicleRideResult`, and `MotorVehicleEventArgs`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.ItemMotorKey.OnUse`, `DolocTown.MotorInteractable.OnInteract`, `DolocTown.AgentControllerState.GetOnMotor`, `DolocTown.AgentControllerState.GetOffMotor`, `DolocTown.MotorController.OnFixedUpdate`, `UnityEngine.SpriteRenderer.color`, `UnityEngine.SpriteRenderer.sprite`, `DolocAPI.UnlockMotor(float)`, `DolocAPI.SetMotorPosition(Room, Vector2)`, `DolocAPI.EnterRoom`, `DolocAPI.Motor`, `DolocAPI.CurrentRoom`, `DolocAPI.AgentPosition`, `AgentControllerState.motorController`, and official content `mod_tbmodstoreextension` for phone-booth key stock.
- Patch type: Harmony Prefix/Postfix plus GameBridge reflection over Unity/game objects. Public APIs expose DTOs and never raw decompiled game types.
- Why this point: custom vehicles need native-like key/use/riding behavior, but native motor ownership is singleton-oriented. `ItemMotorKey.OnUse` checks `archiveHandle.IsMotorUnlocked()` before invoking the native route, and `MotorDataManager`/`DolocAPI.Motor`/archive vehicle slots do not expose a dynamic custom-vehicle list, so DTMAPI must own custom motor runtime clones and keep ordinary mods away from fragile raw game types.
- Failure behavior: if the owner/source is disabled, or the current room is in-house or disables motors, key/summon requests return failed result DTOs and log failure reasons; if clone/routing fails, the GameBridge runs DTMAPI-owned residue cleanup and restores the original `AgentControllerState.motorController` plus original motor snapshot where possible. Clone setup first rebinds key native fields to clone-owned components and only falls back to native `Init()` when rebinding cannot prove ownership; failed clone stages report `ensure-stage=...` and lifecycle cleanup sweeps orphan `DTMAPI.SecondMotor.*` GameObjects. Original `DolocAPI.Motor` is not replaced, unlocked, or tinted. New `RegisterCustomMotor` defaults to 1x native speed and supports multiple key item ids for the same vehicle; compatibility `RegisterSecondMotor` keeps its historical 2x fallback. Scoped sprite appearance loads from the owning mod's DTMAPI-named private package path (`Content/DTMAPI/assets/second-motor/dtmapi_second_motor.png` for the current sample) and must not install global replacement `sprite_vehicle_motor*` assets; reflected sprite failures are recorded and fall back to instance-scoped tint instead of aborting summon.
- Mods/tests depending on it: `DTMAPI.SecondMotorMod`, smoke harness `-AutoExerciseVehicle`.
- Evidence:
  - Build: DTMAPI 0.5.2-alpha Release build/test passed 2026-06-14 with 0 warnings / 0 errors and `DTMAPI.UnitTests: OK`.
  - Follow-up: 2026-06-15 Release build/test passed after the scoped asset rename and clone cleanup hardening. Retained failed smoke `GAME-SMOKE/20260615-191720` proved the intermediate reset-stage failure was missing clone-owned progress UI; final slot 8 smoke `GAME-SMOKE/20260615-192916` and slot 9 smoke `GAME-SMOKE/20260615-193040` passed with scoped sprite isolation, ride/dismount, clean process exit, and no fatal window.
  - Save: new smoke contract requires explicit local slot 8 / index 7 and slot 9 / index 8. Global third-save defaults are not valid for the new vehicle fixture.
  - Code/static evidence: `RegisterCustomMotor` and `CustomMotorDefinition` added; `SecondMotorMod` now registers a 1x native-flying custom motor, uses phone-booth `mod_tbmodstoreextension` content for `dtmapi_second_motor_key`, and no longer calls `IMailDeliveryApi`.
  - Unit evidence: `MotorVehicleCustomApiNormalizesKeysAndNativeSpeedDefaults` verifies primary-first multi-key normalization, fallback key behavior, new API 1x default speed, old compatibility 2x fallback, and speed clamping.
  - Game evidence: slot 8 smoke `GAME-SMOKE/20260614-063456` passed for native-unlocked custom motor key/scoped-sprite/summon/ride/dismount without synthetic teleport and clean process/fatal checks. Slot 9 smoke `GAME-SMOKE/20260614-063639` passed for custom key/scoped-sprite/summon/ride while the native vehicle remained locked, also with clean process/fatal checks.
  - Historical support only: 0.3.1 vehicle smoke `GAME-SMOKE/20260606-150357`, disabled-source smoke `GAME-SMOKE/20260604-111901`, appearance-isolation smoke `GAME-SMOKE/20260603-082749`, and retained disabled-room smoke `GAME-SMOKE/20260602-112141` are useful for old route regressions but do not complete the new phone-booth/multi-custom-motor contract.
- Regression cases: VEHICLE-001, DEBUGITEMS-001, CONFIG-009, MANUALQA-025-SECOND-MOTOR, MANUALQA-031-REGRESSION-NEWCONTENT

## Diagnostic: Config.PendingPreviewConditionalVisibility

- Status: experimental
- Public surface: `IConfigMenuPendingPreview` plus existing conditional `IConfigMenuItem.IsVisible/CanEdit` renderers.
- Game build: 23465763 workshop
- Game method/type: DTMAPI title settings retained Unity UI and fallback ImGui overlay.
- Patch type: runtime/config registry preview scope; no game Harmony patch.
- Why this point: conditional rows such as AnimalHusbandryProgress `填充颜色` must respond to unsaved/pending selection of the `+`/Custom color swatch before the player presses Save.
- Failure behavior: if preview fails, committed preset visibility is shown and custom input does not appear until after saving; smoke status remains failed/pending.
- Mods/tests depending on it: `Yuuka.DTMAPI.AnimalHusbandryProgress`, `DTMAPI.UnitTests`.
- Evidence:
  - Build: DTMAPI 0.2.5 Release build/unit passed 2026-06-04 with 0 errors; only NU1900 vulnerability metadata warnings occurred under restricted network access.
  - Save: title homepage smoke, no save slot.
  - Log line: `Smoke staged AnimalHusbandryProgress Custom color preset for pending-preview screenshot`, followed by `Smoke.TitleSettingsConfigPageScreenshot.animal-husbandry-progress-custom = verified`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260603-160739/DTMAPI-evidence/UI-004/20260603-160812/title-settings-config-animal-husbandry-progress.png` hides the hex input for an ordinary preset; `title-settings-config-animal-husbandry-progress-custom.png` shows `填充颜色` and `F0F0F0` after staging Custom.
- Regression cases: MANUALQA-024-B, CONFIG-008

## Diagnostic: Debug.InstantSaveAndTeleportCsv

- Status: experimental
- Public surface: `IInstantSaveDebugApi`, `ITeleportDebugApi.ExportDestinationsCsv`, `TeleportDestination.SuggestedDisplayName`, `TeleportDestination.Source`, and `TeleportCsvExportResult`.
- Game build: 23465763 workshop
- Game method/type: native `DolocAPI.SaveGame(int)`, whitelisted native mark/station teleport destination enumeration, and DTMAPI evidence CSV writer. Historical debug reload evidence used `DolocAPI.LoadGame(int)`, but 0.2.6 disables immediate reload from the player-facing Y console.
- Patch type: reflected native calls plus debug-console UI actions; no raw save-file edits and no arbitrary coordinate exposure.
- Why this point: the player-visible Y console needs a discoverable "save here" action and a durable teleport audit file for manual name screening.
- Failure behavior: save-only snapshots are logged with before/after room and distance; `reloadAfterSave=true` returns `reload-disabled`; CSV failures return a structured result and keep the UI action experimental.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseInstantSave`, `-AutoExerciseDebugTeleport`.
- Evidence:
  - Build: DTMAPI 0.2.6 Release build/unit passed 2026-06-05 with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: 0.2.6 focused smoke `GAME-SMOKE/20260605-181224` logs `Smoke exercise InstantSave OK ... sameRoom=True, distance=0, reloadDisabled=True`, `Smoke exercise DebugTeleportCsv OK rows=80`, and `Smoke exercise DebugTeleport OK ... changedRoom=True`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260605-181224`; historical CSV proof remains `docs/debug/evidence/GAME-SMOKE/20260603-152451`.
- Regression cases: MANUALQA-024-D, SAVE-003, DEBUGTELEPORT-001, MANUALQA-026-MINE-Y-CONSOLE

## Hook: Fishing.MiniGameUpdate

- Status: experimental
- Public surface: `IFishingAutomationApi.Configure` experimental `FishingResultMode.AutoCompleteVisibleMiniGame`, `FishingResultMode.SkipMiniGameNativeResult`, `FishingAnimationMode.FastCastPull`, `FishingAutomationOptions.AnimationMultiplier`, and `FishingAutomationOptions.CastChargeRatio`.
- Game build: 23465763 workshop
- Game method/type: `AgentStateFishingReady.OnEnter/OnPlay/NextState`, `AgentStateFishingCast`, `AgentStateFishingWait`, `AgentStateFishingPull`, `FishingGameScrollBar.StartGame/UpdateGame/StopGame`, `DolocUserInput` fishing/tool/item getter reads, `DolocUserInput.NormalUseToolInProgress` Ready charge hold/release, selected rod renderer animator, `FishRodRenderer.CastHook`, and `FishRodRenderer.Pull` / `PullCancel`.
- Patch type: Harmony phase hooks plus `FishingGameScrollBar.UpdateGame` prefix/postfix observation, scoped native input getter prefixes during that update, GameBridge-owned reflected animator writes, Ready charge timer extra tick, Ready charge target input hold/release, cast-hook physics scaling, and native pull-duration result scaling.
- Why this point: skip-minigame and visible auto-complete are different native result strategies. Skip routes a bite-ready state through the native no-minigame Pull/result path, while visible auto-complete now supplies native-style input to the real mini-game instead of writing success after a delay.
- Failure behavior: if the mini-game object/status/note data cannot be read safely, the bridge leaves the mini-game alone and records pending evidence rather than faking a fish reward. If Ready charge target state cannot be read, the bridge releases native input instead of forcing power. Internal high-frequency service exceptions are throttled by FishingAutomation operation so `FishingGameScrollBar.UpdateGame` cannot flood diagnostics or failed hook-status logs every frame. Mini-game handle/input state is cleared on save/title/environment reset, and animator speed writes are restored from the FishingAutomation service snapshot on fishing/state/lifecycle exits.
- Mods/tests depending on it: `Yuuka.DTMAPI.AutoFishing`.
- Evidence:
  - Build: DTMAPI 0.2.4 Release build/unit passed 2026-06-03; latest 2026-06-13 Release build/test passed after the native input rewrite, animation multiplier fix, and cast charge target setting.
  - Save: local slot 5 / index 4 for current AutoFishing fixture evidence; historical evidence used local slot 3 / index 2.
  - Log line: `Smoke.AutoFishingMiniGameComplete = verified ... behavior=AutoPlayVisibleMiniGame, status=Success, stableHoldFrames=82, releaseFrames=49`, `Smoke.AutoFishingAnimationSpeed = verified ... phase=Pull multiplier=3`, `Smoke.AutoFishingAnimationSpeed = verified ... behavior=FastPullDuration, source=FishRodRenderer.Pull, duration=0.104->0.035`, and `Smoke.AutoFishingCastCharge = verified ... source=AgentStateFishingReady.NextState, target=0.5, progress=0.54, input=release`.
  - 2026-06-09 restore log line: `GAME-SMOKE/20260609-170646` records `Smoke.AutoFishingAnimationSpeedRestore = experimental. reason=AgentStateFishingPull.OnExit, restored=2` with `RunStatus=Passed`, `AutoFishingMiniGameComplete=Passed`, `ProcessExited=Passed`, and no fatal popup.
  - 2026-06-10 lifecycle isolation log line: `GAME-SMOKE/20260610-012052` records the AutoFishing hotkey/phase/minigame-complete route passed with clean exit and no `Lifecycle callback failed` entries after `FishingPullExitPostfix` switched to independent callback isolation plus finally-equivalent restore.
  - 2026-06-13 input/multiplier evidence: `docs/debug/evidence/GAME-SMOKE/20260613-153632` proves native input-driven minigame success without citing the old direct `visibleSeconds=0.76` success path, and user manual QA confirmed red/green/yellow handling succeeds. `docs/debug/evidence/GAME-SMOKE/20260613-154252` proves Cast/Pull animator and Pull-duration scaling but is incomplete for visible cast speed. `docs/debug/evidence/GAME-SMOKE/20260613-160354` proves cast-hook physics scaling (`FastCastHookPhysics`, hook velocity/gravity), Pull duration scaling, hook-physics restore, report export, clean exit, and no fatal popup. `docs/debug/evidence/GAME-SMOKE/20260613-163812` proves Ready charge native `_castTimer.Tick(extraDt)` through `FastReadyCharge`, Cast hook physics, Pull duration scaling, final `readyCharge=True, fastAnimation=True`, report export, clean exit, and no fatal popup. Failed `docs/debug/evidence/GAME-SMOKE/20260613-153850` is retained as the old missing-multiplier migration bug; failed `docs/debug/evidence/GAME-SMOKE/20260613-163257` is retained as the old smoke-gate false negative before Ready charge was tracked independently.
  - 2026-06-13 cast-charge evidence: `docs/debug/evidence/GAME-SMOKE/20260613-171405` proves configured target `0.5` through native Ready input hold/release (`Smoke.AutoFishingCastCharge = verified ... progress=0.54, input=release`) while FastAnimations still proves Ready timer speed, Cast hook physics, Pull duration scaling, report export, clean exit, and no fatal popup. Failed/false-positive `docs/debug/evidence/GAME-SMOKE/20260613-170514` and failed `docs/debug/evidence/GAME-SMOKE/20260613-171104` are retained as script-gate evidence around the fixed PowerShell clamp bug.
  - 2026-06-13 toggle-key evidence: `docs/debug/evidence/GAME-SMOKE/20260613-175055` proves the F7 rebound path. `summary.txt` records `AutoFishingToggleKey=F7`, and `DTMAPI-latest.log` records `AutoFishing native loop policy registered. Toggle=F7`, `Input F7 pressed dispatched to DTMAPI mods`, `AutoFishing automation enabled reason=hotkey F7`, plus final loop `AutoCast:True->Wait:True->BiteReady:True->BattleOrPull:True->PullExit:True->NextAutoCast:True`. The implementation is intentionally AutoFishingMod/config/input ownership: it uses existing `IDtmConfigMenuApi.AddKeybindOption`, `IInputHelper.RegisterButton`, and `IInputHelper.UnregisterButton`, and does not change the native fishing GameBridge owner path.
  - Screenshot/report: current report `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260613-160443.zip`; historical skip/animation smoke `docs/debug/evidence/GAME-SMOKE/20260603-154816`; historical skip=false minigame smoke `docs/debug/evidence/GAME-SMOKE/20260603-173435`; animator restore smoke `docs/debug/evidence/GAME-SMOKE/20260609-170646`; current and historical process checks say no `DolocTown.exe`.
- Regression cases: AUTOFISHING-TOGGLE-KEY-REBIND-20260613, AUTOFISHING-MINIGAME-INPUT-ANIMATION-20260613, FISHING-NATIVE-RESPONSIBILITY-REVIEW-20260610, MANUALQA-024-C, AUTOFISH-001, FISHING-ANIMATOR-RESTORE-20260609

## Hook: Resources.OilCoalDrop

- Status: experimental
- Public surface: OilMod content plus GameBridge resource-hit bridge; no stable public API yet.
- Game build: 23465763 workshop
- Game method/type: native `DolocTown.ToolCollider.HandleTools` prefix/postfix around resource removal, existing one-action resource-hit completion path, and native item generation/backpack placement for `crude_oil`.
- Patch type: `OilCoalDropFeature`/`OilCoalDropService` runtime logic using the shared `ToolCollider.HandleTools` Prefix/Postfix installed by `ToolColliderHitHookBridge`; no official/Workshop JSON mutation.
- Smoke owner: `Smoke/Cases/OilCoalDropSmokeCase.cs` owns Oil metadata, coal-resource preparation, and coal-drop smoke helpers; `Smoke/ContentSmoke.cs` keeps NewContent/Mine scheduling and shared helpers.
- Why this point: Oil should remain an official JSON item while DTMAPI supplies the fragile coal-drop behavior through the bridge.
- Failure behavior: if the selected resource is not recognized as coal or the item cannot be generated/placed, no extra drop is awarded and the result summary records the skipped/failed path. Pending coal-resource hits are cleared on SaveLoaded, ReturnedToTitle, and EnvironmentReset so stale collider/resource pairs do not survive save/title/environment boundaries.
- Mods/tests depending on it: `DTMAPI.OilMod`.
- Evidence:
  - Build: DTMAPI 0.2.8 Release build/unit passed 2026-06-06 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: 0.2.8 new-content smoke `GAME-SMOKE/20260606-031316` logs `OilMod content item=crude_oil fuelEnergy=1500 officialJson=item_tbitem.json`, `Smoke.NewContentOilItemMetadata = verified` with `id=crude_oil`, native probe `found crude_oil in DolocConfig.Tables.TbItem`, and `OilMod mining drop OK ... oilDrop=crude_oil ... placement={Placed crude_oil x1 through native backpack placement.}`.
  - Feature split log line: `GAME-SMOKE/20260610-124357` records `Feature.OilCoalDrop = ready`, `Resources.OilCoalDrop = experimental`, `OilMod mining drop OK source=native-tool-hit`, `OilMod.MiningDrop = experimental`, `Smoke.NewContentOilCoalDrop = verified`, `NewContentOilCoalDrop=Passed`, `NewContentApis=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed` after Oil coal-drop state and the smoke force flag moved into `OilCoalDropService`.
  - Shared-route regression: `GAME-SMOKE/20260610-124511` records `Feature.ActionCompletion = ready`, `Feature.OilCoalDrop = ready`, unchanged `Actions.OneActionComplete = verified` and `Actions.OneActionFuelFeed = verified`, plus `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, and clean process/fatal checks; this verifies the shared ToolCollider postfix still dispatches ActionCompletion before OilCoalDrop.
  - Lifecycle cleanup: unit coverage on 2026-06-10 seeds the private pending-hit cache and verifies `OilCoalDropFeature.SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset` clear it. DirectExe regression smoke `GAME-SMOKE/20260610-135456` keeps OneAction resource/wrong-tool/fuel/feed/vegetation paths passed with `Feature.ActionCompletion = ready` and `Feature.OilCoalDrop = ready`; `GAME-SMOKE/20260610-135605` keeps `Feature.OilCoalDrop = ready`, `Resources.OilCoalDrop = experimental`, `OilMod.MiningDrop = experimental`, `Smoke.NewContentOilCoalDrop = verified`, `NewContentOilCoalDrop=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
  - Shared hook-owner regression: `GAME-SMOKE/20260610-140419` records the unchanged OneAction callback path after the ToolCollider hook moved to `ToolColliderHitHookBridge`; `GAME-SMOKE/20260610-140641` records `Feature.OilCoalDrop = ready`, `Resources.OilCoalDrop = experimental`, `OilMod.MiningDrop = experimental`, `Smoke.NewContentOilCoalDrop = verified`, `NewContentOilCoalDrop=Passed`, `NewContentApis=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`, verifying Oil pre-hit capture and post-hit drop still run on the shared Prefix/Postfix route.
  - Smoke case split: `GAME-SMOKE/20260610-141632` records `SchemaVersion=2`, `NewContentOilItemMetadata=Passed`, `NewContentOilCoalDrop=Passed`, `NewContentEquipmentSlots=Passed`, `NewContentMineOfficialJson=Passed`, `NewContentMineProduction=Passed`, `NewContentApis=Passed`, `Feature.OilCoalDrop = ready`, `Resources.OilCoalDrop = experimental`, `Smoke.NewContentOilItemMetadata = verified`, `OilMod.MiningDrop = experimental`, `Smoke.NewContentOilCoalDrop = verified`, clean process/fatal checks, and unchanged result/status field meanings after the Oil smoke methods moved to `Smoke/Cases/OilCoalDropSmokeCase.cs`.
  - Callback-isolation regression: `GAME-SMOKE/20260610-163928` records `NewContentOilItemMetadata=Passed`, `NewContentOilCoalDrop=Passed`, `NewContentEquipmentSlots=Passed`, `NewContentMineOfficialJson=Passed`, `NewContentMineProduction=Passed`, `NewContentApis=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed` after the shared ToolCollider postfix split ActionCompletion and OilCoalDrop into independent safe wrappers.
  - Screenshot/report: current metadata + coal-drop evidence `docs/debug/evidence/GAME-SMOKE/20260606-031316` with `NewContentOilItemMetadata=true`, `NewContentOilCoalDrop=true`, clean exit, and no fatal popup. The 0.2.4 `dtmapi_oil` evidence is retained as historical pre-rename proof.
  - Report note: the 2026-06-10 Oil feature split, pending-cleanup, shared hook-owner, and smoke-case split smokes did not export fresh report zips; their `latest-report.txt` files point to previous diagnostics/AnimalViewer reports and are not cited as OilCoalDrop report evidence.
- Regression cases: NEWCONTENT-024-F, MANUALQA-028-README

## Hook: Machine.ProductionRuntimeLoop

- Status: experimental
- Public surface: `IMachineProductionApi`, `MachineDefinition`, `MachineRecipeInput`, `MachineOutputRule`, `MachineProductionState`, and `MachineRegisterResult`. `MachineDefinition` exposes experimental native-tech route hints, recipe inputs, and electric-only or fuel-capable machine settings for JSON-backed machines. `MachineProductionState` exposes experimental telemetry for item/equipment/recipe/group ids, visual scale, electric-only/fuel-capable state, default mode, fuel capacity/remaining, fuel costs when fuel mode is enabled, electric cycle costs, cycle minutes/TUs, next due TUs, mode, last output/costs, output target, machine-owned storage fill/capacity, storage line capacity, and native tech-tree summary.
- Game build: 23465763 workshop
- Game method/type: DTMAPI update loop, `DolocAPI.ArchiveData`, current/root/archive/farm room candidate equipment enumeration, placed `Equipment`/`Case` inventory, `LinearInventory.PlaceItemAt`, native equipment `IElectronicComponent` / `ElectronicComponentAppliance.Launch()`, `ArchiveDataHandle.PassTimeNoControl`-driven time jumps observed through the runtime loop, `DolocAPI.assets.techTrees`, and runtime `DolocConfig.Tables.TbTechNode` injection.
- Patch type: GameBridge runtime loop and reflection over room/equipment/tech-tree/recipe state; official JSON owns the mine item/equipment/recipe. 0.2.7+ smoke may force the poll/observation pass but does not force production due.
- Why this point: a stable public machine contract should not expose raw Doloc Town equipment types, while the bridge can own fragile placed-equipment discovery and output delivery.
- Failure behavior: missing archive/current room/equipment data leaves the API registered but production idle; Mine production fails rather than silently falling back to backpack when Mine-owned storage is unavailable or full. Electric mode calls the native component `Launch()` before producing; if native power is unavailable, due time is retained and the low-power reason is logged. If fuel mode is disabled, normalized state forces fuel capacity and fuel costs to `0` and reports `fuel=disabled`; fuel-capable definitions still retain the older fuel telemetry. Bounded catch-up stops at the safety cap with telemetry rather than silently discarding cycles. DTMAPI reports electric/cycle/storage/tech-tree state through experimental config/API telemetry rather than exposing raw native machine UI types.
- Mods/tests depending on it: `DTMAPI.MineMod`.
- Evidence:
  - Build: DTMAPI 0.3.1 Release build/unit passed 2026-06-06 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: 0.3.1 new-content smoke `GAME-SMOKE/20260606-150834` remains historical fuel-capable evidence. The 2026-06-12 bottom-layer smoke `GAME-SMOKE/20260612-170708` verifies electric-only/no-fuel (`electricOnly=True`, `fuel=disabled`, `electricPowerCost=10`), production/storage, official tech-tree UI, HookProbe, SaveLoaded, and clean process/fatal checks.
  - Screenshot/report: current 0.5.1 Mine evidence is `docs/debug/evidence/GAME-SMOKE/20260612-170708`; game-side screenshots are under `NEWCONTENT-025/20260612-170750`. Historical retained Mine/Oil/Equipment evidence remains `docs/debug/evidence/GAME-SMOKE/20260606-150834`.
- Regression cases: NEWCONTENT-024-G, MANUALQA-025-MINE-STORAGE, MANUALQA-026-MINE-Y-CONSOLE, MANUALQA-027-ROOT-CAUSE, MANUALQA-028-README, MANUALQA-029-README, MANUALQA-031-REGRESSION-NEWCONTENT

## Hook: Machine.MineVisualContainment

- Status: experimental
- Public surface: no new public API; this is GameBridge-owned visual containment for `IMachineProductionApi` Mine definitions.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.EquipmentRenderer.OnReuse`, `DolocTown.EquipmentBuilder.CreateIndicator`, `DolocTown.EquipmentBuilder.TurnIndicator`, runtime current-room equipment enumeration, and GameBridge environment-reset refresh.
- Patch type: Harmony postfix hooks plus instance-scoped reflected `Transform.localScale` writes.
- Why this point: Mine needs a visible 2x sprite and 2x placement preview, but scaling shared renderers/prefabs can contaminate unrelated machines, chests, or decorative equipment.
- Failure behavior: pooled non-Mine renderers are reset to `1x1x1`; preview scale applies only when the builder's equipment proto/id resolves to `dtmapi_mine`; environment reset forces a quick machine-production visual poll so placed Mine scale is reapplied after room/camera transitions; smoke fails if any non-Mine equipment remains scaled.
- Mods/tests depending on it: `DTMAPI.MineMod`, smoke harness `-AutoExerciseMineContentApis`.
- Evidence:
  - Build: DTMAPI 0.2.9 Release build/unit passed 2026-06-06 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: 0.2.9 new-content smoke `GAME-SMOKE/20260606-053233` logs `Machine.VisualScale = verified. dtmapi_mine visualScale=2, rendererScale=2x2, applied=True`, `Machine.MineVisualContainment = verified. containment=True, contamination=False`, and `EquipmentRenderer.OnReuse resets localScale to 1`.
  - 2026-06-12 bottom-layer implementation: builder preview id resolution was widened, `Machine.MinePreviewScale` records preview scaling from `EquipmentBuilder.CreateIndicator/TurnIndicator`, and `DolocTownGameBridge.NotifyGameBridgeFeaturesEnvironmentReset` forces a visual poll before feature reset dispatch. Smoke `GAME-SMOKE/20260612-170708` logs `Machine.MineVisualContainment=verified` with `containment=True`, `contamination=False`, `mineScaled=1`, `nonMineScaled=0`, plus placement `rendererScale=2x2`.
  - Screenshot/report: current evidence is `docs/debug/evidence/GAME-SMOKE/20260612-170708`; placement screenshot is under game-side `NEWCONTENT-025/20260612-170750`; process check says no `DolocTown.exe`. Historical placement evidence remains `docs/debug/evidence/GAME-SMOKE/20260606-053233`.
- Regression cases: MANUALQA-026-MINE-Y-CONSOLE, MANUALQA-029-README

## Hook: Player.EquipmentSlotsApi

- Status: experimental
- Public surface: `IEquipmentSlotsApi`, `EquipmentSlotsOptions`, `EquipmentSlotsState`, `EquipmentSlotInfo`, `EquipmentSlotEquipResult`, `EquipmentSlotsRecoveryResult`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.GameData.AgentEquipmentManager.ReloadParams`, `DolocTown.UI.AccessoriesBar.__Init`, `DolocTown.UI.AccessoriesBar.OnStartShow`, and reflected native `DolocTown.UI.AccessorySlot` clone click/hover binding.
- Patch type: Harmony Postfix observation/update bridge plus interactive UI clone rendering; no raw equipment UI type exposed publicly.
- Why this point: `AgentEquipmentManager.ReloadParams` is the lowest-risk observed stats refresh point for DTMAPI-managed extra-slot attributes, while `AccessoriesBar` lifecycle hooks let DTMAPI render extra slot affordances without taking over vanilla visual equipment slots.
- Failure behavior: API state records hook installation, UI render state, stored/applied counts, click/hover availability, and recovery messages; populated extra-slot recovery returns items through native backpack placement with overflow email enabled. Runtime mutations mark DTMAPI protected sidecar storage dirty, but only a native SaveGame postfix persists sidecar JSON under the current save scope. SaveLoaded/ReturnedToTitle clear unsaved in-memory state. If Unity screenshot capture is unavailable in-save, logs keep `uiRendered`, occupied slot, interactive/hoverable counts, and policy evidence.
- Mods/tests depending on it: `DTMAPI.MoreEquipmentSlotsMod`.
- Evidence:
  - Build: DTMAPI 0.3.1 Release build/unit passed 2026-06-06 with 0 errors.
  - Build: 2026-06-14 protected-storage branch Release build passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`; third-save basic equipment smoke `GAME-SMOKE/20260614-011222` passed; user manual QA confirmed no-copy/cross-save behavior and disabled-mod backpack recovery; automated per-save orphan recovery, mail overflow, and hot-disable visual cleanup are pending.
  - Save: local slot 3 / index 2.
  - Log line: 0.3.1 new-content smoke `GAME-SMOKE/20260606-150834` logs `rendered=3, interactive=3, hoverable=3, readOnly=false`, passive `grandmas_button` equip/recover, attribute-only hat `straw_hat` equip/recover, native hat preserved `miner_helmet->miner_helmet->miner_helmet`, `preserveVanillaVisualSlots=true`, and final stored/recovered state `recoveredStored=0`. Latest protected-storage branch smoke `GAME-SMOKE/20260614-011222` revalidates those same equip/recover basics after local install and records legacy global storage adoption into save scope `slot-2`; user manual QA confirms stored-item save/reload, cross-save isolation, and disabled-mod backpack recovery. Same-process disable still leaves inert extra-slot visuals until restart, so an unregister/hide follow-up is needed.
  - Screenshot/report: current evidence `docs/debug/evidence/GAME-SMOKE/20260606-150834` rechecks `NewContentEquipmentSlots=true`, `interactive=3`, `hoverable=3`, `readOnly=false`, `attributeOnly=true`, `preserveVanillaVisualSlots=true`, clean exit, no fatal popup, and no leftover `DolocTown.exe`. Earlier read-only player equipment strip evidence remains historical; the strip screenshot fallback still returned unavailable, so the retained proof is log/summary based.
- Regression cases: NEWCONTENT-024-H, MANUALQA-028-README, MANUALQA-029-README, MANUALQA-031-REGRESSION-NEWCONTENT

## Hook: Player.EquipmentSlotsSaveTransaction

- Status: experimental
- Public surface: no new public API; this is GameBridge-owned persistence behavior behind `IEquipmentSlotsApi`.
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.SaveGame` / `DolocTown.GameData.DataPersistenceManager.SaveGame` postfix, `SaveLoaded`, and `ReturnedToTitle` runtime boundaries.
- Patch type: Harmony Postfix for native save plus DTMAPI runtime lifecycle callbacks.
- Why this point: equipment-slot sidecars must not commit no-save runtime mutations, must not be shared across saves, and still need to persist after the game has completed its own save.
- Failure behavior: sidecar JSON is not written when an equip/unequip occurs; dirty owner IDs are flushed only after native SaveGame. SaveLoaded and ReturnedToTitle clear in-memory state and dirty flags so unsaved mutations are discarded. New protected storage writes to `config/protected-items/equipment-slots/slot-<archiveIndex>/equipment-slots-<owner>.json` with archive/player/save-clock metadata; title-page registration waits for a loaded archive before reading sidecars; orphan recovery scans the current save scope and recovers tail slots first. Legacy global `equipment-slots-<owner>.json` is treated as migration input and archived after a successful per-save persist.
- Mods/tests depending on it: `DTMAPI.MoreEquipmentSlotsMod`, smoke harness `-AutoExerciseNewContentApis` and delayed `-AutoExerciseInstantSave`.
- Evidence:
  - Build: DTMAPI 0.2.9 Release build/unit passed 2026-06-06 with 0 errors.
  - Build: 2026-06-14 protected-storage branch Release build passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`; third-save basic equipment smoke `GAME-SMOKE/20260614-011222` passed and logged legacy global storage adoption/dirty state for save scope `slot-2`; user manual QA passed equip-save-reload, cross-save isolation, and disabled-mod backpack recovery. Automated disabled/unsubscribed orphan recovery, mail overflow, and hot-disable visual cleanup remain pending.
  - Save: local slot 3 / index 2.
  - Log line: no-save smoke `GAME-SMOKE/20260606-052246` logs `Player.EquipmentSlotsSaveTransaction = dirty` and no `SaveSaved`/`storage persisted`; the sidecar timestamp stayed at its prior value. Delayed instant-save smoke `GAME-SMOKE/20260606-053233` logs `Player.EquipmentSlotsSaveTransaction = dirty`, `SaveSaved hook dispatched`, `EquipmentSlots storage persisted owner=DTMAPI.MoreEquipmentSlotsMod reason=SaveSaved slot=2`, and `Player.EquipmentSlotsSaveTransaction = verified. Flushed 1 dirty equipment-slot owner(s) after native SaveGame completed.`
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260606-052246` and `docs/debug/evidence/GAME-SMOKE/20260606-053233`; process checks say no `DolocTown.exe`.
- Regression cases: NEWCONTENT-024-H, MANUALQA-029-README

## Hook: Save.MoreSlotsApi

- Status: experimental
- Public surface: `ISaveSlotsApi`, `SaveSlotsOptions`, `SaveSlotsState`, and `SaveSlotsRegisterResult`.
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.gameManager.archiveFileCount`, official `LocalSave.GetAllArchiveInfo`, and `GameDataUiState.Show -> GameDataPanel.Render`.
- Patch type: GameBridge runtime refresh/reflection plus Harmony `GameDataUiState.Show` Postfix for official save UI layout evidence; no direct save-file edits, no custom replacement save UI, and no inherited `GameDataPanel.Select(int)` hook in the fixed 12-slot path.
- Implementation owner: `SaveSlotsFeature` registers `ISaveSlotsApi`; `SaveSlotsService` owns registration state, throttled runtime `archiveFileCount` refresh, and expanded official-panel paging state; `DolocTownExperimentalBridgeApi` no longer implements the API.
- Why this point: More Saves should expand the official save screen while the game continues to own archive files, slot rendering, load, delete, and copy behavior.
- Failure behavior: if `DolocAPI.gameManager` is not available, registration reports pending and runtime refresh retries on a short 750 ms cadence until the manager appears. Once configured, refresh uses a 3 second heartbeat, and `SaveLoaded` forces a refresh. Enabled `SaveSlotsOptions` requests are normalized to exactly 12 total official slots for the MoreSaves player-facing path; disabling restores the vanilla target count of 6 but does not delete extra files. SaveSlots official-panel layout logic now rejects non-`DolocTown.UI.GameDataPanel` targets before calling `ResetLayoutSize`, so homepage and pause `DolocGridUI<T>` instances cannot be mistaken for save panels.
- Mods/tests depending on it: `DTMAPI.MoreSavesMod`, smoke harness official save UI evidence during save-slot selection.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors after the refresh throttle.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260610-100337` logs `Feature.SaveSlots = ready`, `Save.MoreSlotsApi = configured-official-archive-count`, one runtime correction `Official save slot count set 6->12 ... reason=runtime refresh native=6 target=12`, one load-boundary force refresh `Official save slot count set 12->12 ... reason=SaveLoaded native=12 target=12`, and `Smoke.MoreSavesOfficialSaveUi = verified. archiveFileCount=12, panelSlotCount=12, renderedSlots=12, path=DolocAPI.gameManager.archiveFileCount -> GameDataUiState.Show -> GameDataPanel.Render.` after moving the evidence recorder into `Smoke/Cases/SaveSlotsSmokeCase.cs`.
  - 2026-06-12 follow-up: MoreSaves no longer exposes a slot-count config option; old saved `SlotCount` values are migration-only and enabled requests normalize to 12. Unit coverage verifies enabled 6/24 requests both normalize to 12 and disabled requests normalize to vanilla 6. Third-save evidence `GAME-SMOKE/20260612-203558` records `MoreSavesOfficialSaveUi=Passed`, `MoreSavesOfficialSaveUiEvidence=Passed`, `archiveFileCount=12`, `panelSlotCount=12`, `renderedSlots=12`, and `visibleSlotsAfterPaging=12`.
  - 2026-06-12 bottom-layer implementation: `Save.MoreSlotsUiPaging` records `officialSaveUiPaging=True`, page bounds, and reason (`GameDataUiState.Show`, `GameDataPanel.Select`, or pager button). Smokes `GAME-SMOKE/20260612-170240`, `GAME-SMOKE/20260612-170331`, `GAME-SMOKE/20260612-170421`, and `GAME-SMOKE/20260612-170512` verify 6/12/18/24 layouts, `visibleSlotsAfterPaging=6/12/6/12`, and targets `1`, `1`, `1|13`, and `1|13|24`.
  - 2026-06-13 root fix: `GameDataPanel.Select` prefix was removed because Harmony resolved the inherited `DolocGridUI<T>.Select` path broadly and let homepage/pause grids enter SaveSlots layout code. Third-save evidence `GAME-SMOKE/20260613-144038` revalidated the fixed 12-slot official UI with `Save.MoreSlotsUiPaging = not-required`, `archiveFileCount=12`, `panelSlotCount=12`, `renderedSlots=12`, `visibleSlotsAfterPaging=12`, and no leftover process/fatal popup.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-100337` and report `dtmapi-report-20260610-100413.zip`; process/fatal checks say no `DolocTown.exe` and no fatal popup.
- Regression cases: MANUALQA-029-README, SAVE-001, OFFICIAL-001

## Hook: Save.MoreSlotsUiPaging

- Status: experimental
- Public surface: none beyond existing `ISaveSlotsApi`; this is an internal GameBridge UI adaptation for expanded official save panels.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.GameDataUiState.Show`.
- Patch type: Harmony Postfix on `GameDataUiState.Show`; the inherited `GameDataPanel.Select(int)` hook is intentionally not installed for the fixed 12-slot path.
- Why this point: fixed 12 save slots remain inside the official save panel while official `LocalSave`, `DataPersistenceManager`, `GameDataUiState`, and `GameDataPanel` keep ownership of archive data, rendering, load, delete, and copy behavior.
- Failure behavior: missing panel/slots/types leaves the official panel unmodified and records pending/failed status; non-`DolocTown.UI.GameDataPanel` callback targets are ignored. 6/12-slot panels restore original layout and destroy any stale DTMAPI pager.
- Mods/tests depending on it: `DTMAPI.MoreSavesMod`, smoke harness `-AutoExerciseMoreSavesOfficialSaveUi`.
- Evidence:
  - Build: 2026-06-12 bottom-layer Release build/test passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`; `git diff --check` passed with line-ending warnings only. 2026-06-13 root fix passed Release build and `git diff --check` after removing the Select hook.
  - Save: local slot 3 / index 2 for smoke evidence.
  - Log line: current fixed-12 evidence `GAME-SMOKE/20260613-144038` logs `Save.MoreSlotsUiPaging = not-required` and `MoreSaves official save UI evidence archiveFileCount=12, panelSlotCount=12, renderedSlots=12, visibleSlotsAfterPaging=12`. Historical expanded-paging smokes `GAME-SMOKE/20260612-170421` and `GAME-SMOKE/20260612-170512` remain pre-root-fix evidence only.
  - Screenshot/report: current SaveSlots screenshot evidence is under game-side `SAVESLOTS-UI/20260613-144113` and copied evidence in `docs/debug/evidence/GAME-SMOKE/20260613-144038`.
- Regression cases: BOTTOM-LAYER-REFACTOR-20260612, MANUALQA-029-README, SAVE-001, OFFICIAL-001

## Hook: Smoke.DiagnosticsSnapshot

- Status: verified
- Public surface: `IDtmDiagnosticsApi.GetSnapshot`
- Game build: 23465763 workshop
- Game method/type: no native game hook; this is a DTMAPI smoke status emitted after the runtime diagnostics API exports a report and reads the structured snapshot.
- Patch type: DTMAPI runtime diagnostics/status.
- Implementation owner: `DtmApiRuntime` registers `IDtmDiagnosticsApi`; `DiagnosticsService` stores errors, warnings, hook statuses, feature statuses, latest log path, latest report path, and internal report-only diagnostics aggregate counters; `DtmApiRuntime.CreateDiagnosticsSnapshot()` synthesizes discovered/loaded/disabled/error mod status rows and structured `StatusCode` values from runtime discovery, loaded mods, official enablement, and diagnostics errors; `DolocTownGameBridge` mirrors `Feature.<Id>` dispatch state into structured feature-status rows, throttling repeated successful `Update` publication to first/failure/recovery/non-`Update`/10-second heartbeat events.
- Why this point: verifies diagnostics snapshot consumers do not need to parse logs to find loaded mods, discovered/disabled/error mod statuses, structured status codes, warnings/errors, hooks, feature statuses, or report/log paths.
- Failure behavior: if an expected `Feature.<Id>` hook/status row is missing, if loaded mods do not have a loaded mod-status row with `StatusCode=loaded`, if any mod status row lacks a `StatusCode`, or latest log/report paths do not point to existing files, `Smoke.DiagnosticsSnapshot` is marked failed and the focused smoke fails.
- Mods/tests depending on it: smoke harness `-AutoExerciseZoom` and ActionSpeed interaction smoke; unit tests `DiagnosticsSnapshotApiExposesRuntimeState` and `OfficialLocalModPackagesRespectOfficialEnablement`.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2.
  - Camera log line: `GAME-SMOKE/20260610-043619` records `Smoke diagnostics snapshot OK scenario=Camera, expectedFeatures=Camera, loadedMods=14, mods=14, modStatusCodes=loaded=14, errors=0, warnings=0, hooks=60, features=5, latestLog=..., latestReport=...dtmapi-report-20260610-043801.zip` and `Smoke.DiagnosticsSnapshot = verified`.
  - ActionSpeed log line: `GAME-SMOKE/20260610-043830` records `Smoke diagnostics snapshot OK scenario=ActionSpeed, expectedFeatures=ActionSpeed, loadedMods=14, mods=14, modStatusCodes=loaded=14, errors=0, warnings=0, hooks=64, features=5, latestLog=..., latestReport=...dtmapi-report-20260610-043910.zip` and `Smoke.DiagnosticsSnapshot = verified`.
  - Unit status code rows: `DependencyVersionApiVersionAndCircularDependencyDiagnostics`, `DiagnosticsSnapshotApiExposesRuntimeState`, and `OfficialLocalModPackagesRespectOfficialEnablement` cover `loaded`, `disabled`, `missing-dependency`, `dependency-cycle`, `entry-dll-error`, `code-load-error`, `api-too-new`, and `unknown-error`.
  - Aggregate counter rows: 2026-06-10 unit coverage records 1005 repeated errors and warnings, keeps the retained error/warning windows capped at 1000, and verifies exported report summaries include two `DIAGNOSTIC-AGGREGATE` lines with total count and latest details. Camera snapshot smoke `GAME-SMOKE/20260610-142415` and ActionSpeed snapshot smoke `GAME-SMOKE/20260610-142626` verify the public snapshot surface stayed unchanged while matching report zips `dtmapi-report-20260610-142555.zip` and `dtmapi-report-20260610-142705.zip` were exported.
  - Report summary: both report zips contain `Errors: 0`, `Warnings: 0`, `LatestLogPath`, `LatestReportPath`, the matching `HOOK Feature.Camera` / `HOOK Feature.ActionSpeed` row, and the matching `FEATURE Camera` / `FEATURE ActionSpeed` structured row.
- Regression cases: DIAGNOSTICS-AGGREGATE-COUNTERS-20260610, DIAGNOSTICS-STATUS-CODES-20260610, DIAGNOSTICS-SNAPSHOT-20260609, DIAGNOSTICS-SNAPSHOT-MOD-STATUS-20260610, FEATURE-STATUS-PUBLISH-THROTTLE-20260610

## Hook: Feature.Camera

- Status: ready
- Public surface: internal GameBridge diagnostics/status only; public camera APIs remain `ICameraViewApi` and obsolete `ICameraZoomApi`.
- Game build: 23465763 workshop
- Game method/type: no native game hook; this status is emitted by the DTMAPI GameBridge feature host around CameraFeature dispatch.
- Patch type: GameBridge runtime diagnostics/status.
- Implementation owner: `DolocTownGameBridge` safe-dispatches every `IGameBridgeFeature` operation, records `Feature.<Id>` hook status, mirrors the same state into `DiagnosticsService` feature-status rows, and keeps an internal feature-status model with feature id, last operation, success/failure, failure count, and last error; repeated successful `Update` publication is throttled to first/failure/recovery/non-`Update`/10-second heartbeat events; `CameraFeature.Id` is `Camera`; Camera hook installation is dispatched through `InstallHooks`.
- Why this point: proves the Camera feature is registered with the feature host instead of being a special one-off bridge path.
- Failure behavior: `RegisterApis`, `PublishHookStatuses`, `InstallHooks`, `Update`, `SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset` dispatches are wrapped per feature. A feature exception records `DTMAPI.GameBridge.Feature.<Id>` diagnostics, marks `Feature.<Id>` failed, updates the internal failure count and last error, logs the exception type/message, and does not block the next feature. Successful `Update` dispatches update the internal model but publish hook/diagnostics status only on the 10-second heartbeat unless a failure/recovery or non-`Update` operation occurs.
- Mods/tests depending on it: internal Camera feature host smoke evidence; `DTMAPI.HookProbeMod` observes the hook status.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2.
  - Latest log line: `GAME-SMOKE/20260610-043619` logs `Feature.Camera = ready` for `PublishHookStatuses`, `InstallHooks`, `Update`, `ReturnedToTitle`, `SaveLoaded`, and `EnvironmentReset`, with `Feature status: id=Camera, lastOperation=..., success=True, failureCount=0, lastError=none`; `Smoke.DiagnosticsSnapshot = verified` confirms the structured diagnostics snapshot also contains `FEATURE Camera`, `mods=14`, and `modStatusCodes=loaded=14`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-043619`; report zip pointer `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-043801.zip`.
- Regression cases: CAMERA-HOOK-OWNER-FEATURE-20260609, GAMEBRIDGE-FEATURE-HOST-HARDENING-20260609, CAMERA-PLAYABLE, DIAGNOSTICS-SNAPSHOT-20260609, DIAGNOSTICS-SNAPSHOT-MOD-STATUS-20260610, DIAGNOSTICS-STATUS-CODES-20260610, FEATURE-STATUS-PUBLISH-THROTTLE-20260610

## Hook: Camera.ViewApi

- Status: experimental
- Public surface: `ICameraViewApi`, `ICameraViewLease`, `CameraViewRequest`, `CameraViewResult`, `CameraViewState`, and `GetSnapshot(string uniqueId)`.
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.mainCamera.orthographicSize`; `DolocAPI.SetEnvCamera(...)` is observed only as a lifecycle boundary where DTMAPI reapplies the active playable-view orthographic size.
- Patch type: GameBridge runtime reflection plus Harmony Postfix on `DolocAPI.SetEnvCamera`; no raw Unity camera object or decompiled game type is exposed through the public API.
- Implementation owner: `DolocTownGameBridge` hosts `IGameBridgeFeature` instances and safe-dispatches `RegisterApis`, `PublishHookStatuses`, `InstallHooks`, `Update`, `SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset` to `CameraFeature`; `CameraFeature.Id` is `Camera`, owns `cameraViewSetEnvCameraPatched`, installs the `DolocAPI.SetEnvCamera` postfix, and registers `ICameraViewApi` through `CameraViewService` plus diagnostics through `CameraDiagnosticsService`; `DolocTownGameBridge` no longer stores `cameraZoomSetEnvCameraPatched`; `SmokeHarness` owns `SmokeUpdate()` scheduling only; `Smoke/Cases/CameraPlayableSmokeCase.cs` owns the `AutoExerciseZoom` / `Smoke.CameraPlayable` case implementation; `DolocTownExperimentalBridgeApi` no longer implements this camera API.
- Why this point: playable zoom should keep the native camera follow/range semantics intact. DTMAPI only changes the gameplay camera orthographic size and lets the native `CameraController.UpdateCamPosition(...)` path continue following the player. The old `CameraController.RefreshResolution()` / `SetPosition(...)` / `RefreshScanner()` / background/fog compensation path is intentionally not used for playable zoom because manual QA showed it mixes panorama semantics into normal play.
- Failure behavior: leases may report pending while the main camera is unavailable; runtime refresh retries the orthographic write. DTMAPI arbitrates active leases by highest priority, then latest update order. Releasing the active lease falls back to the next active lease or restores vanilla `1x`. `SaveLoaded`, `ReturnedToTitle`, explicit release/reset, and environment-camera transitions restore or reapply only the orthographic-size playable-view state. Feature dispatch exceptions are isolated by the GameBridge feature host and recorded under `DTMAPI.GameBridge.Feature.Camera`. UI scale remains unchanged.
- Mods/tests depending on it: `DTMAPI.ZoomMod`, smoke harness `-AutoExerciseZoom` / `Smoke.CameraPlayable`.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2 required.
  - Latest log line: `GAME-SMOKE/20260611-031502` includes `Feature.Camera = ready`, `Camera.ViewEnvironmentLifecycle = experimental`, `Camera.ViewApi = contract`, `Camera.ZoomApi = obsolete-compatibility`, `Smoke.CameraPlayable = verified`, `Smoke.Zoom = verified`, `Smoke.DiagnosticsSnapshot = verified`, and smoke result `DiagnosticsReportExport=Passed`; prior diagnostics snapshot detail evidence remains `GAME-SMOKE/20260610-043619`.
  - 2026-06-12 native-owner review: `docs/reviews/api/2026/20260612-camera-background-native-owner-review.md` keeps background/fog/panorama synchronization deferred. `DolocAPI.LoadBackground`, `BackgroundRenderer`, `BackgroundLayerRenderer`, `EnvCovariantController`, and `DepthFogController` are separate native owners from the playable orthographic-size lease, so no runtime patch was added in this batch. The bottom-layer branch records this deferral in update `20260612-0012`; Steam HookProbe regression `GAME-SMOKE/20260612-170928` still verifies `Feature.Camera=ready`, `Camera.ViewApi=vanilla/contract` lifecycle statuses, SaveLoaded, clean exit, and no leftover process.
  - Latest screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-040940`; camera screenshots, telemetry, and summary under `DTMAPI-evidence/CAMERA-PLAYABLE/20260610-041023`; report zip pointer `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-041125.zip`.
  - Manual QA gate: `docs/reviews/manual-qa/2026/20260610-0006-cameraview-manual-play-gate.md` is pending user confirmation for 2x/4x true-input movement, background flicker, native clamp, building transition, return-to-title reload, and ZoomMod hotkey/config interaction; the 2026-06-11 handoff keeps the gate pending, adds `docs/goals/2026/20260611-0001-cameraview-manual-play-handoff.md`, and uses `GAME-SMOKE/20260611-031502` as the latest supporting automated evidence only.
  - Latest case-file validation: `GAME-SMOKE/20260609-110528` plus `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/CameraPlayableSmokeCase.cs` with the same content hash as the previous `Smoke/CameraSmoke.cs`; result schema and screenshot/evidence names stayed unchanged.
  - Prior feature-host split log line: `GAME-SMOKE/20260609-031302` on `Refactor` includes `Smoke exercise CameraPlayable OK`, active 4x `DTMAPI.ZoomMod` lease, fallback 2x `DTMAPI.CameraViewCompetingSmoke` lease after high-priority release, reset to 1x, `nativeRefresh=not-called-playable`, `uiScale=unchanged`, `Smoke.CameraPlayable = verified`, and `Smoke.Zoom = verified`.
  - Prior feature-host split screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260609-031302`; camera screenshots, telemetry, and summary under `DTMAPI-evidence/CAMERA-PLAYABLE/20260609-031342`; report zip `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260609-031339.zip`.
  - Log line: `GAME-SMOKE/20260608-150914` includes `Smoke exercise CameraPlayable OK`, active 4x `DTMAPI.ZoomMod` lease, fallback 2x `DTMAPI.CameraViewCompetingSmoke` lease after high-priority release, reset to 1x, `nativeRefresh=not-called-playable`, `uiScale=unchanged`, `Smoke.CameraPlayable = verified`, and `Smoke.Zoom = verified`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260608-150914`; camera screenshots and summary under `D:\Steam\steamapps\common\Doloc Town\DTMAPI\evidence\CAMERA-PLAYABLE\20260608-150952`.
- Regression cases: CAMERA-PLAYABLE, CAMERAVIEW-MANUAL-QA-GATE-20260610, CAMERA-HOOK-OWNER-FEATURE-20260609, GAMEBRIDGE-FEATURE-HOST-HARDENING-20260609, ZOOM-030-F, ZOOM-042-API-REBUILD

## Hook: Camera.ZoomApi

- Status: obsolete-compatibility
- Public surface: `ICameraZoomApi`, `CameraZoomOptions`, `CameraZoomRegisterResult`, `CameraZoomResult`, `CameraZoomState`, and `GetSnapshot(string uniqueId)`.
- Game build: 23465763 workshop
- Game method/type: compatibility wrapper over `ICameraViewApi`; no direct CameraController/background/fog/scanner owner path.
- Patch type: API redirect only.
- Implementation owner: `DolocTownGameBridge` feature-host dispatch calls `CameraFeature.RegisterApis(...)`; `CameraFeature` registers `ICameraZoomApi` through `CameraZoomCompatibilityService`; `DolocTownExperimentalBridgeApi` no longer implements this obsolete compatibility API.
- Why this point: existing migrated mods can continue compiling while moving to lease-based playable camera view. New code should use `ICameraViewApi`.
- Failure behavior: calls are redirected to a per-owner compatibility lease. Obsolete `CameraZoomOptions.RefreshCameraController`, `CompensateBackground`, `CompensateDepthFog`, and `RefreshScanners` are ignored for playable zoom.
- Mods/tests depending on it: legacy callers only.
- Evidence:
  - Build: 2026-06-09 Release build/test passed with 0 warnings and 0 errors.
  - Save: n/a for compatibility wrapper by itself; `CAMERA-PLAYABLE` smoke validates the real playable path.
  - Log line: `GAME-SMOKE/20260609-031302` includes `Camera.ZoomApi = obsolete-compatibility`.
  - Screenshot/report: use `Camera.ViewApi` evidence from `GAME-SMOKE/20260609-031302` instead.
- Regression cases: CAMERA-PLAYABLE

## Hook: Inventory.ChestLocatorEnhancer

- Status: experimental
- Public surface: `IChestLocatorEnhancerApi`, `ChestLocatorEnhancerOptions`, `ChestLocatorEnhancerRegisterResult`, and `ChestLocatorEnhancerState`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.GameData.ArchiveDataHandle.GetAvailableInventories(Vector2Int anchor, Vector2Int area, bool useBox)`, native `LinearInventory[]`, reflected current/root/farm/building-room enumeration, shared `Case` inventories, and shared `StorageShelf` item boxes.
- Patch type: Harmony postfix over the native inventory-array return value. `ChestLocatorEnhancerFeature` registers `IChestLocatorEnhancerApi` through `ChestLocatorEnhancerService`; `ChestLocatorEnhancerHookBridge` owns the patch installation. `Smoke/Cases/ChestLocatorEnhancerSmokeCase.cs` owns the `Smoke.ChestLocatorEnhancer` focused case implementation. GameBridge appends native inventory instances; ordinary mods register policy only and do not own reflection/Harmony traversal.
- Why this point: native recipe/material code already calls `CountItem`, `MaxCostItem`, and `TryCostItem` extension methods over the available-inventory array. Extending the array keeps native transaction behavior while letting shared chests outside the immediate room participate.
- Failure behavior: if the postfix is missing, the API state stays configured/pending and native behavior is unchanged. DTMAPI merges all enabled owner policies before each native callback: `Enabled` is any enabled owner; `IncludeSharedCases`, `IncludeSharedStorageShelfBoxes`, and `VerboseLogging` are any enabled true; `RespectNativeAutoUseBoxSetting` is all enabled true, so any owner can opt into forced shared box scanning. Effective owners and merged booleans are reported through existing state messages and runtime summaries. Inventories are deduped from live native objects on each callback; no cross-frame inventory cache or raw game type is exposed through public DTOs.
- Mods/tests depending on it: `DTMAPI.ChestLocatorEnhancerMod`, smoke harness `-AutoExerciseChestLocatorEnhancer`.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2.
  - Latest log line: `GAME-SMOKE/20260610-050506` logs `Feature.ChestLocatorEnhancer = ready`, `ChestLocatorEnhancer API register success=True`, `Inventory.ChestLocatorEnhancer = verified`, `ChestLocatorEnhancer inventories owner=DTMAPI.ChestLocatorEnhancerMod, effectiveOwners=DTMAPI.ChestLocatorEnhancerMod, includeSharedCases=True, includeSharedStorageShelfBoxes=True, respectNativeAutoUseBox=True, verboseLogging=True, useBox=True, nativeAutoUseBox=True, base=1, appended=5, roots=1, equipments=283, sharedCases=5, sharedStorageBoxes=0`, and `Smoke.ChestLocatorEnhancer = verified` with `item=dtmapi_mine, baseline=0, afterPlace=3, afterCost=1`.
  - Dual-owner policy unit evidence: `DTMAPI.UnitTests` method `ChestLocatorPoliciesMergeEnabledOwners` verifies deterministic `effectiveOwners=DTMAPI.Tests.ChestPolicyA|DTMAPI.Tests.ChestPolicyB`, any-true `IncludeSharedCases` / `IncludeSharedStorageShelfBoxes` / `VerboseLogging`, and all-true `RespectNativeAutoUseBoxSetting` behavior through existing `LastMessage`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-050506`; result has `ChestLocatorEnhancer=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`; evidence zip is `docs/debug/evidence/GAME-SMOKE/20260610-050506.zip`. This ChestLocator-only smoke did not export a fresh runtime report zip; its stale `latest-report.txt` pointer is not cited as branch report evidence.
- Regression cases: CHESTLOCATOR-SMOKE-CASE-SPLIT-20260610, CHESTLOCATOR-MERGED-POLICY-20260610, CHESTLOCATOR-030-G, CHESTLOCATOR-FEATURE-SPLIT-20260610

## Hook: Farming.StrongPlantingGun

- Status: experimental
- Public surface: `IStrongPlantingGunApi`, `StrongPlantingGunOptions`, `StrongPlantingGunRegisterResult`, and `StrongPlantingGunState`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.ItemFarmingGun` constructors, `ItemFarmingGun.OnUseAsTool`, `DolocTown.FarmingGunUiState.HandlePlaceToOtherSide/HandleSwapOneItem`, official `ItemFunctionFarmingGun` capacity fields, native `LinearInventory`, and official private farming-gun `CheckCanInteract` / `DoInteract` basin checks.
- Patch type: Harmony constructor postfix and tool/UI transfer prefixes owned by `StrongPlantingGunHookBridge`. `StrongPlantingGunService` preserves or expands native inventory capacity only up to the verified fixed three-slot seed/film/fertilizer contract and routes those slots through native use checks; ordinary mods register policy only. Wider slot counts and real range expansion are not supported by this contract.
- Why this point: the official farming gun already owns basin area selection and per-item plant/film/fertilizer checks. Expanding its storage and delegating to those checks keeps slot behavior compatible with native farming rules instead of hand-rolling crop placement.
- Failure behavior: if tool/UI hooks are missing, registration reports configured/pending and native one-slot behavior remains unchanged. Inventory expansion is scoped to official farming gun instances, UI transfer hooks preserve native backpack cost/place transactions, and public DTOs expose only policy/status/count telemetry. Disabled state restores native one-slot behavior, but already-expanded visible UI slots may remain inactive until re-equip/restart; the migrated config UI now warns not to store items there while disabled.
- Mods/tests depending on it: `DTMAPI.StrongPlantingGunMod`, smoke harness `-AutoExerciseStrongPlantingGun`.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors after the feature split.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260610-101436` logs `Feature.StrongPlantingGun = ready`, `Farming.StrongPlantingGun = experimental`, `StrongPlantingGun API register success=True ... toolHook=True uiHook=True`, `StrongPlantingGun use owner=DTMAPI.StrongPlantingGunMod, slots=3, equipments=1, seedActions=1, filmActions=1, fertilizerActions=1, waterActions=0, consumed=3`, `Farming.StrongPlantingGun = verified`, and `Smoke.StrongPlantingGun = verified ... capacities=inventory:3/total:3/line:3 ... basinState=planted:True,protected:True,fertilized:True`.
  - Smoke case split: `GAME-SMOKE/20260610-122933` logs the same `Feature.StrongPlantingGun = ready`, `Farming.StrongPlantingGun = verified`, and `Smoke.StrongPlantingGun = verified` statuses after moving only the StrongPlantingGun smoke body/helpers into `Smoke/Cases/StrongPlantingGunSmokeCase.cs`; report `dtmapi-report-20260610-123010.zip`.
  - 2026-06-11 Manual QA Batch 1: service normalization now forces `SlotCount=3`, the migrated config no longer exposes unsupported slot count/range controls, and smoke `GAME-SMOKE/20260611-214654` records `StrongPlantingGun=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, clean process/fatal checks, and report `dtmapi-report-20260611-214731.zip`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-101436` and report `dtmapi-report-20260610-101512.zip`; result has `StrongPlantingGun=Passed`, `SaveLoaded=Passed`, `HookProbe=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Regression cases: STRONGPLANTINGGUN-FEATURE-SPLIT-20260610, STRONGPLANT-030-H

## Hook: Crops.HarvestingApi

- Status: experimental
- Public surface: `ICropHarvestingApi`, `CropHarvestRequest`, `CropHarvestResult`, `CropHarvestTargetResult`, `CropHarvestScope`, `CropHarvestTargetKind`, and `CropHarvestTargetStatus`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.PlantBasin.CouldHarvest`, `DolocTown.PlantBasin.IsCropMature`, `DolocTown.PlantBasin.Harvest(bool putInBackpack, bool sendMessage)`, diagnostic `DolocTown.Crop.isMature`, farm-root equipment containers, and farm building-room traversal.
- Patch type: explicit GameBridge API request through `CropHarvestingFeature` / `CropHarvestingService`; no Harmony patch is installed for ordinary crop harvesting.
- Why this point: official `PlantBasin.Harvest` owns crop output, broadcast, crop after-harvest state, and basin cleanup. DTMAPI should delegate to this native responsibility instead of hand-spawning harvested items or exposing raw `PlantBasin` / `Crop` objects to ordinary mods.
- Failure behavior: overlapping batches return `Busy`; no mature executable targets are successful no-ops; non-mature or already-harvested targets are reported without mutation; `TreeBasinCrop`/cocoa-style `PlantBasinTree` and `GrassForageBasin` are classified as scan-only/unsupported instead of executed; native invocation failures are recorded as diagnostics and `NativeHarvestFailed`.
- Mods/tests depending on it: `Yuuka.DTMAPI.AutoHarvest` sample mod, explicit-install developer-only `DTMAPI.CropHarvestingQaMod` manual QA fixture, smoke harness `-AutoExerciseCropHarvestingApi` using owner `DTMAPI.Smoke.CropHarvesting`.
- Evidence:
  - Build: 2026-06-12 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2.
  - Manual QA handoff: `docs/reviews/manual-qa/2026/20260612-0003-crops-harvesting-real-field-manual-qa.md` is pending user confirmation and uses `CropHarvestingQaMod` for real-field ordinary/vine/mushroom-bag/bush/tree-basin/grass-forage checks. The fixture is not installed by default developer-local installs; use `install-to-game.ps1 -InstallQaFixtures`, then use the DTMAPI Settings buttons or manually bind optional hotkeys.
  - Cleanup log line: `GAME-SMOKE/20260612-101549` logs `CropHarvesting API Scan owner=DTMAPI.Smoke.CropHarvesting ... mature=0 ... failed=0`, then `Harvest owner=DTMAPI.Smoke.CropHarvesting ... mature=1 harvested=1 failed=0`, `CropHarvestingApi=Passed`, and `CropHarvestingApiEvidence=Passed`.
  - Hardening log line: `GAME-SMOKE/20260612-072544` logs `CropHarvesting API Scan ... mature=0 harvested=0 skipped=172 failed=0 targetFilter=1` and `noTargetScan={success=True ... mature=0 ... failed=0}`, then logs the ordinary transient `PlantBasinSimple` path with `scan={success=True ... mature=1 ... failed=0}`, `harvest={success=True ... harvested=1 ... failed=0}`, `Crops.HarvestingApi = verified`, and `Smoke.CropHarvestingApi = verified`.
  - Log line: `GAME-SMOKE/20260612-062154` logs `Feature.CropHarvesting = ready`, `Crops.HarvestingApi = scan-verified`, `Crops.HarvestingApi = verified`, and `Smoke.CropHarvestingApi = verified` with a transient `PlantBasinSimple`, `seed_endyam`, native planting setup, `mature=False->True`, `scan={success=True ... mature=1 harvested=0 skipped=171 failed=0 targetFilter=1}`, and `harvest={success=True ... mature=1 harvested=1 skipped=171 failed=0 targetFilter=1}`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260612-062154`; process/fatal checks say no `DolocTown.exe` and no fatal popup.
- Regression cases: CROPS-HARVESTING-API-20260612
