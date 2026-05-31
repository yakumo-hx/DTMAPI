using DolocTown.GameData;

namespace DolocTown;

public class AgentStateTouchGround : AgentStateBase
{
	public override bool SupportJump => true;

	public override bool SupportUseItem => true;

	public override bool SupportInteract => true;

	public AgentStateTouchGround(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		if (IsAnimationDone("touch_ground"))
		{
			return parent.GetState<AgentStateIdle>();
		}
		if (status.HorizontalMoveFactor != 0f)
		{
			return parent.GetState<AgentStateMove>();
		}
		if (body.ShouldEnterToolState)
		{
			return parent.GetState<AgentStateTool>();
		}
		return this;
	}

	public override void OnEnter()
	{
		parent.GetState<AgentStateJump>().jumpTimes = DolocAPI.archiveHandle.DoubleJumpTimes();
		body.AllowDash(animated: true);
		DolocAPI.RaiseInstantAnimEffects(body.transform.position, InstAnimEffectType.PLAYER_LAND_SMOKE);
		body.PlayAnimation("touch_ground");
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_FOOTSTEP_DROP);
		body.SortingOrder = 0;
		body.SortingLayerName = "Default";
		status.VelocityX = 0f;
		status.VelocityY = 0f;
	}
}
