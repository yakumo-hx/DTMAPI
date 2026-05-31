# DolocTown Logging / Diagnostics API Map

## Build

- Steam build: 23249387
- Branch: workshop
- Assembly-CSharp hash: 247ACDAA8D6ED0490A406C6FC8EB035F957D0EC1AE3F036F10E2092B8AC10367
- Research date: 2026-05-17
- Source status: Confirmed from local decompiled metadata; not yet Verified in-game unless noted.

## User-facing goals

- Find visible diagnostics paths.
- Support per-mod log routing.
- Map command-console opportunities for dev-only helpers.

## Decompiled scope

- Matched types: 384
- Matched methods: 2649
- Matched fields: 1405
- Matched properties: 533
- Matched events: 4
- Matched call edges: 13925
- Matched strings: 1550
- Raw indexes: `maps/index/Logging_Diagnostics-*.csv`

## Decompiled types

| Type | Role | Stability | Notes |
| --- | --- | --- | --- |
| DolocTown.CommandDefines | class | Confirmed | visibility=public; methods=105; fields=5 |
| DolocTown.DialogueRunner | class | Confirmed | visibility=public; methods=46; fields=21 |
| DolocTown.DialogueManager | class | Confirmed | visibility=public; methods=55; fields=10 |
| DolocTown.DialogueState | class | Confirmed | visibility=public; methods=42; fields=17 |
| DolocTown.FishTankEcological | class | Confirmed | visibility=public; methods=45; fields=10 |
| RedSaw.CommandLineInterface.VirtualMachine | class | Confirmed | visibility=public; methods=36; fields=9 |
| RedSaw.CommandLineInterface.ConsoleController`1 | class | Confirmed | visibility=public; methods=27; fields=17 |
| RedSaw.CommandLineInterface.Lexer | class | Confirmed | visibility=public; methods=14; fields=29 |
| DolocTown.DialoguePlayer | class | Confirmed | visibility=public; methods=27; fields=14 |
| RedSaw.CommandLineInterface.UnityImpl.GameConsole | class | Confirmed | visibility=public; methods=24; fields=16 |
| RedSaw.CommandLineInterface.UnityImpl.GameConsoleRenderer | class | Confirmed | visibility=public; methods=31; fields=7 |
| DolocTown.UI.BubbleDialoguePanel | class | Confirmed | visibility=public; methods=22; fields=14 |
| DolocTown.AnimalPathFinderDebugger | class | Confirmed | visibility=public; methods=19; fields=12 |
| DolocTown.InteractableObjectLogic | class | Confirmed | visibility=public; methods=22; fields=6 |
| RedSaw.CommandLineInterface.CommandSystem | class | Confirmed | visibility=public; methods=15; fields=11 |
| RedSaw.CommandLineInterface.IConsoleRenderer | interface | Confirmed | visibility=public; methods=25; fields=0 |
| DolocTown.UpdateLogHelper | class | Confirmed | visibility=public; methods=21; fields=3 |
| DolocTown.WeaponDebuggerSO | class | Confirmed | visibility=public; methods=5; fields=18 |
| DolocTown.AsideDialoguePanel | class | Confirmed | visibility=public; methods=17; fields=5 |
| RedSaw.CommandLineInterface.CSharpUtils | static class | Confirmed | visibility=private; methods=21; fields=0 |
| DolocTown.Config.Dialogue.DialogueEntityInfo | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.Config.Room.DialogueObjectInfo | class | Confirmed | visibility=public; methods=15; fields=5 |
| DolocTown.DialogueData | class | Confirmed | visibility=public; methods=16; fields=4 |
| DolocTown.DefaultDialogueTarget | class | Confirmed | visibility=public; methods=19; fields=0 |
| DolocTown.NoneDialogueTarget | class | Confirmed | visibility=public; methods=19; fields=0 |
| DolocTown.VersionCommand | static class | Confirmed | visibility=public; methods=19; fields=0 |
| RedSaw.CommandLineInterface.InputBehaviourState | class | Confirmed | visibility=private; methods=16; fields=3 |
| RedSaw.CommandLineInterface.SyntaxAnalyzer | class | Confirmed | visibility=public; methods=16; fields=3 |
| RedSaw.CommandLineInterface.TokenType | enum | Confirmed | visibility=public; methods=0; fields=19 |
| DolocTown.IDialogueEntity | interface | Confirmed | visibility=public; methods=18; fields=0 |
| DolocTown.UI.SceneDialogueBox | class | Confirmed | visibility=public; methods=14; fields=4 |
| RedSaw.CommandLineInterface.DebugField | class | Confirmed | visibility=public; methods=9; fields=9 |
| RedSaw.CommandLineInterface.LogManager`1/<GetLastLogs>d__16 | class | Confirmed | visibility=private; methods=9; fields=9 |
| RedSaw.CommandLineInterface.SyntaxTreeCode | enum | Confirmed | visibility=public; methods=0; fields=18 |
| DolocTown.Config.Dialogue.DialogueOptionInfo | class | Confirmed | visibility=public; methods=13; fields=4 |
| DolocTown.UI.EnvOptimizerConsole | class | Confirmed | visibility=public; methods=10; fields=7 |
| RedSaw.CommandLineInterface.CommandCreator/<CollectProperties>d__1`1 | class | Confirmed | visibility=private; methods=8; fields=9 |
| DolocTown.CopyUpdateLogTest/<get_TotalUnhandledLogs>d__6 | class | Confirmed | visibility=private; methods=10; fields=6 |
| RedSaw.CommandLineInterface.DebugProgressBarAttribute | class | Confirmed | visibility=public; methods=11; fields=5 |
| RedSaw.CommandLineInterface.DefaultTypeParserDefination | static class | Confirmed | visibility=private; methods=16; fields=0 |
| RedSaw.CommandLineInterface.LogManager`1/<GetLastLogs>d__15 | class | Confirmed | visibility=private; methods=8; fields=8 |
| RedSaw.CommandLineInterface.VirtualMachine/<get_AllProperties>d__14 | class | Confirmed | visibility=private; methods=10; fields=6 |
| DolocTown.Config.Dialogue.DialogueTextStyleInfo | class | Confirmed | visibility=public; methods=11; fields=4 |
| DolocTown.CutsceneObjectLogic | class | Confirmed | visibility=public; methods=11; fields=4 |
| DolocTown.DialogueVariableStorage | class | Confirmed | visibility=public; methods=13; fields=2 |
| DolocTown.UI.DialogueHistoryList | class | Confirmed | visibility=public; methods=9; fields=6 |
| DolocTown.UpdateLogHelper/<GetAllFileInDirectory>d__15 | class | Confirmed | visibility=private; methods=8; fields=7 |
| RedSaw.CommandLineInterface.CommandCreator/<CollectCommands>d__0`1 | class | Confirmed | visibility=private; methods=8; fields=7 |
| RedSaw.CommandLineInterface.CommandCreator/<CollectValueParsers>d__2`1 | class | Confirmed | visibility=private; methods=8; fields=7 |
| RedSaw.CommandLineInterface.CommandSystem/<>c | class | Confirmed | visibility=private; methods=8; fields=7 |
| RedSaw.CommandLineInterface.StackMethod | class | Confirmed | visibility=public; methods=11; fields=4 |
| RedSaw.CommandLineInterface.UnityImpl.GameConsoleClickable | class | Confirmed | visibility=public; methods=9; fields=6 |
| DolocTown.AutomateParamLogistics | class | Confirmed | visibility=public; methods=6; fields=8 |
| DolocTown.UI.DialogueOptionPanel | class | Confirmed | visibility=public; methods=11; fields=3 |
| RedSaw.CommandLineInterface.LogManager`1 | class | Confirmed | visibility=public; methods=10; fields=4 |
| RedSaw.CommandLineInterface.VirtualMachine/<GetAllDelegate>d__20 | class | Confirmed | visibility=private; methods=9; fields=5 |
| RedSaw.ExceptionTriggerPoint | struct | Confirmed | visibility=public; methods=10; fields=4 |
| DolocAPI/<Command_DropMoney>d__912 | struct | Confirmed | visibility=private; methods=2; fields=11 |
| DolocTown.CopyUpdateLogTest/<get_LogFiles>d__3 | class | Confirmed | visibility=private; methods=8; fields=5 |
| RedSaw.CommandLineInterface.CSharpUtils/<>c | class | Confirmed | visibility=private; methods=7; fields=6 |

## Lifecycle hints

| Member | Use | Risk |
| --- | --- | --- |
| BoxUiState.ShowBoxUsedUpWarning | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_EnterDungeon | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_LoadGame | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_LockInteractableObject | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_ObtainItem | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenAllDoc | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenAnimalPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenBoardMissionPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenBuildingPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenCalendarPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenChipDoc | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenDemoEndPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenExchangeStore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenFactionPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenPlantDoc | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenSeedUnLockPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenStore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenTutorialPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_OpenWeatherReportPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RefreshBoardMission | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RefreshCameraResolution | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RefreshMonster | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RefreshNpcCelebrationOrder | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RefreshResource | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RefreshResourceByType | candidate lifecycle or hook point | Medium |
| DolocAPI.Command_RefreshStore | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RefreshVegetation | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RefreshWater | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_RestartMission | candidate lifecycle or hook point | Medium |
| DolocAPI.Command_SaveGame | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_StartAllFactionMissions | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_StartAllMissionChains | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_StartBoardMission | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_StartFactionMission | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_StartMissionChain | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_StartProcess | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_UnlockAllInteractableObject | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_UnlockInteractableObject | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.Command_UseMod | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI.StartDialogueNode | candidate lifecycle or hook point | Medium |
| DolocAPI/<>c.<Command_OpenAnimalPanel>b__977_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass927_0.<Command_OpenExchangeStore>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass961_0.<Command_OpenTutorialPanel>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocAPI/<>c__DisplayClass966_0.<Command_OpenFactionPanel>b__0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Animal.DEBUG_CloseHome | candidate lifecycle or hook point | Medium |
| DolocTown.Animal.DEBUG_OpenHome | candidate lifecycle or hook point | Medium |
| DolocTown.Animal.DEBUG_SetHomeClosed | candidate lifecycle or hook point | Medium |
| DolocTown.AnimalPathFinderDebugger.InitializeRoomEnvironment | candidate lifecycle or hook point | Medium |
| DolocTown.AnimalPathFinderDebugger.RefreshAnimalMap | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.__Init | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.<__Init>b__8_0 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.<__Init>b__8_1 | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.OnStartHide | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.OnStartShow | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.AsideDialoguePanel.Pause | candidate lifecycle or hook point | Medium |
| DolocTown.AutomateParamLogistics.LoadDefault | candidate lifecycle or hook point | Medium |
| DolocTown.CityEquipmentLogic.Interact | candidate lifecycle or hook point | Medium |
| DolocTown.CityEquipmentLogic.OnAfterLoadArchiveData | candidate lifecycle or hook point | Medium |
| DolocTown.CityEquipmentLogic.OnInteract | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CityEquipmentLogic.OnUpdatePerSecond | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CommandDefines.Command_OpenChipSubmitPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CommandDefines.Command_OpenPlantSubmitPanel | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CommandDefines.EnterCinemaScreen | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CommandDefines.ExitCinemaScreen | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CommandDefines.HideCurrentInteractable | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CommandDefines.InitNpcInFestival | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CommandDefines.InteractWith | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CommandDefines.RefreshBgmLater | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CommandDefines.SetTargetToCurrentInteractable | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CommandDefines.StartDialogueNode | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CommandDefines.StartFestival | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.StaticTextInfo.get_BoxPanelUsedUpWarning | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.get_BoxPanelUsedUpWarning_l10n_key | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Localization.StaticTextInfo.set_BoxPanelUsedUpWarning | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Localization.TbStaticText.get_BoxPanelUsedUpWarning | candidate lifecycle or hook point | Medium |
| DolocTown.Config.NPC.NpcInfo.get_InitialDialogueEntry | candidate lifecycle or hook point | Medium |
| DolocTown.Config.NPC.NpcInfo.set_InitialDialogueEntry | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.Config.Room.DialogueObjectInfo.get_RefreshType | candidate lifecycle or hook point | Medium |
| DolocTown.Config.Room.DialogueObjectInfo.set_RefreshType | candidate lifecycle or hook point | Risky: non-public or generated path |
| DolocTown.CopyUpdateLogTest.ShowUsername | candidate lifecycle or hook point | Risky: non-public or generated path |

## Public members

| Member | Use | Risk |
| --- | --- | --- |
| DebugLight.Test() | direct call candidate | Medium; needs instance source |
| DevHelper.get_Console() | direct call candidate | Medium; needs instance source |
| DevHelper.HideConsole() | direct call candidate | Medium; needs instance source |
| DolocAPI.AddDialogueNode(System.String nodeName; System.String npcName) | direct call candidate | Medium |
| DolocAPI.AppendDialogueHistoryLine(System.String npcName; System.String npcTitle; System.String content) | direct call candidate | Medium |
| DolocAPI.AppendDialogueHistoryOption(System.String content) | direct call candidate | Medium |
| DolocAPI.CheckDialogueEvent(System.String npcName) | direct call candidate | Medium |
| DolocAPI.Command_AddMissionItem(System.String missionItemId; System.String markPointId) | direct call candidate | Medium |
| DolocAPI.Command_CanUpgradeCellar() | direct call candidate | Medium |
| DolocAPI.Command_ExtendBuilding() | direct call candidate | Medium |
| DolocAPI.Command_HasCellar() | direct call candidate | Medium |
| DolocAPI.Command_HasHerbPouch() | direct call candidate | Medium |
| DolocAPI.Command_IsNpcBirthday(System.String npcName; System.Int32 dayOffset) | direct call candidate | Medium |
| DolocAPI.Command_RefreshResourceByType(System.String type; System.String roomId) | direct call candidate | Medium |
| DolocAPI.Command_RestartMission(System.String chainId; System.String missionId; System.Boolean fromParent) | direct call candidate | Medium |
| DolocAPI.Command_SetBackpackCapacity(System.Int32 count) | direct call candidate | Medium |
| DolocAPI.Command_UpgradeCellar() | direct call candidate | Medium |
| DolocAPI.ExecuteCommand(System.String input; System.Object& result) | direct call candidate | Medium |
| DolocAPI.get_CurrentDialogueHostTransform() | direct call candidate | Medium |
| DolocAPI.GetAllCommandFunctions() | direct call candidate | Medium |
| DolocAPI.GetCommandFunction(System.String commandName) | direct call candidate | Medium |
| DolocAPI.GetDialogueLineView(System.String targetName) | direct call candidate | Medium |
| DolocAPI.GetDialogueTargetViewOrDefault(System.String targetName; System.Boolean force) | direct call candidate | Medium |
| DolocAPI.GetMissionLogs(System.String missionId) | direct call candidate | Medium |
| DolocAPI.outputError(System.String cnt) | direct call candidate | Medium |
| DolocAPI.outputWarning(System.String cnt) | direct call candidate | Medium |
| DolocAPI.RemoveDialogueNode(System.String nodeName; System.String npcName) | direct call candidate | Medium |
| DolocAPI.RevertAllDialogueEntitiesSortingOrder() | direct call candidate | Medium |
| DolocAPI.SetDialogueEntrance(System.String nodeName; System.String npcName) | direct call candidate | Medium |
| DolocAPI.SetDialogueTargetForeground(System.String targetName; System.Boolean active) | direct call candidate | Medium |
| DolocAPI.ShowDebugInfos(System.Object instance; System.String title) | direct call candidate | Medium |
| DolocAPI.ShowMessageBoxNodeError(System.String msg; System.Boolean useSound) | direct call candidate | Medium |
| DolocAPI.ShowSceneDialogueBox(UnityEngine.Vector2 worldPosition; System.String content; System.Action callback) | direct call candidate | Medium |
| DolocAPI.StartDialogueNode(System.String nodeName; System.String npcName) | direct call candidate | Medium |
| DolocAPI.UIRaiseError() | direct call candidate | Medium |
| DolocInputSource/BaseInputActions.get_ContinueDialogue() | direct call candidate | Medium; needs instance source |
| DolocInputSource/BaseInputActions.get_ToggleDialogueHistory() | direct call candidate | Medium; needs instance source |
| DolocInputSource/IBaseInputActions.OnContinueDialogue(UnityEngine.InputSystem.InputAction/CallbackContext context) | direct call candidate | Medium; needs instance source |
| DolocInputSource/IBaseInputActions.OnToggleDialogueHistory(UnityEngine.InputSystem.InputAction/CallbackContext context) | direct call candidate | Medium; needs instance source |
| DolocInputSource/INormalInputActions.OnDebug(UnityEngine.InputSystem.InputAction/CallbackContext context) | direct call candidate | Medium; needs instance source |
| DolocInputSource/NormalInputActions.get_Debug() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.DEBUG_CheckHusbandryContribution() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.DEBUG_CloseHome() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.DEBUG_OpenHome() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.DEBUG_RemoveThisAnimal() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.DEBUG_ResetBreedInterval() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.DEBUG_ResetEatInterval() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.DEBUG_ResetExcreteInterval() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.DEBUG_ResetMetabolismInterval() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.DEBUG_SetAdult(System.Boolean value) | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.DEBUG_SetHomeClosed(System.Boolean value) | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.DEBUG_SetHusbandryValue(System.String key; System.Int32 value) | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.DEBUG_SimulateOutput() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.get_DEBUG_BreedingValue() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.get_DEBUG_CurrentAIState() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.get_DEBUG_IsEscaped() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.get_DEBUG_MatureType() | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.set_DEBUG_BreedingValue(System.Single value) | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.set_DEBUG_IsEscaped(System.Boolean value) | direct call candidate | Medium; needs instance source |
| DolocTown.Animal.set_DEBUG_MatureType(DolocTown.AnimalMatureType value) | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalPathFinderDebugger.ClearAvailablePositions() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalPathFinderDebugger.FindAvailablePositions() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalPathFinderDebugger.FindPath() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalPathFinderDebugger.FindPathStair() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalPathFinderDebugger.FindPathWidth() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalPathFinderDebugger.get_PathFinder() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalPathFinderDebugger.get_PathFinderStair() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalPathFinderDebugger.get_PathFinderWidth() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalPathFinderDebugger.InitializeRoomEnvironment() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalPathFinderDebugger.IsWalkable() | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalWork_Log.GenTask(DolocTown.Animal animal; RedSaw.AI.LinearTask.LinearTask& task) | direct call candidate | Medium; needs instance source |
| DolocTown.AnimalWork_Log.get_Title() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.get_InAnimation() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.get_InRender() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.get_isPlaying() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.get_IsPlaying() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.Pause() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.Render(System.String npcName; System.String content) | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.Resume() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.SetLineViewPosition(UnityEngine.Vector2 worldPosition) | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.Skip() | direct call candidate | Medium; needs instance source |
| DolocTown.AsideDialoguePanel.WaitOption() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateBot.DebugSetPower(System.Int32 value) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamLogistics.FilterCases(System.Collections.Generic.IEnumerable`1<DolocTown.Case> cases) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamLogistics.LoadDefault() | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamLogistics.MatchInput(DolocTown.Case container) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateParamLogistics.MatchOutput(DolocTown.Case container) | direct call candidate | Medium; needs instance source |
| DolocTown.AutomateSystemLocker.Command_IsLockedByAutomate() | direct call candidate | Medium |
| DolocTown.CellarExtension.DEBUG_Building() | direct call candidate | Medium |
| DolocTown.CityEquipmentLogic.BindRender(DolocTown.Room room; DolocTown.EquipmentRenderer renderer) | direct call candidate | Medium; needs instance source |
| DolocTown.CityEquipmentLogic.get_Equipment() | direct call candidate | Medium; needs instance source |
| DolocTown.CityEquipmentLogic.Interact() | direct call candidate | Medium; needs instance source |
| DolocTown.CityEquipmentLogic.OnAfterLoadArchiveData(System.Boolean isNewGame) | direct call candidate | Medium; needs instance source |
| DolocTown.CityEquipmentLogic.UnBindRender(DolocTown.EquipmentRenderer renderer) | direct call candidate | Medium; needs instance source |
| DolocTown.CommandDefines.BackupBool(System.Boolean value) | direct call candidate | Medium |
| DolocTown.CommandDefines.BackupNumber(System.Single number) | direct call candidate | Medium |
| DolocTown.CommandDefines.BackupString(System.String str) | direct call candidate | Medium |
| DolocTown.Config.Automate.AutomateBotFunctionLogistics.DeserializeAutomateBotFunctionLogistics(SimpleJSON.JSONNode _json) | direct call candidate | Medium |
| DolocTown.Config.Automate.AutomateBotFunctionLogistics.GetTypeId() | direct call candidate | Medium; needs instance source |
| DolocTown.Config.Automate.AutomateBotFunctionLogistics.Resolve(System.Collections.Generic.Dictionary`2<System.String,System.Object> _tables) | direct call candidate | Medium; needs instance source |

## Private members

| Member | Use | Risk | Fallback |
| --- | --- | --- | --- |
| DebugLight.lightEmissionTex | UnityEngine.Sprite | Risky: reflection/private field | Disable dependent feature if missing |
| DebugLight.sr | UnityEngine.SpriteRenderer | Risky: reflection/private field | Disable dependent feature if missing |
| DevHelper.<Console>k__BackingField | RedSaw.CommandLineInterface.UnityImpl.GameConsole | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_DropMoney>d__912.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_DropMoney>d__912.<host>5__2 | DolocTown.IDropItemHost | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_DropMoney>d__912.<i>5__6 | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_DropMoney>d__912.<moneyPerDrop>5__3 | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_DropMoney>d__912.<pos>5__5 | UnityEngine.Vector3 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_DropMoney>d__912.<remainingMoney>5__4 | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_GiftItemToNpc>d__957.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_GoRoom>d__907.<>8__1 | DolocAPI/<>c__DisplayClass907_0 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_GoRoom>d__907.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_SubmitBoardMissionItem>d__959.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_SubmitItemToNpc>d__945.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocAPI/<Command_SubmitItemToNpcByInfo>d__946.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ContinueDialogue | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_BaseInput_ToggleDialogueHistory | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocInputSource.m_NormalInput_Debug | UnityEngine.InputSystem.InputAction | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderDebugger._pathFinderStair | DolocTown.AnimalPathFinderStair | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderDebugger._pathFinderWidth | DolocTown.AnimalPathFinderWidth | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderDebugger.animalMap | DolocTown.AnimalMap | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderDebugger.animalWidth | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderDebugger.currentAvailablePositions | UnityEngine.Vector2Int[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderDebugger.currentAvailableStairs | DolocTown.AnimalStair[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderDebugger.currentPath | UnityEngine.Vector2Int[] | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderDebugger.drawAnimalMapForBuilding | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderDebugger.end | DolocTown.Editor.PositionMarker | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderDebugger.pathFinder | DolocTown.AnimalPathFinder | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderDebugger.shouldOptimize | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalPathFinderDebugger.start | DolocTown.Editor.PositionMarker | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalStatusTip.debugOffset | UnityEngine.Vector2 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalWork_Log.content | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimalWork_SearchHoneyCombToProduce.shouldLog | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AnimatedGate/<TransportAnim>d__39.<player>5__3 | DolocTown.IDialogueEntity | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AsideDialoguePanel.<isPlaying>k__BackingField | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AsideDialoguePanel.contentText | TMPro.TMP_Text | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AsideDialoguePanel.currentCharacterIndex | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AsideDialoguePanel.currentContent | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AsideDialoguePanel.textPlayer | Febucci.UI.TextAnimatorPlayer | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBotDecisionMakerLogistics._paramLogistics | DolocTown.AutomateParamLogistics | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateBotRenderer.debug | RedSaw.Physical.SteeringDebug | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamLogistics.filterInputLabel | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamLogistics.filterInputSkinIdx | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamLogistics.filterOutputLabel | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamLogistics.filterOutputSkinIdx | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamLogistics.inputLabelSet | System.Collections.Generic.HashSet`1<System.String> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamLogistics.inputSkinIdxSet | System.Collections.Generic.HashSet`1<System.Int32> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamLogistics.outputLabelSet | System.Collections.Generic.HashSet`1<System.String> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.AutomateParamLogistics.outputSkinIdxSet | System.Collections.Generic.HashSet`1<System.Int32> | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.BodyController.isForegroundInDialogue | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CityEquipment.logic | DolocTown.CityEquipmentLogic | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CityEquipmentLogic.equipment | DolocTown.Equipment | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines.backupBool | System.Boolean | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines.backupCamPos | UnityEngine.Vector2 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines.backupNumber | System.Single | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines.backupPlayerPos | UnityEngine.Vector2 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines.backupString | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<EnterCinemaScreen>d__47.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<ExitCinemaScreen>d__48.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<FadeIn>d__45.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<FadeOut>d__46.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<MoveCam>d__65.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<MoveCam>d__66.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<MoveCamX>d__67.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<MoveCamY>d__68.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<PlayMelody>d__76.<>7__wrap2 | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<PlayMelody>d__76.<>7__wrap3 | System.Int32 | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<PlayMelody>d__76.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<PlayMelody>d__76.<eventFormat>5__2 | System.String | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<PlayNpcAnimation>d__42.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<RaiseContinuesPS>d__34.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<RaiseContinuesPS>d__34.<effects>5__2 | DolocTown.ContinuesParticleEffects | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<RaiseContinuesPS>d__35.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<RaiseContinuesPS>d__35.<effects>5__2 | DolocTown.ContinuesParticleEffects | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<RevertCam>d__52.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<ShakeScreen>d__49.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<ShowEmotion>d__33.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<WaitForSeconds>d__89.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<WalkLeft>d__39.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<WalkRight>d__40.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<WalkTo>d__36.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<WalkToMark>d__37.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<WalkToMarkGlobal>d__38.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
| DolocTown.CommandDefines/<WalkToTarget>d__41.<>u__1 | Cysharp.Threading.Tasks.UniTask/Awaiter | Risky: reflection/private field | Disable dependent feature if missing |
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
| DolocTown.Config.Equipment.EquipmentFuncEquipmentAnimationBase.<DialogueNode>k__BackingField | System.String | Risky: reflection/private field | Disable dependent feature if missing |

## Events worth exposing

| Event | Trigger | Data | Risk |
| --- | --- | --- | --- |
| RedSaw.CommandLineInterface.LinearSelector.OnSelectionChanged | native event | System.Action`1<System.Int32> | Confirmed metadata; verify usage |
| RedSaw.CommandLineInterface.LogManager`1.OnReceivedMessage | native event | System.Action`1<RedSaw.CommandLineInterface.LogManager`1/Log<T>> | Confirmed metadata; verify usage |
| RedSaw.CommandLineInterface.ConsoleController`1.OnFocusOut | native event | System.Action | Confirmed metadata; verify usage |
| RedSaw.CommandLineInterface.ConsoleController`1.OnFocus | native event | System.Action | Confirmed metadata; verify usage |
| BoxUiState.ShowBoxUsedUpWarning | Harmony patch candidate |  | Risky |
| DolocAPI.Command_EnterDungeon | Harmony patch candidate | System.String dungeonName | Risky |
| DolocAPI.Command_LoadGame | Harmony patch candidate | System.Int32 index | Risky |
| DolocAPI.Command_LockInteractableObject | Harmony patch candidate | System.String lockObjectId | Risky |
| DolocAPI.Command_OpenBoardMissionPanel | Harmony patch candidate |  | Risky |
| DolocAPI.Command_OpenFactionPanel | Harmony patch candidate | System.String factionName | Risky |
| DolocAPI.Command_RefreshBoardMission | Harmony patch candidate |  | Risky |
| DolocAPI.Command_RefreshCameraResolution | Harmony patch candidate |  | Risky |
| DolocAPI.Command_RefreshMonster | Harmony patch candidate | System.String sceneName | Risky |
| DolocAPI.Command_RefreshNpcCelebrationOrder | Harmony patch candidate |  | Risky |
| DolocAPI.Command_RefreshResource | Harmony patch candidate | System.String roomId | Risky |
| DolocAPI.Command_RefreshResourceByType | Harmony patch candidate | System.String type; System.String roomId | Medium |
| DolocAPI.Command_RefreshStore | Harmony patch candidate | System.String storeName | Risky |
| DolocAPI.Command_RefreshVegetation | Harmony patch candidate | System.String sceneName | Risky |
| DolocAPI.Command_RefreshWater | Harmony patch candidate | System.String roomId | Risky |
| DolocAPI.Command_RestartMission | Harmony patch candidate | System.String chainId; System.String missionId; System.Boolean fromParent | Medium |
| DolocAPI.Command_SaveGame | Harmony patch candidate | System.Int32 index | Risky |
| DolocAPI.Command_StartAllFactionMissions | Harmony patch candidate |  | Risky |
| DolocAPI.Command_StartAllMissionChains | Harmony patch candidate |  | Risky |
| DolocAPI.Command_StartBoardMission | Harmony patch candidate | System.String missionId | Risky |
| DolocAPI.Command_StartFactionMission | Harmony patch candidate | System.String name | Risky |
| DolocAPI.Command_StartMissionChain | Harmony patch candidate | System.String name; System.Boolean force | Risky |
| DolocAPI.Command_UnlockAllInteractableObject | Harmony patch candidate |  | Risky |
| DolocAPI.Command_UnlockInteractableObject | Harmony patch candidate | System.String lockObjectId | Risky |
| DolocAPI.Command_UseMod | Harmony patch candidate |  | Risky |

## Helpers worth exposing

| Helper | Methods | Stability |
| --- | --- | --- |
| `IMonitor` | Query first; mutate only after two mod validations | Experimental |

## Content pack candidates

| Content type | Data source | Can add? | Can edit? |
| --- | --- | --- | --- |
| DolocAPI/<Command_SubmitItemToNpcByInfo>d__946 | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Automate.AutomateBotFunctionLogistics | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Dialogue.DialogueEntityInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Dialogue.DialogueEntityType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Dialogue.DialogueOptionInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Dialogue.DialogueTextStyleInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Dialogue.TbDialogueEntity | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Dialogue.TbDialogueOption | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Dialogue.TbDialogueTextStyle | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Drone.DroneFunctionProtoDebugger | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Equipment.EquipmentFuncFishTankEcological | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.DialogueObjectInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.Config.Room.TbDialogueObject | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.DialogueBlockHistoryData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.DialogueData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.DialogueLineHistoryData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.DialogueNodeData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.DroneStructDebugSlot | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.DroneStructDebugSO | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.GameInitConfig/CommandScript | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.GameInitConfig/VersionCommandScriptGroup | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.MissionLog | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.GameData.MissionLogManager | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.InteractableObjectLogic | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.NodeCanvas.DialogueNodeOperationData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.NodeCanvas.DialogueTask_CheckDocumentsStatus | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.NodeCanvas.DialogueTask_CheckDocumentsStatus/DocumentType | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.NodeCanvas.DialogueTask_SpawnMonsterByName/MonsterInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.NodeCanvas.DialogueTask_UnlockDocument | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.NodeCanvas.DialogueTask_UnlockInteractableObject | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.NodeCanvas.DialogueTask_UnlockSpecialDocument | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UI.EnvOptimizerConsoleBlockData | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| DolocTown.UpdateLogHelper/LogInfos | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| RedSaw.CommandLineInterface.DebugFieldOfFieldInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| RedSaw.CommandLineInterface.DebugFieldOfPropertyInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| RedSaw.CommandLineInterface.DebugInfo | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |
| RedSaw.CommandLineInterface.DebugInfoAttribute | decompiled metadata / official JSON cross-check needed | Unknown | Unknown |

## Verified mods

| Mod | Feature | Result |
| --- | --- | --- |
| _pending_ | Needs in-game run after API draft | Not verified |

## API proposal

```csharp
// Draft only. Keep under DolocTown.SMAPI.Experimental until two real mods verify it.
public interface IMonitor
{
    // Query methods should be stabilized before mutation methods.
}
```

## Open questions

- Which matched types are actually alive in the current scene lifecycle?
- Which private fields survive the next game update?
- Which official JSON/PNG content formats should be reused before SMAPI invents its own format?
- Prefer SMAPI-owned logs; official console hooks are optional.
