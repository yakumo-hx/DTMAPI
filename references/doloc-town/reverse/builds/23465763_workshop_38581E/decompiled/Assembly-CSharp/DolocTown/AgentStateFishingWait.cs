using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class AgentStateFishingWait : AgentStateFishing
{
	private readonly RSTimer _animationTimer = new RSTimer();

	private readonly RSTimer _tuCounter = new RSTimer();

	private bool _isPlayAnimationNow;

	private string _currentAnimationName;

	private bool _waitForFishBite;

	private float _fishOnHookDuration;

	private float _hookProbability;

	private bool _hasRolled;

	public bool IsWaitNow => _waitForFishBite;

	public AgentStateFishingWait(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		if (_isPlayAnimationNow && IsAnimationDone(_currentAnimationName))
		{
			_isPlayAnimationNow = false;
			_animationTimer.SetInterval(Random.Range(3f, 6f));
			body.PlayAnimation("fishing_wait");
		}
		if (body.Status.HorizontalMoveFactor != 0f)
		{
			return GetState(delegate(AgentStateFishingPull s)
			{
				s.IsFailed = true;
			});
		}
		if (_waitForFishBite)
		{
			return this;
		}
		if (body.FishingCache.FishProto == null)
		{
			InvokeNoFishTip();
			return GetState(delegate(AgentStateFishingPull s)
			{
				s.IsFailed = true;
			});
		}
		if (_fishOnHookDuration <= 0f)
		{
			InvokeFishRunAwayTip();
			return GetState(delegate(AgentStateFishingPull s)
			{
				s.IsFailed = true;
			});
		}
		if (body.UserInput.NormalUseTool || body.UserInput.NormalUseItem || body.UserInput.NormalFishing)
		{
			DolocAPI.CostEnergy(DolocAPI.GlobalParameter.FishingEnergyCost);
			if (body.FishingCache.FishProto.IsFish && !DolocAPI.gameManager.gameInitConfig.skipFishingGame)
			{
				return GetState<AgentStateFishingBattle>();
			}
			return GetState(delegate(AgentStateFishingPull s)
			{
				s.IsFailed = false;
			});
		}
		return this;
	}

	public override void OnPlay()
	{
		HandleFishOnHook();
		HandleWaitBehaviours();
	}

	private void HandleFishOnHook()
	{
		if (_waitForFishBite)
		{
			if (!_tuCounter.Tick(Time.fixedDeltaTime))
			{
				return;
			}
			if (!RandomUtils.Dice(_hookProbability))
			{
				_hookProbability += DolocAPI.GlobalParameter.FishingBiteAdditionalProbability;
				return;
			}
			if (_hasRolled)
			{
				if (_hookProbability >= 1f)
				{
					_waitForFishBite = false;
				}
				return;
			}
			_hasRolled = true;
			bool flag = RollFish();
			if (DolocAPI.gameManager.gameInitConfig.forceRollFish)
			{
				for (int i = 0; i < 100; i++)
				{
					if (body.FishingCache.FishProto.IsFish)
					{
						break;
					}
					flag = RollFish();
				}
			}
			InvokeFishOnHookTip();
			if (flag)
			{
				_waitForFishBite = false;
			}
			body.fishRodRenderer.Line.UseStraightLine();
			body.fishRodRenderer.EnableFishShadow();
			_fishOnHookDuration = (DolocAPI.gameManager.gameInitConfig.skipFishingWait ? 100f : DolocAPI.GlobalParameter.PullTiming);
		}
		else if (_fishOnHookDuration > 0f)
		{
			_fishOnHookDuration -= Time.fixedDeltaTime;
		}
	}

	private void HandleWaitBehaviours()
	{
		if (!_isPlayAnimationNow && _animationTimer.Tick(Time.fixedDeltaTime))
		{
			_isPlayAnimationNow = true;
			body.PlayAnimation(RandomUtils.Dice(0.9f) ? "fishing_wait_blink" : "fishing_wait_yawn");
		}
	}

	private void InvokeFishRunAwayTip()
	{
		DolocAPI.RaiseInstantAnimEffects(body.fishRodRenderer.FishHookPosition, InstAnimEffectType.IMPACT_01);
	}

	private void InvokeNoFishTip()
	{
		DolocAPI.RaiseEmotionLimited(body.transform, EmotionName.NOCOMMENT);
	}

	private void InvokeFishOnHookTip()
	{
		DolocAPI.RaiseInstantPSEffects(body.fishRodRenderer.FishHookPosition, InstantParticleEffectsType.SPARKS, "SceneUI", 1);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_FISHING_FISH_BITE);
	}

	public override void OnEnter()
	{
		_waitForFishBite = true;
		_hasRolled = false;
		_tuCounter.SetInterval(DolocAPI.GlobalParameter.FishingRollInterval);
		_hookProbability = (DolocAPI.gameManager.gameInitConfig.skipFishingWait ? 1f : DolocAPI.GlobalParameter.FishingBiteInitProbability);
		_fishOnHookDuration = 0f;
		_animationTimer.Reset();
		body.fishRodRenderer.SetVisible(value: true);
		body.fishRodRenderer.Play(base._fishRod.proto.Id, "fishing_wait");
		status.Clear();
	}
}
