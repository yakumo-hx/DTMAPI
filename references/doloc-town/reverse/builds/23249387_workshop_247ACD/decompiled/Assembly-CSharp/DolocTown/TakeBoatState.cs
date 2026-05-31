using System;
using Cysharp.Threading.Tasks;
using DolocTown.Config;
using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public class TakeBoatState : DolocTownGameStateBase
{
	private FerryBoat boat;

	private Vector2 _leftDisembark;

	private Vector2 _rightDisembark;

	private float fadeInTime;

	private float fadeOutTime;

	private BindableValue<bool> disembark = new BindableValue<bool>(value: false);

	public override bool ForceShowBasicTip => true;

	public override bool ForceShowQuickInventory => true;

	public override bool ForceShowOperationTip => true;

	private AgentControllerState agentController => DolocAPI.gameStateManager.agentController;

	private FerryBoatController boatController => boat.boatController;

	private bool startUpdate
	{
		get
		{
			return boatController.startUpdate;
		}
		set
		{
			boatController.startUpdate = value;
		}
	}

	public TakeBoatState(FerryBoat boat, float fadeInTime = 0.5f, float fadeOutTime = 0.5f)
		: base(DolocAPI.userInput, DolocInputType.NORMAL, shouldPauseGame: false, shouldLateUpdate: false, supportCutscenes: false)
	{
		this.boat = boat;
		this.fadeInTime = fadeInTime;
		this.fadeOutTime = fadeOutTime;
		_leftDisembark = DolocConfig.Tables.TbMarkPoint.GetOrDefault(boat.disembarkLeftId).Position;
		_rightDisembark = DolocConfig.Tables.TbMarkPoint.GetOrDefault(boat.disembarkRightId).Position;
		disembark.OnValueChanged.AddListener(delegate(bool value)
		{
			if (value)
			{
				DolocAPI.agent.ShowSceneOperationTip(DolocAPI.agent.PositionHeadTop + new Vector2(0f, 3f), DolocConfig.StaticTexts.UiOperationDisembark, DolocAPI.UserInput.GlobalInteractActionName);
			}
			else
			{
				DolocAPI.agent.HideSceneOperationTip();
			}
		});
	}

	public override void OnUpdate(float deltaTime)
	{
		if (startUpdate)
		{
			disembark.Value = boatController.Disembark;
			agentController.OnUpdateInShipState(deltaTime);
			if (userInput.NormalInteract && disembark.Value)
			{
				startUpdate = false;
				FadeInOut(StopDriving).Forget();
			}
		}
	}

	public override void OnEnter()
	{
		startUpdate = false;
		agentController.ScannerInteractable.Manager.Clear();
		DolocAPI.ClearSceneOperationTips();
		agentController.OnResume();
		DolocAPI.agent.ResetVelocity();
		DolocAPI.agent.EnableRbGravity = false;
		DolocAPI.agent.EnableBodyCollider = false;
		FadeInOut(StartDriving).Forget();
		DolocAPI.battleSystem.ClearBulletsAndSkills();
	}

	public override void OnExit()
	{
		startUpdate = false;
		agentController.ScannerInteractable.ResetCollider();
		DolocAPI.ClearSceneOperationTips();
		DolocAPI.agent.EnableRbGravity = true;
		DolocAPI.agent.ResetCollider();
		boatController.ClearVelocity();
	}

	public override void OnPause()
	{
		agentController.OnPause();
	}

	public override void OnResume()
	{
		boatController.ClearVelocity();
		agentController.OnResume();
	}

	private void StartDriving()
	{
		startUpdate = true;
		boat.position = boat.GetBoatTargetPosition();
		DolocAPI.agent.IsFaceRight = Mathf.Abs(DolocAPI.AgentPosition.x - _leftDisembark.x) < Mathf.Abs(DolocAPI.AgentPosition.x - _rightDisembark.x);
		DolocAPI.AgentPosition = boatController.WorldPosition;
		boat.HideTouchEffect();
		DolocAPI.DelayFrame(boat.HideTouchEffect, 3);
	}

	private void StopDriving()
	{
		startUpdate = false;
		DolocAPI.AgentPosition = ((Mathf.Abs(DolocAPI.AgentPosition.x - _leftDisembark.x) > Mathf.Abs(DolocAPI.AgentPosition.x - _rightDisembark.x)) ? _rightDisembark : _leftDisembark);
		gameController.PopState();
	}

	private async UniTaskVoid FadeInOut(Action onFadeIn)
	{
		if (fadeInTime > 0f)
		{
			DolocAPI.ppm.FadeIn(fadeInTime);
			await UniTask.Delay((int)(fadeInTime * 1000f));
		}
		onFadeIn?.Invoke();
		if (fadeOutTime > 0f)
		{
			DolocAPI.ppm.FadeOut(fadeOutTime, null, shouldReset: false, "FadeInOut");
			await UniTask.Delay((int)(fadeOutTime * 1000f));
		}
	}
}
