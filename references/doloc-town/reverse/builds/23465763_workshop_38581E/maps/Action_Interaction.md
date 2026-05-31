# DolocTown Action / Interaction API Map

## Build

- Steam build: 23465763
- Branch: workshop
- Assembly-CSharp hash: 38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228
- Research date: 2026-05-29
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Map interaction entry points.
- Identify events worth exposing around tool/item use.
- Avoid over-patching generic action states.

## Decompiled scope

- Matched types: 163
- Matched methods: 2855
- Matched fields: 1299
- Matched properties: 817
- Matched events: 19
- Matched call edges: 12886
- Matched strings: 753
- Raw indexes: `maps/index/Action_Interaction-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.InteractableObject | class | Confirmed | visibility=public; methods=72; fields=39 |
| DolocTown.UI.RebindActionSlot | class | Confirmed | visibility=public; methods=50; fields=15 |
| DolocTown.Config.Mission.FactionMissionInfo | class | Confirmed | visibility=public; methods=36; fields=17 |
| DolocInputSource/BaseInputActions | struct | Confirmed | visibility=public; methods=51; fields=1 |
| DolocTown.Config.Mission.TreatyPortFactionInfo | class | Confirmed | visibility=public; methods=36; fields=16 |
| DolocInputSource/NormalInputActions | struct | Confirmed | visibility=public; methods=45; fields=1 |
| DolocInputSource/IBaseInputActions | interface | Confirmed | visibility=public; methods=44; fields=0 |
| DolocTown.Config.Settings.RebindActionInfo | class | Confirmed | visibility=public; methods=31; fields=11 |
| DolocTown.UI.RebindActionUI | class | Confirmed | visibility=public; methods=31; fields=11 |
| DolocInputSource/INormalInputActions | interface | Confirmed | visibility=public; methods=38; fields=0 |
| DolocTown.FactionMissionUiState | class | Confirmed | visibility=public; methods=29; fields=7 |
| DolocTown.InteractiveWater | class | Confirmed | visibility=public; methods=26; fields=7 |
| DolocTown.UI.FactionMissionViewer | class | Confirmed | visibility=public; methods=12; fields=17 |
| DolocTown.Config.UI.GameKeyActionInfo | class | Confirmed | visibility=public; methods=21; fields=7 |
| DolocTown.InteractableObjectLogic | class | Confirmed | visibility=public; methods=22; fields=6 |
| DolocTown.UI.InputActionKeyIcon | class | Confirmed | visibility=public; methods=13; fields=13 |
| DolocTown.UI.TreatyPortFactionData | struct | Confirmed | visibility=public; methods=13; fields=12 |
| DolocTown.FactionMission | class | Confirmed | visibility=public; methods=20; fields=4 |
| AgentStateBase | class | Confirmed | visibility=public; methods=20; fields=3 |
| DolocTown.TreatyPortFaction | class | Confirmed | visibility=public; methods=18; fields=5 |
| DolocTown.UI.RebindActionMask | class | Confirmed | visibility=public; methods=13; fields=10 |
| DolocTown.TreatyPortFactionManager | class | Confirmed | visibility=public; methods=19; fields=3 |
| DolocInputSource/BuilderInputActions | struct | Confirmed | visibility=public; methods=20; fields=1 |
| AgentStateManager | class | Confirmed | visibility=public; methods=16; fields=4 |
| DolocTown.GlobalInteractableObjectManager | class | Confirmed | visibility=public; methods=17; fields=3 |
| DolocTown.RoomScanner | class | Confirmed | visibility=public; methods=13; fields=7 |
| DolocTown.AgentStateFishing | class | Confirmed | visibility=public; methods=18; fields=1 |
| DolocTown.Config.Settings.InputActionBinds | class | Confirmed | visibility=public; methods=15; fields=4 |
| DolocTown.AgentStateFishingWait | class | Confirmed | visibility=public; methods=10; fields=8 |
| DolocTown.FactionMissionManager | class | Confirmed | visibility=public; methods=16; fields=1 |
| DolocTown.RoomInteractableObjectManager | class | Confirmed | visibility=public; methods=14; fields=3 |
| DolocTown.UI.FactionMissionWidget | class | Confirmed | visibility=public; methods=11; fields=6 |
| DolocTown.Config.Mission.FactionMissionTypeInfo | class | Confirmed | visibility=public; methods=12; fields=4 |
| DolocTown.Config.Mission.FactionTypeInfo | class | Confirmed | visibility=public; methods=12; fields=4 |
| DolocTown.ScannerGate | class | Confirmed | visibility=public; methods=11; fields=4 |
| DolocTown.UI.FactionMissionData | struct | Confirmed | visibility=public; methods=2; fields=13 |
| RedSaw.ActionTask | class | Confirmed | visibility=public; methods=12; fields=3 |
| DolocTown.AgentStateDash | class | Confirmed | visibility=public; methods=8; fields=6 |
| DolocTown.BuildingScanner | class | Confirmed | visibility=public; methods=9; fields=5 |
| DolocTown.Config.Settings.RebindActionSettingComponent | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.NodeCanvas.MissionNodeAction | class | Confirmed | visibility=public; methods=13; fields=1 |
| DolocTown.UI.CandidateFactionViewer | class | Confirmed | visibility=public; methods=7; fields=7 |
| DolocTown.UI.FactionListViewer | class | Confirmed | visibility=public; methods=10; fields=4 |
| DolocInputSource/IBuilderInputActions | interface | Confirmed | visibility=public; methods=13; fields=0 |
| DolocTown.Config.Mission.FactionMissionType | enum | Confirmed | visibility=public; methods=0; fields=13 |
| DolocTown.InteractableObjectExclude | class | Confirmed | visibility=public; methods=11; fields=2 |
| DolocInputSource/GlobalActions | struct | Confirmed | visibility=public; methods=11; fields=1 |
| DolocTown.AgentStateInteract | class | Confirmed | visibility=public; methods=8; fields=4 |
| DolocTown.AgentStateTool | class | Confirmed | visibility=public; methods=9; fields=3 |
| DolocTown.GameData.RoutineNode_Action | class | Confirmed | visibility=public; methods=11; fields=1 |
| DolocTown.MotorInteractable | class | Confirmed | visibility=public; methods=10; fields=2 |
| DolocTown.UI.FactionSubSeriesSlot | class | Confirmed | visibility=public; methods=7; fields=5 |
| DolocTown.AgentStateFishingWait/<>c | class | Confirmed | visibility=private; methods=6; fields=5 |
| DolocTown.UI.CandidateFactionData | struct | Confirmed | visibility=public; methods=6; fields=5 |
| DolocTown.UI.RebindActionMask/<DotLoop>d__15 | class | Confirmed | visibility=private; methods=6; fields=5 |
| DolocTown.UI.RebindActionSlot/<WaitToInterrupt>d__61 | class | Confirmed | visibility=private; methods=6; fields=5 |
| DolocTown.UI.TreatyPortFactionViewer | class | Confirmed | visibility=public; methods=3; fields=8 |
| DolocTown.AgentStateFishingPull | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.AgentStateFishingReady | class | Confirmed | visibility=public; methods=7; fields=3 |
| DolocTown.AgentStateWater | class | Confirmed | visibility=public; methods=9; fields=1 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| AgentStateBase.get_SupportInteract | candidate lifecycle or hook point | Medium |
| AgentStateBase.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| AgentStateBase.OnEnter | candidate lifecycle or hook point | Medium |
| AgentStateBase.OnExit | candidate lifecycle or hook point | Medium |
| AgentStateBase.Update | candidate lifecycle or hook point | Medium |
| AgentStateManager.InitStates | candidate lifecycle or hook point | Risky: non-public or generated path |
| BoxUiState.HandleStartUpArgs | candidate lifecycle or hook point | Medium |
| DolocAPI.__LoadStaticAssets | candidate lifecycle or hook point | Medium |
| DolocAPI.Command_LockInteractableObject | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenFactionPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_StartAllFactionMissions | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_StartFactionMission | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_UnlockAllInteractableObject | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_UnlockInteractableObject | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.EnterCity | candidate lifecycle or hook point | Medium |
| DolocAPI.EnterCity | candidate lifecycle or hook point | Medium |
| DolocAPI.EnterDungeon | candidate lifecycle or hook point | Medium |
| DolocAPI.EnterFarm | candidate lifecycle or hook point | Medium |
| DolocAPI.EnterFarm | candidate lifecycle or hook point | Medium |
| DolocAPI.EnterRoom | candidate lifecycle or hook point | Medium |
| DolocAPI.EnterRoom | candidate lifecycle or hook point | Medium |
| DolocAPI.FastButton_OpenFactionPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.get_IsCurrentStateSupportInteract | candidate lifecycle or hook point | Medium |
| DolocAPI.OpenSubmitSingleItemPanel | candidate lifecycle or hook point | Medium |
| DolocAPI.RefreshScanner | candidate lifecycle or hook point | Medium |
| DolocAPI.SetResidentUiInteractable | candidate lifecycle or hook point | Medium |
| DolocAPI.StartFactionMission | candidate lifecycle or hook point | Medium |
| DolocAPI/<>c.<FastButton_OpenFactionPanel>b__1004_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass966_0.<Command_OpenFactionPanel>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAssetCache.Load | candidate lifecycle or hook point | Medium |
| DolocBundleManager.InitDataAsync | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadAsync | candidate lifecycle or hook point | Medium |
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
| DolocResources.loadSpriteAtlas | candidate lifecycle or hook point | Medium |
| DolocResources.loadSpriteAtlas | candidate lifecycle or hook point | Medium |
| DolocTown._TimeInvoker.Start | candidate lifecycle or hook point | Medium |
| DolocTown.AffectorElectric.<OnInteract>b__26_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AffectorElectric.OnInteract | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentControllerState.get_ScannerInteractable | candidate lifecycle or hook point | Medium |
| DolocTown.AgentControllerState.InteractContinues | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentControllerState.UseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AgentControllerState.UseItemContinues | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentControllerState.UseTool | candidate lifecycle or hook point | Medium |
| DolocTown.AgentControllerState.UseToolContinues | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentControllerState.UseToolOrItem | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentControllerState/<>c.<UseTool>b__75_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentStateClimb.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateClimb.OnExit | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateClimbJump.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateDash.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateDash.OnExit | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateDrop.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateDrop.OnExit | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateEat.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFaint.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishing.get_SupportInteract | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishing.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishing.UseFishCam | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentStateFishingBattle.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishingBattle.OnExit | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishingCast.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishingPull.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishingPull.OnExit | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishingReady.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishingReady.OnExit | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateFishingWait.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateHit.get_SupportUseItem | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateHit.OnEnter | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateHit.OnExit | candidate lifecycle or hook point | Medium |
| DolocTown.AgentStateIdle.get_SupportInteract | candidate lifecycle or hook point | Medium |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| AgentStateBase.Check(System.Func`2<T,System.Boolean> condition) | direct call candidate | Medium; needs instance source |
| AgentStateBase.Check() | direct call candidate | Medium; needs instance source |
| AgentStateBase.get_animationNormalizedTime() | direct call candidate | Medium; needs instance source |
| AgentStateBase.get_SupportDash() | direct call candidate | Medium; needs instance source |
| AgentStateBase.get_SupportInteract() | direct call candidate | Medium; needs instance source |
| AgentStateBase.get_SupportJump() | direct call candidate | Medium; needs instance source |
| AgentStateBase.get_SupportScrollQuickInventoryUI() | direct call candidate | Medium; needs instance source |
| AgentStateBase.get_SupportUseItem() | direct call candidate | Medium; needs instance source |
| AgentStateBase.GetState() | direct call candidate | Medium; needs instance source |
| AgentStateBase.GetState(System.Action`1<T> beforeEnter) | direct call candidate | Medium; needs instance source |
| AgentStateBase.OnEnter() | direct call candidate | Medium; needs instance source |
| AgentStateBase.OnExit() | direct call candidate | Medium; needs instance source |
| AgentStateBase.OnPlay() | direct call candidate | Medium; needs instance source |
| AgentStateBase.Update() | direct call candidate | Medium; needs instance source |
| AgentStateManager.__SetState(AgentStateBase state) | direct call candidate | Medium; needs instance source |
| AgentStateManager.CheckState() | direct call candidate | Medium; needs instance source |
| AgentStateManager.FixState(System.Action callback) | direct call candidate | Medium; needs instance source |
| AgentStateManager.get_current() | direct call candidate | Medium; needs instance source |
| AgentStateManager.get_IsFixed() | direct call candidate | Medium; needs instance source |
| AgentStateManager.GetState() | direct call candidate | Medium; needs instance source |
| AgentStateManager.GetStates() | direct call candidate | Medium; needs instance source |
| AgentStateManager.Overwrite(System.Action`1<T> beforeEnter; System.Boolean shouldQuit) | direct call candidate | Medium; needs instance source |
| AgentStateManager.Overwrite(System.Boolean shouldQuit) | direct call candidate | Medium; needs instance source |
| AgentStateManager.Overwrite(AgentStateBase state; System.Boolean shouldQuit) | direct call candidate | Medium; needs instance source |
| AgentStateManager.UnsetFixed() | direct call candidate | Medium; needs instance source |
| BoxUiState.HandleStartUpArgs(DolocTown.IContainer box; System.Action onExit) | direct call candidate | Medium; needs instance source |
| CityPath.GeneratePathActions(System.String src; System.String dest; UnityEngine.Vector2 currentPos; UnityEngine.Vector2 targetPos) | direct call candidate | Medium; needs instance source |
| CityPath/InwalkableArea.Touch(System.Single l; System.Single r) | direct call candidate | Medium; needs instance source |
| DolocAPI.__LoadStaticAssets(System.Action callback) | direct call candidate | Medium |
| DolocAPI.Delay(System.Single time; System.Action callback) | direct call candidate | Medium |
| DolocAPI.DelayFrame(System.Action callback; System.Int32 frame) | direct call candidate | Medium |
| DolocAPI.DoTransport(System.String markPointId; System.Action callback; System.Boolean resetVelocity; System.Boolean disableFadeIn; System.Boolean disableFadeOut) | direct call candidate | Medium |
| DolocAPI.DoTransport(System.String markPointId; UnityEngine.Vector2 offset; System.Action callback; System.Boolean resetVelocity; System.Boolean disableFadeIn; System.Boolean disableFadeOut; DolocTown.TransportOverrideInfo overrideInfo) | direct call candidate | Medium |
| DolocAPI.EnterCity(System.String shortName; UnityEngine.Vector2 remotePosition; System.Action callback) | direct call candidate | Medium |
| DolocAPI.EnterCity(DolocTown.CityRoom room; UnityEngine.Vector2 remotePosition; System.Action callback) | direct call candidate | Medium |
| DolocAPI.EnterDungeon(System.String name; System.Action callback) | direct call candidate | Medium |
| DolocAPI.EnterFarm(DolocTown.TemplateRoom room; UnityEngine.Vector2 position; System.Action callback) | direct call candidate | Medium |
| DolocAPI.EnterFarm(System.String roomGuid; UnityEngine.Vector2 position; System.Action callback) | direct call candidate | Medium |
| DolocAPI.EnterRoom(System.String roomId; UnityEngine.Vector2 position; System.Action callback) | direct call candidate | Medium |
| DolocAPI.EnterRoom(DolocTown.Room room; UnityEngine.Vector2 position; System.Action callback) | direct call candidate | Medium |
| DolocAPI.ExtendFarm(System.String farmName; System.Action`1<System.Boolean> callback) | direct call candidate | Medium |
| DolocAPI.get_CurrentWater() | direct call candidate | Medium |
| DolocAPI.get_IsCurrentStateSupportInteract() | direct call candidate | Medium |
| DolocAPI.GetActionKeyIconGroup(DolocTown.DolocInputDeviceType deviceType; System.String actionName; DolocTown.ActionIconGroup& iconGroup) | direct call candidate | Medium |
| DolocAPI.GetActionKeyIconGroup(System.String actionName; DolocTown.ActionIconGroup& iconGroup) | direct call candidate | Medium |
| DolocAPI.GetAllActionKeyIconGroup(DolocTown.DolocInputDeviceType deviceType; System.String actionName; System.Collections.Generic.List`1<DolocTown.ActionIconGroup>& iconGroups) | direct call candidate | Medium |
| DolocAPI.IsFactionMissionComplete(System.String factionMissionId) | direct call candidate | Medium |
| DolocAPI.OpenSubmitSingleItemPanel(System.Func`2<DolocTown.Item,System.Boolean> itemFilter; System.Func`1<System.Boolean> submitConditionChecker; System.Func`2<DolocTown.Item,System.Int32> itemSubmitCountGetter; System.Func`3<DolocTown.Item,System.Int32,System.String> confirmTextGetter; System.Action`1<System.Int32> onFailedSubmit; System.Action`2<System.Int32,System.Int32> onSubmit) | direct call candidate | Medium |
| DolocAPI.QueryFactionJoinState(DolocTown.Config.Mission.FactionType factionType) | direct call candidate | Medium |
| DolocAPI.QueryTreatyPortFaction(System.String factionName; DolocTown.TreatyPortFaction& faction) | direct call candidate | Medium |
| DolocAPI.RefreshScanner() | direct call candidate | Medium |
| DolocAPI.RegisterMsgListener(DolocTown.Config.Settings.UserSettingType evtType; System.Action`2<System.Object,DolocTown.GameEventArgs> callback) | direct call candidate | Medium |
| DolocAPI.RunGameProcess(DolocTown.NodeCanvas.GameProcessGraph graph; System.Action callback) | direct call candidate | Medium |
| DolocAPI.RunGameProcess(System.String name; System.Action callback) | direct call candidate | Medium |
| DolocAPI.SetResidentUiInteractable(System.Boolean value) | direct call candidate | Medium |
| DolocAPI.ShowConfirmBox(System.String content; System.Action onConfirm) | direct call candidate | Medium |
| DolocAPI.ShowConfirmBox(DolocTown.Config.UI.AlignmentText title; DolocTown.Config.UI.AlignmentText content; DolocTown.Config.UI.AlignmentText signature; System.Action onConfirm) | direct call candidate | Medium |
| DolocAPI.ShowMessageBoxInSceneManual(System.String msg; UnityEngine.Vector2 pos; System.Single durShow; DG.Tweening.Ease ease) | direct call candidate | Medium |
| DolocAPI.ShowMessageBoxInSceneWithIconManual(System.String msg; UnityEngine.Sprite icon; UnityEngine.Vector2 pos; System.Single durShow; DG.Tweening.Ease ease) | direct call candidate | Medium |
| DolocAPI.ShowQuestionBox(System.String title; System.Action onConfirm; System.Action onCancel; System.Boolean firstSelectConfirm) | direct call candidate | Medium |
| DolocAPI.ShowSceneDialogueBox(UnityEngine.Vector2 worldPosition; System.String content; System.Action callback) | direct call candidate | Medium |
| DolocAPI.ShowSleepMenu(System.Action onEnd) | direct call candidate | Medium |
| DolocAPI.ShowSmallTextMenu(System.String[] titles; UnityEngine.Vector2 screenPosition; System.Action`1<System.String> onConfirm) | direct call candidate | Medium |
| DolocAPI.StartFactionMission(System.String missionId) | direct call candidate | Medium |
| DolocAPI.SwingPlantOnTouch(UnityEngine.Animator animator; System.Boolean light) | direct call candidate | Medium |
| DolocAPI.TransitAnimation(System.Action callbackOnAnimation; System.Single waitDuration) | direct call candidate | Medium |
| DolocAPI.TransitFadeInout(System.Action callbackOnFadeIn; System.Single fadeInDuration; System.Single fadeOutDuration; System.Single waitDuration) | direct call candidate | Medium |
| DolocAPI.TryExecute(System.Action callback) | direct call candidate | Medium |
| DolocAPI.UnregisterMsgListener(DolocTown.Config.Settings.UserSettingType evtType; System.Action`2<System.Object,DolocTown.GameEventArgs> handler) | direct call candidate | Medium |
| DolocAPI.WaitUntil(System.Func`1<System.Boolean> predicate; System.Action callback) | direct call candidate | Medium |
| DolocAPI.WaitWhile(System.Func`1<System.Boolean> predicate; System.Action callback) | direct call candidate | Medium |
| DolocAssetCache.Load(System.Action callback) | direct call candidate | Medium; needs instance source |
| DolocBundleManager.LoadAsync(System.Action`1<System.Boolean> callback) | direct call candidate | Medium; needs instance source |
| DolocInputSource.Contains(UnityEngine.InputSystem.InputAction action) | direct call candidate | Medium; needs instance source |
| DolocInputSource.FindAction(System.String actionNameOrId; System.Boolean throwIfNotFound) | direct call candidate | Medium; needs instance source |
| DolocInputSource.FindBinding(UnityEngine.InputSystem.InputBinding bindingMask; UnityEngine.InputSystem.InputAction& action) | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_asset() | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_BaseInput() | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_BuilderInput() | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_Global() | direct call candidate | Medium; needs instance source |
| DolocInputSource.get_NormalInput() | direct call candidate | Medium; needs instance source |
| DolocInputSource.GetEnumerator() | direct call candidate | Medium; needs instance source |
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

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| AgentStateBase.body | DolocTown.BodyController | Risky: reflection/private field | Disable dependent feature if missing |
| AgentStateBase.parent | AgentStateManager | Risky: reflection/private field | Disable dependent feature if missing |
| AgentStateBase.status | DolocTown.AgentPhysicalStatus | Risky: reflection/private field | Disable dependent feature if missing |
| AgentStateManager.<current>k__BackingField | AgentStateBase | Risky: reflection/private field | Disable dependent feature if missing |
| AgentStateManager.<IsFixed>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| AgentStateManager.body | DolocTown.BodyController | Risky: reflection/private field | Disable dependent feature if missing |
| AgentStateManager.states | System.Collections.Generic.Dictionary`2<System.Type,AgentStateBase> | Risky: reflection/private field | Disable dependent feature if missing |
| DevHelper.devMenuItems | System.Collections.Generic.Dictionary`2<System.String,System.Action> | Risky: reflection/private field | Disable dependent feature if missing |
| DevHelper.funcList | System.Collections.Generic.Dictionary`2<System.String,System.Action> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI.ActionTagRegex | System.Text.RegularExpressions.Regex | Risky: reflection/private field | Disable dependent feature if missing |
| DolocCoroutineLoader.callback | System.Action | Risky: reflection/private field | Disable dependent feature if missing |
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
| DolocInputSource.m_Global | UnityEngine.InputSystem.InputActionMap | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_Global_Click | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_Global_CursorPosition | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_Global_Point | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_Global_RightClick | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_GlobalActionsCallbackInterface | DolocInputSource/IGlobalActions | Risky: reflection/private field | Disable dependent feature if missing |
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

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| RedSaw.ActionTask.OnActionEnded | native event | System.Action`1<System.Type> | Confirmed metadata; verify usage |
| RedSaw.CommandLineInterface.LinearSelector.OnSelectionChanged | native event | System.Action`1<System.Int32> | Confirmed metadata; verify usage |
| RedSaw.CommandLineInterface.LogManager`1.OnReceivedMessage | native event | System.Action`1<RedSaw.CommandLineInterface.LogManager`1/Log<T>> | Confirmed metadata; verify usage |
| RedSaw.CommandLineInterface.ConsoleController`1.OnFocusOut | native event | System.Action | Confirmed metadata; verify usage |
| RedSaw.CommandLineInterface.ConsoleController`1.OnFocus | native event | System.Action | Confirmed metadata; verify usage |
| RedSaw.AI.LinearTask.DecisionMaker.OnUpdated | native event | System.Action`1<RedSaw.AI.LinearTask.LinearTask> | Confirmed metadata; verify usage |
| RedSaw.AI.LinearTask.DecisionMaker.OnBegined | native event | System.Action`1<RedSaw.AI.LinearTask.LinearTask> | Confirmed metadata; verify usage |
| RedSaw.AI.LinearTask.DecisionMaker.OnFailure | native event | System.Action`1<RedSaw.AI.LinearTask.LinearTask> | Confirmed metadata; verify usage |
| RedSaw.AI.LinearTask.DecisionMaker.OnBreaked | native event | System.Action`1<RedSaw.AI.LinearTask.LinearTask> | Confirmed metadata; verify usage |
| RedSaw.AI.LinearTask.DecisionMaker.OnSuccessed | native event | System.Action`1<RedSaw.AI.LinearTask.LinearTask> | Confirmed metadata; verify usage |
| DolocTown.ApcManager.OnUpdateEvent | native event | System.Action`1<System.Single> | Confirmed metadata; verify usage |
| DolocTown.ApcManager.OnFixedUpdateEvent | native event | System.Action`1<System.Single> | Confirmed metadata; verify usage |
| DolocTown.ApcManager.OnPauseEvent | native event | System.Action | Confirmed metadata; verify usage |
| DolocTown.ApcManager.OnResumeEvent | native event | System.Action | Confirmed metadata; verify usage |
| DolocTown.MonsterController.OnHurtResumed | native event | System.Action | Confirmed metadata; verify usage |
| DolocTown.SkillFactory.OnFixedUpdated | native event | System.Action`1<System.Single> | Confirmed metadata; verify usage |
| DolocTown.GameEvent.OnEventTriggered | native event | System.Action`2<System.Object,DolocTown.GameEventArgs> | Confirmed metadata; verify usage |
| DolocTown.FishRodHook.OnHooked | native event | System.Action`1<DolocTown.FishingPool> | Confirmed metadata; verify usage |
| DolocTown.DolocUserInput.OnInputDeviceChanged | native event | System.Action`1<DolocTown.DolocInputDeviceType> | Confirmed metadata; verify usage |
| AgentStateBase.get_SupportInteract | Harmony patch candidate |  | Medium |
| AgentStateBase.get_SupportUseItem | Harmony patch candidate |  | Medium |
| AgentStateBase.OnEnter | Harmony patch candidate |  | Medium |
| AgentStateBase.OnExit | Harmony patch candidate |  | Medium |
| AgentStateBase.Update | Harmony patch candidate |  | Medium |
| DolocAPI.__LoadStaticAssets | Harmony patch candidate | System.Action callback | Medium |
| DolocAPI.Command_LockInteractableObject | Harmony patch candidate | System.String lockObjectId | Risky |
| DolocAPI.Command_OpenFactionPanel | Harmony patch candidate | System.String factionName | Risky |
| DolocAPI.Command_StartAllFactionMissions | Harmony patch candidate |  | Risky |
| DolocAPI.Command_StartFactionMission | Harmony patch candidate | System.String name | Risky |
| DolocAPI.Command_UnlockAllInteractableObject | Harmony patch candidate |  | Risky |
| DolocAPI.Command_UnlockInteractableObject | Harmony patch candidate | System.String lockObjectId | Risky |
| DolocAPI.EnterCity | Harmony patch candidate | System.String shortName; UnityEngine.Vector2 remotePosition; System.Action callback | Medium |
| DolocAPI.EnterCity | Harmony patch candidate | DolocTown.CityRoom room; UnityEngine.Vector2 remotePosition; System.Action callback | Medium |
| DolocAPI.EnterDungeon | Harmony patch candidate | System.String name; System.Action callback | Medium |
| DolocAPI.EnterFarm | Harmony patch candidate | DolocTown.TemplateRoom room; UnityEngine.Vector2 position; System.Action callback | Medium |
| DolocAPI.EnterFarm | Harmony patch candidate | System.String roomGuid; UnityEngine.Vector2 position; System.Action callback | Medium |
| DolocAPI.EnterRoom | Harmony patch candidate | System.String roomId; UnityEngine.Vector2 position; System.Action callback | Medium |
| DolocAPI.EnterRoom | Harmony patch candidate | DolocTown.Room room; UnityEngine.Vector2 position; System.Action callback | Medium |
| DolocAPI.FastButton_OpenFactionPanel | Harmony patch candidate |  | Risky |
| DolocAPI.get_IsCurrentStateSupportInteract | Harmony patch candidate |  | Medium |
| DolocAPI.RefreshScanner | Harmony patch candidate |  | Medium |
| DolocAPI.SetResidentUiInteractable | Harmony patch candidate | System.Boolean value | Medium |
| DolocAPI.StartFactionMission | Harmony patch candidate | System.String missionId | Medium |
| DolocAPI/<>c.<FastButton_OpenFactionPanel>b__1004_0 | Harmony patch candidate | DolocTown.FactionMissionUiState state | Risky |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IInteractionHelper.Experimental` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocTown.AnimalTask_HandleInteractable | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.FactionMissionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.FactionMissionType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.FactionMissionTypeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.FactionType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.FactionTypeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbFactionMission | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbFactionMissionType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbFactionType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbTreatyPortFaction | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TreatyPortFactionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.PortalInteractKey | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Settings.InputActionBinds | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Settings.RebindActionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Settings.RebindActionSettingComponent | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Settings.TbRebindAction | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.UI.GameKeyActionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.UI.TbGameKeyAction | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.EquipmentInteractableBridge | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.MissionAttachModule_TimeAction | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.MissionAttachModule_TimeAction/ActionType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.MissionAttachModule_TimeAction/TimingType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.RoutineNode_Action | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.TransactionScheduleConnection | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GlobalInteractableObjectManager | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GlobalInteractableObjectManager/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.IAnimalInteractable | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.IInteractable | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.IInteractableExclude | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.IMonsterInteractable | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.InteractableManager | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.InteractableManager/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.InteractableManagerEx | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.InteractableManagerEx/<>c__DisplayClass5_0 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.InteractableObject | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.InteractableObject/<>c | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.InteractableObject/ToggleType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.InteractableObjectExclude | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.InteractableObjectLogic | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.InteractableSavePoint | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.MonsterInteractableScanner | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.MotorInteractable | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.NodeCanvas.DialogueTask_UnlockInteractableObject | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.RewardInteractableObjectUnlock | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.RoomInteractableObjectManager | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.ScannerInteractable | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.ScannerInteractableOfMotor | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.CandidateFactionData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.FactionItemData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.FactionMissionData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IInteractionHelper
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- Keyword is intentionally broad; consult index CSV before choosing patch points.
