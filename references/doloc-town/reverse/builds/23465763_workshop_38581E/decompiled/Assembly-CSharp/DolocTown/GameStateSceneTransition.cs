using System;
using UnityEngine;

namespace DolocTown;

public class GameStateSceneTransition : GameStateNoInput
{
	private Room targetRoom;

	private Vector2 remotePosition;

	private Action callback;

	private bool shouldFade;

	private Room currentRoom => DolocAPI.CurrentRoom;

	private bool shouldChangeScene => currentRoom?.SceneRawName != targetRoom?.SceneRawName;

	private bool shouldChangeRoom => currentRoom?.RoomId != targetRoom?.RoomId;

	public GameStateSceneTransition(GameStateMachine userInput)
		: base(userInput)
	{
	}

	public void Start(Room targetRoom, Vector2 remotePosition, Action callback, bool shouldFadeFirst)
	{
		this.targetRoom = targetRoom;
		this.remotePosition = remotePosition;
		this.callback = callback;
		shouldFade = shouldFadeFirst;
		if (targetRoom != null)
		{
			gameController.PushState(this);
		}
	}

	public override void OnEnter()
	{
		base.OnEnter();
		DolocAPI.AgentEnabled = false;
		DolocAPI.ClearSceneOperationTips();
		if (shouldFade)
		{
			DolocAPI.ppm.FadeIn(DolocAPI.eftConfig.sceneTransitionFadeInTime, TryScreenTransitionFadeIn);
		}
		else
		{
			TryScreenTransitionFadeIn();
		}
	}

	private void TryScreenTransitionFadeIn()
	{
		try
		{
			if (!shouldChangeScene)
			{
				OnSceneLoaded();
			}
			else if (!DolocAPI.sceneManager.LoadSceneAsync(targetRoom.SceneRawName, OnSceneLoaded))
			{
				Debug.LogError("无法加载场景 " + targetRoom.SceneRawName);
			}
		}
		catch (Exception innerException)
		{
			if (innerException == null)
			{
				innerException = innerException.InnerException;
			}
			Debug.LogError("进行场景移动时遇到异常：" + innerException.Message);
			Debug.LogException(innerException);
		}
	}

	private void OnSceneLoaded()
	{
		if (shouldChangeRoom)
		{
			DolocAPI.QuitCurrentRoom(targetRoom, shouldChangeScene);
		}
		DolocAPI.RoomGizmos.CurrentRoom = targetRoom;
		DolocAPI.AgentPosition = remotePosition;
		DolocAPI.AgentEnabled = true;
		try
		{
			DolocAPI.effectProvider.ClearAllContinusEffects();
			callback.InvokeSafe();
			DolocAPI.archiveHandle.cityData.npcManager.OnAgentRoomChanged(targetRoom);
		}
		catch (Exception ex)
		{
			Debug.LogError("场景加载完成时遇到异常：" + ex.Message);
			Debug.LogException(ex);
		}
		Exit();
	}

	private void Exit()
	{
		if (shouldFade)
		{
			DolocAPI.ppm.FadeOut(DolocAPI.eftConfig.sceneTransitionFadeOutTime, delegate
			{
				gameController.PopState();
			}, shouldReset: false, "Exit");
		}
		else
		{
			gameController.PopState();
		}
	}
}
