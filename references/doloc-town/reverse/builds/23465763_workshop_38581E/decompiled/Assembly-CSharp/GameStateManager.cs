using System;
using System.Collections.Generic;
using DolocTown;
using UnityEngine;

public class GameStateManager
{
	private Dictionary<int, IDolocGameState> extraStates;

	public GameStateSceneTransition sceneTransition { get; private set; }

	public DungeonTransitionState dungeonTransition { get; private set; }

	public AgentControllerState agentController { get; private set; }

	public NormalGameState normalGameState { get; private set; }

	public DialogueState dialogState { get; private set; }

	public FestivalState festivalState { get; private set; }

	public GameStateManager(GameStateMachine userInput)
	{
		dialogState = new DialogueState(userInput);
		agentController = new AgentControllerState(DolocAPI.agent, DolocAPI.Motor, DolocAPI.uiSystem.inventoryQuick, DolocAPI.cameraController, DolocAPI.UserInput);
		Transform subContainer = DolocUtils.GetSubContainer(DolocAPI.gameManager.globalContainer, "dungeon");
		normalGameState = new NormalGameState(agentController, subContainer, userInput, shouldPauseGame: false, shouldLateUpdate: true, supportCutscenes: true);
		festivalState = new FestivalState(agentController, subContainer, userInput);
		sceneTransition = new GameStateSceneTransition(userInput);
		dungeonTransition = new DungeonTransitionState(userInput);
		extraStates = new Dictionary<int, IDolocGameState>();
	}

	public void TransitScene(Room room, Vector2 pos, Action cb, bool shouldFade = true)
	{
		sceneTransition.Start(room, pos, cb, shouldFade);
	}

	public void TransitSceneDungeon(DungeonRoom room, Action callback = null)
	{
		dungeonTransition.Start(room, callback);
	}
}
