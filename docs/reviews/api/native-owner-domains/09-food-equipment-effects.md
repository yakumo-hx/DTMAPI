# 09 - Food Equipment Effects

Status: Partial
Created: 2026-06-13
Reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`

## User Semantic Target

Food effects and equipment effects: reuse effects, unify multi-effect items, add new effects, and support item/equipment effect expansion.

## Official Workshop Support

| Capability | Official support | Evidence | Boundary |
| --- | --- | --- | --- |
| Beauty replacement | Not relevant | Beauty docs | No effect behavior |
| Base content mod | Yes for cooking/items/recipes | `033_*`, `046_*`, `049_*`, `052_*` | Data-backed known effects only |
| Advanced content mod | Partial for equipment functions/content | `047_*`, `048_*`, equipment ID docs | Does not prove brand-new code behavior |
| Runtime behavior mutation | Not public | No official custom effect code API found | DTMAPI GameBridge only |

## Native Owner Map

| Semantic target | Exact native names | Where found | State holder / lifecycle owner | Responsibility | Risk | API concept | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Food consume flow | `ItemFood : IEatable`, `IEatable.Eat`; `ItemFunctionFood.EatingEffect` | `ItemFood.cs`; `IEatable.cs`; `Config/Item/ItemFunctionFood.cs`; `Items_Inventory.md` | Food item instance and item function config | Consume item, emit native use-item path, apply effects, and create outputs | Medium raw native item risk | `IEatingEffectDefinition` DTO plus consume result | Found for existing food flow |
| Food effect application | `ItemFood.DoEffects`, `GetAllEffects`, `FetchExtraEffects`, `GetExtraEffects_BetterWater`; `AgentEquipmentFunctionFoodEffectsAddition` | `ItemFood.cs`; equipment function classes | Food item/effect config plus equipment effect listener | Applies configured/fetched effects; `DoEffects` does not own item consumption | Medium raw native item risk | `FoodEffectDefinition` DTO and bridge-owned apply adapter for known effects | Partial |
| Reusable multi-effect data | `EatingEffectInfo.Effects`, `OutputItems`; `FoodEffect.Buff`, `FoodEffect.Scale` | `EatingEffectInfo.cs`; `FoodEffect.cs`; official cooking docs | Eating effect config tables | Multiple buff/output effects per food | Medium | Content helper for known `FoodEffect` entries | Found for known effect data |
| Buff runtime | `BuffInfo`, `BuffComponentProto`, `BuffBasicType`, `BuffManager.loadedBuffs`, `BuffManager.Add`, `Remove`, `UpdatePerTU`, `RefreshUI`; `Buff.Apply`, `Buff.Remove`; `BuffComponentBasic.Apply/Remove`; `DolocAPI.HasBuff`, `DolocAPI.AddBuff` | buff config/runtime classes; `DolocAPI.cs` | `BuffManager` and active buff instances | Timed/scaled stat effects, UI, active buff list; instant buffs are not stored, timed duplicate add refreshes timer rather than reapplying/replacing scale | Medium/high duplicate/cleanup risk | Experimental `IBuffApi` for known buffs | Partial |
| Equipment effects | `AgentEquipmentSkillInfo.Function`; `AgentEquipmentFuncProto*`; `AgentEquipmentFunction.CreateAgentEquipmentFunction`, `DoExtraConfig`, `OnReceiveMessage`, `UpdatePerTu`, `AfterEnterRoom`, `Dispose`; `AgentEquipmentManager.SendMessage`; `AgentEquipmentParams.Commit`; `AgentEquipmentAbility` | equipment/player config classes; `AgentEquipmentFunction*.cs`; `AgentEquipmentManager.cs` | Equipment function runtime and equipment manager | Event/tick/room equipment functions and ability params | High reflection/native naming risk | Read/observe known native functions; bridge adapters only; plugin subclass extension not stable | Partial |
| Brand-new effect behavior | `ItemFunctionBase`; `ItemFactory.GetProtoInstanceName`, `GenerateItem`, `ValidateItem`; native `AgentEquipmentFunction*` subclasses | item function and equipment function classes | Native type/function discovery | New behavior requires code class or bridge logic, not data only | High/blocking | DTMAPI-owned custom effect handlers | Blocked for stable data-only API |

## API Translation Notes

- Separate data-only known-effect definitions from code-backed custom behavior.
- Buff APIs must define duplicate-add, duration stacking, owner cleanup, and disable/restore semantics.
- Equipment effects should not expose native function class names as public contracts.
- Known data effects are safer than code-backed behavior. DTMAPI custom effect handlers would be sidecar/bridge-owned, not official native JSON behavior.
- Round 3 confidence: existing eat flow 94; `DoEffects` without consume owner 90; known multi-effect data 86; equipment food-effect addition 90; buff duplicate/timer/queue/save behavior 90; `AgentEquipmentFunction*` lifecycle found but not plugin extension point 80; brand-new effect behavior blocked for stable JSON-only API 92.

## Blockers And Follow-Up

- Brand-new item/equipment behavior has no stable native JSON-only extension point.
- Equipment function discovery appears class/subtype/name based and needs method-body review before any plugin extension promise.
- Buff cleanup and duplicate semantics need game evidence.

## Evidence Checked

Maps: `Items_Inventory.md`, `Recipe_Crafting.md`, `UI.md`, `Assets_Content.md`.
Classes/symbols: `ItemFood`, `IEatable`, `ItemFunctionFood`, `EatingEffectInfo`, `FoodEffect`, `BuffManager`, `Buff`, `BuffComponentBasic`, `AgentEquipmentFunction*`, `AgentEquipmentManager`, `ItemFunctionBase`, `ItemFactory`.
