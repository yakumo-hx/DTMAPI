using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class KillTimeState : DolocTownGameStateBase
{
	private Action callback;

	private readonly SitParams sitParams;

	private readonly RSTimer timer;

	private readonly RSTimer subTimer;

	private float timeScale;

	private bool forceShowBasicTip;

	private bool skipOptions;

	private SingleSpriteRender spriteRender;

	private bool ignoreMoveInputDetect;

	private bool ignoreUiInputDetect;

	private CancellationTokenSource cancelTokenSource;

	private Tween tween;

	private int recoverTick;

	public override bool ShowOutline => false;

	public override bool ForceShowBasicTip => forceShowBasicTip;

	public override bool ForceHideQuickInventory => true;

	public SitParams SitParams => sitParams;

	private KillTimeState(SitParams sitParams, Action callback, bool shouldPauseGame, bool forceShowBasicTip, bool skipOptions)
		: base(DolocAPI.userInput, DolocInputType.All, shouldPauseGame, shouldLateUpdate: false, supportCutscenes: false)
	{
		this.callback = callback;
		this.sitParams = sitParams;
		timer = new RSTimer(sitParams.lifeTimer * (float)DolocAPI.GlobalParameter.TULength);
		subTimer = new RSTimer(DolocAPI.GlobalParameter.TULength);
		timeScale = 1f;
		this.forceShowBasicTip = forceShowBasicTip;
		this.skipOptions = skipOptions;
	}

	public static void EntryKillTimeState(SitParams sitParams, Action callback)
	{
		bool shouldPauseGame = DolocAPI.userInput.CurrentState.ShouldPauseGame;
		bool flag = !shouldPauseGame;
		bool flag2 = shouldPauseGame || (sitParams.timeScale <= 1f && !sitParams.save);
		new KillTimeState(sitParams, callback, shouldPauseGame, flag, flag2).Startup();
	}

	public override void OnEnter()
	{
		if (!skipOptions)
		{
			DolocAPI.SetPPM_CinemaScreen(value: true);
		}
		DolocAPI.SetSceneOperationTipEnabled(value: false);
		ignoreMoveInputDetect = false;
		ignoreUiInputDetect = true;
		cancelTokenSource = new CancellationTokenSource();
		SitDown(cancelTokenSource.Token).Forget();
		recoverTick = 0;
	}

	private async UniTaskVoid SitDown(CancellationToken token)
	{
		DolocAPI.agent.InCutscene = true;
		DolocAPI.AgentZ = sitParams.sitPosition.z;
		if (sitParams.foreGroundSprite != null)
		{
			spriteRender = DolocAPI.EntitySystem.Next<SingleSpriteRender>();
			spriteRender.sprite = sitParams.foreGroundSprite;
			Vector3 chairPosition = sitParams.chairPosition;
			chairPosition.z = sitParams.sitPosition.z - 0.001f;
			spriteRender.position = chairPosition;
		}
		float num = DolocAPI.AgentPosition.x - sitParams.sitPosition.x;
		float num2 = 0.375f;
		if (Mathf.Abs(num) > 0.25f)
		{
			DolocAPI.AgentFaceRight = num < 0f;
			Vector3 endValue = new Vector3(Mathf.Sign(num) * num2 + sitParams.sitPosition.x, DolocAPI.AgentPosition.y, sitParams.sitPosition.z);
			float num3 = Mathf.Abs(endValue.x - DolocAPI.AgentPosition.x) / 6f;
			DolocAPI.agent.PlayAnimation("run");
			tween = DolocAPI.AgentTransform.DOMove(endValue, num3);
			await UniTask.Delay(Mathf.CeilToInt(1000f * num3), ignoreTimeScale: false, PlayerLoopTiming.Update, token);
			await UniTask.DelayFrame(1, PlayerLoopTiming.Update, token);
		}
		DolocAPI.AgentFaceRight = sitParams.faceRight;
		float num4 = Mathf.Abs(sitParams.sitPosition.y - DolocAPI.AgentPosition.y) / 10f;
		DolocAPI.agent.PlayAnimation("jump");
		tween = DolocAPI.AgentTransform.DOMove(sitParams.sitPosition + new Vector3(0f, 0.25f, 0f), num4);
		await UniTask.Delay(Mathf.CeilToInt(1000f * num4), ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		await UniTask.DelayFrame(1, PlayerLoopTiming.Update, token);
		DolocAPI.agent.PlayAnimation("drop");
		DolocAPI.AgentPosition = sitParams.sitPosition;
		DolocAPI.AgentZ = sitParams.sitPosition.z;
		await UniTask.Delay(100, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		DolocAPI.agent.StateManager.Overwrite<AgentStateSit>();
		await UniTask.Delay((int)(DolocAPI.GlobalParameter.SitOptionDelay * 1000f), ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		if (gameController.CurrentState != this || skipOptions)
		{
			return;
		}
		ignoreMoveInputDetect = true;
		DolocAPI.EnterUI((KillTimeUiState state) => state.HandleKillTimeStartUpArgs(this, SetTimeScale, delegate
		{
			DolocAPI.DelayFrame(delegate
			{
				ignoreMoveInputDetect = false;
				ignoreUiInputDetect = false;
			});
		}));
	}

	private void SetTimeScale(float timeScale)
	{
		if (timeScale <= 0f)
		{
			gameController.PopState();
			return;
		}
		this.timeScale = timeScale;
		if (timeScale > 1f)
		{
			SetTimeScaleEnable(value: true);
		}
	}

	private void EndKillTime()
	{
		DolocAPI.SetPPM_CinemaScreen(value: false);
		DolocAPI.agent.InCutscene = false;
		DolocAPI.agent.ResetCollider();
		DolocAPI.RefreshScanner();
		SetTimeScaleEnable(value: false);
		DolocAPI.AgentZ = DolocAPI.agent.DefaultZ;
		DolocAPI.agent.StateManager.Overwrite<AgentStateIdle>();
		callback?.Invoke();
		DolocAPI.EntitySystem.Recycle(spriteRender);
		spriteRender = null;
		DolocAPI.SetSceneOperationTipEnabled(value: true);
	}

	public override void OnUpdate(float deltaTime)
	{
		if (!ignoreMoveInputDetect && (userInput.BaseInteract || userInput.NormalJump || Mathf.Abs(userInput.NormalMoveFactor) > 0.1f))
		{
			tween?.Kill();
			cancelTokenSource.Cancel();
			cancelTokenSource.Dispose();
			gameController.PopState();
		}
		if (ignoreUiInputDetect)
		{
			return;
		}
		if (!base.ShouldPauseGame)
		{
			if (sitParams.lifeTimer > 0f && timer.Tick(deltaTime))
			{
				gameController.PopState();
			}
			if (subTimer.Tick(deltaTime))
			{
				RecoverPlayerValue();
			}
		}
		DolocAPI.gameStateManager.agentController.OnUpdateInKillTimeState(deltaTime);
	}

	public override void OnExit()
	{
		EndKillTime();
	}

	public override void OnPause()
	{
		base.OnPause();
		SetTimeScaleEnable(value: false);
	}

	public override void OnResume()
	{
		base.OnResume();
		SetTimeScaleEnable(value: true);
	}

	private void SetTimeScaleEnable(bool value)
	{
		if (value)
		{
			DolocAPI.SetTimeScale(timeScale, showTipInMiddle: true);
		}
		else
		{
			DolocAPI.RevertTimeScale();
		}
	}

	private void RecoverPlayerValue()
	{
		int num = sitParams.addHealth;
		int num2 = sitParams.addEnergy;
		float recoveryAdditionPercent = DolocAPI.AgentEquipmentParams.recoveryAdditionPercent;
		if (DolocAPI.AgentEquipmentParams.recoveryAdditionPercent > 0f)
		{
			recoverTick++;
			if (recoverTick >= Mathf.RoundToInt(1f / recoveryAdditionPercent))
			{
				num *= 2;
				num2 *= 2;
				recoverTick = 0;
			}
		}
		if (num > 0 && DolocAPI.AddHealth(num) > 0)
		{
			DolocAPI.RaiseUiEffects(num, DolocUiColor.AGENT_HEALTH);
		}
		if (num2 > 0 && DolocAPI.AddEnergy(num2) > 0)
		{
			DolocAPI.RaiseUiEffects(num2, DolocUiColor.AGENT_ENERGY);
		}
	}
}
