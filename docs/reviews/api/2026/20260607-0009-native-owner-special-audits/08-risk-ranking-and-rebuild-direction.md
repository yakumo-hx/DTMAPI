# 08 - Risk Ranking And Rebuild Direction

This file is the Task H summary for the seven 0009 domains. It does not create an implementation goal.

## Risk Ranking

| Rank | Domain | Risk | Why it ranks here |
| --- | --- | --- | --- |
| 1 | `IEquipmentSlotsApi` | P0 Gap | Native stats are reached, but storage/UI/transaction ownership is DTMAPI sidecar. Ordinary mods risk cross-save pollution, unsaved mutation loss, cloned UI drift, and partial recovery. |
| 2 | `IMotorVehicleApi` second motor | P0 Gap | DTMAPI simulates a second vehicle by cloning `DolocAPI.Motor` and routing `AgentControllerState.motorController`; native owner is singleton-shaped. |
| 3 | `ITimeDebugApi` | P1 Watch | Reaches native pass-time, but exposes a global debug jump without a transaction model for machines, crops, animals, NPC schedules, UI, and save state. |
| 4 | `ISaveSlotsApi` | P1 Watch | Reaches official archive count, but one global count has no per-mod/per-save namespace or conflict policy. |
| 5 | `IInstantSaveDebugApi` | P1 Watch | Save-only path is native-backed, but reload is deliberately blocked due scene residue. Must stay debug-only. |
| 6 | `IInventoryDebugApi.GiveItem` | P2 Watch | Native backpack placement is real, but it is a debug injection path that bypasses economy/reward/drop semantics. |
| 7 | `ITeleportDebugApi` | P2 Watch | Whitelisted native transport is adequate for debug; ordinary scripted teleport needs completion and room/dungeon layers. |
| 8 | `IWorkshopHelper` / `IContentQueryHelper` | P2 Watch | Read-only metadata is safe, but DTO names can be overread as runtime-load proof unless docs stress official owner boundaries. |
| 9 | Save lifecycle events | P3 OK/Watch | Native hook coverage is credible; remaining risk is event-order documentation around DTMAPI sidecar cleanup/flush. |

## Minimum Rebuild Slices

| Domain | Minimum stable slice | Experimental slice to isolate |
| --- | --- | --- |
| EquipmentSlots | Attribute-function adapter over `AgentEquipmentManager` with item validation and explicit apply/remove result. | Extra slot sidecar storage, cloned `AccessoriesBar`, recovery, save transaction, per-save namespace. |
| Vehicle/Motor | Original motor state/events and restricted native summon wrapper. | Second motor clone registry, key routing, controller swap, room transition sync, global tuning patch. |
| Time | Debug time-skip remains separate. Stable future slice should be a queued scheduler with pre/post native time transaction events. | Fast-forward arbitrary time, machine/crop/NPC catch-up, sleep/Y-console parity. |
| SaveSlots | Official archive count request/observe with conflict policy. | Any claim of per-mod save slots, profile-specific expansion, or save-file namespace ownership. |
| InstantSave | Save request and save-completed event only. | Save-then-reload, scene cleanup, mid-scene checkpointing. |
| Inventory/GiveItem | Dedicated reward/mail/drop APIs per native owner. | Debug give and source-only content injection. |
| Teleport | Mark-point transport with accepted/completed events. | Direct room-position/dungeon teleport and vehicle-follow transitions. |
| Workshop/Content | Read-only discovery and source metadata. | Enable/disable, load-order mutation, runtime reload, managed Workshop code hotload. |

## Next Review/Implementation Entry Points

- EquipmentSlots should be first if the next goal is developer-facing API stability. Start with per-save storage identity and native stat adapter boundaries.
- Vehicle should be first if user-visible clone residue or map transition issues recur. Start with deciding whether DTMAPI owns clone vehicles permanently or blocks public registry wording.
- Time should be first if MachineProduction remains unstable around sleep/Y-console time jumps. Start with a time transaction bus instead of direct public skipping.
- Workshop/Content should be first if developer docs are next. Start with terminology: `Indexed` means source metadata; `RuntimeLoaded` or native query means game table availability.

## Non-Goals

- No runtime/API/mod/game/Workshop files were changed.
- No game smoke was run.
- No repair goal was created from this review.
