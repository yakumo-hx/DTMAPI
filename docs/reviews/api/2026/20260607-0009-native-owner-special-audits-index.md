# 20260607-0009 - DTMAPI Native Owner Special Audits, Round 2

Status: complete
Scope: docs-only code-level audit for Save, Time, Teleport, Inventory/GiveItem, EquipmentSlots, Vehicle/Motor, and Workshop/Content index APIs.
Rule: this round only reviews and records. It does not modify runtime, public API, mods, game files, Workshop files, or official/decompiled source, and it does not create an implementation goal.

## Purpose

This round continues the 0008 mode: read public interfaces, implementation bodies, callers, callees, GameBridge/Harmony/reflection paths, existing debug/hook/update evidence, and reverse/research native-owner candidates. It is not a coverage inventory and intentionally avoids a full public-symbol list.

## Volumes

| Volume | Area | Verdict |
| --- | --- | --- |
| 01 | [Save APIs](20260607-0009-native-owner-special-audits/01-save-apis.md) | `Partial/Watch`: save lifecycle events and save-only instant save reach native save/load hooks; MoreSaves reaches official save UI count, but global slot-count and reload-disabled boundaries remain debug/DTMAPI-only risks. |
| 02 | [Time API](20260607-0009-native-owner-special-audits/02-time-api.md) | `Partial/Watch`: `ITimeDebugApi` reaches `ArchiveDataHandle.PassTimeNoControl` and `DolocAPI.OnWakeUp`, but exposes only a debug weather-period skip, not a general ordinary-mod scheduler. |
| 03 | [Teleport API](20260607-0009-native-owner-special-audits/03-teleport-api.md) | `OK/Watch`: whitelisted debug destinations reach native `DolocAPI.DoTransport`; arbitrary room/coordinate teleport and dungeon/fallback routing are not public contract. |
| 04 | [Inventory/GiveItem API](20260607-0009-native-owner-special-audits/04-inventory-giveitem-api.md) | `OK/Watch`: `GiveItem` reaches native item table and backpack placement for runtime-loaded enabled items; still debug-only and not a stable reward/drop/mail contract. |
| 05 | [EquipmentSlots API](20260607-0009-native-owner-special-audits/05-equipment-slots-api.md) | `Gap`: native stats and UI touchpoints are reached, but DTMAPI owns sidecar storage, cloned UI, recovery, and save transaction semantics. |
| 06 | [Vehicle/Motor API](20260607-0009-native-owner-special-audits/06-vehicle-motor-api.md) | `Gap`: original motor calls reach native owners; second motor is a DTMAPI clone/routing layer around a singleton native motor model. |
| 07 | [Workshop/Content index APIs](20260607-0009-native-owner-special-audits/07-workshop-content-index-apis.md) | `OK/Watch`: helpers are read-only discovery/index APIs; official enablement, load order, Steam subscription, and runtime table merge remain official `ModManager`/`DolocConfig` owners. |
| 08 | [Risk Ranking And Rebuild Direction](20260607-0009-native-owner-special-audits/08-risk-ranking-and-rebuild-direction.md) | Cross-domain risk order and minimum native-owner rebuild slices. |

## Final Decision Table

| API / domain | Result | Native owner | Ordinary mod usability | Recommendation |
| --- | --- | --- | --- | --- |
| `ISaveEvents.SaveLoaded/SaveSaving/SaveSaved` | OK/Watch | `DolocAPI.LoadGame`, `DolocAPI.AfterLoadArchiveData` or `OnAfterLoadArchiveData`, `DolocAPI.SaveGame` / `DataPersistenceManager.SaveGame` hooks. | 普通 mod 可用 with caution | Keep as lifecycle events, but document that event ordering follows native save hooks and DTMAPI sidecars such as EquipmentSlots flush on `SaveGame` postfix. |
| `IInstantSaveDebugApi.Save` | Watch | `DolocAPI.SaveGame(int)` only; reload path intentionally blocked. | debug-only | Keep debug-only. Ordinary mods relying on it for gameplay save/load flows risk scene residue, stale objects, and user-unexpected persistence because `reloadAfterSave` returns `reload-disabled`. |
| `ISaveSlotsApi.RegisterSlots` | Watch | `DolocAPI.gameManager.archiveFileCount` feeds official `LocalSave` and `GameDataPanel`. | 仅 DTMAPI 自家 mod 可用 / ordinary caution | Do not present as a multi-profile save system. Ordinary mods can collide globally on slot count, assume per-save ownership that does not exist, or exceed UI/load/delete/copy behavior not tested beyond the official count path. |
| `ITimeDebugApi.SkipToNextWeatherPeriod` | Watch | `ArchiveDataHandle.PassTimeNoControl` plus `DolocAPI.OnWakeUp`. | debug-only | Keep debug-only. Ordinary mods need a queued scheduler/catch-up contract; direct dependence can desync machines/crops/NPC/weather-facing systems and surprise players with global time jumps. |
| `ITeleportDebugApi.GetDestinations/Teleport` | OK/Watch | Destination list from `GameInitConfig.initMarkPoint`, `TbStation`, `TbMarkPoint`; execution through `DolocAPI.DoTransport`. | debug-only | Keep whitelist and debug label. Ordinary mods need explicit room-transition API layers; current API can fail outside whitelisted marks and does not promise dungeon/direct-coordinate routing. |
| `IInventoryDebugApi.GetItems/GiveItem` | OK/Watch | `TbItem`, `QueryItemProto`, `CanPlaceItem`, `TryPlaceInBackpack`, `CountItem`; content source index is DTMAPI read-only metadata. | debug-only | Keep as Y-console/debug injection. Ordinary mods should use dedicated reward/mail/drop APIs, because this bypasses economy/progression semantics and rejects source-only or disabled content. |
| `IEquipmentSlotsApi.*` | Gap | Partial: `AgentEquipmentManager.ReloadParams`, `AgentEquipmentFunction.CreateAgentEquipmentFunction`, native backpack/mail overflow, `AccessoriesBar` UI clone. DTMAPI owns sidecar storage and extra slot UI. | 仅 DTMAPI 自家 mod 可用 | Split stable attribute-function adapter from experimental extra-slot storage/UI. Ordinary mods risk sidecar loss on unsaved exit, cross-save/global owner pollution, UI clone drift, and stats not applying until native manager exists. |
| `IMotorVehicleApi` original motor calls | Watch | `DolocAPI.UnlockMotor`, `DolocAPI.SetMotorPosition`, `MotorController.AutoFlyTo`, native room policy. | debug-only / restricted | Keep original motor state/summon as restricted debug or DTMAPI-owned feature. Ordinary mods can conflict with user position, room bans, and riding state. |
| `IMotorVehicleApi` second motor calls | Gap | DTMAPI clone of `DolocAPI.Motor`; hooks `ItemMotorKey.OnUse`, `MotorInteractable.OnInteract`, `AgentControllerState.GetOnMotor/GetOffMotor`, `MotorController.OnFixedUpdate`, `DolocAPI.EnterRoom/SetMotorPosition`. | 仅 DTMAPI 自家 mod 可用 | Do not document as stable vehicle registry. Ordinary mods risk singleton pollution, controller routing failure, room-transition mismatch, global tuning contamination, and clone cleanup residue. |
| `IWorkshopHelper`, `IWorkshopEvents.ModListChanged` | OK/Watch | Official reload hook: `ModManager.ReloadMods`; Core discovery reads official/Steam/local roots and `mod_infos.json`. | 普通 mod 可用 as read-only metadata | Keep read-only. Ordinary mods must not infer they can toggle official enablement or load managed Workshop code. |
| `IContentQueryHelper`, `IContentItemInfo` | OK/Watch | DTMAPI file/index scanner plus official `ModManager`/`DolocConfig` runtime load as external owner. | 普通 mod 可用 as source metadata | Document semantic boundary: indexed content is not proof of runtime table load. Ordinary mods should check `RuntimeLoaded`/debug give result or native query before assuming item existence. |

## Evidence Rules Applied

- Every volume cites concrete DTMAPI files, function names, and line numbers.
- Existing smoke/update/hook evidence was read; no game smoke was run.
- Reverse/decompiled material was used only as local candidate maps and research notes. This review does not copy or distribute decompiled source or official DLL content.
- When a native owner is not fully proved, the volume lists the searched candidate paths and the missing evidence explicitly.

## No Implementation Goal

This review intentionally stops at documentation. Severe findings are recorded only; no repair or rebuild goal was created.
