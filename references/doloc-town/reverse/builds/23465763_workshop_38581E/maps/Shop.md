# DolocTown Shop / Merchant / Price API Map

## Build

- Steam build: 23465763
- Branch: workshop
- Assembly-CSharp hash: 38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228
- Research date: 2026-05-29
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Map shop tables and price calculation.
- Support read-only item availability helpers.
- Defer dynamic shop patching until content format is confirmed.

## Decompiled scope

- Matched types: 53
- Matched methods: 786
- Matched fields: 342
- Matched properties: 279
- Matched events: 0
- Matched call edges: 2800
- Matched strings: 491
- Raw indexes: `maps/index/Shop-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.StoreUiState | class | Confirmed | visibility=public; methods=49; fields=8 |
| DolocTown.Store | class | Confirmed | visibility=public; methods=48; fields=8 |
| DolocTown.Config.Store.StoreInfo | class | Confirmed | visibility=public; methods=30; fields=11 |
| DolocTown.ExchangeStore | class | Confirmed | visibility=public; methods=36; fields=5 |
| DolocTown.Config.Store.ExchangeStoreItemData | class | Confirmed | visibility=public; methods=30; fields=10 |
| DolocTown.Config.Store.ExchangeStoreInfo | class | Confirmed | visibility=public; methods=22; fields=9 |
| DolocTown.Config.Store.StoreItemSeasonData | class | Confirmed | visibility=public; methods=21; fields=6 |
| DolocTown.Config.SteamWorkshopUploader | class | Confirmed | visibility=public; methods=15; fields=10 |
| DolocTown.Config.Store.StoreItemUnlockInfo | class | Confirmed | visibility=public; methods=17; fields=6 |
| DolocTown.Config.Mod.ModStoreExtensionInfo | class | Confirmed | visibility=public; methods=13; fields=5 |
| DolocTown.Config.Mod.ModExchangeStoreExtensionInfo | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.Config.Store.StoreScaleOfType | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.StoreManager | class | Confirmed | visibility=public; methods=10; fields=6 |
| DolocTown.Config.Store.StoreItemListInfo | class | Confirmed | visibility=public; methods=11; fields=4 |
| DolocTown.Config.Store.StorePriceScaleData | class | Confirmed | visibility=public; methods=11; fields=4 |
| DolocTown.Config.Item.RangedItemWithPriceThreshold | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Store.StoreItemSpawnData | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Store.StorePriceScaleInfo | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.ExchangeStoreRecipe | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.UI.StoreItemData | class | Confirmed | visibility=public; methods=3; fields=10 |
| DolocTown.UI.StorePanel | class | Confirmed | visibility=public; methods=10; fields=2 |
| DolocTown.Config.Store.StoreSeasonData | class | Confirmed | visibility=public; methods=9; fields=2 |
| DolocTown.UI.StoreQuantitySubmitPanel | class | Confirmed | visibility=public; methods=4; fields=7 |
| DolocTown.Config.Store.TbExchangeStore | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Store.TbStore | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Store.TbStoreItemList | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Store.TbStoreItemUnlock | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Store.TbStorePriceScale | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.StoreUiState/<>c__DisplayClass35_0 | class | Confirmed | visibility=private; methods=2; fields=7 |
| DolocTown.UI.StoreItemSlot | class | Confirmed | visibility=public; methods=2; fields=7 |
| DolocTown.UI.StoreWidget | class | Confirmed | visibility=public; methods=8; fields=1 |
| DolocTown.StoreUiState/<>c__DisplayClass42_0 | class | Confirmed | visibility=private; methods=2; fields=6 |
| DolocTown.Config.WorkshopUploadResult | class | Confirmed | visibility=public; methods=4; fields=3 |
| DolocTown.StoreUiState/<>c__DisplayClass42_1 | class | Confirmed | visibility=private; methods=2; fields=4 |
| DolocTown.Config.Mod.TbModExchangeStoreExtension | class | Confirmed | visibility=public; methods=4; fields=1 |
| DolocTown.Config.Mod.TbModStoreExtension | class | Confirmed | visibility=public; methods=4; fields=1 |
| DolocTown.Config.WorkshopUploadPlan | class | Confirmed | visibility=public; methods=3; fields=2 |
| DolocTown.NodeCanvas.DialogueTask_UnlockStoreItem | class | Confirmed | visibility=public; methods=3; fields=2 |
| DolocTown.Store/<>c | class | Confirmed | visibility=private; methods=3; fields=2 |
| DolocTown.StoreUiState/<>c | class | Confirmed | visibility=private; methods=3; fields=2 |
| DolocTown.StoreUiState/<>c__DisplayClass35_1 | class | Confirmed | visibility=private; methods=2; fields=3 |
| DolocTown.UI.StoreQuantitySubmitData | class | Confirmed | visibility=public; methods=1; fields=4 |
| DolocTown.Config.SteamWorkshopUploader/<>c__DisplayClass27_0 | class | Confirmed | visibility=private; methods=2; fields=2 |
| DolocTown.Config.SteamWorkshopUploader/WorkshopL10nText | struct | Confirmed | visibility=private; methods=1; fields=3 |
| DolocTown.Config.Store.StoreItemUnlockType | enum | Confirmed | visibility=public; methods=0; fields=4 |
| DolocTown.IStore | interface | Confirmed | visibility=public; methods=4; fields=0 |
| DolocTown.StoreUiState/<>c__DisplayClass43_0 | class | Confirmed | visibility=private; methods=2; fields=2 |
| DolocTown.Config.WorkshopUploadMode | enum | Confirmed | visibility=public; methods=0; fields=3 |
| DolocTown.StoreItemRef | struct | Confirmed | visibility=public; methods=1; fields=2 |
| DolocTown.StoreUiState/FocusMode | enum | Confirmed | visibility=private; methods=0; fields=3 |
| DolocTown.UI.StoreSubmitType | enum | Confirmed | visibility=public; methods=0; fields=3 |
| DolocTown.Config.ModWorkshopInfo | class | Confirmed | visibility=public; methods=1; fields=1 |
| DolocTown.StoreQuantitySubmitUiState | class | Confirmed | visibility=public; methods=1; fields=0 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.Command_OpenExchangeStore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenStore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RefreshStore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.FastButton_OpenSeedStore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.OpenStore | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshStore | candidate lifecycle or hook point | Medium |
| DolocAPI/<>c.<FastButton_OpenSeedStore>b__1003_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass680_0.<OpenStore>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass927_0.<Command_OpenExchangeStore>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.StaticTextInfo.get_UiModOpenWorkshop | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_UiModOpenWorkshop_l10n_key | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.set_UiModOpenWorkshop | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.TbStaticText.get_UiModOpenWorkshop | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModInfo.TryLoadWorkshopInfo | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModManager.CacheLocalModUploadPlan | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModManager.ResolveLocalModUploadPlan | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModManager.TryGetCachedLocalModUploadPlan | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModManager.TryGetResolvedLocalModUploadPlan | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModManager.UploadLocalMod | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModManager/<>c__DisplayClass82_0.<ProcessNextLocalModUploadPlanRequest>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModManager/<>c__DisplayClass85_0.<UploadLocalMod>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.SteamWorkshopUploader.BuildTextsToUpload | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.SteamWorkshopUploader.CompleteUpload | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.SteamWorkshopUploader.get_IsResolvingUploadPlan | candidate lifecycle or hook point | Medium |
| DolocTown.Config.SteamWorkshopUploader.get_IsUploading | candidate lifecycle or hook point | Medium |
| DolocTown.Config.SteamWorkshopUploader.ResolveUploadPlan | candidate lifecycle or hook point | Medium |
| DolocTown.Config.SteamWorkshopUploader.UploadMod | candidate lifecycle or hook point | Medium |
| DolocTown.Config.SteamWorkshopUploader.UploadUpdate | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Store.ExchangeStoreInfo.get_RefreshImmediatly | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Store.ExchangeStoreInfo.set_RefreshImmediatly | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Store.ExchangeStoreItemData.get_UnlockOnInit | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Store.StoreInfo.get_InitMoney | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Store.StoreInfo.get_InitMoneyRange | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Store.StoreInfo.set_InitMoney | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Store.StoreInfo.set_InitMoneyRange | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ExchangeStore.InitItems | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ExchangeStore.Refresh | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.ArchiveOperationCity.RefreshAllStores | candidate lifecycle or hook point | Medium |
| DolocTown.GlobalBuilderState.RefreshOperationTip | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.IStore.Refresh | candidate lifecycle or hook point | Medium |
| DolocTown.ModUiState.OpenWorkshop | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ModUiState.ShowUploadResultMessage | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ModUiState/<>c__DisplayClass31_0.<RefreshCurrentModUploadPlan>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ModUiState/<>c__DisplayClass43_0.<UploadMod>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ModUiState/<>c__DisplayClass43_0.<UploadMod>b__2 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.HandleExchangeStoreStartUpArgs | candidate lifecycle or hook point | Medium |
| DolocTown.RecipePanelUiState/<>c__DisplayClass75_0.<HandleExchangeStoreStartUpArgs>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState/<>c__DisplayClass75_0.<HandleExchangeStoreStartUpArgs>b__1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Store.Refresh | candidate lifecycle or hook point | Medium |
| DolocTown.Store.RefreshItems | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Store.RefreshMoney | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.StoreManager.AfterLoadData | candidate lifecycle or hook point | Medium |
| DolocTown.StoreManager.InitCache | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.StoreManager.RefreshAllStores | candidate lifecycle or hook point | Medium |
| DolocTown.StoreUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.StoreUiState.HandleStartUpArgs | candidate lifecycle or hook point | Medium |
| DolocTown.StoreUiState.OnUiUpdate | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.StoreUiState.RefreshTip | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.StoreUiState.RefreshView | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.MapPanel.RefreshOperationTip | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.StorePanel.OnStartShow | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.StorePanel.RefreshView | candidate lifecycle or hook point | Medium |
| DolocTown.UI.StoreQuantitySubmitPanel.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.StoreQuantitySubmitPanel.RefreshView | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.StoreWidget.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.StoreWidget.OnInitSlot | candidate lifecycle or hook point | Risky: non-public or generated path |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.GetItemBuyingPrice(System.String itemName) | direct call candidate | Medium |
| DolocAPI.GetItemSellingPrice(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.GetItemSellingPrice(System.String itemName) | direct call candidate | Medium |
| DolocAPI.OpenStore(System.String storeId) | direct call candidate | Medium |
| DolocAPI.RefreshStore(System.String storeName) | direct call candidate | Medium |
| DolocAPI.UnlockStoreItem(System.String storeName; System.String itemName) | direct call candidate | Medium |
| DolocTown.AutomateBotRenderer.RestoreMove(UnityEngine.Vector2 velocityDir) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Animal.AnimalInfo.get_AdultPrice() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Animal.AnimalInfo.get_ChildPrice() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Animal.AnimalLevelData.get_Price() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncParkingApron.get_PriceIncreasedMonths() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncParkingApron.get_StoreName() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncParkingApron.get_StoreName_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemInfo.get_BuyingPrice() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemInfo.get_SellingPrice() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.RangedItemWithPriceThreshold.DeserializeRangedItemWithPriceThreshold(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Item.RangedItemWithPriceThreshold.get_PriceThreshold() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.RangedItemWithPriceThreshold.get_RangedItem() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.RangedItemWithPriceThreshold.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.RangedItemWithPriceThreshold.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.RangedItemWithPriceThreshold.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.RangedItemWithPriceThreshold.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_DropoffBoxPriceIncreased() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_DropoffBoxPriceIncreased_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_ItemTipBasicsPrice() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_ItemTipBasicsPrice_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_ItemTipPrice() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_ItemTipPrice_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreItemNotSaleable() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreItemNotSaleable_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreItemNotSaleableComment() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreItemNotSaleableComment_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreItemSoldOut() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreItemSoldOut_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreItemTipPlantbasinLocked() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreItemTipPlantbasinLocked_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreLackOfAsset() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreLackOfAsset_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StorePlayerMoneyNotEnough() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StorePlayerMoneyNotEnough_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitBuying() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitBuying_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitCountInBack() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitCountInBack_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitCurrentMoneyBuying() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitCurrentMoneyBuying_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitCurrentMoneySelling() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitCurrentMoneySelling_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitSelling() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitSelling_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitTotalMoneyBuying() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitTotalMoneyBuying_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitTotalMoneySelling() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitTotalMoneySelling_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitUnitPrice() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreQuantitySubmitUnitPrice_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreSoldOutIcon() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreSoldOutIcon_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreStoreMoneyNotEnough() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreStoreMoneyNotEnough_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreUpgradeConfirm() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreUpgradeConfirm_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreUpgradeEmptyInfo() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreUpgradeEmptyInfo_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreUpgradePrice() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_StoreUpgradePrice_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_TeleportBuyTicket() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_TeleportBuyTicket_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiModOpenWorkshop() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiModOpenWorkshop_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiModSourceWorkshop() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiModSourceWorkshop_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiOperationSell() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiOperationSell_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipBuy() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipBuy_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipBuyAllGamepad() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipBuyAllGamepad_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipBuyBackpack() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipBuyBackpack_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipBuyBackpackInfo() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipBuyBackpackInfo_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipBuyConfirm() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipBuyConfirm_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipBuyOne() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipBuyOne_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipSell() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipSell_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipSellAll() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipSellAll_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipSellConfirm() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipSellConfirm_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipSellOne() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipSellOne_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_DropoffBoxPriceIncreased() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_ItemTipBasicsPrice() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_ItemTipPrice() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_StoreItemNotSaleable() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_StoreItemNotSaleableComment() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_StoreItemSoldOut() | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| DolocTown.Config.Animal.AnimalLevelData.<Price>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncParkingApron.<PriceIncreasedMonths>k__BackingField | System.Collections.Generic.List`1<System.Int32> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncParkingApron.<StoreName_Ref>k__BackingField | DolocTown.Config.Store.StoreInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncParkingApron.<StoreName>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemInfo.<BuyingPrice>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemInfo.<SellingPrice>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.RangedItemWithPriceThreshold.<PriceThreshold>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.RangedItemWithPriceThreshold.<RangedItem>k__BackingField | DolocTown.RangedItem | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<DropoffBoxPriceIncreased_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<DropoffBoxPriceIncreased>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<ItemTipBasicsPrice_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<ItemTipBasicsPrice>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<ItemTipPrice_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<ItemTipPrice>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreItemNotSaleable_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreItemNotSaleable>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreItemNotSaleableComment_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreItemNotSaleableComment>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreItemSoldOut_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreItemSoldOut>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreItemTipPlantbasinLocked_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreItemTipPlantbasinLocked>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreLackOfAsset_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreLackOfAsset>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StorePlayerMoneyNotEnough_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StorePlayerMoneyNotEnough>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitBuying_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitBuying>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitCountInBack_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitCountInBack>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitCurrentMoneyBuying_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitCurrentMoneyBuying>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitCurrentMoneySelling_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitCurrentMoneySelling>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitSelling_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitSelling>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitTotalMoneyBuying_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitTotalMoneyBuying>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitTotalMoneySelling_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitTotalMoneySelling>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitUnitPrice_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreQuantitySubmitUnitPrice>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreSoldOutIcon_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreSoldOutIcon>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreStoreMoneyNotEnough_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreStoreMoneyNotEnough>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreUpgradeConfirm_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreUpgradeConfirm>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreUpgradeEmptyInfo_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreUpgradeEmptyInfo>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreUpgradePrice_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<StoreUpgradePrice>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<TeleportBuyTicket_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<TeleportBuyTicket>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModOpenWorkshop_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModOpenWorkshop>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModSourceWorkshop_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiModSourceWorkshop>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationSell_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationSell>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipBuy_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipBuy>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipBuyAllGamepad_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipBuyAllGamepad>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipBuyBackpack_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipBuyBackpack>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipBuyBackpackInfo_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipBuyBackpackInfo>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipBuyConfirm_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipBuyConfirm>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipBuyOne_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipBuyOne>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipSell_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipSell>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipSellAll_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipSellAll>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipSellConfirm_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipSellConfirm>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipSellOne_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipSellOne>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModExchangeStoreExtensionInfo.<ExtraItems>k__BackingField | DolocTown.Config.Store.ExchangeStoreItemData[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModExchangeStoreExtensionInfo.<Id_Ref>k__BackingField | DolocTown.Config.Store.ExchangeStoreInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModExchangeStoreExtensionInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModStoreExtensionInfo.<ExtraItems>k__BackingField | DolocTown.Config.Store.StoreItemSeasonData[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModStoreExtensionInfo.<Id_Ref>k__BackingField | DolocTown.Config.Store.StoreInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModStoreExtensionInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.TbModExchangeStoreExtension._dataList | System.Collections.Generic.List`1<DolocTown.Config.Mod.ModExchangeStoreExtensionInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.TbModStoreExtension._dataList | System.Collections.Generic.List`1<DolocTown.Config.Mod.ModStoreExtensionInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.<workshopId>k__BackingField | System.UInt64 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.WorkshopInfoFileName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.pendingLocalModUploadPlanCallbacks | System.Collections.Generic.Dictionary`2<System.String,System.Collections.Generic.List`1<System.Action`1<DolocTown.Config.WorkshopUploadPlan>>> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.uploader | DolocTown.Config.SteamWorkshopUploader | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.WorkshopInfoFileName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Recipe.DishInfo.<OutputItemByThresholds>k__BackingField | System.Collections.Generic.List`1<DolocTown.Config.Item.RangedItemWithPriceThreshold> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Recipe.DishInfo.orderedItems | DolocTown.Config.Item.RangedItemWithPriceThreshold[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<ItemsCanNotBuyback_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<ItemsCanNotBuyback>k__BackingField | System.String[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<SeedStoreIds_Ref>k__BackingField | DolocTown.Config.Store.StoreInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<SeedStoreIds>k__BackingField | System.String[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.SteamWorkshopUploader.createItemResult | Steamworks.CallResult`1<Steamworks.CreateItemResult_t> | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| DolocAPI.Command_RefreshStore | Harmony patch candidate | System.String storeName | Risky |
| DolocAPI.FastButton_OpenSeedStore | Harmony patch candidate |  | Risky |
| DolocAPI.RefreshStore | Harmony patch candidate | System.String storeName | Medium |
| DolocAPI/<>c.<FastButton_OpenSeedStore>b__1003_0 | Harmony patch candidate | DolocTown.StoreUiState state | Risky |
| DolocTown.Config.ModInfo.TryLoadWorkshopInfo | Harmony patch candidate | System.String rootPath; System.UInt64& workshopId | Medium |
| DolocTown.Config.ModManager.CacheLocalModUploadPlan | Harmony patch candidate | DolocTown.Config.ModInfo modInfo; DolocTown.Config.WorkshopUploadPlan plan | Risky |
| DolocTown.Config.ModManager.ResolveLocalModUploadPlan | Harmony patch candidate | DolocTown.Config.ModInfo modInfo; System.Action`1<DolocTown.Config.WorkshopUploadPlan> onResolved | Medium |
| DolocTown.Config.ModManager.TryGetCachedLocalModUploadPlan | Harmony patch candidate | DolocTown.Config.ModInfo modInfo; DolocTown.Config.WorkshopUploadPlan& plan | Risky |
| DolocTown.Config.ModManager.TryGetResolvedLocalModUploadPlan | Harmony patch candidate | DolocTown.Config.ModInfo modInfo; DolocTown.Config.WorkshopUploadPlan& plan | Medium |
| DolocTown.Config.ModManager.UploadLocalMod | Harmony patch candidate | DolocTown.Config.ModInfo modInfo; DolocTown.Config.WorkshopUploadPlan plan; System.Action`1<DolocTown.Config.WorkshopUploadResult> onCompleted | Medium |
| DolocTown.Config.ModManager/<>c__DisplayClass82_0.<ProcessNextLocalModUploadPlanRequest>b__0 | Harmony patch candidate | DolocTown.Config.WorkshopUploadPlan plan | Risky |
| DolocTown.Config.ModManager/<>c__DisplayClass85_0.<UploadLocalMod>b__0 | Harmony patch candidate | DolocTown.Config.WorkshopUploadResult result | Risky |
| DolocTown.Config.SteamWorkshopUploader.BuildTextsToUpload | Harmony patch candidate | DolocTown.Config.ModInfo mod | Risky |
| DolocTown.Config.SteamWorkshopUploader.CompleteUpload | Harmony patch candidate | System.Boolean success; System.UInt64 workshopId | Risky |
| DolocTown.Config.SteamWorkshopUploader.get_IsResolvingUploadPlan | Harmony patch candidate |  | Medium |
| DolocTown.Config.SteamWorkshopUploader.get_IsUploading | Harmony patch candidate |  | Medium |
| DolocTown.Config.SteamWorkshopUploader.ResolveUploadPlan | Harmony patch candidate | DolocTown.Config.ModInfo mod; System.Action`1<DolocTown.Config.WorkshopUploadPlan> onResolved | Medium |
| DolocTown.Config.SteamWorkshopUploader.UploadMod | Harmony patch candidate | DolocTown.Config.ModInfo mod; DolocTown.Config.WorkshopUploadPlan plan; System.Action`1<DolocTown.Config.WorkshopUploadResult> onCompleted | Medium |
| DolocTown.Config.SteamWorkshopUploader.UploadUpdate | Harmony patch candidate | System.UInt64 workshopId; DolocTown.Config.ModInfo mod | Risky |
| DolocTown.Config.Store.ExchangeStoreInfo.get_RefreshImmediatly | Harmony patch candidate |  | Medium |
| DolocTown.Config.Store.ExchangeStoreInfo.set_RefreshImmediatly | Harmony patch candidate | System.Boolean value | Risky |
| DolocTown.Config.Store.ExchangeStoreItemData.get_UnlockOnInit | Harmony patch candidate |  | Medium |
| DolocTown.Config.Store.StoreInfo.get_InitMoney | Harmony patch candidate |  | Medium |
| DolocTown.Config.Store.StoreInfo.get_InitMoneyRange | Harmony patch candidate |  | Medium |
| DolocTown.Config.Store.StoreInfo.set_InitMoney | Harmony patch candidate | System.Int32 value | Risky |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IShopHelper.Experimental` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocTown.Config.Item.RangedItemWithPriceThreshold | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.ModExchangeStoreExtensionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.ModStoreExtensionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.TbModExchangeStoreExtension | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.TbModStoreExtension | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.ModWorkshopInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.SteamWorkshopUploader | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.SteamWorkshopUploader/<>c__DisplayClass27_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.SteamWorkshopUploader/WorkshopL10nText | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.ExchangeStoreInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.ExchangeStoreItemData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.StoreInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.StoreItemListInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.StoreItemSeasonData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.StoreItemSpawnData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.StoreItemUnlockInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.StoreItemUnlockType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.StorePriceScaleData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.StorePriceScaleInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.StoreScaleOfType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.StoreSeasonData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.TbExchangeStore | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.TbStore | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.TbStoreItemList | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.TbStoreItemUnlock | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Store.TbStorePriceScale | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.WorkshopUploadMode | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.WorkshopUploadPlan | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.WorkshopUploadResult | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.StoreItemData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.StoreQuantitySubmitData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IShopHelper
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- Official shop ID docs should be treated as the content-pack source of truth.
