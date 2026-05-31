# DolocTown UI / Menu / IMGUI API Map

## Build

- Steam build: 23465763
- Branch: workshop
- Assembly-CSharp hash: 38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228
- Research date: 2026-05-29
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Find safe draw/update hooks for overlays.
- Map menu open/close lifecycle.
- Separate IMGUI overlays from game UI objects.

## Decompiled scope

- Matched types: 1433
- Matched methods: 15606
- Matched fields: 7685
- Matched properties: 5352
- Matched events: 0
- Matched call edges: 69173
- Matched strings: 10109
- Raw indexes: `maps/index/UI-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.Config.Localization.StaticTextInfo | class | Confirmed | visibility=public; methods=2227; fields=1481 |
| DolocTown.Config.Localization.TbStaticText | class | Confirmed | visibility=public; methods=743; fields=1 |
| DolocTown.Config.Building.BuildingInfo | class | Confirmed | visibility=public; methods=113; fields=44 |
| DolocTown.Equipment | class | Confirmed | visibility=public; methods=121; fields=17 |
| DolocTown.Building | class | Confirmed | visibility=public; methods=94; fields=16 |
| DolocTown.Config.Equipment.EquipmentInfo | class | Confirmed | visibility=public; methods=74; fields=19 |
| DolocTown.UI.DolocPagedGridUI`2 | class | Confirmed | visibility=public; methods=63; fields=20 |
| DolocTown.ContainerBaseUiState | class | Confirmed | visibility=public; methods=65; fields=16 |
| DolocTown.UI.CityMapPanel | class | Confirmed | visibility=public; methods=42; fields=36 |
| DolocTown.UI.DolocPagedLinearUI`2 | class | Confirmed | visibility=public; methods=57; fields=21 |
| DolocTown.UI.DolocUiObject | class | Confirmed | visibility=public; methods=72; fields=4 |
| DolocTown.UI.DolocUIPanel | class | Confirmed | visibility=public; methods=50; fields=22 |
| DolocTown.UI.DolocNavigationButton | class | Confirmed | visibility=public; methods=43; fields=26 |
| DolocTown.RecipePanelUiState | class | Confirmed | visibility=public; methods=48; fields=20 |
| DolocTown.UI.QuickInventoryPanel | class | Confirmed | visibility=public; methods=53; fields=12 |
| DolocTown.UI.RebindActionSlot | class | Confirmed | visibility=public; methods=50; fields=15 |
| DolocTown.UI.SettingPanel | class | Confirmed | visibility=public; methods=41; fields=24 |
| DolocUiSystem | class | Confirmed | visibility=public; methods=49; fields=16 |
| DolocTown.UI.DolocGridUI`1 | class | Confirmed | visibility=public; methods=50; fields=12 |
| DolocTown.CollectionBookUiState | class | Confirmed | visibility=public; methods=50; fields=11 |
| DolocTown.UI.DolocButtonComponent | class | Confirmed | visibility=public; methods=34; fields=27 |
| DolocTown.BuilderState`2 | class | Confirmed | visibility=public; methods=49; fields=10 |
| DolocTown.StorageShelfUiState | class | Confirmed | visibility=public; methods=52; fields=7 |
| DolocTown.GlobalBuilderState | class | Confirmed | visibility=public; methods=44; fields=14 |
| DolocTown.EquipmentBarUiState | class | Confirmed | visibility=public; methods=53; fields=4 |
| DolocTown.GameData.AgentEquipmentManager | class | Confirmed | visibility=public; methods=50; fields=7 |
| DolocTown.StoreUiState | class | Confirmed | visibility=public; methods=49; fields=8 |
| DolocTown.EquipmentPanelUiState | class | Confirmed | visibility=public; methods=44; fields=9 |
| DolocTown.UI.OperationTipMulti | class | Confirmed | visibility=public; methods=27; fields=26 |
| DolocTown.IEquipmentHost | interface | Confirmed | visibility=public; methods=50; fields=0 |
| DolocTown.ModUiState | class | Confirmed | visibility=public; methods=46; fields=4 |
| DolocTown.BuildingBuilder | class | Confirmed | visibility=public; methods=32; fields=17 |
| DolocTown.UI.RecipeData | struct | Confirmed | visibility=public; methods=25; fields=24 |
| DolocTown.UI.BasicTipGroup | class | Confirmed | visibility=public; methods=29; fields=17 |
| DolocTown.UI.EquipmentData | struct | Confirmed | visibility=public; methods=25; fields=21 |
| DolocTown.Config.Platform.BuildingSupportInfo | class | Confirmed | visibility=public; methods=31; fields=13 |
| DolocTown.UI.AccessoriesBar | class | Confirmed | visibility=public; methods=31; fields=13 |
| DolocTown.Config.Equipment.EquipmentFuncParkingApron | class | Confirmed | visibility=public; methods=31; fields=11 |
| DolocTown.UI.RebindActionUI | class | Confirmed | visibility=public; methods=31; fields=11 |
| DolocTown.UI.TechTreeWidget | class | Confirmed | visibility=public; methods=23; fields=19 |
| DolocTown.EquipmentWorker | class | Confirmed | visibility=public; methods=32; fields=8 |
| DolocTown.UI.InventoryPanel | class | Confirmed | visibility=public; methods=35; fields=5 |
| DolocTown.EquipmentRenderer | class | Confirmed | visibility=public; methods=31; fields=8 |
| DolocTown.FishTankUiState | class | Confirmed | visibility=public; methods=32; fields=7 |
| DolocTown.UI.ContainerWidget | class | Confirmed | visibility=public; methods=25; fields=14 |
| DolocTown.Config.Equipment.LampInfo | class | Confirmed | visibility=public; methods=27; fields=11 |
| DolocTown.Config.Localization.LocalizationInfo | class | Confirmed | visibility=public; methods=27; fields=11 |
| DolocTown.TechTreeUiState | class | Confirmed | visibility=public; methods=34; fields=4 |
| DolocTown.DolocGameUiState | class | Confirmed | visibility=public; methods=32; fields=4 |
| DolocTown.FactionMissionUiState | class | Confirmed | visibility=public; methods=29; fields=7 |
| DolocTown.IBuildingHost | interface | Confirmed | visibility=public; methods=36; fields=0 |
| DolocTown.UI.BubbleDialoguePanel | class | Confirmed | visibility=public; methods=22; fields=14 |
| DolocTown.UI.EnvOptimizerPanel | class | Confirmed | visibility=public; methods=21; fields=15 |
| DolocTown.UI.MapPanel | class | Confirmed | visibility=public; methods=26; fields=10 |
| DolocTown.Config.Equipment.ChairInfo | class | Confirmed | visibility=public; methods=25; fields=10 |
| DolocTown.UI.InputNameBox | class | Confirmed | visibility=public; methods=23; fields=12 |
| BoxUiState | class | Confirmed | visibility=public; methods=33; fields=1 |
| DolocTown.Equipment/WeatherDecoratorManager | class | Confirmed | visibility=public; methods=28; fields=6 |
| RedSaw.UI.RedSawButtonBase | class | Confirmed | visibility=public; methods=26; fields=8 |
| DolocTown.Config.Equipment.EquipmentFuncCase | class | Confirmed | visibility=public; methods=24; fields=9 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| BoxUiState.get_isBoxUsedUp | candidate lifecycle or hook point | Risky: non-public or generated path |
| BoxUiState.HandleStartUpArgs | candidate lifecycle or hook point | Medium |
| BoxUiState.RefreshColorTag | candidate lifecycle or hook point | Risky: non-public or generated path |
| BoxUiState.ShowBoxUsedUpWarning | candidate lifecycle or hook point | Risky: non-public or generated path |
| BoxUiState.UpdateBoxTitle | candidate lifecycle or hook point | Risky: non-public or generated path |
| DevHelper.InitDevMenuItems | candidate lifecycle or hook point | Risky: non-public or generated path |
| DevHelper.InitFastButtons | candidate lifecycle or hook point | Risky: non-public or generated path |
| DevHelper/<>c__DisplayClass12_0.<InitFastButtons>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DevHelper/<>c__DisplayClass13_0.<InitDevMenuItems>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenAnimalPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenBoardMissionPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenBuildingPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenCalendarPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenDemoEndPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenFactionPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenSeedUnLockPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenTutorialPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenWeatherReportPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.DevMenuItem_EnterDolocMountain | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.DevMenuItem_EnterFarm | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.DevMenuItem_EnterOldCity | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.DevMenuItem_EnterTeleportPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.DevMenuItem_EnterWetland | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.EnterUI | candidate lifecycle or hook point | Medium |
| DolocAPI.EnterUI | candidate lifecycle or hook point | Medium |
| DolocAPI.FastButton_OpenFactionPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.FastButton_OpenSeedStore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.FastButton_ReloadConfigTables | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.FastButton_ReloadMods | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.get_gameLoadingTip | candidate lifecycle or hook point | Medium |
| DolocAPI.OpenPanel | candidate lifecycle or hook point | Medium |
| DolocAPI.OpenPermissionViewChipDoc | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.OpenPermissionViewPlantDoc | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.OpenSubmitSingleItemPanel | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshQuickInventory | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshQuickInventory | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshQuickInventorySelected | candidate lifecycle or hook point | Medium |
| DolocAPI.set_gameLoadingTip | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.SetResidentUiInteractable | candidate lifecycle or hook point | Medium |
| DolocAPI/<>c.<Command_OpenAnimalPanel>b__977_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c.<FastButton_OpenFactionPanel>b__1004_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c.<FastButton_OpenSeedStore>b__1003_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass673_0.<OpenSubmitSingleItemPanel>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass680_0.<OpenStore>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass696_0.<OpenMap>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass927_0.<Command_OpenExchangeStore>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass961_0.<Command_OpenTutorialPanel>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass966_0.<Command_OpenFactionPanel>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBuilder.EnterHelpState | candidate lifecycle or hook point | Medium |
| DolocBuilder.ExitHelpState | candidate lifecycle or hook point | Medium |
| DolocBuilder.Init | candidate lifecycle or hook point | Medium |
| DolocBuilder.LateUpdate | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocInfoPanelBox.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AchievementSystem._StartMission | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AchievementSystem.LoadAchievementMissions | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentControllerState.EnterUICheck | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentEquipmentFunction.AfterEnterRoom | candidate lifecycle or hook point | Medium |
| DolocTown.AgentEquipmentFunction.InitializeFunctionTypeCache | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentEquipmentFunction.UpdatePerTu | candidate lifecycle or hook point | Medium |
| DolocTown.AgentEquipmentFunction.UpdatePerTuNoRender | candidate lifecycle or hook point | Medium |
| DolocTown.AgentEquipmentFunctionAreaSpeedUp.AfterEnterRoom | candidate lifecycle or hook point | Medium |
| DolocTown.AgentEquipmentFunctionLightningConductor.UpdatePerTu | candidate lifecycle or hook point | Medium |
| DolocTown.AllDocumentUiState.OnUiUpdate | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMap.LoadBuildingStairs | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMap/<>c.<LoadBuildingStairs>b__17_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMap/<>c.<LoadBuildingStairs>b__17_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalMapForBuilding.Refresh | candidate lifecycle or hook point | Medium |
| DolocTown.AnimalPanelUiState.HandleStartUpArgs | candidate lifecycle or hook point | Medium |
| DolocTown.AnimalPanelUiState.OnPause | candidate lifecycle or hook point | Medium |
| DolocTown.AnimalPanelUiState.OnUiUpdate | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AnimalStation/<>c__DisplayClass4_0.<OnInteract>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.<__Init>b__8_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.<__Init>b__8_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.OnStartHide | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.OnStartShow | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.Pause | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBot.AfterLoadEquipment | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBot.EnterBuilding | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBot.EnterMainFarm | candidate lifecycle or hook point | Medium |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| AgentStateBase.get_SupportScrollQuickInventoryUI() | direct call candidate | Medium; needs instance source |
| BoxUiState.HandleStartUpArgs(DolocTown.IContainer box; System.Action onExit) | direct call candidate | Medium; needs instance source |
| DevHelper.DevMenuCallback(System.String menuItemName) | direct call candidate | Medium; needs instance source |
| DevHelper.get_DevMenuItemTitles() | direct call candidate | Medium; needs instance source |
| DevHelper.OnGUI() | direct call candidate | Medium; needs instance source |
| DolocAPI.CalcUiPopPosition(UnityEngine.Transform transform; System.Single rate) | direct call candidate | Medium |
| DolocAPI.Command_ExtendBuilding() | direct call candidate | Medium |
| DolocAPI.EnterUI() | direct call candidate | Medium |
| DolocAPI.EnterUI(System.Func`2<T,System.Boolean> handleStartUpArgs) | direct call candidate | Medium |
| DolocAPI.EquipActiveItem(DolocTown.Item item; DolocTown.Item& oldItem) | direct call candidate | Medium |
| DolocAPI.EquipDrone(DolocTown.Item item; DolocTown.Item& oldDrone) | direct call candidate | Medium |
| DolocAPI.EquipHat(System.String hatName; DolocTown.Item& oldHat) | direct call candidate | Medium |
| DolocAPI.EquipHat(DolocTown.ItemHat hatItem; DolocTown.Item& oldHat) | direct call candidate | Medium |
| DolocAPI.EquipPassiveItem(DolocTown.Item item; DolocTown.Item& oldItem) | direct call candidate | Medium |
| DolocAPI.EquipPassiveItem1(DolocTown.Item item; DolocTown.Item& oldItem) | direct call candidate | Medium |
| DolocAPI.EquipPassiveItem2(DolocTown.Item item; DolocTown.Item& oldItem) | direct call candidate | Medium |
| DolocAPI.get_AgentEquipmentManager() | direct call candidate | Medium |
| DolocAPI.get_AgentEquipmentParams() | direct call candidate | Medium |
| DolocAPI.get_CurrentL10nInfo() | direct call candidate | Medium |
| DolocAPI.get_dolocBuilder() | direct call candidate | Medium |
| DolocAPI.get_gameLoadingTip() | direct call candidate | Medium |
| DolocAPI.get_gameUiStates() | direct call candidate | Medium |
| DolocAPI.get_HoverBoxGroup() | direct call candidate | Medium |
| DolocAPI.get_SelectedBuilding() | direct call candidate | Medium |
| DolocAPI.get_SelectedEquipment() | direct call candidate | Medium |
| DolocAPI.get_uiEffectsProvider() | direct call candidate | Medium |
| DolocAPI.get_uiSystem() | direct call candidate | Medium |
| DolocAPI.GetBuildingTitle(System.String buildingName) | direct call candidate | Medium |
| DolocAPI.GetDialogueLineView(System.String targetName) | direct call candidate | Medium |
| DolocAPI.GetDialogueTargetViewOrDefault(System.String targetName; System.Boolean force) | direct call candidate | Medium |
| DolocAPI.GetEquipmentLimitation(System.String name) | direct call candidate | Medium |
| DolocAPI.GetEquipmentTitle(System.String name) | direct call candidate | Medium |
| DolocAPI.GetInventoriesAroundEquipment(DolocTown.Equipment equipment) | direct call candidate | Medium |
| DolocAPI.GetItemBorder(DolocTown.UI.DolocUiObject obj; DolocTown.UI.BorderType borderType) | direct call candidate | Medium |
| DolocAPI.GetItemBorder(UnityEngine.RectTransform obj; DolocTown.UI.BorderType borderType) | direct call candidate | Medium |
| DolocAPI.HideHoverBox(DolocTown.UI.DolocUiObject obj) | direct call candidate | Medium |
| DolocAPI.HideItemBorder(DolocTown.UI.DolocUiObject obj) | direct call candidate | Medium |
| DolocAPI.HidePanel() | direct call candidate | Medium |
| DolocAPI.HoverItemViewer(DolocTown.UI.DolocUiObject obj; DolocTown.UI.ItemData itemData; DolocTown.UI.UIAlignmentType targetAnchor; DolocTown.UI.UIAlignmentType hoverPivot) | direct call candidate | Medium |
| DolocAPI.HoverItemViewer(UnityEngine.RectTransform rectTransform; DolocTown.UI.ItemData itemData; DolocTown.UI.UIAlignmentType targetAnchor; DolocTown.UI.UIAlignmentType hoverPivot) | direct call candidate | Medium |
| DolocAPI.HoverText(DolocTown.UI.DolocUiObject obj; DolocTown.UI.TextGroup data; DolocTown.UI.UIAlignmentType targetAnchor; DolocTown.UI.UIAlignmentType hoverPivot) | direct call candidate | Medium |
| DolocAPI.HoverTextSmall(UnityEngine.RectTransform rectTransform; System.String text; DolocTown.UI.UIAlignmentType targetAnchor; DolocTown.UI.UIAlignmentType hoverPivot; System.Boolean autoFade; DolocTown.UI.HoverBoxStyle style; TMPro.TextAlignmentOptions alignment) | direct call candidate | Medium |
| DolocAPI.HoverTextSmall(DolocTown.UI.DolocUiObject obj; System.String text; DolocTown.UI.UIAlignmentType targetAnchor; DolocTown.UI.UIAlignmentType hoverPivot; System.Boolean autoFade; DolocTown.UI.HoverBoxStyle style; TMPro.TextAlignmentOptions alignment) | direct call candidate | Medium |
| DolocAPI.IsEquippedActive(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.IsEquippedDrone(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.IsEquippedPassive1(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.IsEquippedPassive2(DolocTown.Item item) | direct call candidate | Medium |
| DolocAPI.OpenPanel() | direct call candidate | Medium |
| DolocAPI.OpenSubmitSingleItemPanel(System.Func`2<DolocTown.Item,System.Boolean> itemFilter; System.Func`1<System.Boolean> submitConditionChecker; System.Func`2<DolocTown.Item,System.Int32> itemSubmitCountGetter; System.Func`3<DolocTown.Item,System.Int32,System.String> confirmTextGetter; System.Action`1<System.Int32> onFailedSubmit; System.Action`2<System.Int32,System.Int32> onSubmit) | direct call candidate | Medium |
| DolocAPI.QueryBuilding(System.String name; DolocTown.Config.Building.BuildingInfo& proto) | direct call candidate | Medium |
| DolocAPI.QueryEquipment(System.String name; DolocTown.Config.Equipment.EquipmentInfo& proto) | direct call candidate | Medium |
| DolocAPI.QuickDeselectCurrentItem() | direct call candidate | Medium |
| DolocAPI.QuickSelectCurrentItem() | direct call candidate | Medium |
| DolocAPI.QuitCurrentRoom(DolocTown.Room nextRoom; System.Boolean shouldUnloadScene) | direct call candidate | Medium |
| DolocAPI.RaiseUiEffects(System.Int32 value; UnityEngine.Color color) | direct call candidate | Medium |
| DolocAPI.RaiseUiNumberFadeUp(System.Int32 value; UnityEngine.Color color; UnityEngine.Vector2 screenPosition; System.Single duration; DG.Tweening.Ease moveEase; System.Single popDistance; System.Single waitDuration) | direct call candidate | Medium |
| DolocAPI.RaiseUiSpriteFadeDown(UnityEngine.Vector2 screenPosition; UnityEngine.Sprite icon; System.Single duration; DG.Tweening.Ease moveEase; System.Single popDistance) | direct call candidate | Medium |
| DolocAPI.RaiseUiSpriteFadeUp(UnityEngine.Vector2 screenPosition; UnityEngine.Sprite icon; System.Single duration; DG.Tweening.Ease moveEase; System.Single popDistance) | direct call candidate | Medium |
| DolocAPI.RaiseUiTextFadeUp(System.String text; UnityEngine.Color color; UnityEngine.Vector2 screenPosition; System.Single duration; DG.Tweening.Ease moveEase; System.Single popDistance) | direct call candidate | Medium |
| DolocAPI.RefreshQuickInventory() | direct call candidate | Medium |
| DolocAPI.RefreshQuickInventory(System.Int32 index) | direct call candidate | Medium |
| DolocAPI.RefreshQuickInventorySelected() | direct call candidate | Medium |
| DolocAPI.RemoveUiState() | direct call candidate | Medium |
| DolocAPI.ReQuickSelectCurrentItem() | direct call candidate | Medium |
| DolocAPI.ResetQuickInventorySelection() | direct call candidate | Medium |
| DolocAPI.RunGameProcess(DolocTown.NodeCanvas.GameProcessGraph graph; System.Action callback) | direct call candidate | Medium |
| DolocAPI.SetQuickInventoryVisible(System.Boolean value) | direct call candidate | Medium |
| DolocAPI.SetResidentUiInteractable(System.Boolean value) | direct call candidate | Medium |
| DolocAPI.SetResidentUiVisible(System.Boolean showBasicTip; System.Boolean showQuickInventory) | direct call candidate | Medium |
| DolocAPI.ShowConfirmBox(DolocTown.Config.UI.AlignmentText title; DolocTown.Config.UI.AlignmentText content; DolocTown.Config.UI.AlignmentText signature; System.Action onConfirm) | direct call candidate | Medium |
| DolocAPI.ShowSceneTextBox(UnityEngine.Vector2 worldPosition; DolocTown.Config.Localization.SceneTextBoxArgs sceneTextArgs; UnityEngine.Transform followedTrans; System.Single duration) | direct call candidate | Medium |
| DolocAPI.ShowSleepMenu(System.Action onEnd) | direct call candidate | Medium |
| DolocAPI.ShowSmallTextMenu(System.String[] titles; UnityEngine.Vector2 screenPosition; System.Action`1<System.String> onConfirm) | direct call candidate | Medium |
| DolocAPI.UIRaiseCancel() | direct call candidate | Medium |
| DolocAPI.UIRaiseConfirm() | direct call candidate | Medium |
| DolocAPI.UIRaiseError() | direct call candidate | Medium |
| DolocAPI.UIRaisePage() | direct call candidate | Medium |
| DolocAPI.UIRaisePopDown() | direct call candidate | Medium |
| DolocAPI.UIRaisePopUp() | direct call candidate | Medium |
| DolocAPI.UIRaiseRoll() | direct call candidate | Medium |
| DolocAPI.WaitWhileInUiSateTask() | direct call candidate | Medium |
| DolocAPI.WorldToAroundUiPosBottom(UnityEngine.Vector3 posWS; UnityEngine.Vector2 uiSize; System.Int32 dst) | direct call candidate | Medium |
| DolocBuilder.EnterHelpState(UnityEngine.Vector2 roomPosition; UnityEngine.Vector2Int size) | direct call candidate | Medium; needs instance source |
| DolocBuilder.ExitHelpState() | direct call candidate | Medium; needs instance source |
| DolocBuilder.GetBuilderTip() | direct call candidate | Medium; needs instance source |
| DolocBuilder.Init() | direct call candidate | Medium; needs instance source |
| DolocBuilder.OnInputDeviceChanged(DolocTown.DolocInputDeviceType type) | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_gameProcessGraphs() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_missionChains() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_npcSchedules() | direct call candidate | Medium; needs instance source |
| DolocIcon.RaiseUiSpriteFadeDown() | direct call candidate | Medium; needs instance source |
| DolocIcon.RaiseUiSpriteFadeDown(UnityEngine.Sprite sprite) | direct call candidate | Medium; needs instance source |
| DolocIcon.RaiseUiSpriteFadeUp() | direct call candidate | Medium; needs instance source |
| DolocIcon.RaiseUiSpriteFadeUp(UnityEngine.Sprite sprite) | direct call candidate | Medium; needs instance source |
| DolocInfoPanelBox.render(System.String[] contents) | direct call candidate | Medium; needs instance source |
| DolocInfoPanelBox.render(System.String[] contents; System.Single width) | direct call candidate | Medium; needs instance source |
| DolocInfoPanelBox.renderOnly(System.String[] contents) | direct call candidate | Medium; needs instance source |
| DolocInfoPanelBox.repos(System.Single lockWidth) | direct call candidate | Medium; needs instance source |
| DolocInfoPanelBox.repos() | direct call candidate | Medium; needs instance source |
| DolocInfoPanelBox.武器信息渲染() | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| BoxUiState.currentSkinIndex | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DevHelper.buttonSize | UnityEngine.Vector2 | Risky: reflection/private field | Disable dependent feature if missing |
| DevHelper.devMenuItems | System.Collections.Generic.Dictionary`2<System.String,System.Action> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI._hoverBoxGroup | DolocTown.UI.HoverBoxGroup | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI._itemBorder | DolocTown.UI.ItemBorder | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.<dolocBuilder>k__BackingField | DolocBuilder | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.<gameLoadingTip>k__BackingField | DolocTown.UI.GameLoadingTip | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.<gameUiStates>k__BackingField | DolocTown.DolocGameUiStateManager | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.<uiEffectsProvider>k__BackingField | DolocTown.GameServiceLocator.IUIEffectsProvider | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.<uiSystem>k__BackingField | DolocUiSystem | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<WaitWhileInUiSateTask>d__287.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBuilder._builderTipBase | DolocTown.BuilderTipBase | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBuilder._lutCache | System.Collections.Generic.Dictionary`2<System.Type,DolocTown.BuilderTipBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<gameProcessGraphs>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.NodeCanvas.GameProcessGraph> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<missionChains>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.NodeCanvas.MissionGraph> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<npcSchedules>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.NodeCanvas.NpcScheduleGraph> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInfoPanelBox.contentWidth | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInfoPanelBox.generator | UnityEngine.TextGenerator | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInfoPanelBox.settings | UnityEngine.TextGenerationSettings[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInfoPanelBox.space | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInfoPanelBox.texts | UnityEngine.UI.Text[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInfoPanelBox.titleHeight | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_QuickSelectNext | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_QuickSelectPrev | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ToggleMenu | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ToggleMissionPanel | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput | UnityEngine.InputSystem.InputActionMap | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput_BuilderDismantle | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput_BuilderDismantleHold | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput_BuilderLast | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput_BuilderNext | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput_BuilderRevocation | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput_BuilderRotate | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput_BuilderSelected | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput_BuilderSwitch | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput_BuilderSwitchInventory | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput_DPadDown | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput_DPadLeft | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput_DPadRight | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInput_DPadUp | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BuilderInputActionsCallbackInterface | DolocInputSource/IBuilderInputActions | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_QuickSelect_0 | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_QuickSelect_1 | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_QuickSelect_2 | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_QuickSelect_3 | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_QuickSelect_4 | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_QuickSelect_5 | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_QuickSelect_6 | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_QuickSelect_7 | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_QuickSelect_8 | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_QuickSelect_9 | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_QuickSelectNext | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_QuickSelectPrev | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_ToggleMenu | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_ToggleMissionPanel | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource/BuilderInputActions.m_Wrapper | DolocInputSource | Risky: reflection/private field | Disable dependent feature if missing |
| DolocOperationManager.tipPanel | DolocTown.UI.OperationTipPanel | Risky: reflection/private field | Disable dependent feature if missing |
| DolocPlainTextTip.__text | UnityEngine.UI.Text | Risky: reflection/private field | Disable dependent feature if missing |
| DolocText.<textbox>k__BackingField | UnityEngine.UI.Text | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AchievementSystem.configuredAchievements | System.Collections.Generic.Dictionary`2<System.String,DolocTown.NodeCanvas.MissionNodeListener> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AchievementSystem.missionGraph | DolocTown.NodeCanvas.MissionGraph | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.ActionTask_AttackEx.attackType | NodeCanvas.Framework.BBParameter`1<DolocTown.MonsterAttackId> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.ActionTask_AttackEx.target | NodeCanvas.Framework.BBParameter`1<UnityEngine.Transform> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Affector.func | DolocTown.Config.Equipment.EquipmentFuncAffector | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.<BuildingScanner>k__BackingField | DolocTown.BuildingScanner | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.quickInventory | DolocTown.UI.QuickInventoryPanel | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.quickInventoryTimer | DolocTown.Timer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunction._manager | DolocTown.GameData.AgentEquipmentManager | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunction.functionTypes | System.Collections.Generic.Dictionary`2<System.String,System.Type> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunction.item | DolocTown.Item | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunction.proto | DolocTown.Config.Player.AgentEquipmentFuncProto | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunction.skill | DolocTown.Config.Player.AgentEquipmentSkillInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionAreaSpeedUp._func | DolocTown.Config.Player.AgentEquipmentFuncProtoAreaSpeedUp | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionCounterBack._func | DolocTown.Config.Player.AgentEquipmentFuncProtoCounterBack | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionDashCdCooler._func | DolocTown.Config.Player.AgentEquipmentFuncProtoDashCdCooler | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionDisguise._func | DolocTown.Config.Player.AgentEquipmentFuncProtoDisguise | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionExtraResource.protoExtraResource | DolocTown.Config.Player.AgentEquipmentFuncProtoExtraResource | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionExtraResource.resourceIds | System.Collections.Generic.HashSet`1<System.String> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionFellCountAdditionOre._func | DolocTown.Config.Player.AgentEquipmentFuncProtoFellCountAdditionOre | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionFishTank.poolName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionFishTank.shouldGenFish | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionFoodEffectsAddition._func | DolocTown.Config.Player.AgentEquipmentFuncProtoFoodEffectsAddition | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionHerbPackage._func | DolocTown.Config.Player.AgentEquipmentFuncProtoHerbPackage | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionHerbPackage._item | DolocTown.ItemHerbPackage | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionIncreaseCriticalRate._func | DolocTown.Config.Player.AgentEquipmentFuncProtoIncreaseCriticalRate | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionLightningConductor._func | DolocTown.Config.Player.AgentEquipmentFuncProtoLightningConductor | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionLightningConductor.cdCounter | RedSaw.Counter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionLightningConductor.isCooling | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionShepherd._func | DolocTown.Config.Player.AgentEquipmentFuncProtoShepherd | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionShield._func | DolocTown.Config.Player.AgentEquipmentFuncProtoShield | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentEquipmentFunctionShield._shieldItem | DolocTown.ItemHatShield | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateDash.shouldQuit | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishing._isUiControlled | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentStateFishingReady._powerBar | DolocTown.UI.ProgressCircle | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AllDocumentUiState.unlockedData | DolocTown.UI.DocumentData[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalAI/Chicken_MetabolismState.buildCount | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalDecorator.func | DolocTown.Config.Equipment.EquipmentFuncAnimalDecorator | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap._buildingStairs | System.Collections.Generic.Dictionary`2<UnityEngine.Vector2Int,DolocTown.AnimalStair> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuilding>d__24.<>1__state | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalMap/<FromBuilding>d__24.<>2__current | DolocTown.AnimalStair | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| BoxUiState.get_isBoxUsedUp | Harmony patch candidate |  | Risky |
| BoxUiState.RefreshColorTag | Harmony patch candidate |  | Risky |
| BoxUiState.ShowBoxUsedUpWarning | Harmony patch candidate |  | Risky |
| BoxUiState.UpdateBoxTitle | Harmony patch candidate |  | Risky |
| DevHelper.InitFastButtons | Harmony patch candidate |  | Risky |
| DevHelper/<>c__DisplayClass12_0.<InitFastButtons>b__0 | Harmony patch candidate |  | Risky |
| DolocAPI.Command_OpenBoardMissionPanel | Harmony patch candidate |  | Risky |
| DolocAPI.Command_OpenFactionPanel | Harmony patch candidate | System.String factionName | Risky |
| DolocAPI.DevMenuItem_EnterDolocMountain | Harmony patch candidate |  | Risky |
| DolocAPI.DevMenuItem_EnterFarm | Harmony patch candidate |  | Risky |
| DolocAPI.DevMenuItem_EnterOldCity | Harmony patch candidate |  | Risky |
| DolocAPI.DevMenuItem_EnterTeleportPanel | Harmony patch candidate |  | Risky |
| DolocAPI.DevMenuItem_EnterWetland | Harmony patch candidate |  | Risky |
| DolocAPI.EnterUI | Harmony patch candidate |  | Medium |
| DolocAPI.EnterUI | Harmony patch candidate | System.Func`2<T,System.Boolean> handleStartUpArgs | Medium |
| DolocAPI.FastButton_OpenFactionPanel | Harmony patch candidate |  | Risky |
| DolocAPI.FastButton_OpenSeedStore | Harmony patch candidate |  | Risky |
| DolocAPI.FastButton_ReloadConfigTables | Harmony patch candidate |  | Risky |
| DolocAPI.FastButton_ReloadMods | Harmony patch candidate |  | Risky |
| DolocAPI.get_gameLoadingTip | Harmony patch candidate |  | Medium |
| DolocAPI.OpenPermissionViewChipDoc | Harmony patch candidate |  | Risky |
| DolocAPI.OpenPermissionViewPlantDoc | Harmony patch candidate |  | Risky |
| DolocAPI.RefreshQuickInventory | Harmony patch candidate |  | Medium |
| DolocAPI.RefreshQuickInventory | Harmony patch candidate | System.Int32 index | Medium |
| DolocAPI.RefreshQuickInventorySelected | Harmony patch candidate |  | Medium |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IUiEvents` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocInfoPanelBox | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.AllDocumentUiState | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.BuildingLinkInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.BuildingLinkInfoForCellar | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.ChipDocumentUiState | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingExteriorData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingExteriorInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingInfo/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingLevelData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingSpriteGroup | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingTileType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingWallpaperData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.BuildingWallpaperInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.FulcrumData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.TbBuilding | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.TbBuildingExterior | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Building.TbBuildingWallpaper | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Drone.SlotVisualSuitableType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.CaseSkin | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.CfgDecalSlotType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.ChairInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.DecalSlotData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EComGeneratorBase | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EComProtoAppliance | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EComProtoBattery | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EComProtoGeneratorCustom | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EComProtoGeneratorFuel | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EComProtoGeneratorSolar | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EComProtoGeneratorWind | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.ElectronicComponentProto | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentEnvType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFitType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncAffector | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncAnimalDecorator | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncAnimalStation | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncAutomateBotStation | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncBarrel | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncBattery | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncBed | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncCase | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncCaseBase | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncCaseLocator | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncChair | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncChickenNest | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncCropTrafficLight | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncDecorator | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncEquipment | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncEquipmentAnimation | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncEquipmentAnimationBase | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IUiEvents
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- UI classes are broad; map is intentionally metadata-first.
