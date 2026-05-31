using System;
using Cysharp.Threading.Tasks;
using RedSaw;

namespace DolocTown;

public class CutSceneState : DolocTownGameStateBase
{
	private bool enableBasicTipInteract;

	private bool forceHideBasicTip;

	private bool forceHideOperationTip;

	private bool forceHideQuickInventory;

	private bool shouldUpdateDrone;

	private RSTimer longPressTimer = new RSTimer(DolocAPI.GlobalParameter.UiButtonLongClickDuration);

	private bool isSpeedUp;

	public override bool ShowOutline => false;

	public override bool EnableBasicTipInteract => enableBasicTipInteract;

	public override bool ForceHideBasicTip => forceHideBasicTip;

	public override bool ForceHideOperationTip => forceHideOperationTip;

	public override bool ForceHideQuickInventory => forceHideQuickInventory;

	public CutSceneState(bool forceHideBasicTip = true, bool forceHideOperationTip = true, bool forceHideQuickInventory = true, bool enableBasicTipInteract = false, bool shouldUpdateDrone = false)
		: base(DolocAPI.userInput, DolocInputType.BASE, shouldPauseGame: true, shouldLateUpdate: false, supportCutscenes: false)
	{
		this.forceHideBasicTip = forceHideBasicTip;
		this.forceHideOperationTip = forceHideOperationTip;
		this.forceHideQuickInventory = forceHideQuickInventory;
		this.enableBasicTipInteract = enableBasicTipInteract;
		this.shouldUpdateDrone = shouldUpdateDrone;
	}

	public static async UniTask PlayTask(UniTask task, Action callback = null, bool forceHideBasicTip = true, bool forceHideOperationTip = true, bool forceHideQuickInventory = true, bool enableBasicTipInteract = false)
	{
		CutSceneState state = new CutSceneState(forceHideBasicTip, forceHideOperationTip, forceHideQuickInventory, enableBasicTipInteract);
		state.Startup();
		await task;
		state.gameController.WaitToPopState(state);
		callback?.Invoke();
	}

	public override void OnUpdate(float deltaTime)
	{
		ContinuouslyPressSpeedUp(deltaTime);
	}

	public override void OnFixedUpdate(float deltaTime)
	{
		if (shouldUpdateDrone)
		{
			DolocAPI.gameStateManager.agentController.droneController.OnFixedUpdate(deltaTime);
		}
	}

	public override void OnEnter()
	{
		base.OnEnter();
		DolocAPI.uiSystem.gameTimeTip.SetSpeedUpOperationTipVisible(value: true);
		DolocAPI.SetSceneOperationTipEnabled(value: false);
		DolocAPI.cameraController.setEnabled(value: false);
		DolocAPI.SetPPM_CinemaScreen(value: true);
		DolocAPI.agent.InCutscene = true;
	}

	public override void OnExit()
	{
		DolocAPI.DelayFrame(DolocAPI.RevertTimeScale);
		DolocAPI.SetSceneOperationTipEnabled(value: true);
		DolocAPI.cameraController.setEnabled(value: true);
		DolocAPI.SetPPM_CinemaScreen(value: false);
		DolocAPI.agent.InCutscene = false;
		base.OnExit();
	}

	public override void OnResume()
	{
		base.OnResume();
		DolocAPI.SetSceneOperationTipEnabled(value: false);
	}

	private bool ContinuouslyPressSpeedUp(float deltaTime)
	{
		bool triggeredInput = userInput.GlobalSpeedUpPressed || userInput.BaseIsCancelPressed;
		bool inProgressInput = userInput.GlobalSpeedUpInProgress || userInput.BaseIsCancelInProgress;
		bool num = ContinuouslyPress(deltaTime, () => triggeredInput, () => inProgressInput, delegate
		{
			EnableSpeedUp(value: true);
		}, longPressTimer);
		if (!num)
		{
			EnableSpeedUp(value: false);
		}
		return num;
	}

	private void EnableSpeedUp(bool value)
	{
		if (isSpeedUp != value)
		{
			isSpeedUp = value;
			if (value)
			{
				DolocAPI.SetTimeScale(DolocAPI.GlobalParameter.SpeedUpTimeScale);
			}
			else
			{
				DolocAPI.RevertTimeScale();
			}
		}
	}
}
