# DolocTown Motor / Vehicle API Map

## Build

- Steam build: 23465763
- Branch: workshop
- Assembly-CSharp hash: 38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228
- Research date: 2026-05-29
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Expose current motor state and summon/unlock helpers.
- Map tuning fields and riding events.
- Avoid promising multi-vehicle registry too early.

## Decompiled scope

- Matched types: 25
- Matched methods: 437
- Matched fields: 216
- Matched properties: 172
- Matched events: 0
- Matched call edges: 1432
- Matched strings: 190
- Raw indexes: `maps/index/Motor-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.MotorController | class | Confirmed | visibility=public; methods=65; fields=46 |
| DolocTown.Config.Item.ToolOverrideInfo | class | Confirmed | visibility=public; methods=19; fields=11 |
| DolocTown.GameData.MotorDataManager | class | Confirmed | visibility=public; methods=17; fields=5 |
| DolocTown.MotorDriverRenderer | class | Confirmed | visibility=public; methods=10; fields=9 |
| DolocTown.Config.Item.ToolOverrideSpawnLut | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.MotorLight | class | Confirmed | visibility=public; methods=7; fields=10 |
| DolocTown.Config.Item.ToolOverrideLevel | class | Confirmed | visibility=public; methods=11; fields=3 |
| DolocTown.MotorController/<GetAllSpriteRenderers>d__109 | class | Confirmed | visibility=private; methods=8; fields=6 |
| DolocTown.MotorController/<FlyToTargetCoroutine>d__112 | class | Confirmed | visibility=private; methods=6; fields=6 |
| DolocTown.MotorInteractable | class | Confirmed | visibility=public; methods=10; fields=2 |
| DolocTown.SpriteOverrideHandler | class | Confirmed | visibility=public; methods=8; fields=4 |
| DolocTown.Config.Item.TbToolOverride | class | Confirmed | visibility=public; methods=8; fields=2 |
| DolocTown.MotorController/<ResumeReboundLatch>d__135 | class | Confirmed | visibility=private; methods=6; fields=4 |
| DolocTown.MotorRenderer | class | Confirmed | visibility=public; methods=6; fields=4 |
| DolocTown.Config.Item.ItemFunctionMotorKey | class | Confirmed | visibility=public; methods=7; fields=1 |
| DolocTown.ItemMotorKey | class | Confirmed | visibility=public; methods=8; fields=0 |
| DolocTown.ScannerInteractableOfMotor | class | Confirmed | visibility=public; methods=6; fields=2 |
| DolocTown.UI.MotorBar | class | Confirmed | visibility=public; methods=2; fields=5 |
| DolocTown.PlayerSpriteOverrideHandler | class | Confirmed | visibility=public; methods=5; fields=1 |
| DolocTown.HatSpriteOverrideHandler | class | Confirmed | visibility=public; methods=5; fields=0 |
| DolocTown.MotorController/<>c__DisplayClass111_0 | class | Confirmed | visibility=private; methods=3; fields=2 |
| DolocTown.PlayerAndMotorChecker/<>c | class | Confirmed | visibility=private; methods=3; fields=2 |
| DolocTown.MotorWorldContentHandle | class | Confirmed | visibility=public; methods=3; fields=0 |
| DolocTown.PlayerAndMotorChecker | class | Confirmed | visibility=public; methods=2; fields=1 |
| DolocTown.TransportOverrideInfo | class | Confirmed | visibility=public; methods=1; fields=2 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.DevMenuItem_EnterDolocMountain | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AgentControllerState.OnUpdateRiding | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.StaticTextInfo.get_UiOperationErrCannotUseIfRiding | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_UiOperationErrCannotUseIfRiding_l10n_key | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.set_UiOperationErrCannotUseIfRiding | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.TbStaticText.get_UiOperationErrCannotUseIfRiding | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Time.TbSeasonWeather.LoadLutOverride | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.DolocUserInput.LoadBindingOverrides | candidate lifecycle or hook point | Medium |
| DolocTown.DolocUserInput.SaveBindingOverrides | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.ArchiveOperationGlobal.UpdateMotorRoom | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.DataPersistenceManager.LoadBindingOverrides | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.DataPersistenceManager.SaveBindingOverrides | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.IFileDataHandler.LoadUserBindingOverrides | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.IFileDataHandler.SaveUserBindingOverrides | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.LocalSave.LoadUserBindingOverrides | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.LocalSave.SaveUserBindingOverrides | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.MotorDataManager.AfterLoadData | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.MotorDataManager.OnEnterRoom | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.MotorDataManager.OnExitRoom | candidate lifecycle or hook point | Medium |
| DolocTown.GameData.MotorDataManager.UpdateMotorRoom | candidate lifecycle or hook point | Medium |
| DolocTown.HatSpriteOverrideHandler.get_useRuntimeOverride | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ItemMotorKey.OnUse | candidate lifecycle or hook point | Medium |
| DolocTown.ItemMotorKey.OnUseAsItem | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ItemMotorKey.OnUseAsTool | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ManagerGate.TryEnterOnMotor | candidate lifecycle or hook point | Medium |
| DolocTown.ManagerGate.TryInteractOnMotor | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ManagerGate.TryInteractOnMotor | candidate lifecycle or hook point | Medium |
| DolocTown.MotorController.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorController.get_motorInteractable | candidate lifecycle or hook point | Medium |
| DolocTown.MotorController.get_scannerInteractable | candidate lifecycle or hook point | Medium |
| DolocTown.MotorController.OnCollisionEnter2D | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorController.OnFixedUpdate | candidate lifecycle or hook point | Medium |
| DolocTown.MotorController.PauseRigidbody | candidate lifecycle or hook point | Medium |
| DolocTown.MotorController.set_motorInteractable | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorController.set_scannerInteractable | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorController.UpdatePerTU | candidate lifecycle or hook point | Medium |
| DolocTown.MotorController.UpdateRotation | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorController.UpdateUiPosition | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorController.UpdateVelocityX | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorController.UpdateVelocityY | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorController.UpdateVelocityYWhileNoInput | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorDriverRenderer.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorDriverRenderer.OnDisable | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorDriverRenderer.OnEnable | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorDriverRenderer.Update | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorDriverRenderer.UpdatePerTU | candidate lifecycle or hook point | Medium |
| DolocTown.MotorInteractable.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorInteractable.get_CanInteractContinues | candidate lifecycle or hook point | Medium |
| DolocTown.MotorInteractable.OnInteract | candidate lifecycle or hook point | Medium |
| DolocTown.MotorLight.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorLight.OnFixedUpdate | candidate lifecycle or hook point | Medium |
| DolocTown.MotorRenderer.Update | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorWorldContentHandle.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.MotorWorldContentHandle.OnTriggerEnter2D | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.PlayerSpriteOverrideHandler.get_useRuntimeOverride | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.PlayerSpriteOverrideHandler.Start | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ScannerInteractableOfMotor.OnTriggerEnter2D | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.ScannerInteractableOfMotor.OnTriggerExit2D | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.SpriteOverrideHandler.Awake | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.SpriteOverrideHandler.get_useModOverride | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.SpriteOverrideHandler.get_useRuntimeOverride | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.SpriteOverrideHandler.LateUpdate | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.SpriteOverrideHandler.OnEnable | candidate lifecycle or hook point | Risky: non-public or generated path |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.DoTransport(System.String markPointId; UnityEngine.Vector2 offset; System.Action callback; System.Boolean resetVelocity; System.Boolean disableFadeIn; System.Boolean disableFadeOut; DolocTown.TransportOverrideInfo overrideInfo) | direct call candidate | Medium |
| DolocAPI.get_IsAgentRiding() | direct call candidate | Medium |
| DolocAPI.get_Motor() | direct call candidate | Medium |
| DolocAPI.GetMotorDirToAgentInMap(UnityEngine.Vector2& dir) | direct call candidate | Medium |
| DolocAPI.GetMotorPosInMap(UnityEngine.Vector2& pos) | direct call candidate | Medium |
| DolocAPI.IsMotor(UnityEngine.Collider2D other) | direct call candidate | Medium |
| DolocAPI.ResetMotorStatus() | direct call candidate | Medium |
| DolocAPI.SetMotorPosition(DolocTown.Room room; UnityEngine.Vector2 position) | direct call candidate | Medium |
| DolocAPI.UnlockMotor(System.Single offset) | direct call candidate | Medium |
| DolocTown.AgentControllerState.get_IsRidingNow() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentControllerState.GetOffIfRiding() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentControllerState.GetOffMotor() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentControllerState.GetOnMotor() | direct call candidate | Medium; needs instance source |
| DolocTown.AgentHatRenderer.SetAsRiding() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimatedGate.get_AvailableToMotor() | direct call candidate | Medium; needs instance source |
| DolocTown.BuildingGate.get_AvailableToMotor() | direct call candidate | Medium; needs instance source |
| DolocTown.BuildingLinkGate.get_AvailableToMotor() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Building.BuildingExteriorData.get_OverrideItemIcon() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionFilm.get_HealingAmount() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionMotorKey.DeserializeItemFunctionMotorKey(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Item.ItemFunctionMotorKey.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionMotorKey.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionMotorKey.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionMotorKey.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ItemFunctionPatch.get_HealingAmount() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.TbToolOverride.Get(System.String key) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.TbToolOverride.get_DataList() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.TbToolOverride.get_DataMap() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.TbToolOverride.get_Item(System.String key) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.TbToolOverride.GetOrDefault(System.String key) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.TbToolOverride.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.TbToolOverride.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideInfo.DeserializeToolOverrideInfo(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Item.ToolOverrideInfo.get_ExtraSpawnLutsByClass() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideInfo.get_ExtraSpawnLutsByName() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideInfo.get_Id() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideInfo.get_Id_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideInfo.get_OverrideLevels() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideInfo.get_OverrideSpawnLuts() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideInfo.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideInfo.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideInfo.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideInfo.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideLevel.DeserializeToolOverrideLevel(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Item.ToolOverrideLevel.get_OverrideLevel() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideLevel.get_TargetResourceClass() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideLevel.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideLevel.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideLevel.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideLevel.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideSpawnLut.DeserializeToolOverrideSpawnLut(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Item.ToolOverrideSpawnLut.get_OverrideSpawnLut() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideSpawnLut.get_OverrideSpawnLut_Ref() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideSpawnLut.get_TargetResourceClass() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideSpawnLut.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideSpawnLut.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideSpawnLut.ToString() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Item.ToolOverrideSpawnLut.TranslateText(System.Func`3<System.String,System.String,System.String> translator) | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_EquipmentBarMotorLock() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_EquipmentBarMotorLock_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_EquipmentBarMotorTitle() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_EquipmentBarMotorTitle_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiOperationErrCannotCallMotor() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiOperationErrCannotCallMotor_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiOperationErrCannotUseIfRiding() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiOperationErrCannotUseIfRiding_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiOperationRide() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiOperationRide_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipCurrentMotorPosition() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipCurrentMotorPosition_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipNotAvailableToMotor() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.StaticTextInfo.get_UiTipNotAvailableToMotor_l10n_key() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_EquipmentBarMotorLock() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_EquipmentBarMotorTitle() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_UiOperationErrCannotCallMotor() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_UiOperationErrCannotUseIfRiding() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_UiOperationRide() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_UiTipCurrentMotorPosition() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Localization.TbStaticText.get_UiTipNotAvailableToMotor() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_HasPlayerBodyOverride() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_HasPlayerHairOverride() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModInfo.get_HasPlayerOverride() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.get_HasPlayerBodyOverride() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.get_HasPlayerHairOverride() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.ModManager.get_HasPlayerOverride() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Player.AgentEquipmentFuncProtoHerbPackage.get_RecoveryAmount() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Player.PlayerAnimationFrameInfo.get_OverrideId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Room.PortalInfo.get_AvailableToMotor() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Room.RoomInfo.get_DisableMotor() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Settings.GlobalParameterInfo.get_DefaultVehiclePreview() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Settings.GlobalParameterInfo.get_MotorCallSpeed() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Settings.GlobalParameterInfo.get_MotorCollisionVerticalSpeed() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Settings.GlobalParameterInfo.get_MotorDropMaxSpeed() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Settings.GlobalParameterInfo.get_MotorEnduranceDuration() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Settings.GlobalParameterInfo.get_MotorEnduranceRecv() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Settings.GlobalParameterInfo.get_MotorGravity() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Settings.GlobalParameterInfo.get_MotorHorizontalAcceleration() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Settings.GlobalParameterInfo.get_MotorHorizontalDeceleration() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Settings.GlobalParameterInfo.get_MotorHorizontalMaxSpeed() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Settings.GlobalParameterInfo.get_MotorHorizontalReboundSpeedRate() | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| DolocAPI.<Motor>k__BackingField | DolocTown.MotorController | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.<IsRidingNow>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.GateScannerOfMotor | DolocTown.ScannerGate | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.interactableScannerOfMotor | DolocTown.ScannerInteractableOfMotor | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.motorController | DolocTown.MotorController | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentControllerState.ScannerDriverOfMotor | DolocTown.ScannerDriver | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererUtils._ridingInstances | System.Collections.Generic.Dictionary`2<System.String,DolocTown.AgentHatRenderer> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Building.BuildingExteriorData.<OverrideItemIcon>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionFilm.<HealingAmount>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionPatch.<HealingAmount>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.TbToolOverride._dataList | System.Collections.Generic.List`1<DolocTown.Config.Item.ToolOverrideInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.TbToolOverride._dataMap | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Config.Item.ToolOverrideInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideInfo.<ExtraSpawnLutsByClass>k__BackingField | DolocTown.Config.Item.ToolExtraSpawnLutByResourceClass[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideInfo.<ExtraSpawnLutsByName>k__BackingField | DolocTown.Config.Item.ToolExtraSpawnLutByResourceName[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideInfo.<Id_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideInfo.<OverrideLevels>k__BackingField | DolocTown.Config.Item.ToolOverrideLevel[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideInfo.<OverrideSpawnLuts>k__BackingField | DolocTown.Config.Item.ToolOverrideSpawnLut[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideLevel.<OverrideLevel>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideLevel.<TargetResourceClass>k__BackingField | DolocTown.Config.Resource.DungeonResourceClass | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideSpawnLut.<OverrideSpawnLut_Ref>k__BackingField | DolocTown.Config.Item.ItemSpawnInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideSpawnLut.<OverrideSpawnLut>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ToolOverrideSpawnLut.<TargetResourceClass>k__BackingField | DolocTown.Config.Resource.DungeonResourceClass | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<EquipmentBarMotorLock_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<EquipmentBarMotorLock>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<EquipmentBarMotorTitle_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<EquipmentBarMotorTitle>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationErrCannotCallMotor_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationErrCannotCallMotor>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationErrCannotUseIfRiding_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationErrCannotUseIfRiding>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationRide_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiOperationRide>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipCurrentMotorPosition_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipCurrentMotorPosition>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipNotAvailableToMotor_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<UiTipNotAvailableToMotor>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.<HasPlayerBodyOverride>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.<HasPlayerHairOverride>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModInfo.<HasPlayerOverride>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.<HasPlayerBodyOverride>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.<HasPlayerHairOverride>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.ModManager.<HasPlayerOverride>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Player.AgentEquipmentFuncProtoHerbPackage.<RecoveryAmount>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Player.PlayerAnimationFrameInfo.<OverrideId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Room.PortalInfo.<AvailableToMotor>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Room.RoomInfo.<DisableMotor>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<DefaultVehiclePreview>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorCallSpeed>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorCollisionVerticalSpeed>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorDropMaxSpeed>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorEnduranceDuration>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorEnduranceRecv>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorGravity>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorHorizontalAcceleration>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorHorizontalDeceleration>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorHorizontalMaxSpeed>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorHorizontalReboundSpeedRate>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorHorizontalRevertAcceleration>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorNaturalJumpAccRange>k__BackingField | UnityEngine.Vector2 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorNaturalJumpMaxSpeed>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorNaturalJumpThreshold>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorRaycastLength>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorVerticalAcceleration>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorVerticalMaxSpeed>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Settings.GlobalParameterInfo.<MotorVerticalReboundSpeedRate>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Tables.<TbToolOverride>k__BackingField | DolocTown.Config.Item.TbToolOverride | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Time.SeasonWeatherInfo.<WeatherOverride>k__BackingField | DolocTown.Config.Weather.WeatherType[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Time.TbSeasonWeather.weatherLutOverride | System.Collections.Generic.Dictionary`2<UnityEngine.Vector2Int,System.ValueTuple`2<System.Int32,DolocTown.Config.Weather.WeatherType>> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.DialogueData.<overrideEntrance>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.EntityOutlineRenderer.overrideSprite | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.GameData.MotorDataManager.<CurrentRoom>k__BackingField | DolocTown.Room | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.GameData.MotorDataManager.<dungeonName>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.GameData.MotorDataManager.<isUnlocked>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.GameData.MotorDataManager.<roomId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.GameData.MotorDataManager.initPosition | UnityEngine.Vector2 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.GameRendererEntity.<overrideOutlineSprite>k__BackingField | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Gate.motorReference | UnityEngine.Transform | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MonsterMoverContextSteering.overridenPath | UnityEngine.Vector2Int[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController._reboundLatch | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.<driverRenderer>k__BackingField | DolocTown.MotorDriverRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.<IsRiding>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.<motorInteractable>k__BackingField | DolocTown.MotorInteractable | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.<motorRenderer>k__BackingField | DolocTown.MotorRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.<rb>k__BackingField | UnityEngine.Rigidbody2D | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.<scannerGate>k__BackingField | DolocTown.ScannerGate | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.<scannerInteractable>k__BackingField | DolocTown.ScannerInteractableOfMotor | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.accelerationRX | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.accelerationX | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.autoFollowCoroutine | UnityEngine.Coroutine | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.currentDstToGround | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.currentEndurance | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.currentInputX | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.currentInputY | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.currentVelocityX | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.currentVelocityY | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.decelerationX | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.droneFollowPoint | UnityEngine.Transform | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.droneInteractableBox | UnityEngine.GameObject | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.MotorController.enduranceDuration | System.Single | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| DolocAPI.DevMenuItem_EnterDolocMountain | Harmony patch candidate |  | Risky |
| DolocTown.AgentControllerState.OnUpdateRiding | Harmony patch candidate | System.Single dt; DolocTown.AgentBehaviorSettings settings | Risky |
| DolocTown.Config.Localization.StaticTextInfo.get_UiOperationErrCannotUseIfRiding | Harmony patch candidate |  | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_UiOperationErrCannotUseIfRiding_l10n_key | Harmony patch candidate |  | Medium |
| DolocTown.Config.Localization.StaticTextInfo.set_UiOperationErrCannotUseIfRiding | Harmony patch candidate | System.String value | Risky |
| DolocTown.Config.Localization.TbStaticText.get_UiOperationErrCannotUseIfRiding | Harmony patch candidate |  | Medium |
| DolocTown.Config.Time.TbSeasonWeather.LoadLutOverride | Harmony patch candidate |  | Risky |
| DolocTown.DolocUserInput.LoadBindingOverrides | Harmony patch candidate |  | Medium |
| DolocTown.DolocUserInput.SaveBindingOverrides | Harmony patch candidate |  | Medium |
| DolocTown.GameData.ArchiveOperationGlobal.UpdateMotorRoom | Harmony patch candidate | DolocTown.GameData.ArchiveDataHandle handle; DolocTown.Room room | Medium |
| DolocTown.GameData.DataPersistenceManager.LoadBindingOverrides | Harmony patch candidate | System.String& rebinds | Medium |
| DolocTown.GameData.DataPersistenceManager.SaveBindingOverrides | Harmony patch candidate | System.String rebinds | Medium |
| DolocTown.GameData.IFileDataHandler.LoadUserBindingOverrides | Harmony patch candidate | System.String& rebinds | Medium |
| DolocTown.GameData.IFileDataHandler.SaveUserBindingOverrides | Harmony patch candidate | System.String rebinds | Medium |
| DolocTown.GameData.LocalSave.LoadUserBindingOverrides | Harmony patch candidate | System.String& rebinds | Medium |
| DolocTown.GameData.LocalSave.SaveUserBindingOverrides | Harmony patch candidate | System.String rebinds | Medium |
| DolocTown.GameData.MotorDataManager.AfterLoadData | Harmony patch candidate |  | Medium |
| DolocTown.GameData.MotorDataManager.OnEnterRoom | Harmony patch candidate | DolocTown.Room room | Medium |
| DolocTown.GameData.MotorDataManager.OnExitRoom | Harmony patch candidate | DolocTown.Room room | Medium |
| DolocTown.GameData.MotorDataManager.UpdateMotorRoom | Harmony patch candidate | DolocTown.Room room | Medium |
| DolocTown.HatSpriteOverrideHandler.get_useRuntimeOverride | Harmony patch candidate |  | Risky |
| DolocTown.ItemMotorKey.OnUse | Harmony patch candidate |  | Medium |
| DolocTown.ItemMotorKey.OnUseAsItem | Harmony patch candidate |  | Risky |
| DolocTown.ItemMotorKey.OnUseAsTool | Harmony patch candidate |  | Risky |
| DolocTown.ManagerGate.TryEnterOnMotor | Harmony patch candidate |  | Medium |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IMotorHelper.Experimental` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocTown.Config.Item.ItemFunctionMotorKey | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.TbToolOverride | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ToolOverrideInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ToolOverrideLevel | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.ToolOverrideSpawnLut | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.MotorDataManager | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.MotorInteractable | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.ScannerInteractableOfMotor | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.TransportOverrideInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IMotorHelper
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- See docs/research-DolocTown-Motor-Vehicle-API.md for the hand-reviewed version.
