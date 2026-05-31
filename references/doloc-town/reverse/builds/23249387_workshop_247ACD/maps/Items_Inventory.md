# DolocTown Items / Inventory API Map

## Build

- Steam build: 23249387
- Branch: workshop
- Assembly-CSharp hash: 247ACDAA8D6ED0490A406C6FC8EB035F957D0EC1AE3F036F10E2092B8AC10367
- Research date: 2026-05-17
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Query item definitions and held items.
- Support safe add/remove operations.
- Map content-pack item extension boundaries.

## Decompiled scope

- Matched types: 362
- Matched methods: 5482
- Matched fields: 1947
- Matched properties: 1669
- Matched events: 0
- Matched call edges: 31491
- Matched strings: 1963
- Raw indexes: `maps/index/Items_Inventory-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.Item | class | Confirmed | visibility=public; methods=78; fields=2 |
| DolocTown.LinearInventory | class | Confirmed | visibility=public; methods=72; fields=3 |
| DolocTown.Config.Item.ItemInfo | class | Confirmed | visibility=public; methods=50; fields=22 |
| DolocTown.UI.QuickInventoryPanel | class | Confirmed | visibility=public; methods=53; fields=12 |
| DolocTown.ItemBox | class | Confirmed | visibility=public; methods=41; fields=5 |
| DolocTown.Config.Mission.ItemSubmitConditionInfo | class | Confirmed | visibility=public; methods=30; fields=12 |
| DolocTown.Config.Store.ExchangeStoreItemData | class | Confirmed | visibility=public; methods=30; fields=10 |
| DolocTown.InventorySystem | class | Confirmed | visibility=public; methods=34; fields=6 |
| DolocTown.ItemSeed | class | Confirmed | visibility=public; methods=34; fields=6 |
| DolocTown.UI.InventoryPanel | class | Confirmed | visibility=public; methods=35; fields=5 |
| DolocTown.Config.Item.ItemFunctionBox | class | Confirmed | visibility=public; methods=28; fields=11 |
| DolocTown.Config.Item.ItemFunctionTool | class | Confirmed | visibility=public; methods=25; fields=10 |
| DolocTown.GarbageShredder | class | Confirmed | visibility=public; methods=32; fields=2 |
| DolocTown.UI.OptionItem | class | Confirmed | visibility=public; methods=20; fields=12 |
| DolocTown.ItemWaterCan | class | Confirmed | visibility=public; methods=29; fields=2 |
| DolocTown.UI.SettingUIItemManager | class | Confirmed | visibility=public; methods=24; fields=7 |
| DolocTown.Config.Item.ToolOverrideInfo | class | Confirmed | visibility=public; methods=19; fields=11 |
| DolocTown.EquipmentItemBuilderTip | class | Confirmed | visibility=public; methods=19; fields=11 |
| DolocTown.Config.Item.ItemFunctionFishingRod | class | Confirmed | visibility=public; methods=21; fields=8 |
| DolocTown.ItemAnimalPackage | class | Confirmed | visibility=public; methods=27; fields=2 |
| DolocTown.Config.Item.ItemSubTypeInfo | class | Confirmed | visibility=public; methods=20; fields=8 |
| DolocTown.ItemFarmingGun | class | Confirmed | visibility=public; methods=26; fields=2 |
| DolocTown.UI.ItemRecipeSlot | class | Confirmed | visibility=public; methods=17; fields=11 |
| DolocTown.Config.Store.StoreItemSeasonData | class | Confirmed | visibility=public; methods=21; fields=6 |
| DolocTown.Config.Equipment.EquipmentFuncGarbageShredder | class | Confirmed | visibility=public; methods=19; fields=7 |
| DolocTown.Config.Item.ItemFunctionAnimalPackage | class | Confirmed | visibility=public; methods=19; fields=7 |
| DolocTown.GarbageShredderUiState | class | Confirmed | visibility=public; methods=22; fields=4 |
| DolocTown.UI.DropItemPickTip | class | Confirmed | visibility=public; methods=10; fields=16 |
| DolocTown.IDropItemHost | interface | Confirmed | visibility=public; methods=25; fields=0 |
| DolocTown.LinearInventory/<>c | class | Confirmed | visibility=private; methods=13; fields=12 |
| DolocTown.UI.ItemDetailData | struct | Confirmed | visibility=public; methods=14; fields=11 |
| DolocTown.UI.ItemRecipeData | struct | Confirmed | visibility=public; methods=13; fields=12 |
| DolocTown.BuildingItemBuilderTip | class | Confirmed | visibility=public; methods=13; fields=11 |
| DolocTown.Config.Item.ItemSpawnInfo | class | Confirmed | visibility=public; methods=18; fields=6 |
| DolocTown.ItemCrop | class | Confirmed | visibility=public; methods=22; fields=2 |
| DolocTown.UI.BoxInventoryWidget | class | Confirmed | visibility=public; methods=19; fields=5 |
| DolocTown.Config.Mission.ItemGeneConditionInfo | class | Confirmed | visibility=public; methods=17; fields=6 |
| DolocTown.Config.Store.StoreItemUnlockInfo | class | Confirmed | visibility=public; methods=17; fields=6 |
| DolocTown.ItemDroneStructure | class | Confirmed | visibility=public; methods=20; fields=3 |
| DolocTown.ItemEquipment | class | Confirmed | visibility=public; methods=21; fields=2 |
| DolocTown.UI.OptionItemUI | class | Confirmed | visibility=public; methods=14; fields=9 |
| DolocTown.Config.Item.ItemAutomationTypeInfo | class | Confirmed | visibility=public; methods=16; fields=6 |
| DolocTown.UI.DroneItemSlot | class | Confirmed | visibility=public; methods=12; fields=10 |
| DolocTown.UI.ItemNavSlot | class | Confirmed | visibility=public; methods=13; fields=9 |
| DolocTown.Config.Item.ItemSpawnRandom | class | Confirmed | visibility=public; methods=16; fields=5 |
| DolocTown.ItemFishFry | class | Confirmed | visibility=public; methods=17; fields=4 |
| DolocTown.ToolCollider | class | Confirmed | visibility=public; methods=14; fields=7 |
| DolocTown.UI.ItemBorder | class | Confirmed | visibility=public; methods=12; fields=9 |
| DolocTown.UI.ItemRecipeListViewer | class | Confirmed | visibility=public; methods=15; fields=6 |
| DolocTown.Config.Item.CfgRangedItem | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Item.EatingEffectInfo | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Item.ItemFunctionCrop | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Item.ItemFunctionFertilizer | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Item.ItemFunctionMissile | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Item.ToolExtraSpawnLutByResourceName | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.DecalEquipmentItemBuilderTip | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.ItemBuilding | class | Confirmed | visibility=public; methods=18; fields=2 |
| DolocTown.ToolRenderer | class | Confirmed | visibility=public; methods=17; fields=3 |
| DolocTown.UI.ItemHoverBox | class | Confirmed | visibility=public; methods=6; fields=14 |
| DolocTown.GarbageShredder/<SpawnItems>d__38 | class | Confirmed | visibility=private; methods=8; fields=11 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| AgentStateBase.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| DevHelper.InitDevMenuItems | candidate lifecycle or hook point | Risky: non-public or generated path |
| DevHelper/<>c__DisplayClass13_0.<InitDevMenuItems>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_ObtainItem | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.DevMenuItem_EnterDolocMountain | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.DevMenuItem_EnterFarm | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.DevMenuItem_EnterOldCity | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.DevMenuItem_EnterTeleportPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.DevMenuItem_EnterWetland | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.OpenSubmitSingleItemPanel | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshQuickInventory | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshQuickInventory | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshQuickInventorySelected | candidate lifecycle or hook point | Medium |
| DolocAPI/<>c__DisplayClass673_0.<OpenSubmitSingleItemPanel>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocInputSource/IBaseInputActions.OnDestroyItem | candidate lifecycle or hook point | Medium |
| DolocInputSource/INormalInputActions.OnUseItem | candidate lifecycle or hook point | Medium |
| DolocInputSource/INormalInputActions.OnUseTool | candidate lifecycle or hook point | Medium |
| DolocInputSource/NormalInputActions.get_UseItem | candidate lifecycle or hook point | Medium |
| DolocInputSource/NormalInputActions.get_UseTool | candidate lifecycle or hook point | Medium |
| DolocTown.AgentControllerState.UseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AgentControllerState.UseItemContinues | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentControllerState.UseTool | candidate lifecycle or hook point | Medium |
| DolocTown.AgentControllerState.UseToolContinues | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentControllerState.UseToolOrItem | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentControllerState/<>c.<UseTool>b__75_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentSkillManager.UseSkillFromItemName | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishing.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateHit.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateIdle.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateInteract.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateMove.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateThrow.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateTool.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateTool.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateTool.OnExit | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateTouchGround.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateWater.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateYawn.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBotDecisionMakerProcessing/<>c.<StartProcessing>b__5_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AutomateBotDecisionMakerProcessing/<>c.<StartProcessing>b__5_2 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AutomateBotStation.Load | candidate lifecycle or hook point | Medium |
| DolocTown.BodyController.get_IsCurrentStateSupportUseItem | candidate lifecycle or hook point | Medium |
| DolocTown.BodyController.get_ShouldEnterToolState | candidate lifecycle or hook point | Medium |
| DolocTown.BodyController.LoadCurrentTool | candidate lifecycle or hook point | Medium |
| DolocTown.BodyController.UseFishRod | candidate lifecycle or hook point | Medium |
| DolocTown.BodyController.UseTool | candidate lifecycle or hook point | Medium |
| DolocTown.BodyController/<>c__DisplayClass136_0.<UseTool>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingItemBuilderTip.__UpdateFunc_JoyStick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingItemBuilderTip.__UpdateFunc_KM | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingItemBuilderTip.ExitBuilder | candidate lifecycle or hook point | Medium |
| DolocTown.BuildingItemBuilderTip.OnEnter | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingItemBuilderTip.OnUpdate | candidate lifecycle or hook point | Medium |
| DolocTown.Case/<>c.<AfterLoadEquipment>b__43_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionBookUiState.<RefreshItemList>b__56_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionBookUiState.RefreshItemList | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionBookUiState/<>c.<RefreshItemList>b__56_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionBookUiState/<>c.<RefreshItemList>b__56_2 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionBookUiState/<>c.<RefreshItemList>b__56_3 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectorDropItem.Init | candidate lifecycle or hook point | Medium |
| DolocTown.CollectorDropItem.OnFixedUpdate | candidate lifecycle or hook point | Medium |
| DolocTown.CollectorDropItem.OnTriggerEnter2D | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectorDropItem.OnTriggerExit2D | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Item.ItemFunctionBox.get_SkinClose | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Item.ItemFunctionBox.get_SkinOpen | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Item.ItemFunctionBox.set_SkinClose | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Item.ItemFunctionBox.set_SkinOpen | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Item.ItemFunctionFishingRod.get_InitProgress | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Item.ItemFunctionFishingRod.set_InitProgress | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.StaticTextInfo.get_ItemChipReloadDurationDecrease | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_ItemChipReloadDurationDecrease_l10n_key | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_ItemConfirmUse | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_ItemConfirmUse_l10n_key | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.set_ItemChipReloadDurationDecrease | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.StaticTextInfo.set_ItemConfirmUse | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.TbStaticText.get_ItemChipReloadDurationDecrease | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.TbStaticText.get_ItemConfirmUse | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Settings.GlobalParameterInfo.get_ContinuouslyUseItemTimer | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Settings.GlobalParameterInfo.get_ContinuouslyUseItemTimer_Ref | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Settings.GlobalParameterInfo.get_InitHat_Ref | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Settings.GlobalParameterInfo.set_ContinuouslyUseItemTimer | candidate lifecycle or hook point | Risky: non-public or generated path |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| AgentStateBase.get_SupportScrollQuickInventoryUI() | direct call candidate | Medium; needs instance source |
| AgentStateBase.get_SupportUseItem() | direct call candidate | Medium; needs instance source |
| DevHelper.get_DevMenuItemTitles() | direct call candidate | Medium; needs instance source |
| DolocAPI.CanAfford(DolocTown.CountItem[] costList; System.Boolean checkBox) | direct call candidate | Medium |
| DolocAPI.CanAfford(DolocTown.CountItem[] costList; System.Boolean checkBox; System.Int32 scale) | direct call candidate | Medium |
| DolocAPI.CanPlaceItem(System.String name; System.Int32 count; System.Int32& firstAvailableIndex) | direct call candidate | Medium |
| DolocAPI.CanPlaceItem(System.String name; System.Int32 count) | direct call candidate | Medium |
| DolocAPI.CanPlaceItem(DolocTown.Item item; System.Int32& firstAvailableIndex) | direct call candidate | Medium |
| DolocAPI.CanPlaceItem(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.Command_AddMissionItem(System.String missionItemId; System.String markPointId) | direct call candidate | Medium |
| DolocAPI.ConvertToCountItems(DolocTown.Item[] items) | direct call candidate | Medium |
| DolocAPI.CostItem(System.String itemName; System.Int32 count; System.Boolean checkBox) | direct call candidate | Medium |
| DolocAPI.CostItem(DolocTown.Item item; System.Int32 count; System.Boolean checkBox; System.Boolean shouldEqualAsItem) | direct call candidate | Medium |
| DolocAPI.CostItemAt(System.Int32 position; System.Int32 count) | direct call candidate | Medium |
| DolocAPI.CostItemNoCheck(System.Collections.Generic.IEnumerable`1<DolocTown.CountItem> itemList; System.Boolean checkBox) | direct call candidate | Medium |
| DolocAPI.CostItemNoCheck(System.String name; System.Int32 count; System.Boolean checkBox) | direct call candidate | Medium |
| DolocAPI.CostSelectedItem(System.Int32 selectedIndex; System.Int32 count; System.Boolean showFadeUpIcon) | direct call candidate | Medium |
| DolocAPI.CostSelectedItem(System.Int32 count; System.Boolean showFadeUpIcon) | direct call candidate | Medium |
| DolocAPI.CostToolEnergy() | direct call candidate | Medium |
| DolocAPI.CountItem(DolocTown.Item item; System.Boolean checkBox; System.Boolean shouldEqualAsItem) | direct call candidate | Medium |
| DolocAPI.CountItem(DolocTown.LinearInventory[] inventories; System.String itemName) | direct call candidate | Medium |
| DolocAPI.CountItem(System.String itemName; System.Boolean checkBox) | direct call candidate | Medium |
| DolocAPI.CountItem(DolocTown.LinearInventory[] inventories; DolocTown.Item item; System.Boolean shouldEqualAsItem) | direct call candidate | Medium |
| DolocAPI.CreateMissionItem(System.String missionItemId; System.String markPointId) | direct call candidate | Medium |
| DolocAPI.DestroyItem(System.Int32 index) | direct call candidate | Medium |
| DolocAPI.DisposeBufferItem() | direct call candidate | Medium |
| DolocAPI.DisposeItem(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.DisposeItem(System.Int32 index) | direct call candidate | Medium |
| DolocAPI.EquipActiveItem(DolocTown.Item item; DolocTown.Item& oldItem) | direct call candidate | Medium |
| DolocAPI.EquipDrone(DolocTown.Item item; DolocTown.Item& oldDrone) | direct call candidate | Medium |
| DolocAPI.EquipHat(System.String hatName; DolocTown.Item& oldHat) | direct call candidate | Medium |
| DolocAPI.EquipHat(DolocTown.ItemHat hatItem; DolocTown.Item& oldHat) | direct call candidate | Medium |
| DolocAPI.EquipPassiveItem(DolocTown.Item item; DolocTown.Item& oldItem) | direct call candidate | Medium |
| DolocAPI.EquipPassiveItem1(DolocTown.Item item; DolocTown.Item& oldItem) | direct call candidate | Medium |
| DolocAPI.EquipPassiveItem2(DolocTown.Item item; DolocTown.Item& oldItem) | direct call candidate | Medium |
| DolocAPI.GenerateDropItem(DolocTown.IDropItemHost host; DolocTown.Item item; UnityEngine.Vector2 pos; System.Boolean shouldSendMsg) | direct call candidate | Medium |
| DolocAPI.GenerateDropItems(DolocTown.IDropItemHost host; DolocTown.Config.Item.ItemSpawnEntry entry; UnityEngine.Vector2 pos; System.Boolean shouldSendMsg) | direct call candidate | Medium |
| DolocAPI.GenerateDropItems(DolocTown.IDropItemHost host; DolocTown.Config.Item.ItemSpawnEntry entry; System.Int32 count; UnityEngine.Vector2 startPos; System.Boolean shouldSendMsg) | direct call candidate | Medium |
| DolocAPI.GenerateDropItems(DolocTown.IDropItemHost host; System.String itemName; UnityEngine.Vector2 pos; System.Int32 count; System.Boolean shouldSendMsg) | direct call candidate | Medium |
| DolocAPI.GenerateDropItems(DolocTown.IDropItemHost host; DolocTown.CountItem countItem; UnityEngine.Vector2 pos; System.Boolean shouldSendMsg) | direct call candidate | Medium |
| DolocAPI.GenerateItem(DolocTown.CountItem countItem) | direct call candidate | Medium |
| DolocAPI.GenerateItem(System.String name; System.Int32 count) | direct call candidate | Medium |
| DolocAPI.GenerateItem(DolocTown.Config.Item.ItemInfo proto; System.Int32 count) | direct call candidate | Medium |
| DolocAPI.get_DisableDisposeItem() | direct call candidate | Medium |
| DolocAPI.get_HasBufferItem() | direct call candidate | Medium |
| DolocAPI.get_SelectedItem() | direct call candidate | Medium |
| DolocAPI.get_SelectedItemIndex() | direct call candidate | Medium |
| DolocAPI.GetBackpackWithInsideBoxes(System.Boolean useSharedContainer) | direct call candidate | Medium |
| DolocAPI.GetExistItemCount(System.String itemName) | direct call candidate | Medium |
| DolocAPI.GetFirstItemWithName(DolocTown.LinearInventory[] inventories; System.String itemName) | direct call candidate | Medium |
| DolocAPI.GetInventoriesAroundAgent(System.Boolean useBox) | direct call candidate | Medium |
| DolocAPI.GetInventoriesAroundAgent() | direct call candidate | Medium |
| DolocAPI.GetInventoriesAroundEquipment(DolocTown.Equipment equipment) | direct call candidate | Medium |
| DolocAPI.GetItemBorder(DolocTown.UI.DolocUiObject obj; DolocTown.UI.BorderType borderType) | direct call candidate | Medium |
| DolocAPI.GetItemBorder(UnityEngine.RectTransform obj; DolocTown.UI.BorderType borderType) | direct call candidate | Medium |
| DolocAPI.GetItemBuyingPrice(System.String itemName) | direct call candidate | Medium |
| DolocAPI.GetItemDescription(System.String itemName) | direct call candidate | Medium |
| DolocAPI.GetItemSellingPrice(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.GetItemSellingPrice(System.String itemName) | direct call candidate | Medium |
| DolocAPI.GetItemSortingOrder(System.String itemName) | direct call candidate | Medium |
| DolocAPI.GetItemSprite(System.String itemName) | direct call candidate | Medium |
| DolocAPI.GetItemTitle(System.String itemName) | direct call candidate | Medium |
| DolocAPI.HasEnoughEnergyForUsingTool() | direct call candidate | Medium |
| DolocAPI.HideItemBorder(DolocTown.UI.DolocUiObject obj) | direct call candidate | Medium |
| DolocAPI.HideItemBorder() | direct call candidate | Medium |
| DolocAPI.HoverItemViewer(DolocTown.UI.DolocUiObject obj; DolocTown.UI.ItemData itemData; DolocTown.UI.UIAlignmentType targetAnchor; DolocTown.UI.UIAlignmentType hoverPivot) | direct call candidate | Medium |
| DolocAPI.HoverItemViewer(UnityEngine.RectTransform rectTransform; DolocTown.UI.ItemData itemData; DolocTown.UI.UIAlignmentType targetAnchor; DolocTown.UI.UIAlignmentType hoverPivot) | direct call candidate | Medium |
| DolocAPI.IsEquippedActive(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.IsEquippedDrone(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.IsEquippedPassive1(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.IsEquippedPassive2(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.IsItemCanPutInToContainer(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.IsItemDisposable(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.IsItemSalable(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.MaxCostItem(DolocTown.LinearInventory[] inventories; System.String itemName; System.Int32 count; System.Int32 startIndex) | direct call candidate | Medium |
| DolocAPI.MaxCostItem(DolocTown.LinearInventory[] inventories; DolocTown.Item item; System.Int32 count; System.Boolean shouldEqualAsItem; System.Int32 startIndex) | direct call candidate | Medium |
| DolocAPI.MaxItemPlaceCount(System.String itemName) | direct call candidate | Medium |
| DolocAPI.OpenSubmitSingleItemPanel(System.Func`2<DolocTown.Item,System.Boolean> itemFilter; System.Func`1<System.Boolean> submitConditionChecker; System.Func`2<DolocTown.Item,System.Int32> itemSubmitCountGetter; System.Func`3<DolocTown.Item,System.Int32,System.String> confirmTextGetter; System.Action`1<System.Int32> onFailedSubmit; System.Action`2<System.Int32,System.Int32> onSubmit) | direct call candidate | Medium |
| DolocAPI.PlaceInBackpackOrGenerateDropItem(DolocTown.Item item; System.Boolean checkBox) | direct call candidate | Medium |
| DolocAPI.PlaceItem(DolocTown.Item item; System.Boolean checkBox; System.Boolean useFade) | direct call candidate | Medium |
| DolocAPI.PlaceItem(System.String name; System.Int32 count; System.Boolean checkBox; System.Boolean useFade) | direct call candidate | Medium |
| DolocAPI.PlaceItemAllForce(System.String itemName; System.Int32 count; System.Int32& overflowCount) | direct call candidate | Medium |
| DolocAPI.PlaceItemFromOutside(System.Int32 index; DolocTown.LinearInventory source; DolocTown.LinearInventory target) | direct call candidate | Medium |
| DolocAPI.QueryItemProto(System.String name; DolocTown.Config.Item.ItemInfo& proto) | direct call candidate | Medium |
| DolocAPI.QueryItemSpawnLut(System.String id; DolocTown.Config.Item.ItemSpawnInfo& spawnLut) | direct call candidate | Medium |
| DolocAPI.QuickDeselectCurrentItem() | direct call candidate | Medium |
| DolocAPI.QuickSelectCurrentItem() | direct call candidate | Medium |
| DolocAPI.RaiseItemObtainTip(System.String itemName; UnityEngine.Sprite icon; System.String title; System.Int32 count; System.Boolean useSound) | direct call candidate | Medium |
| DolocAPI.RefreshQuickInventory(System.Int32 index) | direct call candidate | Medium |
| DolocAPI.RefreshQuickInventory() | direct call candidate | Medium |
| DolocAPI.RefreshQuickInventorySelected() | direct call candidate | Medium |
| DolocAPI.ReQuickSelectCurrentItem() | direct call candidate | Medium |
| DolocAPI.ResetQuickInventorySelection() | direct call candidate | Medium |
| DolocAPI.SendItemAsEmail(System.String itemName; System.Int32 count; System.String emailName; System.String content; System.String sender; System.String templateName) | direct call candidate | Medium |
| DolocAPI.SetItemBorderVisible(System.Boolean value) | direct call candidate | Medium |
| DolocAPI.SetQuickInventoryVisible(System.Boolean value) | direct call candidate | Medium |
| DolocAPI.SpawnItems(System.String itemName; UnityEngine.Vector2Int countRange) | direct call candidate | Medium |
| DolocAPI.SpawnMonsterDropItems(DolocTown.Monster monster) | direct call candidate | Medium |
| DolocAPI.SpawnResourceDropItems(DolocTown.DungeonResource resource; System.Boolean isRender; System.String overrideSpawnLut) | direct call candidate | Medium |
| DolocAPI.SwapHalfItemFromInventory(System.Int32 index) | direct call candidate | Medium |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| DevHelper.devMenuItems | System.Collections.Generic.Dictionary`2<System.String,System.Action> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI._itemBorder | DolocTown.UI.ItemBorder | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_DropMoney>d__912.<host>5__2 | DolocTown.IDropItemHost | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_GiftItemToNpc>d__957.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_SubmitBoardMissionItem>d__959.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_SubmitItemToNpc>d__945.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_SubmitItemToNpcByInfo>d__946.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<GetAnimalAllProduce>d__262.<>7__wrap1 | System.Collections.Generic.List`1/Enumerator<DolocTown.Config.Item.ItemSpawnData> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_DestroyItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_DisposeItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_DisposeItemHold | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_LockItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_PutAllItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_PutMaxItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_PutMaxItemHold | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_SortItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_SortItemHold | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_SplitItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_SubmitItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput_BuilderSwitchInventory | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_DisposeItemInBackpack | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_ScrollInventoryDown | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_ScrollInventoryUp | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_UseItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_UseTool | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.collectorDropItem | DolocTown.CollectorDropItem | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.quickInventory | DolocTown.UI.QuickInventoryPanel | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.quickInventoryTimer | DolocTown.Timer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.useItemTimer | DolocTown.Timer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunction.item | DolocTown.Item | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionHerbPackage._item | DolocTown.ItemHerbPackage | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionShield._shieldItem | DolocTown.ItemHatShield | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentPhysicalStatus.<ToolLatch>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateTool._endProcess | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateTool.<tool>k__BackingField | DolocTown.ItemTool | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateTool.currentAnimName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateWater.<waterCan>k__BackingField | DolocTown.ItemWaterCan | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBot.<inventory>k__BackingField | DolocTown.LinearInventory | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamGathering.gatherDropItems | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamGathering.gatherEquipmentItems | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllDropItems>d__11.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllDropItems>d__11.<>2__current | DolocTown.DropItem | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllDropItems>d__11.<>7__wrap1 | System.Collections.Generic.IEnumerator`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateStationEnv/RoomEnv/<get_AllDropItems>d__11.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv.lockedDropItems | System.Collections.Generic.HashSet`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<get_AllDropItems>d__11.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<get_AllDropItems>d__11.<>2__current | DolocTown.DropItemBase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<get_AllDropItems>d__11.<>7__wrap1 | System.Collections.Generic.IEnumerator`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<get_AllDropItems>d__11.<>7__wrap2 | System.Collections.Generic.IEnumerator`1<DolocTown.Building> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<get_AllDropItems>d__11.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.<>2__current | DolocTown.DropItemBase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.<>7__wrap1 | System.Collections.Generic.IEnumerator`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetAllDropItemsInRoom>d__12.roomGuid | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetDropItemsNearStation>d__7.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetDropItemsNearStation>d__7.<>2__current | DolocTown.DropItemBase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetDropItemsNearStation>d__7.<>7__wrap2 | System.Collections.Generic.IEnumerator`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetDropItemsNearStation>d__7.<>l__initialThreadId | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetDropItemsNearStation>d__7.<room>5__2 | DolocTown.TemplateRoom | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemEnv/<GetDropItemsNearStation>d__7.station | DolocTown.AutomateBotStation | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemLocker.dropItemLocker | DolocTown.AutomateLocker`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateSystemLocker.lockedDropItems | System.Collections.Generic.HashSet`1<DolocTown.DropItemBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskPickupItem.dropItem | DolocTown.DropItemBase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuilderState`2.CurrentItem | T | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.<TempBuildingList>k__BackingField | DolocTown.LinearInventory | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingBuilder.isTemporaryItem | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip._anchor | UnityEngine.Vector2Int | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.areaEmpty | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.buildingSupport | DolocTown.BuildingSupport | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.coveredRatio | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.currentItem | DolocTown.ItemBuilding | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.doorEmpty | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.groundCheckValid | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.heightValid | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.indicaterRenderer | DolocTown.BuildingBuilderRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.lastPos | UnityEngine.Vector2Int | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingItemBuilderTip.posWS | UnityEngine.Vector3 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingPanelUiState.inventoriesAround | DolocTown.LinearInventory[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Bullet.<ToolLevel>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Case.<inventory>k__BackingField | DolocTown.LinearInventory | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectionBookUiState._currentItem | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectionBookUiState.itemList | System.Collections.Generic.List`1<System.String> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectionBookUiState.itemTypeIndex | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectorDropItem.dropitems | System.Collections.Generic.List`1<DolocTown.DropItemRenderer> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectorDropItem.maxMoveSpeed | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectorDropItem.minMoveSpeed | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectorDropItem.pickDistance | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectorDropItem.radiusReciprocal | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Animal.AnimalHusbandryData.<Output_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Animal.AnimalInfo.<ProduceSpawnEntry>k__BackingField | DolocTown.Config.Item.ItemSpawnEntry | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Animal.FeedInfo.<Id_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.PlantDocumentInfo.<Id_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Automate.AutomateBotFunctionFilling.<DefaultAnimalFeedsType_Ref>k__BackingField | DolocTown.Config.Item.ItemSubTypeInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Automate.AutomateBotFunctionFilling.<DefaultFishFeedsType_Ref>k__BackingField | DolocTown.Config.Item.ItemSubTypeInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Automate.AutomateBotFunctionFilling.<DefaultFuelType_Ref>k__BackingField | DolocTown.Config.Item.ItemSubTypeInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Automate.AutomateBotInfo.<Id_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Automate.AutomateBotPerformanceInfo.<InventoryCapacity>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Building.BuildingExteriorData.<OverrideItemIcon>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Building.BuildingInfo.<BlueprintItem_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| AgentStateBase.get_SupportUseItem | Harmony patch candidate |  | Medium |
| DolocAPI.DevMenuItem_EnterDolocMountain | Harmony patch candidate |  | Risky |
| DolocAPI.DevMenuItem_EnterFarm | Harmony patch candidate |  | Risky |
| DolocAPI.DevMenuItem_EnterOldCity | Harmony patch candidate |  | Risky |
| DolocAPI.DevMenuItem_EnterTeleportPanel | Harmony patch candidate |  | Risky |
| DolocAPI.DevMenuItem_EnterWetland | Harmony patch candidate |  | Risky |
| DolocAPI.RefreshQuickInventory | Harmony patch candidate | System.Int32 index | Medium |
| DolocAPI.RefreshQuickInventory | Harmony patch candidate |  | Medium |
| DolocAPI.RefreshQuickInventorySelected | Harmony patch candidate |  | Medium |
| DolocInputSource/IBaseInputActions.OnDestroyItem | Harmony patch candidate | UnityEngine.InputSystem.InputAction/CallbackContext context | Medium |
| DolocInputSource/INormalInputActions.OnUseItem | Harmony patch candidate | UnityEngine.InputSystem.InputAction/CallbackContext context | Medium |
| DolocInputSource/INormalInputActions.OnUseTool | Harmony patch candidate | UnityEngine.InputSystem.InputAction/CallbackContext context | Medium |
| DolocInputSource/NormalInputActions.get_UseItem | Harmony patch candidate |  | Medium |
| DolocInputSource/NormalInputActions.get_UseTool | Harmony patch candidate |  | Medium |
| DolocTown.AgentControllerState.UseItem | Harmony patch candidate | System.Boolean force | Medium |
| DolocTown.AgentControllerState.UseItemContinues | Harmony patch candidate | System.Single dt | Risky |
| DolocTown.AgentControllerState.UseTool | Harmony patch candidate | System.Boolean force | Medium |
| DolocTown.AgentControllerState.UseToolContinues | Harmony patch candidate |  | Risky |
| DolocTown.AgentControllerState.UseToolOrItem | Harmony patch candidate | System.Single dt | Risky |
| DolocTown.AgentControllerState/<>c.<UseTool>b__75_0 | Harmony patch candidate | DolocTown.AgentStateFishingPull s | Risky |
| DolocTown.AgentSkillManager.UseSkillFromItemName | Harmony patch candidate | System.String itemName; DolocTown.AgentSkillManager/TriggerType type | Medium |
| DolocTown.AgentStateFishing.get_SupportUseItem | Harmony patch candidate |  | Medium |
| DolocTown.AgentStateHit.get_SupportUseItem | Harmony patch candidate |  | Medium |
| DolocTown.AgentStateIdle.get_SupportUseItem | Harmony patch candidate |  | Medium |
| DolocTown.AgentStateInteract.get_SupportUseItem | Harmony patch candidate |  | Medium |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IItemHelper.Experimental` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocAPI/<Command_SubmitItemToNpcByInfo>d__946 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Drone.DroneFunctionProtoGarbageCombustionEngine | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncGarbageShredder | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.BoxSkin | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.CfgCountItem | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.CfgRangedItem | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.CfgRewardProto | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.CountItemListInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.EatingEffectInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.FoodEffect | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemAutomationTypeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunction | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionAgentEquipment | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionAnimalPackage | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionAnimation | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionAnimationBase | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionAutomateBot | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionBase | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionBattery | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionBottle | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionBox | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionBuilding | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionBuildingExterior | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionConstructController | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionCrop | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionDemolitionTool | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionDoorplate | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionDroneAssist | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionDroneChip | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionDroneEngine | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionDroneStructure | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionDroneWeapon | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionEdenFruit | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionEquipment | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionFarmingGun | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionFertilizer | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionFilm | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionFishFry | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionFishingRod | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionFishRoe | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionFood | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionGeneCapsule | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionHat | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionHatBase | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionHatShield | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionHerbPackage | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionMissile | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionMotorKey | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionPassive | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionPassiveBase | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IItemHelper
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- Write operations need save-risk labels and fallback behavior.
