using System;
using DolocTown;
using RedSaw;
using UnityEngine;

public abstract class DolocTownGameStateBase : IDolocGameState
{
	private readonly bool _shouldPauseGame;

	private readonly bool _shouldLateUpdate;

	private readonly bool _supportCutscenes;

	private readonly DolocInputType _type;

	protected readonly GameStateMachine gameController;

	protected readonly DolocUserInput userInput;

	public virtual bool ShowOutline => true;

	public DolocInputType InputType => _type;

	public virtual bool DisableUseItem => false;

	public bool ShouldPauseGame => _shouldPauseGame;

	public bool ShouldLateUpdate => _shouldLateUpdate;

	public bool SupportCutscenes => _supportCutscenes;

	public virtual bool SupportFestival => false;

	public Action DisposableEnter { get; set; }

	public Action DisposableExit { get; set; }

	public Action DisposablePause { get; set; }

	public Action DisposableResume { get; set; }

	public virtual bool ForceShowOperationTip => false;

	public virtual bool ForceHideOperationTip => false;

	public virtual bool EnableBasicTipInteract => true;

	public virtual bool ForceShowBasicTip => false;

	public virtual bool ForceHideBasicTip => false;

	public virtual bool ForceShowQuickInventory => false;

	public virtual bool ForceHideQuickInventory => false;

	public virtual bool ShowPauseTip => false;

	protected DolocTownGameStateBase(GameStateMachine userInput, DolocInputType type, bool shouldPauseGame, bool shouldLateUpdate, bool supportCutscenes)
	{
		_type = type;
		_shouldPauseGame = shouldPauseGame;
		_shouldLateUpdate = shouldLateUpdate;
		_supportCutscenes = supportCutscenes;
		gameController = userInput;
		this.userInput = DolocAPI.UserInput;
	}

	public abstract void OnUpdate(float deltaTime);

	public virtual void OnFixedUpdate(float deltaTime)
	{
	}

	public virtual void OnLateUpdate(float deltaTime)
	{
	}

	public bool __enter()
	{
		userInput.InputType = _type;
		try
		{
			OnEnter();
			if (DisposableEnter != null)
			{
				DisposableEnter();
				DisposableEnter = null;
			}
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("Enter <" + GetType().Name + "> Failed!");
			Debug.LogException(exception);
			return false;
		}
	}

	public bool __exit()
	{
		try
		{
			OnExit();
			if (DisposableExit != null)
			{
				DisposableExit();
				DisposableExit = null;
			}
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("Exit <" + GetType().Name + "> Failed!");
			Debug.LogException(exception);
			return false;
		}
	}

	public bool __pause()
	{
		try
		{
			OnPause();
			if (DisposablePause != null)
			{
				DisposablePause();
				DisposablePause = null;
			}
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("Pause <" + GetType().Name + "> Failed!");
			Debug.LogException(exception);
			return false;
		}
	}

	public bool __resume()
	{
		userInput.InputType = _type;
		try
		{
			OnResume();
			if (DisposableResume != null)
			{
				DisposableResume();
				DisposableResume = null;
			}
			return true;
		}
		catch (Exception exception)
		{
			Debug.LogError("Resume <" + GetType().Name + "> Failed!");
			Debug.LogException(exception);
			return false;
		}
	}

	public virtual void OnPause()
	{
	}

	public virtual void OnResume()
	{
	}

	public virtual void OnEnter()
	{
	}

	public virtual void OnExit()
	{
	}

	public virtual void OnInputDeviceChanged(DolocInputDeviceType type)
	{
	}

	public virtual void Startup()
	{
		gameController.PushState(this);
	}

	protected bool ContinuouslyPress(float deltaTime, Func<bool> triggeredGetter, Func<bool> inProgressGetter, Action callback, RSTimer timer)
	{
		if (triggeredGetter == null || inProgressGetter == null || timer == null)
		{
			return false;
		}
		if (triggeredGetter())
		{
			callback?.Invoke();
			timer.Reset();
			return true;
		}
		if (inProgressGetter())
		{
			if (timer.Tick(deltaTime))
			{
				callback?.Invoke();
			}
			return true;
		}
		return false;
	}
}
