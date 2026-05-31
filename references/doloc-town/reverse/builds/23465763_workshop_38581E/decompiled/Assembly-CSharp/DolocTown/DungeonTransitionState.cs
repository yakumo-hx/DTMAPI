using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace DolocTown;

public class DungeonTransitionState : GameStateNoInput
{
	public static class TransitionHelper
	{
		private static Tween tween;

		public static Transform transform { get; set; }

		public static void TransiteScene(Vector2 position, Action callback)
		{
			if (tween != null)
			{
				tween.Kill();
			}
			tween = ShortcutExtensions.DOMove(endValue: new Vector3(position.x, position.y, transform.position.z), target: transform, duration: DolocAPI.eftConfig.dungeonRoomTransitionTime).SetEase(DolocAPI.eftConfig.dungeonRoomTransitionEase).OnComplete(delegate
			{
				callback();
			});
		}
	}

	private DungeonRoom nextRoom;

	private Action callback;

	public DungeonTransitionState(GameStateMachine userInput)
		: base(userInput)
	{
		TransitionHelper.transform = DolocAPI.cameraController.transform;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		DolocAPI.cameraController.setEnabled(value: false);
		DolocAPI.agent.Pause = true;
		DolocAPI.StartCoroutine(AsyncLoad());
	}

	public override void OnExit()
	{
		base.OnExit();
		DolocAPI.cameraController.setEnabled(value: true);
		DolocAPI.agent.Pause = false;
	}

	private IEnumerator AsyncLoad()
	{
		yield return new WaitForSeconds(0.2f);
		TransitionHelper.TransiteScene(DolocAPI.cameraController.Constraint(nextRoom.RoomPosition, nextRoom.SceneSize, DolocAPI.AgentPosition), delegate
		{
			DolocAPI.RoomGizmos.CurrentRoom = nextRoom;
			DolocAPI.SetEnvCamera(nextRoom.CameraPosition, nextRoom.CameraSize, nextRoom.ShouldShowBackground, nextRoom.ShouldMaskBackground);
			callback.InvokeSafe();
			gameController.PopState();
		});
	}

	public void Start(DungeonRoom room, Action callback = null)
	{
		nextRoom = room;
		this.callback = callback;
		gameController.PushState(this);
	}
}
