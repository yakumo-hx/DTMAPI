# DTMAPI Public API Stability Matrix

Status date: 2026-06-09

This matrix is the public API stability contract. It is intentionally stricter than the smoke-evidence ledger: a smoke run can prove that a code path executed, but it does not by itself make an API stable for ordinary mods.

## Status Vocabulary

| Status | Meaning | Compatibility Promise |
| --- | --- | --- |
| Stable | Public contract is DTMAPI-owned or has proven native owner coverage. Breaking changes require migration notes, compatibility handling, and a new update record. | Ordinary mods may depend on it. |
| StableCandidate | Contract shape is likely correct, but it still lacks one or more promotion gates such as two real mods, manual QA, or regression matrix coverage. | Ordinary mods may experiment, but should expect final polish changes. |
| Experimental | API is usable for test mods or carefully scoped real mods, but native ownership, lifecycle, conflict behavior, or failure modes are not fully proven. | Ordinary mods should treat it as version-sensitive. |
| Diagnostic | Debug, smoke, report, console, or developer-support surface. It may call powerful native paths, but it is not a stable gameplay API. | Ordinary gameplay mods should not depend on it. |
| Proposed | Design direction or desired API surface with no current stable implementation guarantee. | Do not depend on it. |
| Failed | Existing implementation or evidence is known insufficient or contradicted by manual QA. | Do not use as completion evidence or stable API proof. |

`Blocked` is a qualifier, not a status. A row may be `Experimental` and explicitly blocked for native runtime creation until its GameBridge adapter is proven.

## Stable Promotion Criteria

An API may move to `Stable` only after all relevant gates are satisfied:

1. It is backed by a clear DTMAPI owner or a reviewed Doloc Town native responsibility owner.
2. It exposes stable DTMAPI contracts and does not expose raw Unity, Harmony, BepInEx, or copied decompiled game types.
3. At least two real mods, not only a smoke helper, use the API successfully.
4. Game evidence proves the API in the appropriate real gameplay path, usually the third local save unless a task states otherwise.
5. Visual, UI, input, camera, save/load, lifecycle, machine, vehicle, or other player-visible APIs have manual QA or screenshot/video evidence that covers the actual user-facing failure mode.
6. The debug regression matrix has a row for the API's main success path and high-risk failure path.
7. Clean exit evidence exists when the API participates in runtime/gameplay behavior: no leftover `DolocTown.exe` and no Steam waiting-for-exit regression.
8. Known failures, rejected hypotheses, and rollback notes are recorded in the relevant debug, review, hook-map, update, or API contract document.

Static screenshot smoke is allowed as supporting visual evidence, but it is not completion evidence for a visual/UI/camera API unless manual QA or an equivalent targeted visual review covers the same failure mode.

## Stability Matrix

| Area | API / Contract | Status | Evidence Scope And Current Boundary |
| --- | --- | --- | --- |
| Framework | `DtmMod`, `IManifest`, `IManifestDependency`, `IDtmHelper` | Stable | DTMAPI-owned mod entry and helper contracts. No Doloc native owner is implied. Manifest/dependency hardening is tracked separately by Core runtime tests. |
| Framework | `IMonitor.Log`, `LogOnce`, `LogException` | Stable | DTMAPI-owned logging contract used across runtime and mods. |
| Framework | `IConfigHelper.ReadConfig<T>`, `WriteConfig<T>`, `GetConfigPath` | Stable | DTMAPI-owned config file contract. Stability depends on preserving bad-JSON recovery and atomic write behavior introduced by Core runtime hardening. |
| Framework | `IConfigHelper.RegisterMigration<T>` | StableCandidate | Contract exists, but needs more real migration cases before stable promotion. |
| Framework | `IModRegistry.IsLoaded`, `Get`, `GetAll`, `GetApi<T>`, helper-bound `RegisterApi<T>` | StableCandidate | DTMAPI-owned registry. `RegisterApi<T>` must remain helper owner-bound so ordinary mods cannot spoof another manifest owner. Promote after the owner-bound contract is released and used by two real mods. |
| Framework | `ITranslationHelper.Language`, `Get` | StableCandidate | Used by migrated mods for Chinese/English config text. Needs broader locale/manual QA before `Stable`. |
| Events | `IGameLoopEvents.GameLaunched` | StableCandidate | DTMAPI Core dispatch after mod entry. Promotion needs regression matrix coverage for load order and failure isolation. |
| Events | `IGameLoopEvents.UpdateTicked`, `OneSecondUpdateTicked` | Experimental | High-frequency events are fragile. Core now has consecutive-failure circuit breakers, but timing, UI blocking, and fallback tick behavior still require regression rows before stable promotion. |
| Events | `IGameLoopEvents.ReturnedToTitle` | Experimental | Lifecycle event exists, but title/save boundary semantics must stay tied to game evidence and clean-exit checks. |
| Events | `ISaveEvents.SaveLoaded`, `SaveSaving`, `SaveSaved` | Experimental | Reaches important native save/load paths, but save transaction semantics, sidecar writes, failed saves, and reload boundaries remain high-risk. |
| Input | `IInputEvents.ButtonPressed`, `ButtonReleased`; `IInputHelper.RegisterButton`, `IsDown`, `WasPressed`, `Suppress` | Experimental | Works for migrated hotkeys. `IInputHelper.Suppress` is currently one-frame DTMAPI helper state cleared by `Input.ClearFrame` with `WasPressed`; it is not consumed by Doloc Town native input hooks and does not provide full native input isolation. Do not mark stable until native/UI conflict policy is proven. |
| Config UI | `IDtmConfigMenuApi` registration/query APIs (`Register`, `Add*`, `SetDisplayName`, `GetKeybindConflicts`), `DtmColorPreset`; page/item state models used by DTMAPI runtime UI | StableCandidate | Ordinary mods may register config controls and query keybind conflicts only. Page enumeration, page lookup, begin/save/reset/cancel, pending preview, and official-enable lock control are internal `IConfigMenuRuntime` responsibilities for DTMAPI Core/UI, not ordinary mod API. Stable promotion still needs real-mod manual UI QA, config save/cancel/reset evidence, scrolling/visibility/conflict regression rows, and official enablement lock checks. |
| UI | `IUiHelper.OpenDtmApiStatusPage`, `OpenModListPage`, `OpenConfigPage`, `OpenErrorPage`, `OpenHookStatusPage` | Experimental | Player-facing DTMAPI UI helpers. Manual QA is required for layout, input blocking, and stale-state/flicker cases before promotion. |
| Diagnostics | `IDiagnosticsHelper.GetErrors`, `ExportLogs`, `GetLatestLogPath`, `IDiagnosticsEvents.LogExported` | StableCandidate | DTMAPI-owned reporting/log-export surface. Promote after report contents and failure paths are covered in regression matrix. |
| Diagnostics | `IDiagnosticsHelper.GetHookStatuses`, `RecordEvidence`, `IDiagnosticsEvents.HookStatusChanged` | Diagnostic | Useful for status pages, smoke harness, and evidence capture. Hook status text is not native gameplay proof and must not be cited as completion evidence by itself. |
| Workshop / Content | `IWorkshopHelper.GetOfficialMods`, `GetDtmApiMods`, `IsOfficialEnablementManaged`, `GetEnablementHint`, `IWorkshopEvents.ModListChanged` | Experimental | Reads official/local/Workshop-capable manifests and respects official enablement. Needs stronger Steam/official UI regression coverage before promotion. |
| Content Query | `IContentQueryHelper.FindAssets`, `GetKnownContentTypes`, `TryReadTextAsset`, `GetIndexedItems`, `GetIndexedItem`, `IContentItemInfo` | Experimental | Read-only official/Workshop source index. It must remain separate from taking ownership of official content loading. |
| Content Pipeline | Content pack edits, ContentPatcher-style actions, update checks, compatibility DB, reflection helper, console command author API | Proposed | Direction exists in planning docs, but no stable public contract is promised here. |
| Migrated Gameplay | `IActionCompletionApi` | Experimental | OneActionComplete evidence reaches several native paths, but resource classification and exception paths remain GameBridge-owned and version-sensitive. |
| Migrated Gameplay | `IActionSpeedApi` | Experimental | Used by ActionSpeedMod with real gameplay smoke evidence. Latest feature-status model evidence is `GAME-SMOKE/20260609-141440`, which verifies tool animation, config apply, interaction slices, auto-fill, `Feature.ActionSpeed = ready` with internal `lastOperation/success/failureCount/lastError` details, and clean exit; post-merge `Refactor` evidence remains `GAME-SMOKE/20260609-134246`; pre-merge feature-host evidence was `GAME-SMOKE/20260609-133725`; earlier branch-package evidence remains `GAME-SMOKE/20260609-120153`. It still patches many interaction/animation paths and needs broader conflict/lifecycle regression coverage. |
| Migrated Gameplay | `IFishingAutomationApi` | Experimental | Used by AutoFishingMod with F6, movement cancel, auto-cast, and minigame evidence. Still high-risk because it owns fishing state transitions. |
| Migrated Gameplay | `IItemTooltipApi` | Experimental | Fish roe tooltip path has smoke evidence, but the migrated player-facing mod currently limits visible usage. |
| Migrated Gameplay | `IAnimalViewerApi` | Experimental | Animal viewer progress rendering has real smoke/manual visual evidence, but UI flicker and single-pass rendering are historically high-risk. |
| Diagnostic APIs | `IDebugConsoleApi`, `IInventoryDebugApi`, `IWeatherDebugApi`, `ITeleportDebugApi`, `IInstantSaveDebugApi`, `ITimeDebugApi`, `IMovementDebugApi`, `IAdvancedDebugApi` | Diagnostic | These are Y-console/debug surfaces. Some call real native methods, but they are for diagnostics and controlled smoke/manual testing, not stable ordinary gameplay APIs. |
| Native Utility | `IMailDeliveryApi` | Experimental | Uses native item mail delivery with source gating and duplicate guards. Current game ignores custom mail title/content/sender, so it remains template-based and experimental. |
| Vehicle | `IMotorVehicleApi` | Experimental | SecondMotor evidence covers key, summon, ride, edge transition, appearance isolation, disabled-source checks, and clean exit. Still experimental because native motor state is singleton-oriented and routing/restoration is fragile. |
| Machine | `IMachineProductionApi` | Experimental | MineMod evidence covers official JSON, tech path, hybrid fuel/electric production, and storage telemetry. Still experimental because it spans recipe, power, storage, renderer, production, and pass-time catch-up owners. |
| Equipment | `IEquipmentSlotsApi` | Experimental | MoreEquipmentSlots evidence covers interactive reflected UI clones, native save transaction sidecar persistence, equip/recover, and recovery checks. Still experimental because it reaches native equipment UI/save paths through fragile adapters. |
| Saves | `ISaveSlotsApi` | Experimental | MoreSaves verifies official save UI expansion to 12 slots. Still experimental because it modifies official save UI count and needs broader slot lifecycle/delete/copy regression coverage. |
| Inventory | `IChestLocatorEnhancerApi` | Experimental | Evidence proves shared inventory lookup can feed native `CountItem/CostItem` in a controlled path. Broader room/building/location policy remains unsettled. |
| Farming | `IStrongPlantingGunApi` | Experimental | Evidence proves seed/film/fertilizer slot expansion in the official farming gun path. Still fragile because it owns `ItemFarmingGun` construction/use/UI transfer hooks. |
| Camera | `ICameraZoomApi` 0.4.2 | Failed / ObsoleteCompatibility | Manual QA has superseded the 0.4.2 smoke claim. Do not cite `GAME-SMOKE/20260607-072228`, the `ZOOM-042` static screenshot set, or "background no longer appears as a small framed rectangle" as completion evidence. Those files are historical/supporting artifacts only. The API is obsolete and redirects compatibility callers to lease-based `ICameraViewApi`; old CameraZoom evidence is not CameraView completion proof. |
| Camera | `ICameraViewApi`, `ICameraViewLease` | Experimental | Lease-based playable camera view contract. DTMAPI arbitrates multiple owner leases by priority and latest update, writes only the playable camera `orthographicSize`, leaves native camera follow/range semantics intact, and does not call `CameraController.RefreshResolution`, `CameraController.SetPosition`, or `DolocAPI.RefreshScanner`. Latest feature-status model smoke evidence is `GAME-SMOKE/20260609-141609`, which verifies `Feature.Camera = ready` with internal `lastOperation/success/failureCount/lastError` details plus HookProbe and Zoom smoke; post-merge `Refactor` evidence remains `GAME-SMOKE/20260609-134412`; CameraPlayable case-file validation remains `GAME-SMOKE/20260609-110528`; earlier lease rebuild evidence remains `GAME-SMOKE/20260608-150914`. Sustained manual play remains follow-up before any promotion beyond `Experimental`. |
| Camera | `IPanoramaCameraApi` | Proposed | Interface draft only. Panorama/background/fog/range compensation is deliberately not mixed into ordinary playable zoom. |
| Custom Entities | Definition/registry portions of `ICustomAnimalApi`, `ICustomMonsterApi`, `ICustomAttackApi`, `ICustomDroneApi` | StableCandidate | DTMAPI Core registry contracts cover namespaced definitions, validation, duplicate checks, owner cleanup, snapshots/status, lifecycle listener isolation, and blocked request DTOs without raw game types. Promotion to `Stable` requires two real mods using the definitions and regression rows; current proof is unit/internal smoke, not real mod adoption. |
| Custom Entities | Native runtime creation verbs: animal/monster `RequestSpawn`, attack `SpawnProjectile`/`ExecuteAttack`, drone `RequestSummon`/`Equip`/`SetMode`, native handles, active runtime snapshots, save restoration | Experimental | Blocked. Current behavior intentionally returns `runtime-creation-blocked` / `RuntimeCreationBlocked`; no native animal, monster, projectile, attack, or drone is created. Ordinary mods must not depend on these runtime verbs until family-specific native adapters are verified in game. |
| Custom Entities | Native animal/monster/attack/drone adapters | Proposed | Future work must start from native owners such as animal proto/room/food/produce/save, monster AI/spawn/drop, bullet factory/collision/damage, and drone controller/weapon/equipment/persistence. |

## Failed / Historical Evidence Rules

- CameraZoom 0.3.0 evidence proves only orthographic-size writes and restore. It is historical, not completion proof.
- CameraZoom 0.4.2 evidence is now marked `Failed / ObsoleteCompatibility` after manual QA. Its static screenshots and smoke logs may remain useful for comparison, but they must not be cited as `ICameraViewApi` completion evidence.
- CustomEntity 0.4.0 smoke proves registry and blocked runtime results only. It does not prove native entity creation.
- HookProbe/TestMod evidence can prove event delivery or internal status paths, but it cannot by itself promote a public gameplay API to `Stable`.

## Current Summary

- Stable: DTMAPI-owned framework contracts with low native-state risk.
- StableCandidate: strong DTMAPI-owned or contract-level APIs that still need adoption/manual/regression gates.
- Experimental: most GameBridge-facing gameplay/content/UI/native utility APIs.
- Diagnostic: Y-console, debug, evidence, and hook-status helper APIs.
- Proposed: content pipeline, Panorama camera API draft, and custom entity native adapters.
- Failed / ObsoleteCompatibility: CameraZoom 0.4.2 as a completed visual/camera API claim.
