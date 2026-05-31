using UnityEngine;

namespace DolocTown;

public class AgentStateHit : AgentStateBase
{
	private readonly Shiner shiner;

	public override bool SupportDash => false;

	public override bool SupportJump => false;

	public override bool SupportUseItem => false;

	public AgentStateHit(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
		shiner = new Shiner(body.GetComponent<SpriteRenderer>());
	}

	protected override AgentStateBase NextState()
	{
		return this;
	}

	public override void OnEnter()
	{
		body.PlayAnimation("hit");
		shiner.Raise(LocMaterials.GAME_MAT_HURT, 0.1f);
		body.SetAttackable(value: false);
		body.Status.Velocity = Vector2.zero;
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_HURT);
	}

	public override void OnExit()
	{
		body.SetAttackable(value: true);
		body.Status.Velocity = Vector2.zero;
	}
}
