# DolocTown Recipe / Crafting / Cooking API Map

## Build

- Steam build: 23465763
- Branch: workshop
- Assembly-CSharp hash: 38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228
- Research date: 2026-05-29
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Query recipes and unlock state.
- Observe crafting completion.
- Prefer official content mod formats for new recipes.

## Decompiled scope

- Matched types: 96
- Matched methods: 1203
- Matched fields: 527
- Matched properties: 458
- Matched events: 0
- Matched call edges: 4628
- Matched strings: 696
- Raw indexes: `maps/index/Recipe_Crafting-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.RecipePanelUiState | class | Confirmed | visibility=public; methods=48; fields=20 |
| DolocTown.Synthesizer | class | Confirmed | visibility=public; methods=50; fields=8 |
| DolocTown.Config.Recipe.DishGroupInfo | class | Confirmed | visibility=public; methods=38; fields=19 |
| DolocTown.UI.RecipeData | struct | Confirmed | visibility=public; methods=25; fields=24 |
| DolocTown.Config.Recipe.RecipeInfo | class | Confirmed | visibility=public; methods=30; fields=12 |
| DolocTown.Config.Recipe.RecipeGroupInfo | class | Confirmed | visibility=public; methods=28; fields=13 |
| DolocTown.Config.Recipe.DishInfo | class | Confirmed | visibility=public; methods=26; fields=10 |
| DolocTown.Config.Recipe.RecipeSubTypeInfo | class | Confirmed | visibility=public; methods=24; fields=11 |
| DolocTown.ConversionRecipeUiState | class | Confirmed | visibility=public; methods=23; fields=10 |
| DolocTown.Config.Recipe.DismantleRecipeInfo | class | Confirmed | visibility=public; methods=21; fields=8 |
| DolocTown.Config.Recipe.DismantleRecipeGroupInfo | class | Confirmed | visibility=public; methods=20; fields=8 |
| DolocTown.UI.ItemRecipeSlot | class | Confirmed | visibility=public; methods=17; fields=11 |
| DolocTown.Config.Recipe.IngredientGroupInfo | class | Confirmed | visibility=public; methods=18; fields=7 |
| DolocTown.UI.ItemRecipeData | struct | Confirmed | visibility=public; methods=13; fields=12 |
| DolocTown.RecipeGroup | class | Confirmed | visibility=public; methods=19; fields=3 |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase | class | Confirmed | visibility=public; methods=16; fields=5 |
| DolocTown.UI.ConversionRecipeData | struct | Confirmed | visibility=public; methods=11; fields=10 |
| DolocTown.UI.ItemRecipeListViewer | class | Confirmed | visibility=public; methods=15; fields=6 |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Mod.ModIngredientGroupExtensionInfo | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Mod.ModRecipeGroupExtensionInfo | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.GeneSynthesizer | class | Confirmed | visibility=public; methods=19; fields=1 |
| DolocTown.IRecipeGroup | interface | Confirmed | visibility=public; methods=18; fields=0 |
| DolocTown.UI.ItemDetailData/<GetRelatedRecipesData>d__34 | class | Confirmed | visibility=private; methods=9; fields=9 |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerGenerator | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.Config.Item.ItemFunctionRecipeGroup | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.UI.CraftViewer`1 | class | Confirmed | visibility=public; methods=9; fields=8 |
| DolocTown.Config.Recipe.RecipeMainTypeInfo | class | Confirmed | visibility=public; methods=12; fields=4 |
| DolocTown.UI.CookPanel | class | Confirmed | visibility=public; methods=11; fields=5 |
| DolocTown.UI.RecipeViewer | class | Confirmed | visibility=public; methods=5; fields=10 |
| DolocTown.Config.Item.ItemFunctionRecipe | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.Config.Recipe.IngredientGroupArray | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.ExchangeStoreRecipe | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.IRecipe | interface | Confirmed | visibility=public; methods=14; fields=0 |
| DolocTown.Recipe | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.UI.CraftPanel`3 | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.BuildingRecipe | class | Confirmed | visibility=public; methods=11; fields=2 |
| DolocTown.Config.Recipe.TbRecipeGroup | class | Confirmed | visibility=public; methods=10; fields=3 |
| DolocTown.RecipeManager | class | Confirmed | visibility=public; methods=11; fields=2 |
| DolocTown.UI.BotRecipeData | struct | Confirmed | visibility=public; methods=7; fields=6 |
| DolocTown.UI.RecipeSubItemSlot | class | Confirmed | visibility=public; methods=7; fields=6 |
| DolocTown.Config.Recipe.TbRecipe | class | Confirmed | visibility=public; methods=9; fields=3 |
| DolocTown.UI.ConversionRecipeViewer | class | Confirmed | visibility=public; methods=7; fields=5 |
| DolocTown.UI.CraftGridPanel`3 | class | Confirmed | visibility=public; methods=9; fields=3 |
| DolocTown.Config.Player.AgentEquipmentFuncProtoCook | class | Confirmed | visibility=public; methods=9; fields=2 |
| DolocTown.UI.BotRecipeSlot | class | Confirmed | visibility=public; methods=4; fields=7 |
| DolocTown.Config.Recipe.TbDish | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Recipe.TbDishGroup | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Recipe.TbDismantleRecipe | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Recipe.TbDismantleRecipeGroup | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Recipe.TbIngredientGroup | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Recipe.TbRecipeMainType | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.Config.Recipe.TbRecipeSubType | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.ItemRecipe | class | Confirmed | visibility=public; methods=9; fields=1 |
| DolocTown.RewardRecipe | class | Confirmed | visibility=public; methods=9; fields=1 |
| DolocTown.GeneSynthesizer/<>c | class | Confirmed | visibility=private; methods=5; fields=4 |
| DolocTown.Synthesizer/<>c | class | Confirmed | visibility=private; methods=5; fields=4 |
| DolocTown.UI.ConfirmCraftButton | class | Confirmed | visibility=public; methods=4; fields=5 |
| DolocTown.UI.ICraftData | interface | Confirmed | visibility=public; methods=9; fields=0 |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizer | class | Confirmed | visibility=public; methods=7; fields=1 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI/<>c__DisplayClass927_0.<Command_OpenExchangeStore>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AircraftEngineSmoke.Update | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingPanelUiState/<>c__DisplayClass13_0.<OnStartButtonClick>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.get_UseConversionMode | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.set_UseConversionMode | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelStartBuild | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelStartBuild_l10n_key | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.set_RecipePanelStartBuild | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.TbStaticText.get_RecipePanelStartBuild | candidate lifecycle or hook point | Medium |
| DolocTown.ConversionRecipeUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ConversionRecipeUiState.HandleStartUpArgs | candidate lifecycle or hook point | Medium |
| DolocTown.ConversionRecipeUiState.RefreshInfo | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CustomSynthesizer.get_useConversionMode | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CustomSynthesizer.OnInteract | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CustomSynthesizer/<>c__DisplayClass4_0.<OnInteract>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.EquipmentPanelUiState.HandleStartUpArgs | candidate lifecycle or hook point | Medium |
| DolocTown.EquipmentPanelUiState.StartCraft | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.EquipmentPanelUiState/<>c__DisplayClass36_0.<StartCraft>b__1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.EquipmentPanelUiState/<>c__DisplayClass36_1.<StartCraft>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.GameData.Workbench.<OnInteract>b__8_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.GameData.Workbench.<OnInteract>b__8_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.GeneSynthesizer.<OnInteract>b__10_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.GeneSynthesizer.OnInteract | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.GeneSynthesizer.OnUiExit | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.GeneSynthesizer.TryStartWork | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.GeneSynthesizer/<>c.<OnInteract>b__10_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ItemRecipe.OnUseAsItem | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ItemRecipe.OnUseAsTool | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ItemRecipe.Use | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ItemRecipeGroup.OnUseAsItem | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ItemRecipeGroup.OnUseAsTool | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ItemRecipeGroup.Use | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.HandleExchangeStoreStartUpArgs | candidate lifecycle or hook point | Medium |
| DolocTown.RecipePanelUiState.HandleStartUpArgs | candidate lifecycle or hook point | Medium |
| DolocTown.RecipePanelUiState.HandleStartUpArgs | candidate lifecycle or hook point | Medium |
| DolocTown.RecipePanelUiState.HandleStartUpArgs | candidate lifecycle or hook point | Medium |
| DolocTown.RecipePanelUiState.OnStartButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.OnStartButtonClickLeft | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.OnStartButtonClickRight | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.OnStartButtonContinuesClickRight | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.OnStartButtonLongClickLeft | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.OnUiUpdate | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.RefreshRecipe | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.RefreshTip | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState/<>c__DisplayClass54_0.<OnStartButtonClick>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState/<>c__DisplayClass75_0.<HandleExchangeStoreStartUpArgs>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState/<>c__DisplayClass75_0.<HandleExchangeStoreStartUpArgs>b__1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Synthesizer.get_useConversionMode | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Synthesizer.InitRecipeGroup | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Synthesizer.OnExitUI | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Synthesizer.OnInteract | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Synthesizer.OnStartWorkInContainerMode | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Synthesizer.QuickStartInternal | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Synthesizer.StartWork | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Synthesizer.StartWorkInternal | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Synthesizer.TryQuickStart | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Synthesizer/<>c.<get_useConversionMode>b__33_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Synthesizer/<>c.<StartWorkInternal>b__57_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Synthesizer/<>c__DisplayClass42_0.<OnInteract>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Synthesizer/<>c__DisplayClass42_0.<OnInteract>b__1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Synthesizer/<>c__DisplayClass57_0.<StartWorkInternal>b__1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.BotRecipeData.get_used | candidate lifecycle or hook point | Medium |
| DolocTown.UI.BotRecipeViewer.OnInitSlot | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.BotRecipeViewer.OnRefreshView | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.BotRecipeViewer.OnStartShow | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.BotRecipeViewer/<>c__DisplayClass2_0.<OnInitSlot>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.BotRecipeViewer/<>c__DisplayClass2_0.<OnInitSlot>b__1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.ConversionRecipeViewer.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.ConversionRecipeViewer.<__Init>b__5_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.ConversionRecipeViewer.<__Init>b__5_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.ConversionRecipeViewer.<__Init>b__5_2 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.ConversionRecipeViewer.<__Init>b__5_3 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.CookPanel.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.CookPanel.get_OnCloseButtonClick | candidate lifecycle or hook point | Medium |
| DolocTown.UI.CookPanel.InitSwitchBar | candidate lifecycle or hook point | Medium |
| DolocTown.UI.CookPanel.OnStartHide | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.CookPanel.OnStartShow | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.CookPanel.RefreshDishPanelNavigation | candidate lifecycle or hook point | Medium |
| DolocTown.UI.CraftGridPanel`3.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.QueryRecipe(System.String name; DolocTown.Recipe& recipe) | direct call candidate | Medium |
| DolocAPI.QueryRecipeProto(System.String recipeName; DolocTown.Config.Recipe.RecipeInfo& proto) | direct call candidate | Medium |
| DolocTown.AttackBehaviourRendererAircraft.OnAttackBegin(DolocTown.MonsterAttackId id; System.Type type; UnityEngine.Transform target) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamProcessing.get_MainRecipe() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamProcessing.get_Recipes() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemEnv.GetNeedFillingSynthesizer(DolocTown.AutomateBotStation station; System.String recipe) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemLocker.GetNeedFillingSynthesizer(DolocTown.AutomateBotStation station; System.String recipe) | direct call candidate | Medium; needs instance source |
| DolocTown.BuildingRecipe.get_CostTime() | direct call candidate | Medium; needs instance source |
| DolocTown.BuildingRecipe.get_InputItems() | direct call candidate | Medium; needs instance source |
| DolocTown.BuildingRecipe.get_isValid() | direct call candidate | Medium; needs instance source |
| DolocTown.BuildingRecipe.get_MoneyCost() | direct call candidate | Medium; needs instance source |
| DolocTown.BuildingRecipe.get_OutputItem() | direct call candidate | Medium; needs instance source |
| DolocTown.BuildingRecipe.get_RecipeId() | direct call candidate | Medium; needs instance source |
| DolocTown.BuildingRecipe.get_RecipeTitle() | direct call candidate | Medium; needs instance source |
| DolocTown.BuildingRecipe.get_Storage() | direct call candidate | Medium; needs instance source |
| DolocTown.BuildingRecipe.get_TechPoint() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncEquipmentWorkbench.get_RecipeGroupName() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncEquipmentWorkbench.get_RecipeGroupName_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncGarbageShredder.get_RecipeGroupName() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncGarbageShredder.get_RecipeGroupName_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer.DeserializeEquipmentFuncGeneSynthesizer(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer.get_AllowCrossSpecies() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer.get_DefaultCapsuleItem() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer.get_DefaultCapsuleItem_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer.get_Interval() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizer.DeserializeEquipmentFuncSynthesizer(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizer.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizer.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizer.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizer.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.DeserializeEquipmentFuncSynthesizerBase(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.get_DishGroupName() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.get_DishGroupName_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.get_RecipeGroupName() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.get_RecipeGroupName_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.get_UseConversionMode() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerGenerator.DeserializeEquipmentFuncSynthesizerGenerator(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerGenerator.get_Interval() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerGenerator.get_OutputItemName() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerGenerator.get_OutputItemName_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerGenerator.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerGenerator.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerGenerator.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerGenerator.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncWorkbench.get_RecipeGroupName() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Equipment.EquipmentFuncWorkbench.get_RecipeGroupName_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionRecipe.DeserializeItemFunctionRecipe(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Item.ItemFunctionRecipe.get_RecipeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionRecipe.get_RecipeId_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionRecipe.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionRecipe.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionRecipe.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionRecipe.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionRecipeGroup.DeserializeItemFunctionRecipeGroup(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Item.ItemFunctionRecipeGroup.get_DialogueNode() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionRecipeGroup.get_RecipeGroupId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionRecipeGroup.get_RecipeGroupId_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionRecipeGroup.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionRecipeGroup.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionRecipeGroup.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionRecipeGroup.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemInfo.get_Cookable() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_AutomateBotPanelRecipeEmpty() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_AutomateBotPanelRecipeEmpty_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_AutomateBotPanelRecipeSubType() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_AutomateBotPanelRecipeSubType_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_AutomateBotPanelRecipeType() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_AutomateBotPanelRecipeType_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_CollectionPanelItemRecipeEmpty() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_CollectionPanelItemRecipeEmpty_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_CollectionPanelItemRecipeTime() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_CollectionPanelItemRecipeTime_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_InventoryPanelGeneSynthesizerLocked() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_InventoryPanelGeneSynthesizerLocked_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_InventoryPanelInfoGeneSynthesizer() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_InventoryPanelInfoGeneSynthesizer_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelAlreadyWorking() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelAlreadyWorking_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelConfirmPopBuffer() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelConfirmPopBuffer_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelConfirmWithTime() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelConfirmWithTime_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelEmpty() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelEmpty_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelExistItems() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelExistItems_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelLimitUp() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelLimitUp_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelMaterialList() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelMaterialList_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelMaxCraftCount() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelMaxCraftCount_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_RecipePanelNoMaterial() | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| DolocTown.AircraftEngineSmoke._collisionLayer | UnityEngine.LayerMask | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AircraftEngineSmoke._detectDistance | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AircraftEngineSmoke._particleSystem | UnityEngine.ParticleSystem | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBotUiState.currentRecipeList | System.Collections.Generic.List`1<System.String> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBotUiState.recipeName | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamProcessing.recipes | System.Collections.Generic.HashSet`1<System.String> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BattleUtils._damageFormula | XLua.LuaFunction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuildingRecipe.<isValid>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncEquipmentWorkbench.<RecipeGroupName_Ref>k__BackingField | DolocTown.Config.Recipe.RecipeGroupInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncEquipmentWorkbench.<RecipeGroupName>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncGarbageShredder.<RecipeGroupName_Ref>k__BackingField | DolocTown.Config.Recipe.DismantleRecipeGroupInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncGarbageShredder.<RecipeGroupName>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer.<AllowCrossSpecies>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer.<DefaultCapsuleItem_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer.<DefaultCapsuleItem>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer.<Interval>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.<DishGroupName_Ref>k__BackingField | DolocTown.Config.Recipe.DishGroupInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.<DishGroupName>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.<RecipeGroupName_Ref>k__BackingField | DolocTown.Config.Recipe.RecipeGroupInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.<RecipeGroupName>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.<UseConversionMode>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerGenerator.<Interval>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerGenerator.<OutputItemName_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerGenerator.<OutputItemName>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncWorkbench.<RecipeGroupName_Ref>k__BackingField | DolocTown.Config.Recipe.RecipeGroupInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncWorkbench.<RecipeGroupName>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionRecipe.<RecipeId_Ref>k__BackingField | DolocTown.Config.Recipe.RecipeInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionRecipe.<RecipeId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionRecipeGroup.<DialogueNode>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionRecipeGroup.<RecipeGroupId_Ref>k__BackingField | DolocTown.Config.Recipe.RecipeGroupInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionRecipeGroup.<RecipeGroupId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemInfo.<Cookable>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<AutomateBotPanelRecipeEmpty_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<AutomateBotPanelRecipeEmpty>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<AutomateBotPanelRecipeSubType_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<AutomateBotPanelRecipeSubType>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<AutomateBotPanelRecipeType_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<AutomateBotPanelRecipeType>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<CollectionPanelItemRecipeEmpty_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<CollectionPanelItemRecipeEmpty>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<CollectionPanelItemRecipeTime_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<CollectionPanelItemRecipeTime>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<InventoryPanelGeneSynthesizerLocked_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<InventoryPanelGeneSynthesizerLocked>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<InventoryPanelInfoGeneSynthesizer_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<InventoryPanelInfoGeneSynthesizer>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelAlreadyWorking_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelAlreadyWorking>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelConfirmPopBuffer_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelConfirmPopBuffer>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelConfirmWithTime_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelConfirmWithTime>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelEmpty_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelEmpty>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelExistItems_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelExistItems>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelLimitUp_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelLimitUp>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelMaterialList_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelMaterialList>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelMaxCraftCount_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelMaxCraftCount>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelNoMaterial_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelNoMaterial>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelNotCookable_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelNotCookable>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelRandomGeneHint_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelRandomGeneHint>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelRestTime_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelRestTime>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelStartBuild_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelStartBuild>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelTask_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelTask>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelTimeInfo_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelTimeInfo>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelTitle_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelTitle>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelUnknowDishComment_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelUnknowDishComment>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelUnlockedDishComment_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelUnlockedDishComment>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelUnlockedRecipeComment_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RecipePanelUnlockedRecipeComment>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RewardInfoRecipeUnlock_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RewardInfoRecipeUnlock>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RewardInfoRecipeUnlockHint_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<RewardInfoRecipeUnlockHint>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModDishGroupExtensionInfo.<ExtraDishes_Ref>k__BackingField | DolocTown.Config.Recipe.DishInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModDishGroupExtensionInfo.<Id_Ref>k__BackingField | DolocTown.Config.Recipe.DishGroupInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModIngredientGroupExtensionInfo.<ExtraItems_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModIngredientGroupExtensionInfo.<ExtraItems>k__BackingField | System.String[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModIngredientGroupExtensionInfo.<Id_Ref>k__BackingField | DolocTown.Config.Recipe.IngredientGroupInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModIngredientGroupExtensionInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModRecipeGroupExtensionInfo.<ExtraRecipes_Ref>k__BackingField | DolocTown.Config.Recipe.RecipeInfo[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModRecipeGroupExtensionInfo.<ExtraRecipes>k__BackingField | System.String[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModRecipeGroupExtensionInfo.<Id_Ref>k__BackingField | DolocTown.Config.Recipe.RecipeGroupInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.ModRecipeGroupExtensionInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.TbModIngredientGroupExtension._dataList | System.Collections.Generic.List`1<DolocTown.Config.Mod.ModIngredientGroupExtensionInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Mod.TbModRecipeGroupExtension._dataList | System.Collections.Generic.List`1<DolocTown.Config.Mod.ModRecipeGroupExtensionInfo> | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| DolocTown.AircraftEngineSmoke.Update | Harmony patch candidate |  | Risky |
| DolocTown.BuildingPanelUiState/<>c__DisplayClass13_0.<OnStartButtonClick>b__0 | Harmony patch candidate | DolocTown.CraftQuantitySubmitUiState state | Risky |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.get_UseConversionMode | Harmony patch candidate |  | Medium |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase.set_UseConversionMode | Harmony patch candidate | System.Boolean value | Risky |
| DolocTown.ConversionRecipeUiState.get_OnCloseButtonClick | Harmony patch candidate |  | Risky |
| DolocTown.ConversionRecipeUiState.RefreshInfo | Harmony patch candidate |  | Risky |
| DolocTown.CustomSynthesizer.get_useConversionMode | Harmony patch candidate |  | Risky |
| DolocTown.CustomSynthesizer.OnInteract | Harmony patch candidate |  | Risky |
| DolocTown.CustomSynthesizer/<>c__DisplayClass4_0.<OnInteract>b__0 | Harmony patch candidate | DolocTown.ConversionRecipeUiState state | Risky |
| DolocTown.GameData.Workbench.<OnInteract>b__8_0 | Harmony patch candidate | DolocTown.RecipePanelUiState state | Risky |
| DolocTown.GameData.Workbench.<OnInteract>b__8_1 | Harmony patch candidate | DolocTown.IRecipe recipe; DolocTown.IRecipeGroup _ | Risky |
| DolocTown.GeneSynthesizer.<OnInteract>b__10_0 | Harmony patch candidate | DolocTown.FishTankUiState state | Risky |
| DolocTown.GeneSynthesizer.OnInteract | Harmony patch candidate |  | Risky |
| DolocTown.GeneSynthesizer.OnUiExit | Harmony patch candidate |  | Risky |
| DolocTown.GeneSynthesizer/<>c.<OnInteract>b__10_1 | Harmony patch candidate |  | Risky |
| DolocTown.ItemRecipe.OnUseAsItem | Harmony patch candidate |  | Risky |
| DolocTown.ItemRecipe.OnUseAsTool | Harmony patch candidate |  | Risky |
| DolocTown.ItemRecipe.Use | Harmony patch candidate |  | Risky |
| DolocTown.ItemRecipeGroup.OnUseAsItem | Harmony patch candidate |  | Risky |
| DolocTown.ItemRecipeGroup.OnUseAsTool | Harmony patch candidate |  | Risky |
| DolocTown.ItemRecipeGroup.Use | Harmony patch candidate |  | Risky |
| DolocTown.RecipePanelUiState.get_OnCloseButtonClick | Harmony patch candidate |  | Risky |
| DolocTown.RecipePanelUiState.OnStartButtonClick | Harmony patch candidate | System.Boolean single; System.Boolean useMaxCount | Risky |
| DolocTown.RecipePanelUiState.OnStartButtonClickLeft | Harmony patch candidate | System.Int32 _ | Risky |
| DolocTown.RecipePanelUiState.OnStartButtonClickRight | Harmony patch candidate | System.Int32 _ | Risky |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IRecipeHelper.Experimental` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocTown.Config.Equipment.EquipmentFuncGeneSynthesizer | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizer | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerBase | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncSynthesizerGenerator | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionRecipe | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ItemFunctionRecipeGroup | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.ModIngredientGroupExtensionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.ModRecipeGroupExtensionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.TbModIngredientGroupExtension | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mod.TbModRecipeGroupExtension | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Player.AgentEquipmentFuncProtoCook | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.DishGroupInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.DishInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.DishInfo/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.DismantleRecipeGroupInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.DismantleRecipeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.IngredientGroupArray | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.IngredientGroupInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.RecipeGroupInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.RecipeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.RecipeInfo/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.RecipeMainTypeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.RecipeSubTypeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.TbDish | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.TbDishGroup | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.TbDismantleRecipe | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.TbDismantleRecipeGroup | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.TbIngredientGroup | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.TbRecipe | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.TbRecipeGroup | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.TbRecipeMainType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Recipe.TbRecipeSubType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.BotRecipeData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.BotRecipeData/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.ConversionRecipeData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.CraftQuantityData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.ICraftData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.ItemDetailData/<GetRelatedRecipesData>d__34 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.ItemRecipeData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.ItemRecipeData/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.RecipeData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IRecipeHelper
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- Compare with official Workshop recipe docs before inventing new formats.
