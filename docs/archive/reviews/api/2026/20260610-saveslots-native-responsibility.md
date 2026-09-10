# SaveSlots Native Responsibility Review - 2026-06-10

## Scope

Public symbol/domain: `ISaveSlotsApi`, `SaveSlotsOptions`, `SaveSlotsRegisterResult`, `SaveSlotsState`, `Save.MoreSlotsApi`, and the MoreSaves official save UI smoke path.

Current matrix status: `Experimental`.

2026-07-13 manual-evidence correction: `docs/reviews/manual-qa/2026/20260713-0001-moresaves-long-term-player-baseline-review.md` records the user's long-term confirmation that slots 7-12 create/save/reload and survive restart, copy/delete work, disable/re-enable is non-destructive, and 12/16 slots work; 18 slots overflow the single-page UI. The native-owner map remains valid. The earlier `Unverified Points` section describes what the cited automated evidence had not proven, not the absence of all player evidence. Current-build formal regression conversion and arbitrary public-API semantics remain open.

Recommended status after this review: keep `Experimental`. The current implementation reaches the native global archive-count owner, but it is not a general save-system or per-mod slot namespace contract.

This review is docs-only. It does not change runtime, public API, mods, game files, Workshop files, official DLLs, or reverse/decompiled reference material.

## Files Read

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/SaveSlotsSmokeCase.cs`
- `testmods/MoreSavesMod/ModEntry.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/reviews/api/2026/20260607-0009-native-owner-special-audits/01-save-apis.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Save_Load.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/GameManager.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocAPI.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/GameData/DataPersistenceManager.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/GameData/LocalSave.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/GameDataUiState.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/UI/GameDataPanel.cs`

## Current DTMAPI Path

```text
MoreSavesMod
  -> helper.ModRegistry.GetApi<ISaveSlotsApi>("DTMAPI.GameBridge.DolocTown")
  -> SaveSlotsService.RegisterSlots(owner, options)
  -> SaveSlotsService.ApplySaveSlotExpansion(...)
  -> reflection write DolocAPI.gameManager.archiveFileCount
  -> hook/status Save.MoreSlotsApi = configured-official-archive-count
  -> official LocalSave / GameDataUiState / GameDataPanel continue to own files and UI
```

`SaveSlotsService` stores per-owner policy state, computes one global maximum requested count, and refreshes `DolocAPI.gameManager.archiveFileCount` on registration, throttled runtime refresh, and `SaveLoaded`. It does not create save files, assign per-mod slot namespaces, patch save/delete/copy, or replace the official save UI.

## Native Owner Map

| Responsibility | Native owner / state holder | Current DTMAPI relation | Review result |
| --- | --- | --- | --- |
| Base slot count | `GameManager.archiveFileCount`, default 6 in `GameManager.cs:28` | DTMAPI writes this one field through reflection and reports `Save.MoreSlotsApi`. | Reached. |
| Archive file naming | `GameManager.archiveFileNameFormat`, default `doloc-archive-{0}.data` in `GameManager.cs:31` | DTMAPI does not change the format. | Native-owned. |
| Archive file count used by filesystem operations | `LocalSave.dataFileCount` reads `DolocAPI.gameManager.archiveFileCount` in `LocalSave.cs:37` | DTMAPI indirectly affects the loop bounds by raising the global count. | Reached by count only. |
| Archive root and file paths | `LocalSave.dataDirPath` and path helpers in `LocalSave.cs:35`, `:67`, `:72`, `:77`, `:82` | DTMAPI does not own or patch paths. | Native-owned. |
| Load current slot | `DolocAPI.LoadGame -> DataPersistenceManager.LoadGame -> LocalSave.LoadGame`, seen in `DolocAPI.cs:701`, `DataPersistenceManager.cs:55`, `LocalSave.cs:175` | DTMAPI smoke loads slot 3, but `ISaveSlotsApi` itself does not own load behavior. | Native-owned; current smoke covers existing slot load only. |
| Save current slot | `DolocAPI.SaveGame -> DataPersistenceManager.SaveGame -> LocalSave.SaveGame`, seen in `DolocAPI.cs:766`, `DataPersistenceManager.cs:91`, `LocalSave.cs:230` | DTMAPI does not patch save file creation for expanded slots. | Native-owned; expanded-slot save creation unverified. |
| Delete slot | `GameDataUiState.OnDelete -> DolocAPI.DeleteGame -> DataPersistenceManager.DeleteGame -> LocalSave.DeleteGame`, seen in `GameDataUiState.cs:39`, `DolocAPI.cs:778`, `DataPersistenceManager.cs:121`, `LocalSave.cs:359` | DTMAPI does not test or patch expanded-slot delete. | Unverified for slots beyond vanilla. |
| Copy slot | `GameDataUiState` duplicate path and `LocalSave` copy logic, with target search using `dataFileCount` in `LocalSave.cs:280` | DTMAPI does not test or patch expanded-slot copy. | Unverified for slots beyond vanilla. |
| Archive discovery | `DolocAPI.GetAllArchiveInfos -> DataPersistenceManager.GetAllArchiveInfo -> LocalSave.GetAllArchiveInfo`, seen in `DolocAPI.cs:826`, `DataPersistenceManager.cs:131`, `LocalSave.cs:415` | DTMAPI count change changes returned array length. | Reached by official discovery path. |
| Save UI render | `GameDataUiState.Show` reads `DolocAPI.GetAllArchiveInfos` and calls `GameDataPanel.Render`, seen in `GameDataUiState.cs:152` and `:160` | Current smoke verifies `archiveFileCount=12`, `panelSlotCount=12`, `renderedSlots=12`. | Reached for 12-slot render only. |
| Panel capacity/navigation | `GameDataPanel.Render` calls `SetCapacity`; `SetCapacity` calls base capacity and navigation build, seen in `GameDataPanel.cs:75`, `:111` | DTMAPI does not own scroll/viewport layout. | Native-owned; 24+ UI behavior remains a manual QA risk. |
| Official enable/disable behavior | MoreSaves config disable recomputes global target, but native files remain on disk | DTMAPI does not delete extra archive files. | DTMAPI policy only; cleanup behavior unverified. |
| Restart recognition | `LocalSave.GetAllArchiveInfo` depends on current `archiveFileCount` after runtime initializes | DTMAPI re-registers/refreshes after launch/load; current evidence does not prove create/save/restart recognition for a newly created expanded slot. | Unverified. |

## Confirmed Evidence

- `GAME-SMOKE/20260610-051735`: feature split evidence; `Feature.SaveSlots=ready`, `Save.MoreSlotsApi=configured-official-archive-count`, official save UI rendered 12 slots, clean exit.
- `GAME-SMOKE/20260610-095455`: refresh-throttle evidence; runtime correction `6->12`, `SaveLoaded` force refresh `12->12`, HookProbe SaveLoaded, clean exit.
- `GAME-SMOKE/20260610-100337`: smoke-case split evidence; `Smoke.MoreSavesOfficialSaveUi=verified`, `archiveFileCount=12`, `panelSlotCount=12`, `renderedSlots=12`, clean exit.

These evidence rows prove the count request and official 12-slot UI rendering path. They do not prove every lifecycle operation on newly expanded slots.

## Unverified Points

- New slot creation through official UI for slot 7+.
- Saving to a newly created expanded slot and then cleanly reloading it.
- Deleting an expanded slot and confirming file/state cleanup.
- Copying into an expanded slot and confirming `archiveIndex`/metadata repair.
- Disabling MoreSaves after creating extra-slot files and confirming vanilla UI behavior.
- Restart recognition after creating or copying an expanded slot.
- UI behavior above 12 slots, especially 24+ slots where previous manual QA found overflow.
- Multi-owner conflict behavior beyond the current max-count aggregation.

## Ordinary-Mod Usability

`ISaveSlotsApi` is usable for the bundled MoreSaves-style feature with caution: a mod can request the official archive-count increase and observe the applied/native count. It is not safe to document as a stable ordinary-mod save-system API yet.

Ordinary mods must not assume:

- per-mod or per-profile save slot ownership;
- a dedicated file namespace for their data;
- safe arbitrary slot counts up to the current clamp of 60;
- that delete/copy/restart lifecycle has been proven for every expanded slot;
- that disabling the feature removes or hides extra-slot files without user-visible edge cases.

## Recommended Matrix

Keep `ISaveSlotsApi` `Experimental`.

Allowed wording: "request and observe the official archive slot count; official `LocalSave`, `DataPersistenceManager`, `GameDataUiState`, and `GameDataPanel` continue to own save files and UI operations."

Disallowed wording: "stable save-slot framework", "per-mod save slots", "new save namespace", or "fully verified lifecycle for slots 7-60."

## Follow-Up Matrix

| Follow-up | Evidence required |
| --- | --- |
| Slot 7 create/save/load | Open official save UI, create a new save in first expanded slot, save, return to title, reload same slot, clean exit. |
| Delete expanded slot | Delete slot 7+ through official UI, verify `LocalSave.GetArchiveInfo` returns empty/null, restart, clean exit. |
| Copy expanded slot | Copy a vanilla slot into slot 7+, verify archive metadata/index, load copied slot, clean exit. |
| Disable/restore | Disable MoreSaves after extra-slot files exist, confirm vanilla six-slot UI behavior and no crash/error, then re-enable and confirm recognition. |
| Restart recognition | Fresh game process after expanded-slot creation/copy sees expected slot data through official UI. |
| 24-slot layout | Manual/visual UI gate for overflow, navigation, scrolling, and selection. |
| Multi-owner policy | Unit or smoke-side two-owner registration verifying max-count aggregation and owner state messages. |

## Decision

No runtime change in this branch. The current `SaveSlotsFeature`/`SaveSlotsService` split is directionally correct because fragile reflection stays in GameBridge, but the public surface remains `Experimental` until the expanded-slot lifecycle matrix above has real evidence.
