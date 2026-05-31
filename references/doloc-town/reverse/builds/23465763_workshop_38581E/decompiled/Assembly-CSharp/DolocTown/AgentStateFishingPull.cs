using UnityEngine;

namespace DolocTown;

public class AgentStateFishingPull : AgentStateFishing
{
	private bool _isAnimationDone;

	private float _pullDuration;

	public bool IsFailed
	{
		get
		{
			return body.FishingCache.IsFailed;
		}
		set
		{
			body.FishingCache.IsFailed = value;
		}
	}

	public AgentStateFishingPull(AgentStateManager parent, BodyController controller)
		: base(parent, controller)
	{
	}

	private void CheckAnimationDone()
	{
		if (!_isAnimationDone)
		{
			_isAnimationDone = IsAnimationDone("fishing_pull");
		}
	}

	protected override AgentStateBase NextState()
	{
		CheckAnimationDone();
		if (_isAnimationDone && _pullDuration <= 0f)
		{
			AgentStateFishing.IsUiControlled = false;
			if (!IsFailed)
			{
				AgentStateFishing.OnCompleteFishing(isBreaking: false);
			}
			return GetState<AgentStateIdle>();
		}
		return this;
	}

	public override void OnPlay()
	{
		if (_isAnimationDone && _pullDuration > 0f)
		{
			_pullDuration -= Time.fixedDeltaTime;
		}
	}

	public override void OnEnter()
	{
		_isAnimationDone = false;
		body.fishRodRenderer.SetVisible(value: true);
		if (IsFailed)
		{
			_pullDuration = body.fishRodRenderer.PullCancel();
		}
		else
		{
			_pullDuration = body.fishRodRenderer.Pull(body.transform.position);
			body.fishRodRenderer.Hook.HookSprite = body.FishingCache.FishItem?.uiSprite;
		}
		body.PlayAnimation("fishing_pull");
		PlayFishRodAnimation("fishing_pull");
		DolocAPI.Sound.PostSoundEvent(IsFailed ? SoundEvents.PLAY_FISHING_FAILED : SoundEvents.PLAY_FISHING_SUCCESS);
		AgentStateFishing.HandleLongDistanceFishingAchievement();
	}

	public override void OnExit()
	{
		body.fishRodRenderer.SetVisible(value: false);
	}
}
