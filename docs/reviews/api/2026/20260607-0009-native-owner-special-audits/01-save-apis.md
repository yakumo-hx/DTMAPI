# 01 - Save APIs

Scope: `ISaveEvents`, `IInstantSaveDebugApi`, and `ISaveSlotsApi`.

## 1. Files read

- `src/DTMAPI.Abstractions/Events.cs`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/EventManager.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `testmods/MoreSavesMod/ModEntry.cs`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260606-0004-029-readme-implementation.md`
- `docs/updates/2026/20260605-0003-026-mine-yconsole-fixes.md`
- `references/doloc-town/reverse/builds/23249387_workshop_247ACD/maps/Save_Load.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md`

## 2. Functions read

- `DTMAPI.Abstractions.ISaveEvents.SaveLoaded/SaveSaving/SaveSaved`, `Events.cs:33`.
- `SaveLoadedEventArgs/SaveSavingEventArgs/SaveSavedEventArgs`, `Events.cs:86`.
- `IInstantSaveDebugApi.GetState/Save/GetStatus`, `ExperimentalGameBridge.cs:91`.
- `ISaveSlotsApi.RegisterSlots/GetState/GetStatus`, `ExperimentalGameBridge.cs:154`.
- `DtmApiRuntime.NotifyLoadGameRequested`, `DtmApiRuntime.cs:146`.
- `DtmApiRuntime.NotifySaveLoaded`, `DtmApiRuntime.cs:152`.
- `DtmApiRuntime.NotifySaveSaving/NotifySaveSaved`, `DtmApiRuntime.cs:167`.
- `EventManager.SaveProxy`, `EventManager.cs:151`.
- `DolocTownHookCallbacks.LoadGamePrefix`, `DolocTownHookCallbacks.cs:13`.
- `DolocTownHookCallbacks.AfterLoadArchiveDataPostfix`, `DolocTownHookCallbacks.cs:18`.
- `DolocTownHookCallbacks.SaveGamePrefix/SaveGamePostfix`, `DolocTownHookCallbacks.cs:27`.
- `DolocTownGameBridge.InstallHarmonyHooks`, save slice, `DolocTownGameBridge.cs:828`.
- `DolocTownGameBridge.TryAutoSave`, `DolocTownGameBridge.cs:2146`.
- `DolocTownGameBridge.TryExerciseInstantSaveForSmoke`, `DolocTownGameBridge.cs:2169`.
- `DolocTownExperimentalBridgeApi.RegisterSlots(ISaveSlotsApi)`, `DolocTownExperimentalBridgeApi.cs:1201`.
- `ApplySaveSlotExpansion`, `DolocTownExperimentalBridgeApi.cs:2238`.
- `RefreshSaveSlotExpansionForRuntime`, `DolocTownExperimentalBridgeApi.cs:2227`.
- `GetNativeArchiveSlotCount` and `TryGetNativeArchiveSlotManager`, `DolocTownExperimentalBridgeApi.cs:2576`.
- `IInstantSaveDebugApi.GetState/Save/GetStatus`, `DolocTownExperimentalBridgeApi.cs:3992`.
- `GetInstantSaveDebugState`, `DolocTownExperimentalBridgeApi.cs:11550`.
- `MoreSavesMod.BindSaveSlotsApi`, `testmods/MoreSavesMod/ModEntry.cs:38`.

## 3. Call graph

Save events:

```text
DolocAPI.LoadGame / DataPersistenceManager.LoadGame
  -> Harmony prefix DolocTownHookCallbacks.LoadGamePrefix
  -> DtmApiRuntime.NotifyLoadGameRequested

DolocAPI.OnAfterLoadArchiveData or DolocAPI.AfterLoadArchiveData
  -> DolocTownHookCallbacks.AfterLoadArchiveDataPostfix
  -> DtmApiRuntime.NotifySaveLoaded
  -> EventManager.DispatchSaveLoaded

DolocAPI.SaveGame / DataPersistenceManager.SaveGame
  -> SaveGamePrefix -> NotifySaveSaving
  -> native save
  -> SaveGamePostfix -> NotifySaveSaved
  -> EquipmentSlots sidecar flush also runs here
```

Instant save:

```text
IInstantSaveDebugApi.Save(owner, reloadAfterSave=false)
  -> GetInstantSaveDebugState reads DolocAPI.archiveHandle.archiveIndex
  -> reflection DolocAPI.SaveGame(int)
  -> GetInstantSaveDebugState after save
```

MoreSaves:

```text
MoreSavesMod.BindSaveSlotsApi
  -> ISaveSlotsApi.RegisterSlots
  -> ApplySaveSlotExpansion
  -> DolocAPI.gameManager.archiveFileCount
  -> official LocalSave / GameDataPanel paths keep owning files, render, load, delete, copy
```

## 4. Function body findings

- Save events are real native hook events, not a DTMAPI timer. `InstallHarmonyHooks` patches `LoadGame`, `SaveGame`, and either subscribes `DolocAPI.OnAfterLoadArchiveData` or patches `AfterLoadArchiveData` (`DolocTownGameBridge.cs:834`, `:844`, `:853`, `:862`).
- `AfterLoadArchiveDataPostfix` performs lifecycle cleanup before dispatching `SaveLoaded`: second-motor cleanup, equipment-slot reset, camera reset, then `NotifySaveLoaded` (`DolocTownHookCallbacks.cs:18`). Mods observing `SaveLoaded` see DTMAPI after those cleanup hooks.
- `SaveGamePostfix` dispatches `SaveSaved` and then flushes dirty EquipmentSlots sidecar state (`DolocTownHookCallbacks.cs:32`). This matters because save events are also a transaction boundary for DTMAPI-owned sidecars.
- `IInstantSaveDebugApi.Save` refuses `reloadAfterSave=true` with `reload-disabled` before touching native load state (`DolocTownExperimentalBridgeApi.cs:4005`). The implemented path is save-only through `DolocAPI.SaveGame(int)` (`:4014`, `:4022`).
- `GetInstantSaveDebugState` only marks saveable when `DolocAPI`, `archiveHandle`, `SaveGame`, and an archive index are available (`DolocTownExperimentalBridgeApi.cs:11555`). It does not validate post-save scene reload safety.
- `ISaveSlotsApi.RegisterSlots` stores per-owner options but applies the maximum requested slot count globally (`DolocTownExperimentalBridgeApi.cs:1206`, `:2307`). The native write is one field: `DolocAPI.gameManager.archiveFileCount` (`:2259`).
- `ApplySaveSlotExpansion` explicitly says the official save slot count was set and that `LocalSave`/`GameDataPanel` still own archive files/UI (`DolocTownExperimentalBridgeApi.cs:2267`).
- `MoreSavesMod` is the only intended consumer read here; it registers 12 slots by config and logs that official save UI and LocalSave own the expanded archive slots (`testmods/MoreSavesMod/ModEntry.cs:21`, `:47`).

## 5. Native owner verdict

- Save lifecycle events: `Reached`. Hooks target `DolocAPI.LoadGame`, `DolocAPI.AfterLoadArchiveData`/`OnAfterLoadArchiveData`, and `DolocAPI.SaveGame`.
- Instant save: `Partial`. It reaches native `SaveGame`, but the native reload owner is intentionally not exposed because running-save reload leaves scene residue.
- Save slots: `Partial/Watch`. It reaches `DolocAPI.gameManager.archiveFileCount`, but does not own save-file format, per-profile slots, save UI behavior, or official load/delete/copy logic.

Reverse/map evidence: `Save_Load.md` confirms `LocalSave` and `DataPersistenceManager` classes (`23465763.../maps/Save_Load.md:35`, `:39`), `DolocAPI.LoadGame/SaveGame` direct candidates (`:190`, `:193`), and Harmony candidates (`:403`, `:406`). Both scanned builds report the same rows.

## 6. Ordinary mod usability

- Save events: `普通 mod 可用` with lifecycle-order caution.
- Instant save: `debug-only`.
- Save slots: `仅 DTMAPI 自家 mod 可用` for slot expansion. Ordinary mods may read state, but should not depend on changing the count as a general save-system extension.

## 7. Concrete failure modes

- A mod calling instant save as a gameplay checkpoint can persist an in-progress scene without a paired reload; the disabled reload path exists because immediate native reload can leave scene residue.
- Two mods registering different save slot counts race through one global `archiveFileCount`; the larger value wins and neither owns per-mod slot allocation.
- A mod treating `SaveLoaded` as "all DTMAPI sidecars loaded and stable" can still observe re-registration work after the event, because MoreSaves and EquipmentSlots mods bind/register on `SaveLoaded`.
- A mod assuming expanded save slots are a new storage namespace can pollute official slot UI expectations; official `LocalSave`/`GameDataPanel` still own the archives.
- A mod writing sidecar state outside native `SaveGame` can diverge from the native save transaction; EquipmentSlots explicitly waits for `SaveGame` postfix to avoid that.

## 8. Minimal rebuild direction

- Keep `ISaveEvents` as stable lifecycle events, but document event ordering around DTMAPI cleanup and sidecar flushes.
- Keep `IInstantSaveDebugApi` in debug-only docs. A stable save API would need separate owners for "request save", "save completed", "safe reload", "scene cleanup", and "save transaction participants".
- Split MoreSaves into a narrow stable contract: "request official archive slot count" and "observe applied count"; do not promise per-mod save slots.
- Add an explicit conflict policy for multiple `ISaveSlotsApi` owners if this ever becomes ordinary-mod documented.

## 9. Evidence gaps

- I did not run game smoke in this round by goal constraint.
- I did not inspect decompiled method bodies for `LocalSave`/`GameDataPanel`; the owner conclusion uses DTMAPI code, existing smoke/update records, hook map, and reverse map candidate rows.
- Native behavior above 12 and up to the code clamp of 60 slots was not re-smoked here.
- The exact official UI failure behavior for conflicting slot-count requests is not separately evidenced; DTMAPI currently only computes one global maximum.
