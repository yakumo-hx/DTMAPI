using UnityEngine;

namespace DolocTown;

public class AgentStateWater : AgentStateBase
{
	public override bool SupportJump => false;

	public override bool SupportInteract => false;

	public override bool SupportUseItem => false;

	public ItemWaterCan waterCan { get; set; }

	public AgentStateWater(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		if (!IsAnimationDone("water"))
		{
			return this;
		}
		return parent.GetState<AgentStateIdle>();
	}

	public override void OnEnter()
	{
		if (waterCan != null)
		{
			body.PlayAnimation("water");
			body.ToolRenderer.ResetWaterCan(waterCan);
			body.ToolRenderer.SetVisible(value: true);
			body.ToolRenderer.Play(waterCan.name, "water");
			body.ToolRenderer.SortingLayerName = (DolocAPI.IsAgentInWater ? "GroundBack" : "GroundFront");
			status.Velocity = Vector2.zero;
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_WATERING);
		}
	}

	public override void OnExit()
	{
		body.ToolRenderer.SetVisible(value: false);
	}
}
