using RedSaw;
using UnityEngine;

namespace DolocTown;

public class AgentStateDash : AgentStateBase
{
	private readonly RSTimer dashTimer = new RSTimer();

	private readonly RSTimer shadowTimer = new RSTimer();

	private Vector2 _dashDir;

	private bool shouldQuit;

	private bool shouldFixedY;

	private float fixedY;

	public Vector2 dashDir
	{
		get
		{
			return _dashDir;
		}
		set
		{
			_dashDir = value;
			shouldFixedY = value.y == 0f;
		}
	}

	public override bool SupportDash => false;

	public AgentStateDash(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		if (shouldQuit)
		{
			if (status.IsGrounded)
			{
				status.Velocity = Vector2.zero;
				parent.GetState<AgentStateTouchGround>();
			}
			return parent.GetState<AgentStateDrop>();
		}
		status.Dash(dashDir);
		return this;
	}

	public override void OnPlay()
	{
		if (dashTimer.Tick(Time.fixedDeltaTime))
		{
			shouldQuit = true;
		}
		if (shadowTimer.Tick(Time.fixedDeltaTime))
		{
			body.GhostShadow();
		}
		if (shouldFixedY)
		{
			Vector3 position = body.position;
			position.y = fixedY;
			body.position = position;
		}
	}

	public override void OnExit()
	{
		body.SortingOrder = 0;
		body.gameObject.layer = LayerMask.NameToLayer("Player");
		body.SetAttackable(value: true);
		if (status.IsGrounded)
		{
			body.AllowDash(animated: true);
		}
	}

	public override void OnEnter()
	{
		fixedY = body.position.y;
		body.StartDashCD();
		body.LimitDash();
		shouldQuit = false;
		body.PlayAnimation("dash");
		body.HatRenderer.OnDash();
		DolocAPI.RaiseInstantAnimEffects(body.transform.position, InstAnimEffectType.PLAYER_WALK_SMOKE, body.transform.localScale.x < 0f);
		body.SortingOrder = 1;
		body.SetAttackable(value: false);
		body.gameObject.layer = LayerMask.NameToLayer("PlatformExclude");
		dashTimer.SetInterval(body.MotionAbility.DashDuration);
		shadowTimer.SetInterval(0.05f);
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_CHARACTER_DASH);
		body.GhostShadow();
		DolocAPI.Broadcast(OperationEventType.SPRINT);
	}
}
