# 02 - NPC Body Behavior

Status: Partial
Created: 2026-06-13
Reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`

## User Semantic Target

NPC body, NPC behavior, animation, skin, encyclopedia/documentation, added story, movement, trading, position detection, and creating a completely new NPC.

## Official Workshop Support

| Capability | Official support | Evidence | Boundary |
| --- | --- | --- | --- |
| Beauty replacement | Yes for existing NPC textures | `009_*`, `018_*`, texture-anchor docs | Existing assets, not new runtime behavior |
| Base content mod | Partial for NPC-related IDs/docs | `040_*` NPC ID table | ID reference, not a full new-NPC pipeline |
| Advanced content mod | Not confirmed for complete new NPC | Official docs checked | No proven schedule/dialogue/save registration |
| Runtime behavior mutation | Not public | No official schedule/movement API found | DTMAPI GameBridge only |

## Native Owner Map

| Semantic target | Exact native names | Where found | State holder / lifecycle owner | Responsibility | Risk | API concept | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| NPC identity/body/docs | `Npc`, `Npc(NpcInfo)`, `NpcManager`, `NpcManager.AddNpc`, `Config.NPC.NpcInfo`, `Config.NPC.NpcDocumentInfo`, `Npc.proto`, `Npc.GetCurrentTitle`, `Npc.RuntimeAnimatorController` | `maps/NPC_Dialogue.md`; `Npc.cs`; metadata CSVs | `Npc`, `NpcManager`, config tables | Identity, title, document, animation roots, manager registration, lookup, and save/load | Medium raw native coupling | `ICharacterInfo`, `ICharacterDocumentApi` DTOs | Found for existing NPC |
| NPC schedules/behavior | `NpcController`, `NpcController._scheduleResult`, `Npc.InvokeSchedule`, `NpcController.MakeDecision`, `NpcScheduleAsset.TryLoadAsset`, `DolocBundleManager.get_npcSchedules`, `LoadNpcSchedules`, `NodeCanvas.NpcScheduleGraph` | `maps/NPC_Dialogue.md`; metadata CSVs | `NpcController` and schedule assets | Schedule graph loading and NPC decision state | High private graph/state risk | Experimental `ICharacterScheduleApi` after graph/mark-point proof | Partial |
| Dialogue/story/gift | `DialogueState.DialogueWithNpc`, `DolocAPI.StartDialogueNode`, `SetDialogueEntrance`, `TryGiftItemToNpc`, `Npc.OnInteract`, `Npc.ForceInteract`, `AsideDialoguePanel` | `maps/NPC_Dialogue.md`; `maps/UI.md` | Dialogue state and NPC interaction path | Starts dialogue, gift, aside/story UI | Medium/high Yarn/UI coupling | `ICharacterDialogueApi`, `IGiftApi` | Partial |
| Animation/skin/render | `NpcRenderer`, `NpcRenderer.CheckAnimation`, `WalkTo`, `UpdateDirection`, `OnSay`, `OnStopSay`, `Npc.UpdateAnimationByMoving`, `Npc.RenderNpc`, `Npc.UnRenderNpc` | `Npc.cs`; `maps/NPC_Dialogue.md`; official beauty docs | `NpcRenderer` and `Npc` render lifecycle | Animation, movement render, speech state, texture use | Medium/high lifecycle risk | Appearance/texture-pack adapter | Partial |
| Movement/location | `Npc.__EnterScene`, `_JourneyTo`, `Move`, `StopMove`, `ForceSetPosition`, `TryGetCurrentRoom`, `NpcManager.GetNpcsInScene`, `DolocAPI.IsNpcAtScene`, `IsNpcAtMarkPoint`, `SetNpcToScene`, `SetNpcToMarkPoint` | `maps/NPC_Dialogue.md`; `maps/Resource_Gathering.md` | `Npc`, `NpcManager`, mark point state | Position, scene/mark-point checks, movement | High for mutation | `ICharacterLocationApi`; experimental movement adapter | Partial |
| Store/trade relation | `StoreManager`, `Store`, `ExchangeStore`, `IStore`, `DolocAPI.OpenStore`, `RefreshStore`, `UnlockStoreItem`; store id/dialogue/task mediated paths; `TryGiftItemToNpc` | `maps/Shop.md`; official store docs | Store manager plus dialogue/task and gift paths | Store open/refresh/unlock is not NPC-core owned; NPC may be an interaction entry point | Medium native item/dialogue coupling | `IStoreApi`, `ICharacterGiftApi` | Partial |
| Completely new NPC | `Npc` constructors, `NpcManager.npcList`, `NpcManager.AddNpc`, `Config.NPC.NpcInfo`, `NpcScheduleAsset`, dialogue/document fields | `Npc.cs`; metadata CSVs; official NPC docs | Distributed across config, manager, schedule, render, dialogue | Stable API blocked; constrained experimental native path requires early `TbNpc`/asset/schedule/dialogue/table injection | High/blocking | `ICharacterContentApi.Proposed` research only | Blocked for stable API |

## API Translation Notes

- Existing NPC query and appearance replacement can become DTO/content APIs first.
- Movement, schedule mutation, and story injection require owner tokens and restore policy.
- Complete new NPC must not be promised until config load, save/load, schedule graph, dialogue entity, render asset, encyclopedia, and manager registration are all proven.
- Round 3 confidence: existing NPC identity/state 88; manager registration/save/load 86; schedule/behavior 72; dialogue/liking/gift 78; movement/location 84; NPC-linked store interaction 64; full new NPC stable API 18; constrained experimental research path 68.

## Blockers And Follow-Up

- No confirmed official or native single-owner pipeline for complete new NPC creation.
- NPC-specific stores need a separate owner mapping between store definitions and NPC interaction.
- Dialogue mutation must handle current UI/dialogue state and save progression.
- Complete new NPC follow-up must prove NodeCanvas schedule graph validity, mark-point validity, Yarn node existence, and save/load rehydrate timing. `NpcDocumentInfo`/ID-table presence is metadata, not creation proof.

## Evidence Checked

Maps: `NPC_Dialogue.md`, `Shop.md`, `Assets_Content.md`, `Action_Interaction.md`, `Resource_Gathering.md`, `UI.md`.
Classes/symbols: `Npc`, `NpcController`, `NpcRenderer`, `NpcScheduleAsset`, `DialogueState`, `StoreManager`, `Store`, `ExchangeStore`.
