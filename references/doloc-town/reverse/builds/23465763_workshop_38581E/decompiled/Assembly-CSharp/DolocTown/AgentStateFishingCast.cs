namespace DolocTown;

public class AgentStateFishingCast : AgentStateFishing
{
	private bool _shouldWait;

	private float _cancelHeight;

	public AgentStateFishingCast(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		if (body.fishRodRenderer.FishHookPosition.y > _cancelHeight)
		{
			if (!_shouldWait)
			{
				return this;
			}
			return GetState<AgentStateFishingWait>();
		}
		AgentStateFishing.IsUiControlled = false;
		body.fishRodRenderer.SetVisible(value: false);
		return GetState<AgentStateIdle>();
	}

	public override void OnPlay()
	{
		status.Clear();
	}

	private void OnTouchPool(FishingPool pool)
	{
		_shouldWait = pool != null;
		body.SetFishingPool(pool);
		AgentStateFishing.IsUiControlled = true;
	}

	public override void OnEnter()
	{
		_shouldWait = false;
		body.FishingCache.Reset();
		body.PlayAnimation("fishing_cast");
		PlayFishRodAnimation("fishing_cast");
		body.fishRodRenderer.SetCastInfo(body.IsFaceRight, OnTouchPool);
		_cancelHeight = DolocAPI.CurrentRoom.Geometry.scenePosition.y;
	}
}
