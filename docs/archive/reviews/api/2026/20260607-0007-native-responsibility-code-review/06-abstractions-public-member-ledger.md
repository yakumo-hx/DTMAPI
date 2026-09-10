# 06 Abstractions public member ledger

Date: 2026-06-07
Status: member-level inventory and coverage-class ledger reviewed

This volume records the current public-member inventory for `src/DTMAPI.Abstractions` and assigns every file-level public surface to a review coverage class. It is intentionally not copied from 0006 conclusions. The counts below come from a fresh source scan; the classifications come from the manual implementation review in volumes 01-05.

## Inventory Evidence

Command evidence from the current worktree:

- Public source files scanned: `src/DTMAPI.Abstractions/*.cs`
- Public type/interface/enum/struct declarations: `234`
- Public property lines: `1013`
- Public concrete/abstract method body/signature lines outside interface declarations: `6`
- `public-api-matrix` rows mapped in volume 04: `82`

Per-file public line distribution:

| File | Public lines | Review class |
| --- | ---: | --- |
| `ApiStatus.cs` | 6 | MatrixGap / DTMAPI metadata |
| `ConfigMenu.cs` | 9 | Matrix partial / DTMAPI config UI |
| `CustomEntities.cs` | 505 | Matrix partial / high semantic risk |
| `DtmMod.cs` | 5 | MatrixGap / DTMAPI mod entry |
| `Events.cs` | 47 | Matrix partial / lifecycle event DTOs |
| `ExperimentalGameBridge.cs` | 679 | Matrix partial / high semantic risk |
| `Helpers.cs` | 14 | Matrix partial / framework helper and DTO interfaces |
| `Logging.cs` | 7 | Matrix partial / DTMAPI logging |
| `Manifest.cs` | 14 | MatrixGap / DTMAPI manifest model |

## Top Risks

1. `CustomEntities.cs` and `ExperimentalGameBridge.cs` together account for most public properties. Their DTO fields are where authors are most likely to mistake DTMAPI status/telemetry for Doloc native runtime truth.
2. The matrix lists four custom entity API families, but not the many public policy/result/snapshot/provider fields whose names imply spawn, summon, execute, save, tick, AI, loot, damage, or equipment runtime.
3. Migrated bridge DTOs such as Machine, EquipmentSlots, MotorVehicle, CameraZoom, and AdvancedDebug mix native slices with DTMAPI sidecar or debug state; field names are easy to overread.
4. Framework MatrixGaps (`DtmMod`, `IManifest`, `IDtmHelper`, status metadata) are safe but still public and should be represented in the matrix so future audits do not rediscover them as surprises.
5. EventArgs and UI/config DTOs are mostly DTMAPI-owned, but fields like `SaveSlot`, `MenuId`, `HookStatus`, `IsVisible`, and `CanEdit` need documentation that names their DTMAPI scope.

## Native Owner Map

| Public member class | Native owner | DTMAPI owner | Verdict |
| --- | --- | --- | --- |
| Manifest/mod base/status/logging/config/translation helpers | None expected | DTMAPI Core services and metadata | OK / MatrixGap where absent |
| Events and EventArgs | BepInEx update plus selected Harmony save/title/workshop hooks | `EventManager` | Watch by event source |
| UI/config menu DTOs | None for native options; bootstrap reflected UI hosts DTMAPI pages | ConfigMenuRegistry / DTMAPI overlay | OK/Watch |
| Content/workshop DTO interfaces | Official/Steam state is observed, not controlled | Manifest/content scanners | Watch; read-only |
| Debug/Y-console DTOs | Mixed native debug commands and DTMAPI UI state | GameBridge + debug console host | debug-only |
| Migrated gameplay DTOs | Narrow native hook slices | DTMAPI policy dictionaries / sidecar state | Watch/Gap |
| Custom entity DTOs/providers | Intended native owners not connected for runtime creation | `CustomEntityRegistryService` | Blocked for runtime fields |

## Ordinary Mod Usability Table

| Member group | Ordinary mod usability | Why |
| --- | --- | --- |
| `ApiStatus`, `DtmApiStatusAttribute` | 普通 mod 可用 as metadata | No runtime/native promise. |
| `DtmMod`, `IManifest`, `IManifestDependency`, `IDtmHelper` | 普通 mod 可用 | DTMAPI-owned mod entry/context contracts. |
| Logging/config/translation helpers | 普通 mod 可用 | File/service-backed DTMAPI infrastructure. |
| Workshop/content helper DTOs | 普通 mod 可用 read-only | They observe source metadata; they do not toggle official/Steam or prove native load. |
| UI/config menu/event DTOs | 普通 mod 可用 with caveats | DTMAPI UI/lifecycle scope, not arbitrary native menu or guaranteed save id. |
| Debug/Y-console DTOs | debug-only | They wrap native/debug mutations and DTMAPI UI state. |
| Migrated gameplay DTOs | 仅 DTMAPI 自家 mod 可用 unless explicitly display-only | They expose narrow hooks and DTMAPI policy/state. |
| Custom entity runtime DTOs/providers | 禁止依赖 for runtime behavior | Runtime adapters return blocked or DTMAPI-only status. |

## Coverage Classes

| Coverage class | Applies to | Matrix status | Required docs action |
| --- | --- | --- | --- |
| `FrameworkOk` | `ApiStatus.cs`, `DtmMod.cs`, `Manifest.cs`, `Logging.cs`, most of `Helpers.cs` | Many MatrixGap | Add matrix rows; no native owner needed. |
| `FrameworkWatch` | Event args, UI/config menu item/page state, diagnostics hook status | Matrix partial | Document DTMAPI scope and hook-source caveats. |
| `ReadOnlySourceWatch` | Workshop/content DTO fields | Matrix partial | Document read-only status and native availability checks. |
| `DisplayOnlyWatch` | Fish roe tooltip and animal viewer display DTOs | Matrix partial | Document no native item/animal data creation. |
| `DebugOnly` | Inventory/mail/weather/teleport/save/time/movement/advanced debug DTOs | Matrix partial | Keep out of ordinary mod docs. |
| `MigratedBridgeGap` | Machine, equipment slots, motor vehicle, camera zoom, chest locator, strong planting gun DTOs | Matrix partial | Split stable contracts from DTMAPI sidecar/native-slice runtime. |
| `CustomEntityRuntimeBlocked` | Custom animal/monster/attack/drone runtime fields and providers | Matrix partial | Split stable definition contracts from blocked runtime adapters. |

## File-Level Member Ledger

### `ApiStatus.cs`

- Public members: `DtmApiStatus`, `DtmApiStatusAttribute.Status`, `Since`, `Notes`.
- Matrix coverage: MatrixGap.
- Result: OK / MatrixGap.
- Native owner: none; .NET metadata only.
- Ordinary mod usability: 普通 mod 可用 as metadata.
- Risk: A status attribute can be misread as runtime enforcement. It is only annotation.
- Follow-up: Add a matrix/docs note that status annotations are informational.

### `DtmMod.cs`

- Public members: `DtmMod.Manifest`, `DtmMod.Monitor`, `AttachContext(...)`, `Entry(...)`.
- Matrix coverage: MatrixGap.
- Result: OK / MatrixGap.
- Native owner: none; DTMAPI Core attaches context and calls `Entry`.
- Ordinary mod usability: 普通 mod 可用.
- Risk: None native. The only risk is documentation invisibility: this is the primary mod entry contract but absent from the matrix.
- Follow-up: Add `DtmMod` and `Entry` to the public matrix/developer docs.

### `Manifest.cs`

- Public members: `IManifest` properties and `IManifestDependency` properties.
- Matrix coverage: MatrixGap.
- Result: OK / MatrixGap.
- Native owner: none; DTMAPI manifest reader and dependency checker own the model.
- Ordinary mod usability: 普通 mod 可用.
- Risk: `MinimumGameVersion` can look like official game compatibility enforcement; current Core enforces DTMAPI dependency/load checks, not a native game contract.
- Follow-up: Add manifest contracts to matrix.

### `Logging.cs`

- Public members: `LogLevel`, `IMonitor.Log`, `LogOnce`, `LogException`, `NullMonitor.Instance` and no-op methods.
- Matrix coverage: `IMonitor` methods listed; `LogLevel` and `NullMonitor` MatrixGap.
- Result: OK.
- Native owner: none; `FileMonitor` writes DTMAPI logs and mirrors warnings/errors to host logger.
- Ordinary mod usability: 普通 mod 可用.
- Risk: none for native responsibility.
- Follow-up: Add `LogLevel`/`NullMonitor` to reverse coverage docs or matrix if strict symbol completeness is required.

### `Helpers.cs`

- Public members: `IDtmHelper`, `ITranslationHelper`, `IConfigHelper`, `IModRegistry`, `IWorkshopHelper`, `IWorkshopModInfo`, `IUiHelper`, `IDiagnosticsHelper`, `IDtmErrorInfo`, `IHookStatusInfo`, `IContentQueryHelper`, `IContentAssetInfo`, `IContentItemInfo`, `IInputHelper`.
- Matrix coverage: mixed. Config/mod registry/workshop/UI/diagnostics/content/input method rows are listed; helper shells and DTO fields are mostly MatrixGap.
- Result: OK/Watch, except `IInputHelper.Suppress` = Gap.
- Native owner: none for helpers except lifecycle/input observations; Workshop/content are read-only source metadata.
- Ordinary mod usability: 普通 mod 可用 for framework/read-only helpers; `Suppress` 禁止依赖 as native action suppression.
- Risk: `IContentItemInfo.Enabled` and `IWorkshopModInfo.IsEnabledByOfficialPath` can be mistaken for runtime control; `IHookStatusInfo.Status` can be mistaken for stable API guarantee; `Suppress` is DTMAPI-only state.
- Follow-up: Add matrix rows for helper shells/DTO interfaces and document read-only/native-availability boundaries.

### `Events.cs`

- Public members: event helper interfaces, event properties, and EventArgs classes/properties.
- Matrix coverage: core event rows listed; many args and UI/diagnostics event rows are MatrixGap.
- Result: Watch.
- Native owner: BepInEx update for update events; Harmony `LoadGame`/`SaveGame`/`ReturnHome`/`ModManager.ReloadMods` for save/title/workshop events; DTMAPI UI/event manager for UI/diagnostics.
- Ordinary mod usability: 普通 mod 可用 with lifecycle caveats.
- Risk: `SaveSlot` is nullable and hook-source dependent; `MenuId` is DTMAPI menu id; `HookStatusChanged` is evidence status, not native API guarantee.
- Follow-up: Add EventArgs field docs and event source table.

### `ConfigMenu.cs`

- Public members: config API methods, page/item/preview interfaces, `DtmColorPreset`.
- Matrix coverage: main config rows listed; many page/item members MatrixGap.
- Result: OK/Watch.
- Native owner: none; DTMAPI config UI/title overlay owns state and callbacks.
- Ordinary mod usability: 普通 mod 可用.
- Risk: `IsVisible`, `CanEdit`, `PendingValue`, `ValidationError`, and preview scope are DTMAPI config UI semantics, not native options menu behavior.
- Follow-up: Add matrix rows for page/item support properties if full symbol coverage is required.

### `ExperimentalGameBridge.cs`

- Public members: 22 experimental bridge interfaces plus debug/gameplay/result/state/options DTOs.
- Matrix coverage: API families listed; most DTO fields MatrixGap.
- Result: mixed:
  - Debug inventory/mail/weather/teleport/save/time/movement/advanced DTOs: debug-only.
  - Action/fishing/speed DTOs: Watch, DTMAPI-owned migrated mod policy.
  - Fish roe and animal viewer DTOs: Watch, display-only.
  - Machine/equipment/motor/camera DTOs: Gap/Watch due native-slice plus DTMAPI sidecar state.
  - SaveSlots DTOs: Watch because native archive count is reached but global.
  - ChestLocator/StrongPlantingGun DTOs: Watch, narrow DTMAPI mod bridges.
- Native owner: method-specific and reviewed in volumes 01-02.
- Ordinary mod usability: mostly debug-only or DTMAPI-owned mod only, except display-only and read-only snapshots with caveats.
- Risk: DTO fields often sound authoritative even when they are DTMAPI policy/telemetry. The highest-risk names are `Success`, `FailureReason`, `Status`, `HookInstalled`, `CurrentRoomId`, `IsRiding`, `ProductionCycleCount`, `RemainingFuel`, `SlotId`, `IsApplied`, `CurrentViewScale`, `CameraOrthographicSize`, and action-count telemetry.
- Follow-up: Add developer docs warning that DTO state is not automatically native state.

### `CustomEntities.cs`

- Public members: custom entity enums, shared handles/positions/assets/localized text, validation/capability/snapshot/result classes, persistence/tick/behavior/provider interfaces, four API interfaces, and animal/monster/attack/drone definition/request/result/snapshot families.
- Matrix coverage: only `ICustomAnimalApi`, `ICustomMonsterApi`, `ICustomAttackApi`, and `ICustomDroneApi` family rows are listed; most public members are MatrixGap.
- Result: Watch for definition/registry metadata; Blocked/Gap for runtime-implying fields and verbs.
- Native owner: Core registry only for definitions/status. Intended native runtime owners are not connected for animal creation, monster spawning, projectile/attack execution, or drone summon/equipment/mode.
- Ordinary mod usability: 普通 mod 可用 for definition registry metadata; 禁止依赖 for spawn/summon/execute/equip/runtime/save behavior.
- Risk: This is the largest semantic-risk surface. Fields and types with `Spawn`, `Summon`, `Execute`, `Runtime`, `Snapshot`, `Persistence`, `Save`, `Tick`, `Behavior`, `Attack`, `Loot`, `Movement`, `Energy`, `Repair`, `Equip`, and `Produce` names can imply native runtime support that currently returns blocked or DTMAPI-only status.
- Follow-up: Split docs into stable contract fields and blocked runtime-adapter fields; add field-level warnings for every runtime-implying class.

## High-Risk Public Field Symbols Requiring Explicit Docs

These fields are already covered by volumes 01-05, but they should be explicitly called out in developer docs before ordinary authors consume the API:

| Symbol group | Why high risk | Current result |
| --- | --- | --- |
| `CustomEntityRequestResult.Succeeded`, `FailureReason`, `RuntimeStatus`, `Handle` | Runtime verbs currently return `runtime-creation-blocked`; `Handle` is not a native object handle. | Blocked/Gap |
| `CustomEntityFamilySnapshot.ActiveRuntimeInstanceCount`, `SaveStateRecordCount`, `RuntimeHandles` | Snapshot is DTMAPI registry/status; save state count is currently zero. | Watch/Gap |
| `CustomAnimalSpeciesDefinition.Habitat`, `Diet`, `Consumption`, `Excrement`, `Breeding`, `HiddenProducts`, `ProduceRules`, `Persistence`, `TickPolicy` | Names imply native animal lifecycle support; spawn adapter is blocked. | Blocked for runtime |
| `CustomMonsterDefinition.SpawnRules`, `AttackSlots`, `Loot`, `Persistence`, `TickPolicy` | Names imply native monster spawn/AI/loot support; spawn adapter is blocked. | Blocked for runtime |
| `CustomAttackDefinition.Damage`, `Hitbox`, `Trajectory`, `Pattern`, `PierceCount`, `BounceCount`, `HomingStrength` | Names imply native projectile/collision/damage support; execution adapter is blocked. | Blocked for runtime |
| `CustomDroneDefinition.OwnerBinding`, `EquipmentSlots`, `AttackIds`, `Energy`, `Movement`, `Repair`, `Summon` | Names imply native companion runtime support; summon/equip/mode adapters are blocked. | Blocked for runtime |
| `MachineProductionState.ProductionCycleCount`, `LastOutputItemId`, `RemainingFuel`, `Status` | DTMAPI runtime loop telemetry can diverge from native equipment lifecycle. | Gap |
| `EquipmentSlotsState.StoredItemCount`, `AppliedItemCount`, `RuntimeUiHookInstalled`, `RuntimeStatsHookInstalled`; `EquipmentSlotInfo.IsApplied` | DTMAPI sidecar slot/UI/stat state can look like native slots. | Gap |
| `MotorVehicleState.IsRiding`, `IsSummoned`, `RoomId`, `SpeedMultiplier`, `SourceItemId` | Original motor is native singleton; second motor is DTMAPI clone/routing. | Watch/Gap |
| `CameraZoomState.CurrentViewScale`, `CameraOrthographicSize` | Only orthographic size is covered; background/fog/parallax/room bounds are not. | Gap |
| `InventoryGiveResult.Success`, `BeforeCount`, `AfterCount`, `GivenCount` | Real native backpack mutation, but debug economy grant. | debug-only |
| `MailItemDeliveryResult.Sent`, `Skipped`, `PendingMailCount`, `SourceEnabled` | Real native mail mutation, but debug/template-based and progression-sensitive. | debug-only |
| `TeleportResult.AfterRequest`, `DestinationId`, `MarkPointId`; `TeleportDestination.X/Y` | Native transport only through whitelist; not arbitrary location API. | debug-only |
| `InstantSaveDebugResult.ReloadAfterSave`, `FailureReason` | Reload path intentionally disabled due scene residue. | debug-only |
| `CreativeModeState.RuntimeHooksInstalled`, `GeneratorRuntimeAvailable`; `CreativeModeResult.Success` | Global debug flags/hooks can leak if used outside debug UI. | debug-only |

## Coverage Decision

The current 0007 review now proves:

- All 82 matrix rows map to a 0007 review target.
- Every Abstractions source file has been scanned and assigned a coverage class.
- High-risk DTO/Result/EventArgs field families have concrete native-owner and ordinary-mod usability conclusions.
- Safe framework MatrixGaps are identified as public docs/matrix omissions rather than runtime bugs.

The current 0007 review intentionally does not include a literal 1013-row table for every public property line. Volume 07 records the final audit decision: the active goal is satisfied by manual method-body/native-owner review of important APIs, 82/82 matrix coverage, all-file/all-type-family Abstractions reverse coverage, and explicit high-risk DTO field review. A generated 1013-row low-risk scalar property table would not add native-owner evidence and would conflict with the goal's warning against template-block coverage.

## Refactor Backlog

| Priority | Item | Reason |
| --- | --- | --- |
| P0 | Add developer-doc warnings for high-risk fields listed above | These fields can cause ordinary mod authors to assume native runtime support. |
| P0 | Split custom entity docs into stable contract and blocked runtime adapter sections | Largest semantic-risk surface. |
| P1 | Add public matrix entries for safe framework MatrixGaps | Prevents future reverse-coverage surprises. |
| P1 | Add per-DTO "state source" docs for migrated bridge DTOs | Separates native owner, DTMAPI policy, and debug telemetry. |
| P2 | Consider machine-readable API status metadata export | Helps keep matrix, docs, and attributes aligned without relying on manual tables. |

## Unknowns

| Unknown | Searched evidence | Next step |
| --- | --- | --- |
| Whether every low-risk DTO scalar needs individual docs | The current source scan found 1013 public property lines; volumes 01-06 classify all by file/type/risk group. | Decide whether to add a literal property inventory artifact or accept grouped coverage for low-risk data-only fields. |
| Exact future native owner for custom animal/monster/attack/drone runtime fields | Core registry and GameBridge status show adapters blocked; hook-map lists candidate owners only at family level. | Future implementation review should map reverse build native owners before any runtime goal. |
| Whether `DtmApiStatusAttribute` should drive matrix generation | Attribute exists but current matrix is hand-maintained. | Consider a docs-only generator/check in a separate requested goal. |
