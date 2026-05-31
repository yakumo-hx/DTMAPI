# DolocTown Input API Map

## Build

- Steam build: 23249387
- Branch: workshop
- Assembly-CSharp hash: 247ACDAA8D6ED0490A406C6FC8EB035F957D0EC1AE3F036F10E2092B8AC10367
- Research date: 2026-05-17
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Map native input action wrappers.
- Expose stable hotkey and button-state helpers.
- Avoid direct dependency on generated Input System internals where possible.

## Decompiled scope

- Matched types: 40
- Matched methods: 761
- Matched fields: 449
- Matched properties: 269
- Matched events: 1
- Matched call edges: 5152
- Matched strings: 179
- Raw indexes: `maps/index/Input-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocInputSource | class | Confirmed | visibility=public; methods=22; fields=110 |
| DolocTown.UI.DolocNavigationButton | class | Confirmed | visibility=public; methods=43; fields=26 |
| DolocTown.UI.DolocButtonComponent | class | Confirmed | visibility=public; methods=34; fields=27 |
| DolocInputSource/BaseInputActions | struct | Confirmed | visibility=public; methods=51; fields=1 |
| DolocInputSource/NormalInputActions | struct | Confirmed | visibility=public; methods=45; fields=1 |
| DolocInputSource/IBaseInputActions | interface | Confirmed | visibility=public; methods=44; fields=0 |
| DolocInputSource/INormalInputActions | interface | Confirmed | visibility=public; methods=38; fields=0 |
| RedSaw.UI.RedSawButtonBase | class | Confirmed | visibility=public; methods=26; fields=8 |
| DolocTown.UI.InputActionKeyIcon | class | Confirmed | visibility=public; methods=13; fields=13 |
| DolocInputSource/BuilderInputActions | struct | Confirmed | visibility=public; methods=20; fields=1 |
| DolocTown.Config.Settings.InputActionBinds | class | Confirmed | visibility=public; methods=15; fields=4 |
| RedSaw.UI.RedSawButtonPrototype | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocInputSource/IBuilderInputActions | interface | Confirmed | visibility=public; methods=13; fields=0 |
| DolocTown.UI.TextButton | class | Confirmed | visibility=public; methods=11; fields=2 |
| DolocInputSource/GlobalActions | struct | Confirmed | visibility=public; methods=11; fields=1 |
| DolocTown.Config.Player.AgentEquipmentFuncProtoGrandmasButton | class | Confirmed | visibility=public; methods=9; fields=2 |
| DolocTown.UI.DolocInputFiledComponent | class | Confirmed | visibility=public; methods=7; fields=3 |
| DolocTown.UI.EnvOptimizerConfirmButton | class | Confirmed | visibility=public; methods=5; fields=5 |
| DolocTown.UI.ConfirmCraftButton | class | Confirmed | visibility=public; methods=4; fields=5 |
| DolocTown.UI.ModFunctionButton | class | Confirmed | visibility=public; methods=6; fields=3 |
| RedSaw.CommandLineInterface.DebugButton | class | Confirmed | visibility=public; methods=3; fields=6 |
| DolocTown.UI.MenuButton | class | Confirmed | visibility=public; methods=6; fields=2 |
| DolocTown.UI.VerticalButtonGroup`1 | class | Confirmed | visibility=public; methods=7; fields=1 |
| RedSaw.CommandLineInterface.DebugInlineButtonAttribute | class | Confirmed | visibility=public; methods=4; fields=4 |
| DolocTown.ButtonAction | class | Confirmed | visibility=public; methods=4; fields=3 |
| DolocTown.DolocInputDeviceType | enum | Confirmed | visibility=public; methods=0; fields=7 |
| DolocTown.DolocInputType | enum | Confirmed | visibility=public; methods=0; fields=6 |
| DolocTown.UI.LinkButton | class | Confirmed | visibility=public; methods=5; fields=1 |
| RedSaw.CommandLineInterface.DebugButtonAttribute | class | Confirmed | visibility=public; methods=4; fields=2 |
| DolocTown.UI.GetUnstuckButton/<>c | class | Confirmed | visibility=private; methods=3; fields=2 |
| DolocTown.UI.LanguageButton | class | Confirmed | visibility=public; methods=4; fields=1 |
| DolocInputSource/IGlobalActions | interface | Confirmed | visibility=public; methods=4; fields=0 |
| DolocTown.InputActionExtension | static class | Confirmed | visibility=public; methods=4; fields=0 |
| DolocTown.UI.GetUnstuckButton | class | Confirmed | visibility=public; methods=3; fields=1 |
| DolocTown.UI.ModFunctionButtonGroup | class | Confirmed | visibility=public; methods=3; fields=1 |
| RedSaw.CommandLineInterface.DebugButtonType | enum | Confirmed | visibility=public; methods=0; fields=4 |
| DolocTown.FastButtonAttribute | class | Confirmed | visibility=public; methods=2; fields=1 |
| DolocTown.AgentEquipmentFunctionGrandmasButton | class | Confirmed | visibility=public; methods=2; fields=0 |
| DolocTown.UI.LanguageButtonGroup | class | Confirmed | visibility=public; methods=2; fields=0 |
| DolocTown.UI.OuterLinkButtonGroup | class | Confirmed | visibility=public; methods=2; fields=0 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| DevHelper.InitFastButtons | candidate lifecycle or hook point | Risky: non-public or generated path |
| DevHelper/<>c__DisplayClass12_0.<InitFastButtons>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.FastButton_OpenFactionPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.FastButton_OpenSeedStore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.FastButton_ReloadConfigTables | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.FastButton_ReloadMods | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c.<FastButton_OpenFactionPanel>b__1004_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c.<FastButton_OpenSeedStore>b__1003_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocInputSource.get_KeyboardMouseScheme | candidate lifecycle or hook point | Medium |
| DolocInputSource/BaseInputActions.get_Interact | candidate lifecycle or hook point | Medium |
| DolocInputSource/IBaseInputActions.OnDestroyItem | candidate lifecycle or hook point | Medium |
| DolocInputSource/IBaseInputActions.OnInteract | candidate lifecycle or hook point | Medium |
| DolocInputSource/INormalInputActions.OnInteract | candidate lifecycle or hook point | Medium |
| DolocInputSource/INormalInputActions.OnRoomInteract | candidate lifecycle or hook point | Medium |
| DolocInputSource/INormalInputActions.OnUseItem | candidate lifecycle or hook point | Medium |
| DolocInputSource/INormalInputActions.OnUseTool | candidate lifecycle or hook point | Medium |
| DolocInputSource/NormalInputActions.get_Interact | candidate lifecycle or hook point | Medium |
| DolocInputSource/NormalInputActions.get_RoomInteract | candidate lifecycle or hook point | Medium |
| DolocInputSource/NormalInputActions.get_UseItem | candidate lifecycle or hook point | Medium |
| DolocInputSource/NormalInputActions.get_UseTool | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBotUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BoardMissionUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuilderTipBase.GetUpdateFunc | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingPanelUiState.OnStartButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingPanelUiState/<>c__DisplayClass13_0.<OnStartButtonClick>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BuildingPanelUiState/<>c__DisplayClass13_0.<OnStartButtonClick>b__1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CalendarUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionBookUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ContainerBaseUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ConversionRecipeUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DemoEndUiSate.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DeveloperListUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DialogueHistoryUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DolocUiState`1.ClickCloseButton | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DolocUiState`1.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DolocUserInput.UpdateActiveDevice | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DolocUserInput/<>c.<UpdateActiveDevice>b__332_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DronePanelUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.EnvOptimizerSubmitUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.EnvOptimizerUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.EquipmentBarUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.EquipmentPanelUiState.OnStartButtonClickLeft | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.EquipmentPanelUiState.OnStartButtonClickRight | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.EquipmentPanelUiState.OnStartButtonContinuesClickRight | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.EquipmentPanelUiState.OnStartButtonLongClickLeft | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FactionMissionUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FarmingGunUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.FishTankUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.GameDataUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.GarbageShredderUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.GlobalBuilderState.RefreshOperationTip | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.InstructionUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MainMenuUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ModUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.PlatformPanelUiState.OnStartButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.QuantitySubmitUiStateBase`2.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.OnStartButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.OnStartButtonClickLeft | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.OnStartButtonClickRight | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.OnStartButtonContinuesClickRight | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState.OnStartButtonLongClickLeft | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecipePanelUiState/<>c__DisplayClass54_0.<OnStartButtonClick>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.RecruitPanelUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.SacrificeBoxUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.SeedUnLockUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.StorageShelfUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.StoreUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.SubmitMultipleItemsUiStateBase.ClickCloseButton | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.SubmitMultipleItemsUiStateBase.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.SubmitSingleItemsUiStateBase.ClickCloseButton | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.SubmitSingleItemsUiStateBase.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.TechTreeUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.TeleportUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.TutorialPanelUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.CityMapPanel/<>c.<__Init>b__38_5 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.UI.CookPanel.get_OnCloseButtonClick | candidate lifecycle or hook point | Medium |
| DolocTown.UI.DeviceDetectImageCom.OnRefresh | candidate lifecycle or hook point | Medium |
| DolocTown.UI.DeviceDetectL10nKeyTextCom.OnRefresh | candidate lifecycle or hook point | Medium |
| DolocTown.UI.DeviceDetectTextCom.OnRefresh | candidate lifecycle or hook point | Medium |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.GetActionKeyIconGroup(DolocTown.DolocInputDeviceType deviceType; System.String actionName; DolocTown.ActionIconGroup& iconGroup) | direct call candidate | Medium |
| DolocAPI.GetAllActionKeyIconGroup(DolocTown.DolocInputDeviceType deviceType; System.String actionName; System.Collections.Generic.List`1<DolocTown.ActionIconGroup>& iconGroups) | direct call candidate | Medium |
| DolocAPI.GetParsedKeystrokeText(DolocTown.DolocInputDeviceType deviceType; System.String text) | direct call candidate | Medium |
| DolocBuilder.OnInputDeviceChanged(DolocTown.DolocInputDeviceType type) | direct call candidate | Medium; needs instance source |
| DolocInputSource.Contains(UnityEngine.InputSystem.InputAction action) | direct call candidate | Medium; needs instance source |
| DolocInputSource.Disable() | direct call candidate | Medium; needs instance source |
| DolocInputSource.Dispose() | direct call candidate | Medium; needs instance source |
| DolocInputSource.Enable() | direct call candidate | Medium; needs instance source |
| DolocInputSource.FindAction(System.String actionNameOrId; System.Boolean throwIfNotFound) | direct call candidate | Medium; needs instance source |
| DolocInputSource.FindBinding(UnityEngine.InputSystem.InputBinding bindingMask; UnityEngine.InputSystem.InputAction& action) | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_asset() | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_BaseInput() | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_bindingMask() | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_bindings() | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_BuilderInput() | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_controlSchemes() | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_devices() | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_GamePadScheme() | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_Global() | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_KeyboardMouseScheme() | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_NormalInput() | direct call candidate | Medium; needs instance source |
| DolocInputSource.GetEnumerator() | direct call candidate | Medium; needs instance source |
| DolocInputSource.set_bindingMask(System.Nullable`1<UnityEngine.InputSystem.InputBinding> value) | direct call candidate | Medium; needs instance source |
| DolocInputSource.set_devices(System.Nullable`1<UnityEngine.InputSystem.Utilities.ReadOnlyArray`1<UnityEngine.InputSystem.InputDevice>> value) | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.Disable() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.Enable() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.Get() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_AddOne() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_AddTen() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_AssistSplit() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_Cancel() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_Confirm() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_ConfirmHold() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_ContinueDialogue() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_DestroyItem() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_DisposeItem() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_DisposeItemHold() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_enabled() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_Interact() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_LockItem() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_MiscellaneousFunction() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_Move() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_MoveDown() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_MoveLast() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_MoveLeft() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_MoveNext() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_MoveRight() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_MoveUp() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_PageDown() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_PageUp() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_PutAllItem() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_PutMaxItem() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_PutMaxItemHold() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_QuickSelectNext() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_QuickSelectPrev() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_Scroll() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_SetMax() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_SetMin() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_SortItem() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_SortItemHold() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_SpeedUp() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_SplitItem() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_SubmitItem() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_SubOne() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_SubTen() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_ToggleBackpack() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_ToggleCollectionBook() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_ToggleDialogueHistory() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_ToggleMap() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_ToggleMenu() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_ToggleMissionPanel() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_ToggleTechTree() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.op_Implicit(DolocInputSource/BaseInputActions set) | direct call candidate | Medium |
| DolocInputSource/BaseInputActions.SetCallbacks(DolocInputSource/IBaseInputActions instance) | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.Disable() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.Enable() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.Get() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.get_BuilderDismantle() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.get_BuilderDismantleHold() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.get_BuilderLast() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.get_BuilderNext() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.get_BuilderRevocation() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.get_BuilderRotate() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.get_BuilderSelected() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.get_BuilderSwitch() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.get_BuilderSwitchInventory() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.get_DPadDown() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.get_DPadLeft() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.get_DPadRight() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.get_DPadUp() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.get_enabled() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BuilderInputActions.op_Implicit(DolocInputSource/BuilderInputActions set) | direct call candidate | Medium |
| DolocInputSource/BuilderInputActions.SetCallbacks(DolocInputSource/IBuilderInputActions instance) | direct call candidate | Medium; needs instance source |
| DolocInputSource/GlobalActions.Disable() | direct call candidate | Medium; needs instance source |
| DolocInputSource/GlobalActions.Enable() | direct call candidate | Medium; needs instance source |
| DolocInputSource/GlobalActions.Get() | direct call candidate | Medium; needs instance source |
| DolocInputSource/GlobalActions.get_Click() | direct call candidate | Medium; needs instance source |
| DolocInputSource/GlobalActions.get_CursorPosition() | direct call candidate | Medium; needs instance source |
| DolocInputSource/GlobalActions.get_enabled() | direct call candidate | Medium; needs instance source |
| DolocInputSource/GlobalActions.get_Point() | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| DevHelper.buttonSize | UnityEngine.Vector2 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.<asset>k__BackingField | UnityEngine.InputSystem.InputActionAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput | UnityEngine.InputSystem.InputActionMap | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_AddOne | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_AddTen | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_AssistSplit | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_Cancel | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_Confirm | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ConfirmHold | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ContinueDialogue | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_DestroyItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_DisposeItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_DisposeItemHold | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_Interact | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_LockItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_MiscellaneousFunction | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_Move | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_MoveDown | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_MoveLast | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_MoveLeft | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_MoveNext | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_MoveRight | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_MoveUp | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_PageDown | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_PageUp | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_PutAllItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_PutMaxItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_PutMaxItemHold | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_QuickSelectNext | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_QuickSelectPrev | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_Scroll | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_SetMax | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_SetMin | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_SortItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_SortItemHold | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_SpeedUp | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_SplitItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_SubmitItem | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_SubOne | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_SubTen | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ToggleBackpack | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ToggleCollectionBook | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ToggleDialogueHistory | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ToggleMap | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ToggleMenu | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ToggleMissionPanel | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ToggleTechTree | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInputActionsCallbackInterface | DolocInputSource/IBaseInputActions | Risky: reflection/private field | Disable dependent feature if missing |
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
| DolocInputSource.m_GamePadSchemeIndex | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_Global | UnityEngine.InputSystem.InputActionMap | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_Global_Click | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_Global_CursorPosition | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_Global_Point | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_Global_RightClick | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_GlobalActionsCallbackInterface | DolocInputSource/IGlobalActions | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_KeyboardMouseSchemeIndex | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput | UnityEngine.InputSystem.InputActionMap | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_AssistMove | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_Cancel | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_Dash | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_Debug | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_DisposeItemInBackpack | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_Fishing | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_Interact | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_Jump | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_JumpDown | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_Move | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
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
| DolocInputSource.m_NormalInput_RoomInteract | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_ScrollInventoryDown | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_ScrollInventoryUp | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_SelectedActive | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_SelectedDrone | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_SpeedUp | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| DolocTown.DolocUserInput.OnInputDeviceChanged | native event | System.Action`1<DolocTown.DolocInputDeviceType> | Confirmed metadata; verify usage |
| DevHelper.InitFastButtons | Harmony patch candidate |  | Risky |
| DevHelper/<>c__DisplayClass12_0.<InitFastButtons>b__0 | Harmony patch candidate |  | Risky |
| DolocAPI.FastButton_OpenFactionPanel | Harmony patch candidate |  | Risky |
| DolocAPI.FastButton_OpenSeedStore | Harmony patch candidate |  | Risky |
| DolocAPI.FastButton_ReloadConfigTables | Harmony patch candidate |  | Risky |
| DolocAPI.FastButton_ReloadMods | Harmony patch candidate |  | Risky |
| DolocAPI/<>c.<FastButton_OpenFactionPanel>b__1004_0 | Harmony patch candidate | DolocTown.FactionMissionUiState state | Risky |
| DolocAPI/<>c.<FastButton_OpenSeedStore>b__1003_0 | Harmony patch candidate | DolocTown.StoreUiState state | Risky |
| DolocInputSource.get_KeyboardMouseScheme | Harmony patch candidate |  | Medium |
| DolocInputSource/BaseInputActions.get_Interact | Harmony patch candidate |  | Medium |
| DolocInputSource/IBaseInputActions.OnDestroyItem | Harmony patch candidate | UnityEngine.InputSystem.InputAction/CallbackContext context | Medium |
| DolocInputSource/IBaseInputActions.OnInteract | Harmony patch candidate | UnityEngine.InputSystem.InputAction/CallbackContext context | Medium |
| DolocInputSource/INormalInputActions.OnInteract | Harmony patch candidate | UnityEngine.InputSystem.InputAction/CallbackContext context | Medium |
| DolocInputSource/INormalInputActions.OnRoomInteract | Harmony patch candidate | UnityEngine.InputSystem.InputAction/CallbackContext context | Medium |
| DolocInputSource/INormalInputActions.OnUseItem | Harmony patch candidate | UnityEngine.InputSystem.InputAction/CallbackContext context | Medium |
| DolocInputSource/INormalInputActions.OnUseTool | Harmony patch candidate | UnityEngine.InputSystem.InputAction/CallbackContext context | Medium |
| DolocInputSource/NormalInputActions.get_Interact | Harmony patch candidate |  | Medium |
| DolocInputSource/NormalInputActions.get_RoomInteract | Harmony patch candidate |  | Medium |
| DolocInputSource/NormalInputActions.get_UseItem | Harmony patch candidate |  | Medium |
| DolocInputSource/NormalInputActions.get_UseTool | Harmony patch candidate |  | Medium |
| DolocTown.AutomateBotUiState.get_OnCloseButtonClick | Harmony patch candidate |  | Risky |
| DolocTown.BoardMissionUiState.get_OnCloseButtonClick | Harmony patch candidate |  | Risky |
| DolocTown.BuilderTipBase.GetUpdateFunc | Harmony patch candidate | DolocTown.DolocInputDeviceType type | Risky |
| DolocTown.BuildingPanelUiState.OnStartButtonClick | Harmony patch candidate | System.Int32 _ | Risky |
| DolocTown.BuildingPanelUiState/<>c__DisplayClass13_0.<OnStartButtonClick>b__0 | Harmony patch candidate | DolocTown.CraftQuantitySubmitUiState state | Risky |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IInputHelper` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocTown.Config.Player.AgentEquipmentFuncProtoGrandmasButton | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Settings.InputActionBinds | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IInputHelper
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- Generated Unity Input System classes are large; prefer wrapping high-level actions.
