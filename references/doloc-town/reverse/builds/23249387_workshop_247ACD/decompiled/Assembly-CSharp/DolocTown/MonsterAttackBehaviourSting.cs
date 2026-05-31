using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourSting : MonsterAttackBehaviourPhysical
{
	private new readonly ISting _proto;

	private Tween _tween;

	private Vector2 _stingDir;

	public MonsterAttackBehaviourSting(Transform host, ISting proto)
		: base(host, proto)
	{
		_proto = proto;
	}

	public override bool OnUpdate(float dt)
	{
		if (_tween == null)
		{
			return true;
		}
		_tween.ManualUpdate(dt, dt);
		return false;
	}

	public override bool IsTargetInAttackRange(Transform target)
	{
		return _proto.DashDistance >= _controller.DistanceToTarget(target);
	}

	public override void Invoke()
	{
		Begin();
		_stingDir = base.DirToTarget;
		if (_proto.HasReadyAction)
		{
			_controller.RaiseDangerWarning01();
			_controller.PostSoundEvent(SoundEvents.PLAY_DRONE_BEE_PRE);
			_tween = DoReadyAction(_proto.ReadyDuration, StartSting);
		}
		else
		{
			StartSting();
		}
	}

	private void StartSting()
	{
		EnableDamageBox(_proto.Damage, _proto.CriticalRate);
		_controller.PostSoundEvent(_proto.DashSound);
		Vector2 vector = host.position;
		Vector2 stingPosition = vector + _stingDir * _proto.DashDistance;
		float dashDuration = _proto.DashDuration;
		float num = dashDuration * 0.7f;
		float duration = dashDuration - num;
		host.localScale = MonsterUtils.GetScaleFromShootDir(_stingDir.x);
		Sequence sequence = DOTween.Sequence();
		sequence.Append(host.DOMove(stingPosition, num)).SetEase(_proto.DashEase);
		sequence.AppendCallback(delegate
		{
			DisableDamageBox();
			DolocAPI.RaiseInstantAnimEffects(stingPosition, RandomUtils.ChoiceFrom<InstAnimEffectType>(InstAnimEffectType.IMPACT_01, InstAnimEffectType.IMPACT_02, InstAnimEffectType.IMPACT_03));
		});
		sequence.Append(host.DOMove(vector, duration)).SetEase(Ease.OutBack);
		sequence.OnComplete(Stop);
		sequence.SetUpdate(UpdateType.Manual, isIndependentUpdate: true);
		_tween = sequence;
	}

	private void Stop()
	{
		End();
		_tween?.Kill();
		_tween = null;
	}
}
