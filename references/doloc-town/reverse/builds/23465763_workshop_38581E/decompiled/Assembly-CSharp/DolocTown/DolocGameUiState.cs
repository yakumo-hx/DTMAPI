using System;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public abstract class DolocGameUiState : DolocTownGameStateBase
{
	private bool skipFirstFrame = true;

	protected RSTimer longPressPrevTimer = new RSTimer(DolocAPI.GlobalParameter.UiButtonLongClickDuration);

	protected RSTimer longPressNextTimer = new RSTimer(DolocAPI.GlobalParameter.UiButtonLongClickDuration);

	public override bool ForceHideBasicTip => true;

	public override bool ForceHideQuickInventory => true;

	public override bool ForceHideOperationTip => true;

	public override bool ShowPauseTip => true;

	public override bool ShowOutline => false;

	public virtual bool ShowFlowPoints => true;

	public virtual bool PermanentState { get; }

	protected DolocGameUiState()
		: base(DolocAPI.userInput, DolocInputType.BASE, shouldPauseGame: true, shouldLateUpdate: false, supportCutscenes: false)
	{
	}

	public void Init()
	{
		OnInit();
	}

	protected virtual void OnInit()
	{
		Debug.Log("Init " + GetType().Name);
	}

	public sealed override void OnUpdate(float deltaTime)
	{
		if (skipFirstFrame)
		{
			skipFirstFrame = false;
		}
		else
		{
			OnUiUpdate(deltaTime);
		}
	}

	protected virtual void OnUiUpdate(float deltaTime)
	{
	}

	public void Destroy()
	{
		OnDestroy();
	}

	protected virtual void OnDestroy()
	{
		Debug.Log("Destroy " + GetType().Name);
	}

	protected abstract void Show();

	protected abstract void Hide();

	public sealed override void OnEnter()
	{
		skipFirstFrame = true;
		Debug.Log("Enter " + GetType().Name);
		DolocAPI.SetSceneOperationTipEnabled(value: false);
		OnUiEnter();
		Show();
	}

	public sealed override void OnExit()
	{
		Debug.Log("Exit " + GetType().Name);
		DolocAPI.SetSceneOperationTipEnabled(value: true);
		OnUiExit();
		Hide();
	}

	public override void OnPause()
	{
		OnUiPause();
		Hide();
	}

	public override void OnResume()
	{
		skipFirstFrame = true;
		DolocAPI.gameUiStates.Access(this);
		DolocAPI.SetSceneOperationTipEnabled(value: false);
		OnUiResume();
		Show();
	}

	protected virtual void OnUiResume()
	{
	}

	protected virtual void OnUiPause()
	{
	}

	protected virtual void OnUiEnter()
	{
	}

	protected virtual void OnUiExit()
	{
	}

	protected bool ContinuouslyPressLast(float deltaTime, Action callback)
	{
		return ContinuouslyPress(deltaTime, () => userInput.BaseIsLastPressed, () => userInput.BaseIsLastInProgress, callback, longPressPrevTimer);
	}

	protected bool ContinuouslyPressNext(float deltaTime, Action callback)
	{
		return ContinuouslyPress(deltaTime, () => userInput.BaseIsNextPressed, () => userInput.BaseIsNextInProgress, callback, longPressNextTimer);
	}

	protected void DelayFrameToPopState()
	{
		if (!skipFirstFrame)
		{
			DolocAPI.DelayFrame(delegate
			{
				gameController.PopState();
			});
		}
	}
}
