# DolocTown NPC / Dialogue / Schedule API Map

## Build

- Steam build: 23465763
- Branch: workshop
- Assembly-CSharp hash: 38581EE024D3808D4D73098E10E5A122F93AC31BEDE9961D709A0B68571D7228
- Research date: 2026-05-29
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Map dialogue and NPC schedule systems.
- Find safe query helpers for NPC state.
- Keep story/quest mutation out of stable API initially.

## Decompiled scope

- Matched types: 487
- Matched methods: 3898
- Matched fields: 1950
- Matched properties: 1227
- Matched events: 0
- Matched call edges: 13543
- Matched strings: 2163
- Raw indexes: `maps/index/NPC_Dialogue-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.Npc | class | Confirmed | visibility=public; methods=83; fields=22 |
| DolocTown.Config.NPC.NpcInfo | class | Confirmed | visibility=public; methods=60; fields=26 |
| DolocTown.DialogueRunner | class | Confirmed | visibility=public; methods=46; fields=21 |
| DolocTown.DialogueManager | class | Confirmed | visibility=public; methods=55; fields=10 |
| DolocTown.DialogueState | class | Confirmed | visibility=public; methods=42; fields=17 |
| DolocTown.Config.Mission.FactionMissionInfo | class | Confirmed | visibility=public; methods=36; fields=17 |
| DolocTown.Config.Mission.TreatyPortFactionInfo | class | Confirmed | visibility=public; methods=36; fields=16 |
| DolocTown.NpcRenderer | class | Confirmed | visibility=public; methods=40; fields=10 |
| DolocTown.Config.Mission.BoardMissionInfo | class | Confirmed | visibility=public; methods=31; fields=13 |
| DolocTown.Config.Mission.ItemSubmitConditionInfo | class | Confirmed | visibility=public; methods=30; fields=12 |
| DolocTown.MissionManager | class | Confirmed | visibility=public; methods=35; fields=7 |
| DolocTown.Config.Mission.MissionInfo | class | Confirmed | visibility=public; methods=28; fields=13 |
| DolocTown.Config.NPC.IdleTalkNodeInfo | class | Confirmed | visibility=public; methods=29; fields=12 |
| DolocTown.DialoguePlayer | class | Confirmed | visibility=public; methods=27; fields=14 |
| DolocTown.Config.NPC.NpcDocumentInfo | class | Confirmed | visibility=public; methods=24; fields=12 |
| DolocTown.FactionMissionUiState | class | Confirmed | visibility=public; methods=29; fields=7 |
| DolocTown.UI.BubbleDialoguePanel | class | Confirmed | visibility=public; methods=22; fields=14 |
| DolocTown.BoardMissionManager | class | Confirmed | visibility=public; methods=30; fields=5 |
| DolocTown.MissionWithBoard | class | Confirmed | visibility=public; methods=28; fields=5 |
| DolocTown.NodeCanvas.MissionNodeListener | class | Confirmed | visibility=public; methods=24; fields=9 |
| DolocTown.Mission | class | Confirmed | visibility=public; methods=25; fields=6 |
| DolocTown.MissionChainHandle | class | Confirmed | visibility=public; methods=28; fields=3 |
| DolocTown.UI.FactionMissionViewer | class | Confirmed | visibility=public; methods=12; fields=17 |
| DolocTown.UI.NpcDetailData | struct | Confirmed | visibility=public; methods=15; fields=14 |
| DolocTown.Config.Mission.MissionTypeInfo | class | Confirmed | visibility=public; methods=20; fields=8 |
| DolocTown.Config.Mission.MissionNodeInfo | class | Confirmed | visibility=public; methods=19; fields=8 |
| DolocTown.IMission | interface | Confirmed | visibility=public; methods=27; fields=0 |
| DolocTown.NodeCanvas.MissionRequireTemplateUniversal | class | Confirmed | visibility=public; methods=14; fields=13 |
| DolocTown.MissionChainManager | class | Confirmed | visibility=public; methods=20; fields=6 |
| DolocTown.NodeCanvas.MissionGraph | class | Confirmed | visibility=public; methods=24; fields=2 |
| DolocTown.UI.MissionTipManager | class | Confirmed | visibility=public; methods=18; fields=8 |
| DolocTown.GameData.ArchiveOperationMission | static class | Confirmed | visibility=public; methods=25; fields=0 |
| DolocTown.Config.Mission.BoardMissionTypeInfo | class | Confirmed | visibility=public; methods=17; fields=7 |
| DolocTown.FactionMission | class | Confirmed | visibility=public; methods=20; fields=4 |
| DolocTown.Config.Festival.NpcFestivalInfo | class | Confirmed | visibility=public; methods=17; fields=6 |
| DolocTown.Config.Mission.ItemGeneConditionInfo | class | Confirmed | visibility=public; methods=17; fields=6 |
| DolocTown.UI.MissionViewer | class | Confirmed | visibility=public; methods=7; fields=16 |
| DolocTown.AsideDialoguePanel | class | Confirmed | visibility=public; methods=17; fields=5 |
| DolocTown.NodeCanvas.MissionNodeSubGraph | class | Confirmed | visibility=public; methods=18; fields=4 |
| DolocTown.Config.Dialogue.DialogueEntityInfo | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Room.DialogueObjectInfo | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.DialogueData | class | Confirmed | visibility=public; methods=16; fields=4 |
| DolocTown.UI.MissionData | struct | Confirmed | visibility=public; methods=3; fields=17 |
| DolocTown.Config.Archives.DocumentMissionTip | class | Confirmed | visibility=public; methods=14; fields=5 |
| DolocTown.DefaultDialogueTarget | class | Confirmed | visibility=public; methods=19; fields=0 |
| DolocTown.GameData.MissionContentHandleMulti | class | Confirmed | visibility=public; methods=16; fields=3 |
| DolocTown.LinearMissionPatch | static class | Confirmed | visibility=public; methods=19; fields=0 |
| DolocTown.NodeCanvas.MissionGraph/<GetMissionNodesInAchievementMode>d__36 | class | Confirmed | visibility=private; methods=10; fields=9 |
| DolocTown.NoneDialogueTarget | class | Confirmed | visibility=public; methods=19; fields=0 |
| DolocTown.NpcManager | class | Confirmed | visibility=public; methods=18; fields=1 |
| DolocTown.SubmitItemToNpcUiState | class | Confirmed | visibility=public; methods=16; fields=3 |
| DolocTown.UI.BoardMissionData | class | Confirmed | visibility=public; methods=11; fields=8 |
| DolocTown.UI.NpcViewer | class | Confirmed | visibility=public; methods=3; fields=16 |
| DolocTown.IDialogueEntity | interface | Confirmed | visibility=public; methods=18; fields=0 |
| DolocTown.NodeCanvas.MissionNodeListenerBase | class | Confirmed | visibility=public; methods=15; fields=3 |
| DolocTown.UI.SceneDialogueBox | class | Confirmed | visibility=public; methods=14; fields=4 |
| DolocTown.Config.Dialogue.DialogueOptionInfo | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.Config.Item.MissionItemInfo | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.Config.Mission.BoardMissionLevelInfo | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.Config.Mission.MapMissionTip | class | Confirmed | visibility=public; methods=13; fields=4 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.Command_OpenBoardMissionPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RefreshBoardMission | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RefreshNpcCelebrationOrder | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RestartMission | candidate lifecycle or hook point | Medium |
| DolocAPI.Command_StartAllFactionMissions | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_StartAllMissionChains | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_StartBoardMission | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_StartFactionMission | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_StartMissionChain | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.InitNpcInFestival | candidate lifecycle or hook point | Medium |
| DolocAPI.OpenPermissionViewChipDoc | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.OpenPermissionViewPlantDoc | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.RefreshResidentMissionTip | candidate lifecycle or hook point | Medium |
| DolocAPI.StartDialogueNode | candidate lifecycle or hook point | Medium |
| DolocAPI.StartFactionMission | candidate lifecycle or hook point | Medium |
| DolocAPI.StartMissionChain | candidate lifecycle or hook point | Medium |
| DolocAPI/<>c.<FastButton_OpenFactionPanel>b__1004_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass966_0.<Command_OpenFactionPanel>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadMissionChains | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocBundleManager.LoadNpcSchedules | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AchievementSystem._StartMission | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AchievementSystem.DolocTown.IMissionManagerComponent.OnMissionStart | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AchievementSystem.LoadAchievementMissions | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AchievementSystem.LoadMissionGraph | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.<__Init>b__8_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.<__Init>b__8_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.OnStartHide | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.OnStartShow | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.Pause | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateBotDecisionMakerProcessing.StartProcessing | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BoardMissionManager.OnLoadData | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BoardMissionManager.RefreshBoardMissionPool | candidate lifecycle or hook point | Medium |
| DolocTown.BoardMissionManager.RefreshFixedMission | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BoardMissionManager.RefreshNoticeBoardTip | candidate lifecycle or hook point | Medium |
| DolocTown.BoardMissionManager.RefreshRandomMission | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BoardMissionManager.StartBoardMission | candidate lifecycle or hook point | Medium |
| DolocTown.BoardMissionMessage.OnMissionStart | candidate lifecycle or hook point | Medium |
| DolocTown.BoardMissionUiState.get_OnCloseButtonClick | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BoardMissionUiState.OnUiUpdate | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.BoardMissionUiState.RefreshPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionBookUiState.OpenMapWithMission | candidate lifecycle or hook point | Medium |
| DolocTown.CollectionBookUiState.OpenNpcArchiveWithId | candidate lifecycle or hook point | Medium |
| DolocTown.CollectionBookUiState.RefreshNpcList | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionBookUiState/<>c__DisplayClass39_0.<OpenNpcArchiveWithId>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CollectionManager.RefreshNpcRecord | candidate lifecycle or hook point | Medium |
| DolocTown.CommandDefines.InitNpcInFestival | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CommandDefines.StartDialogueNode | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Festival.NpcFestivalInfo.get_AutoInit | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Festival.NpcFestivalInfo.set_AutoInit | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.StaticTextInfo.get_MissionUpdate | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_MissionUpdate_l10n_key | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.set_MissionUpdate | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.TbStaticText.get_MissionUpdate | candidate lifecycle or hook point | Medium |
| DolocTown.Config.ModManager.ProcessNextLocalModUploadPlanRequest | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.ModManager/<>c__DisplayClass82_0.<ProcessNextLocalModUploadPlanRequest>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.NPC.NpcInfo.get_InitialDialogueEntry | candidate lifecycle or hook point | Medium |
| DolocTown.Config.NPC.NpcInfo.get_InitialMarkPoint | candidate lifecycle or hook point | Medium |
| DolocTown.Config.NPC.NpcInfo.get_InitialMarkPoint_Ref | candidate lifecycle or hook point | Medium |
| DolocTown.Config.NPC.NpcInfo.get_ScheduleInitialState | candidate lifecycle or hook point | Medium |
| DolocTown.Config.NPC.NpcInfo.get_ShouldPreload | candidate lifecycle or hook point | Medium |
| DolocTown.Config.NPC.NpcInfo.LoadStreetPoints | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.NPC.NpcInfo.set_InitialDialogueEntry | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.NPC.NpcInfo.set_InitialMarkPoint | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.NPC.NpcInfo.set_InitialMarkPoint_Ref | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.NPC.NpcInfo.set_ScheduleInitialState | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.NPC.NpcInfo.set_ShouldPreload | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.NPC.NpcInfo/<>c.<LoadStreetPoints>b__115_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.NPC.NpcInfo/<>c.<LoadStreetPoints>b__115_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Room.DialogueObjectInfo.get_RefreshType | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Room.DialogueObjectInfo.set_RefreshType | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Settings.GlobalParameterInfo.get_MissionRefreshDay | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Settings.GlobalParameterInfo.get_MissionRefreshWeights | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Settings.GlobalParameterInfo.set_MissionRefreshDay | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Settings.GlobalParameterInfo.set_MissionRefreshWeights | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Settings.TbGlobalParameter.get_MissionRefreshDay | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Settings.TbGlobalParameter.get_MissionRefreshWeights | candidate lifecycle or hook point | Medium |
| DolocTown.CropGeneFunctionTimeGift.UpdateEx | candidate lifecycle or hook point | Medium |
| DolocTown.DefaultDialogueTarget.StartSay | candidate lifecycle or hook point | Medium |
| DolocTown.DialogueData.get_useCandidateNodes | candidate lifecycle or hook point | Medium |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| DolocAPI.AddDialogueNode(System.String nodeName; System.String npcName) | direct call candidate | Medium |
| DolocAPI.AddResidentMissionTip(System.String id; System.Boolean useSound) | direct call candidate | Medium |
| DolocAPI.AddTargetNpcLikingValue(System.String npcName; System.Single value) | direct call candidate | Medium |
| DolocAPI.AppendDialogueHistoryLine(System.String npcName; System.String npcTitle; System.String content) | direct call candidate | Medium |
| DolocAPI.AppendDialogueHistoryOption(System.String content) | direct call candidate | Medium |
| DolocAPI.CheckDialogueEvent(System.String npcName) | direct call candidate | Medium |
| DolocAPI.ClearAllMissionTip() | direct call candidate | Medium |
| DolocAPI.Command_AddMissionItem(System.String missionItemId; System.String markPointId) | direct call candidate | Medium |
| DolocAPI.Command_IsNpcBirthday(System.String npcName; System.Int32 dayOffset) | direct call candidate | Medium |
| DolocAPI.Command_RestartMission(System.String chainId; System.String missionId; System.Boolean fromParent) | direct call candidate | Medium |
| DolocAPI.CreateMissionItem(System.String missionItemId; System.String markPointId) | direct call candidate | Medium |
| DolocAPI.DisableNpcSchedule(System.String npcName; System.String markPointId) | direct call candidate | Medium |
| DolocAPI.DoWebRequest(UnityEngine.Networking.UnityWebRequest req) | direct call candidate | Medium |
| DolocAPI.EnableNpcSchedule(System.String npcName) | direct call candidate | Medium |
| DolocAPI.get_CurrentDialogueHostTransform() | direct call candidate | Medium |
| DolocAPI.GetDefaultNpcNameForNode(System.String nodeName) | direct call candidate | Medium |
| DolocAPI.GetDialogueLineView(System.String targetName) | direct call candidate | Medium |
| DolocAPI.GetDialogueTargetViewOrDefault(System.String targetName; System.Boolean force) | direct call candidate | Medium |
| DolocAPI.GetMissionLogs(System.String missionId) | direct call candidate | Medium |
| DolocAPI.GetNpcLikingLvLimit(System.String npcName) | direct call candidate | Medium |
| DolocAPI.GetNpcsInScene(System.Int32 sceneIndex) | direct call candidate | Medium |
| DolocAPI.GetNpcTitle(System.String npcName; System.Boolean ignoreUnknown) | direct call candidate | Medium |
| DolocAPI.InitNpcInFestival(System.String npcName) | direct call candidate | Medium |
| DolocAPI.IsFactionMissionComplete(System.String factionMissionId) | direct call candidate | Medium |
| DolocAPI.IsMissionComplete(System.String id) | direct call candidate | Medium |
| DolocAPI.IsMissionInProcess(System.String missionId) | direct call candidate | Medium |
| DolocAPI.IsMissionListening(System.String id) | direct call candidate | Medium |
| DolocAPI.IsNpcAtMarkPoint(System.String npcName; System.String markPointName; System.Single threshold) | direct call candidate | Medium |
| DolocAPI.IsNpcAtScene(System.String npcName; System.String sceneName) | direct call candidate | Medium |
| DolocAPI.IsNpcBirthday(System.String npcName; DolocTown.GameData.DateInfo dateInfo) | direct call candidate | Medium |
| DolocAPI.IsVisitedNpcName(System.String npcName) | direct call candidate | Medium |
| DolocAPI.MoveNpcToVoid(System.String npcName) | direct call candidate | Medium |
| DolocAPI.QueryFactionJoinState(DolocTown.Config.Mission.FactionType factionType) | direct call candidate | Medium |
| DolocAPI.QueryGiftLevel(System.String npcName; System.String itemName) | direct call candidate | Medium |
| DolocAPI.QueryNpc(System.String npcName; DolocTown.Npc& npc) | direct call candidate | Medium |
| DolocAPI.QueryNpcBirthdayByDate(System.Int32 month; System.Int32 day) | direct call candidate | Medium |
| DolocAPI.QueryNpcBirthdayByName(System.String npcName; System.Int32& month; System.Int32& day) | direct call candidate | Medium |
| DolocAPI.QueryNpcLikingInfo(System.String npcName; DolocTown.Liking& liking) | direct call candidate | Medium |
| DolocAPI.QueryNpcLikingLv(System.String npcName) | direct call candidate | Medium |
| DolocAPI.QueryNpcRenderer(System.String npcName; DolocTown.NpcRenderer& npcRenderer) | direct call candidate | Medium |
| DolocAPI.RefreshResidentMissionTip(System.String id) | direct call candidate | Medium |
| DolocAPI.RemoveDialogueNode(System.String nodeName; System.String npcName) | direct call candidate | Medium |
| DolocAPI.RemoveResidentMissionTip(System.String id) | direct call candidate | Medium |
| DolocAPI.RevertAllDialogueEntitiesSortingOrder() | direct call candidate | Medium |
| DolocAPI.SetAllNpcAutoFlipState(System.Boolean value) | direct call candidate | Medium |
| DolocAPI.SetAllNpcEventState(System.Boolean value) | direct call candidate | Medium |
| DolocAPI.SetDialogueEntrance(System.String nodeName; System.String npcName) | direct call candidate | Medium |
| DolocAPI.SetDialogueTargetForeground(System.String targetName; System.Boolean active) | direct call candidate | Medium |
| DolocAPI.SetNpcToCurrentScene(System.String npcName) | direct call candidate | Medium |
| DolocAPI.SetNpcToMarkPoint(System.String npcName; System.String markPointName) | direct call candidate | Medium |
| DolocAPI.SetNpcToScene(System.String npcName; System.String sceneName) | direct call candidate | Medium |
| DolocAPI.ShowQuestionBox(System.String title; System.Action onConfirm; System.Action onCancel; System.Boolean firstSelectConfirm) | direct call candidate | Medium |
| DolocAPI.ShowSceneDialogueBox(UnityEngine.Vector2 worldPosition; System.String content; System.Action callback) | direct call candidate | Medium |
| DolocAPI.StartDialogueNode(System.String nodeName; System.String npcName) | direct call candidate | Medium |
| DolocAPI.StartFactionMission(System.String missionId) | direct call candidate | Medium |
| DolocAPI.StartMissionChain(System.String missionId) | direct call candidate | Medium |
| DolocAPI.TryGiftItemToNpc(System.String targetNpc; DolocTown.Item item; System.Int32 itemIndex) | direct call candidate | Medium |
| DolocBundleManager.get_missionChains() | direct call candidate | Medium; needs instance source |
| DolocBundleManager.get_npcSchedules() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_ContinueDialogue() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_ToggleDialogueHistory() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_ToggleMissionPanel() | direct call candidate | Medium; needs instance source |
| DolocInputSource/IBaseInputActions.OnContinueDialogue(UnityEngine.InputSystem.InputAction/CallbackContext context) | direct call candidate | Medium; needs instance source |
| DolocInputSource/IBaseInputActions.OnToggleDialogueHistory(UnityEngine.InputSystem.InputAction/CallbackContext context) | direct call candidate | Medium; needs instance source |
| DolocInputSource/IBaseInputActions.OnToggleMissionPanel(UnityEngine.InputSystem.InputAction/CallbackContext context) | direct call candidate | Medium; needs instance source |
| DolocInputSource/INormalInputActions.OnToggleMissionPanel(UnityEngine.InputSystem.InputAction/CallbackContext context) | direct call candidate | Medium; needs instance source |
| DolocInputSource/NormalInputActions.get_ToggleMissionPanel() | direct call candidate | Medium; needs instance source |
| DolocResources.get_defaultNpcController() | direct call candidate | Medium |
| DolocTown.AchievementSystem.get_LockedAchievements() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.get_InAnimation() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.get_InRender() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.get_IsPlaying() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.get_isPlaying() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.Pause() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.Render(System.String npcName; System.String content) | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.Resume() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.SetLineViewPosition(UnityEngine.Vector2 worldPosition) | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.Skip() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.WaitOption() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateUtils.ClipMissions(System.Collections.Generic.Dictionary`2<DolocTown.Case,System.Collections.Generic.List`1<T>> plantMissions) | direct call candidate | Medium |
| DolocTown.BoardMission.CreateMissionContent(DolocTown.GameData.MissionContent& content) | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMission.get_Content() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMission.get_Disappear() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMission.get_GameEventType() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMission.get_Id() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMission.get_IsFixedMission() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMission.get_IsValid() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMission.get_MissionLv() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMission.get_RewardId() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMission.get_Rewards() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMission.get_TimeLimit() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMissionManager.AcceptBoardMission(System.String missionId) | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMissionManager.AddFixedMission(System.String missionId) | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMissionManager.CanTakeMission(System.String missionLv) | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMissionManager.get_HasNewMission() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMissionManager.get_IssueAllMissions() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMissionManager.get_MissionLevel() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMissionManager.get_Parameter() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMissionManager.GetCurrentBattleLv() | direct call candidate | Medium; needs instance source |
| DolocTown.BoardMissionManager.GetMissionExp(System.String missionLv) | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| DebugLight.lightEmissionTex | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_GiftItemToNpc>d__957.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_SubmitBoardMissionItem>d__959.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_SubmitItemToNpc>d__945.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_SubmitItemToNpcByInfo>d__946.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<missionChains>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.NodeCanvas.MissionGraph> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocBundleManager.<npcSchedules>k__BackingField | DolocTown.GameData.SimpleDatabase`1<DolocTown.NodeCanvas.NpcScheduleGraph> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ContinueDialogue | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ToggleDialogueHistory | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ToggleMissionPanel | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_ToggleMissionPanel | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AchievementSystem.configuredAchievements | System.Collections.Generic.Dictionary`2<System.String,DolocTown.NodeCanvas.MissionNodeListener> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AchievementSystem.lockedAchievements | System.Collections.Generic.List`1<DolocTown.IMission> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AchievementSystem.missionGraph | DolocTown.NodeCanvas.MissionGraph | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererDrone66Mask.emissionColor | UnityEngine.Color | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererDrone66Mask.emissionShader | UnityEngine.Shader | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererMinerHelmet.emissionColor | UnityEngine.Color | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AgentHatRendererMinerHelmet.emissionShader | UnityEngine.Shader | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimatedGate/<TransportAnim>d__39.<player>5__3 | DolocTown.IDialogueEntity | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AsideDialoguePanel.<isPlaying>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AsideDialoguePanel.contentText | TMPro.TMP_Text | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AsideDialoguePanel.currentCharacterIndex | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AsideDialoguePanel.currentContent | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AsideDialoguePanel.textPlayer | Febucci.UI.TextAnimatorPlayer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBotDecisionMakerProcessing.lastMission | DolocTown.ProcessingMission | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateTaskPlant._missionInfo | DolocTown.AutomatePlantMissionInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BoardMission.leftTime | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BoardMission.rewardId | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BoardMissionManager.<IssueAllMissions>k__BackingField | DolocTown.BoardMission[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BoardMissionManager.fixedMissionAlternativePool | System.Collections.Generic.List`1<System.String> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BoardMissionManager.fixedMissionPool | System.Collections.Generic.Queue`1<System.String> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BoardMissionManager.hasNewMission | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BoardMissionManager.randomMissionPool | System.Collections.Generic.List`1<System.String> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BodyController.isForegroundInDialogue | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Bomb.emissionColor | UnityEngine.Color | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Bomb.emissionMask | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BuffManager.requestList | System.Collections.Generic.Queue`1<DolocTown.BuffManager/BuffRequest> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CollectionBookUiState.npcList | System.Collections.Generic.List`1<System.String> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<PlayNpcAnimation>d__42.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.ConditionChecker.factionMissionCondition | DolocTown.FactionMissionCondition | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.ConditionChecker.missionCondition | DolocTown.MissionCondition | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Animal.AnimalInfo.<ScheduleId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.DocumentMissionTip.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.DocumentMissionTip.<LikingLevel>k__BackingField | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.DocumentMissionTip.<Tip_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Archives.DocumentMissionTip.<Tip>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.DialogueEntityInfo.<ContentColor>k__BackingField | UnityEngine.Color | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.DialogueEntityInfo.<EntityType>k__BackingField | DolocTown.Config.Dialogue.DialogueEntityType | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.DialogueEntityInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.DialogueEntityInfo.<SpeakerColor>k__BackingField | UnityEngine.Color | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.DialogueOptionInfo.<IconText>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.DialogueOptionInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.DialogueOptionInfo.<Order>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.DialogueTextStyleInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.DialogueTextStyleInfo.<Labels>k__BackingField | DolocTown.Config.General.TextLabel[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.TbDialogueEntity._dataList | System.Collections.Generic.List`1<DolocTown.Config.Dialogue.DialogueEntityInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.TbDialogueEntity._dataMap | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Config.Dialogue.DialogueEntityInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.TbDialogueOption._dataList | System.Collections.Generic.List`1<DolocTown.Config.Dialogue.DialogueOptionInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.TbDialogueOption._dataMap | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Config.Dialogue.DialogueOptionInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.TbDialogueTextStyle._dataList | System.Collections.Generic.List`1<DolocTown.Config.Dialogue.DialogueTextStyleInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Dialogue.TbDialogueTextStyle._dataMap | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Config.Dialogue.DialogueTextStyleInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Email.CfgEmailAttachMission.<AutoAccept>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Email.CfgEmailAttachMission.<MissionChainId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncEquipmentAnimationBase.<DialogueNode>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.EquipmentFuncFishIncubator.<EmissionSprite>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.LampInfo.<EmissionColor>k__BackingField | UnityEngine.Color | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.LampInfo.<EmissionIntensity>k__BackingField | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.LampInfo.<EmissionRevertSpriteAsset>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Equipment.LampInfo.<EmissionSpriteAsset>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Festival.FestivalInfo.<DisableNpcActing>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Festival.FestivalInfo.<NpcInfos>k__BackingField | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Config.Festival.NpcFestivalInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Festival.NpcFestivalInfo.<AutoInit>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Festival.NpcFestivalInfo.<DialogueNode>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Festival.NpcFestivalInfo.<FaceLeft>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Festival.NpcFestivalInfo.<MarkPoint_Ref>k__BackingField | DolocTown.Config.Room.MarkPointInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Festival.NpcFestivalInfo.<MarkPoint>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.CfgRewardProto.<RewardType>k__BackingField | DolocTown.Config.Mission.RewardType | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionAnimationBase.<DialogueNode>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.ItemFunctionRecipeGroup.<DialogueNode>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.MissionItemInfo.<Id_Ref>k__BackingField | DolocTown.Config.Item.ItemInfo | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.MissionItemInfo.<Id>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.MissionItemInfo.<SceneAsset>k__BackingField | DolocTown.SpriteAsset | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.TbMissionItem._dataList | System.Collections.Generic.List`1<DolocTown.Config.Item.MissionItemInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Item.TbMissionItem._dataMap | System.Collections.Generic.Dictionary`2<System.String,DolocTown.Config.Item.MissionItemInfo> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.LocalizationInfo.<DialogueL10nId>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionAcceptFail_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionAcceptFail>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionEmpty_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionEmpty>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionExpTip_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionExpTip>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionLowLevel_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionLowLevel>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionLv_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionLv>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionOverdue_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionOverdue>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionPanelTitle_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionPanelTitle>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.Config.Localization.StaticTextInfo.<BoardMissionTitle_l10n_key>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| DolocAPI.Command_OpenBoardMissionPanel | Harmony patch candidate |  | Risky |
| DolocAPI.Command_RefreshBoardMission | Harmony patch candidate |  | Risky |
| DolocAPI.Command_RefreshNpcCelebrationOrder | Harmony patch candidate |  | Risky |
| DolocAPI.Command_RestartMission | Harmony patch candidate | System.String chainId; System.String missionId; System.Boolean fromParent | Medium |
| DolocAPI.Command_StartAllFactionMissions | Harmony patch candidate |  | Risky |
| DolocAPI.Command_StartAllMissionChains | Harmony patch candidate |  | Risky |
| DolocAPI.Command_StartBoardMission | Harmony patch candidate | System.String missionId | Risky |
| DolocAPI.Command_StartFactionMission | Harmony patch candidate | System.String name | Risky |
| DolocAPI.Command_StartMissionChain | Harmony patch candidate | System.String name; System.Boolean force | Risky |
| DolocAPI.OpenPermissionViewChipDoc | Harmony patch candidate |  | Risky |
| DolocAPI.OpenPermissionViewPlantDoc | Harmony patch candidate |  | Risky |
| DolocAPI.RefreshResidentMissionTip | Harmony patch candidate | System.String id | Medium |
| DolocAPI.StartFactionMission | Harmony patch candidate | System.String missionId | Medium |
| DolocAPI.StartMissionChain | Harmony patch candidate | System.String missionId | Medium |
| DolocAPI/<>c.<FastButton_OpenFactionPanel>b__1004_0 | Harmony patch candidate | DolocTown.FactionMissionUiState state | Risky |
| DolocAPI/<>c__DisplayClass966_0.<Command_OpenFactionPanel>b__0 | Harmony patch candidate | DolocTown.FactionMissionUiState state | Risky |
| DolocBundleManager.LoadMissionChains | Harmony patch candidate |  | Risky |
| DolocBundleManager.LoadNpcSchedules | Harmony patch candidate |  | Risky |
| DolocTown.AchievementSystem._StartMission | Harmony patch candidate | DolocTown.NodeCanvas.MissionNodeListener missionNode | Risky |
| DolocTown.AchievementSystem.DolocTown.IMissionManagerComponent.OnMissionStart | Harmony patch candidate | DolocTown.IMission mission | Risky |
| DolocTown.AchievementSystem.LoadAchievementMissions | Harmony patch candidate | DolocTown.NodeCanvas.MissionGraph graph | Risky |
| DolocTown.AchievementSystem.LoadMissionGraph | Harmony patch candidate |  | Risky |
| DolocTown.AsideDialoguePanel.OnStartHide | Harmony patch candidate |  | Risky |
| DolocTown.AsideDialoguePanel.OnStartShow | Harmony patch candidate |  | Risky |
| DolocTown.AsideDialoguePanel.Pause | Harmony patch candidate |  | Medium |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `INpcHelper.Experimental` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocAPI/<Command_SubmitItemToNpcByInfo>d__946 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.AutomateFarmingMissionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.AutomatePlantMissionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Archives.DocumentMissionTip | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Asset.CfgNpcScheduleAsset | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Asset.CfgSwitchScheduleAsset | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Dialogue.DialogueEntityInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Dialogue.DialogueEntityType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Dialogue.DialogueOptionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Dialogue.DialogueTextStyleInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Dialogue.TbDialogueEntity | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Dialogue.TbDialogueOption | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Dialogue.TbDialogueTextStyle | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Email.CfgEmailAttachMission | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Festival.NpcFestivalInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.MissionItemInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Item.TbMissionItem | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.BoardMissionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.BoardMissionLevelInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.BoardMissionType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.BoardMissionTypeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.CustomEventInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.FactionMissionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.FactionMissionType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.FactionMissionTypeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.FactionType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.FactionTypeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.ItemGeneConditionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.ItemSubmitConditionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.MapMissionTip | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.MapMissionTipType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.MissionContent | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.MissionDecoratorInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.MissionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.MissionNodeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.MissionTypeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.PositionTypeInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.RewardPoolInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.RewardType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbBoardMission | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbBoardMissionLevel | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbBoardMissionType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbCustomEvent | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbFactionMission | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbFactionMissionType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbFactionType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbItemGeneCondition | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbItemSubmitCondition | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbMission | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Mission.TbMissionDecorator | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface INpcHelper
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- Yarn and dialogue state are complex; use read-only helpers first.
