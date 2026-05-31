# Fish Roe Display Debugging Notes

This note records the debugging path for the fish roe display mod after the in-game tooltip still showed the vanilla text:

> 鱼卵 / 动物产出 / 不知道是什么产下的卵，该放去鹿制罐还是孵化器呢？

The goal was to answer two questions:

- Does a produced fish roe item actually keep the hidden fish identity?
- If the identity exists, why does `Yuuka.FishBreedingAssistant` not display it?

## Current Architecture

- Official fish tank code stores pending products as string ids in `FarmFishTank._products`.
- `FarmFishTank.GetProductItems()` converts farm fish ids into generic roe items:
  - Generate roe item from `FarmFishInfo.RoeItem_Ref`.
  - If the generated item is `ItemFishRoe`, call `ItemFishRoe.SetFishName(product)`.
- `ItemFishRoe` has a hidden persisted property:
  - `public string fishName { get; private set; } = "fish";`
- Official fish incubator does not use title or description. It checks `item is ItemFishRoe`, calls `ItemFishRoe.Incubate()`, and then `ItemFishRoe.ToFry()`.
- `ToFry()` generates a fish fry and copies the hidden `fishName` into the fry.

Therefore the official incubator can identify fish roe because the fish species is stored on the item object itself, not because the UI exposes it.

## Confirmed Findings

The temporary SMAPI diagnostics confirmed that backpack/container hover sees real fish roe objects:

```text
[FishRoeDiag:ContainerBaseUiState.ConvertItemData]
type='DolocTown.ItemFishRoe'
itemIdentity=True
itemId='fish_roe_2'
directFishName='skeleton_fish'
fishNameValid=False
tagged=<none>
roeIdentity=False
```

and:

```text
directFishName='nightbone_fish'
```

This proves:

- The roe item is a real `DolocTown.ItemFishRoe`.
- The hidden `fishName` field is present.
- New and stored fish roe do not lose fish identity just by being collected, placed in a box, or hovered.

The failing step was not the game data. It was SMAPI identity resolution.

## Root Cause Found So Far

SMAPI's fish roe helper successfully read `fishName`, but then tried to validate the id through the wrong config path.

The game table lives at:

```text
DolocTown.Config.DolocConfig.Tables.TbFarmFish
```

The old SMAPI helper only tried:

```text
DolocAPI.Tables
DolocAPI.Config
```

As a result, `directFishName` could be non-empty while `fishNameValid=False` and `roeIdentity=False`.

## UI Hook Finding

Backpack and container hover did not produce useful diagnostics through only:

```text
DolocTown.Item.get_title
DolocTown.Item.get_description
DolocTown.Item.GetDetailInfo
```

The path that actually revealed fish roe identity during hover was:

```text
DolocTown.ContainerBaseUiState.ConvertItemData(Item item)
```

This means the current UI path builds `ItemData` for hover in a way that is not reliably covered by the original title/description hooks alone. A robust SMAPI item display API should hook the container UI item-data conversion layer, or another shared UI data construction layer, in addition to base `Item` title/description accessors.

## Failed Diagnostic Attempt

Patching `DolocTown.UI.ItemData` constructor directly caused a Harmony IL compile failure:

```text
InvalidProgramException: Invalid IL code in DolocTown.UI.ItemData::.ctor
HarmonyException: IL Compile Error
```

Avoid patching this value-type constructor. Use `ContainerBaseUiState.ConvertItemData(Item)` or another normal method around item data creation instead.

## Latest Check: Still No Display After SMAPI Test Patch

After the temporary SMAPI build was changed to try writing modified title/description back through `ContainerBaseUiState.ConvertItemData`, the game still showed vanilla text.

The log confirmed:

```text
Patched experimental hook: DolocTown.ContainerBaseUiState.ConvertItemData -> AfterContainerItemDataConverted.
Fish roe diagnostic logging is active for this temporary build.
Loaded assembly for Yuuka.FishBreedingAssistant
Started DolocTown SMAPI mod Yuuka.FishBreedingAssistant
```

but there were no later lines like:

```text
[FishRoeDiag:ContainerBaseUiState.ConvertItemData]
```

This means the latest postfix shape did not execute for the hover path, even though an earlier simpler postfix on the same method did execute. The likely cause is Harmony's handling of the value-type `ItemData` return value / pass-through postfix signature, not a disabled mod.

Practical lesson:

- `void Postfix(object __0)` on `ContainerBaseUiState.ConvertItemData(Item)` was enough for diagnostics and did run.
- A postfix that tries to mutate or pass through `DolocTown.UI.ItemData` may silently fail to execute on this game/runtime combination.
- Avoid relying on value-type return rewriting for the production fix unless it is proven in logs.

## Current Temporary SMAPI Changes

The temporary SMAPI build has been used to test:

- Logging `ItemFishRoe` identity at container item-data conversion time.
- Reading `DolocTown.Config.DolocConfig.Tables`.
- Attempting to pass converted item titles/descriptions through SMAPI item display events.

These changes are diagnostic and should be cleaned before release:

- Remove noisy diagnostic logging.
- Keep the corrected `DolocConfig.Tables` lookup.
- Keep a stable UI item-data hook only if verified to affect displayed hover text.
- Avoid `ItemData` constructor patching.

## Recommended Fix Direction

Preferred fix: upgrade SMAPI item display support.

1. `TryGetFishRoeIdentity` should read `ItemFishRoe.fishName` directly.
2. It should validate the fish id through `DolocTown.Config.DolocConfig.Tables.TbFarmFish`.
3. The item display event should be raised on the UI path used by backpack/container hover.
4. The event must write back to the actual `ItemData` returned to the UI.

Fallback fix: patch the fish roe display mod directly against `ItemFishRoe` or container hover UI.

This is less clean because multiple mods may want item display hooks. The better long-term home for the feature is SMAPI.

## Resolution With SMAPI 0.8.20

The latest working SMAPI path fixed both earlier failure points:

- `TryGetFishRoeIdentity` now reads real `ItemFishRoe.fishName` first, then validates the fish id through `DolocTown.Config.DolocConfig.Tables.TbFarmFish`.
- Item display events are applied to the UI data that is actually rendered by backpack/container hover. The stable path rebuilds `DolocTown.UI.ItemData`, raises title/description rendering events, and writes the changed text back before the hover box is shown.

This confirms the fish roe display mod did not need its own private game patches once SMAPI exposed the correct item identity and hover write-back path.

The fish roe mod 1.1.3 now keeps the title label `鱼卵（鱼名）`, preserves the vanilla description, and only appends hatch fish, hatch time, and growth time.

## Quick Log Checklist

After a fresh game restart and hovering fish roe, useful log lines are:

```text
Fish roe diagnostic logging is active
Patched experimental hook: DolocTown.ContainerBaseUiState.ConvertItemData
[FishRoeDiag:ContainerBaseUiState.ConvertItemData]
```

Healthy identity resolution should eventually look like:

```text
directFishName='...'
fishNameValid=True
roeIdentity=True
```

If `directFishName` is present but `roeIdentity=False`, the table lookup or identity builder is still wrong.

If `roeIdentity=True` and the title after event changes, but the UI still shows vanilla text, the patched method is not writing back to the displayed hover data.

If no `[FishRoeDiag:...]` line appears after hover, the selected hook path is not being hit.
