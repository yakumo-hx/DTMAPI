using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourImpact : MonsterAttackBehaviourPhysical
{
	private new readonly IImpact _proto;

	private readonly MonsterAttackHelperTween _handle;

	private readonly RSTimer ghostShadowTimer = new RSTimer(0.05f);

	public MonsterAttackBehaviourImpact(Transform host, IImpact proto)
		: base(host, proto)
	{
		_proto = proto;
		_handle = new MonsterAttackHelperTween(base.End);
	}

	public override bool OnUpdate(float dt)
	{
		return _handle.Update(dt);
	}

	public override void Invoke()
	{
		Begin();
		if (_proto.HasReadyAction)
		{
			_controller.RaiseDangerWarning01();
			_controller.PostSoundEvent(SoundEvents.PLAY_DRONE_BOMB_COUNT_DOWN);
			_handle.SetTween(DoReadyAction(_proto.ReadyDuration, StartImpact, _controller.IsDirectional));
		}
		else
		{
			StartImpact();
		}
	}

	public override bool IsTargetInAttackRange(Transform target)
	{
		return _controller.DistanceToTarget(target) <= _proto.DashDistance;
	}

	private void StartImpact()
	{
		EnableDamageBox(_proto.Damage, _proto.CriticalRate);
		_controller.PostSoundEvent(SoundEvents.STOP_DRONE_BOMB_COUNT_DOWN);
		_controller.PostSoundEvent(_proto.DashSound);
		Vector2 dirToTarget = base.DirToTarget;
		Vector2 vector = (Vector2)host.transform.position + dirToTarget * _proto.DashDistance;
		host.transform.localScale = MonsterUtils.GetScaleFromShootDir(dirToTarget.x);
		Sprite sprite = host.GetComponent<SpriteRenderer>().sprite;
		_handle.SetFinalTween(host.DOMove(vector, _proto.DashDuration).OnUpdate(delegate
		{
			if (_proto.HasAfterImage && ghostShadowTimer.Tick(Time.fixedDeltaTime))
			{
				DolocAPI.RaiseGhostShadow(host.transform.position, host.transform.localScale, sprite);
			}
		}).SetEase(_proto.DashEase), base.DisableDamageBox);
	}
}
