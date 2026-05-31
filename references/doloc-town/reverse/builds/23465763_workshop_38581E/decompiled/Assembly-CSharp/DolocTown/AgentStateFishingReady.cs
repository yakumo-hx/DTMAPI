using DolocTown.UI;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class AgentStateFishingReady : AgentStateFishing
{
	private readonly CastTimer _castTimer;

	private readonly ProgressCircle _powerBar;

	private bool _isAnimationDone;

	public AgentStateFishingReady(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
		_castTimer = new CastTimer(DolocAPI.GlobalParameter.CastDuration, DolocAPI.GlobalParameter.PerfectCastDuration);
		_powerBar = CreateProgressBar();
	}

	private void CheckAnimationDone()
	{
		if (!_isAnimationDone)
		{
			_isAnimationDone = IsAnimationDone("fishing_ready");
		}
	}

	private ProgressCircle CreateProgressBar()
	{
		GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_PROGRESSBAR_CIRCLE);
		if (asset != null)
		{
			ProgressCircle component = Object.Instantiate(asset, DolocAPI.uiSystem.rectTransform).GetComponent<ProgressCircle>();
			if (component != null)
			{
				component.Init();
				component.SetVisible(value: false);
				return component;
			}
			Debug.LogWarning("AgentStateFishingReady: ProgressCircle component not found in prefab");
		}
		return null;
	}

	protected override AgentStateBase NextState()
	{
		CheckAnimationDone();
		if (body.UserInput.NormalUseToolInProgress)
		{
			_powerBar.transform.position = body.fishRodRenderer.CastProgressBarPositionScreen;
			return this;
		}
		if (!_isAnimationDone)
		{
			return this;
		}
		return parent.GetState<AgentStateFishingCast>();
	}

	public override void OnPlay()
	{
		if (_isAnimationDone)
		{
			_castTimer.Tick(Time.fixedDeltaTime);
			float progress = _castTimer.Progress;
			_powerBar.Progress = progress;
			_powerBar.Color = body.fishRodRenderer.GetCastForceColor(progress);
		}
		status.Clear();
	}

	public override void OnExit()
	{
		_powerBar.SetVisible(value: false);
		if (_castTimer.IsPerfect)
		{
			DolocAPI.RaiseInstantPSEffects(body.fishRodRenderer.CastProgressBarPosition, InstantParticleEffectsType.SPARKS);
		}
		body.fishRodRenderer.SetPower(_castTimer.Progress);
	}

	public override void OnEnter()
	{
		_castTimer.Reset();
		_isAnimationDone = false;
		body.FishingCache.Reset();
		_powerBar.SetVisible(value: true);
		_powerBar.transform.position = body.fishRodRenderer.CastProgressBarPositionScreen;
		_powerBar.Progress = 0f;
		body.Status.Clear();
		body.PlayAnimation("fishing_ready");
		PlayFishRodAnimation("fishing_ready");
		body.fishRodRenderer.Line.color = base._fishRod.lineColor;
		body.fishRodRenderer.Line.SetVisible(value: false);
		body.fishRodRenderer.Hook.SetVisible(value: false);
	}
}
