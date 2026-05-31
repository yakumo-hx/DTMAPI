using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourSkillTimeBomb : MonsterAttackBehaviourSkill
{
	private new readonly ITimeBomb _proto;

	private readonly MonsterAttackHelperTween _handle;

	public MonsterAttackBehaviourSkillTimeBomb(Transform host, ITimeBomb proto, SkillManager skillManager)
		: base(host, proto, skillManager)
	{
		_proto = proto;
		_handle = new MonsterAttackHelperTween(base.End);
	}

	public override bool IsTargetInAttackRange(Transform target)
	{
		return _controller.DistanceToTarget(target) <= _proto.ValidDistance;
	}

	public override bool OnUpdate(float dt)
	{
		return _handle.Update(dt);
	}

	public override void Invoke()
	{
		Begin();
		Vector2 hdir = base.DirToTargetHorizontal;
		_controller.RaiseDangerWarning01();
		_handle.SetFinalTween(DoRecoil(hdir, delegate
		{
			_Start(hdir);
		}, _proto.ReadyDuration));
	}

	private void _Start(Vector2 hdir)
	{
		if (_proto.Count <= 0)
		{
			return;
		}
		if (_proto.Count == 1)
		{
			DropTimeBomb(hdir, _proto.MoveSpeed);
			return;
		}
		float num = (_proto.MaxMoveSpeed - _proto.MoveSpeed) / (float)_proto.Count;
		for (int i = 0; i < _proto.Count; i++)
		{
			DropTimeBomb(hdir, _proto.MoveSpeed + num * (float)i);
		}
	}

	private void DropTimeBomb(Vector2 hdir, float moveSpeed)
	{
		if (!MakeSkill(out TimeBomb skill))
		{
			host.RaiseEmotion(EmotionName.CONFUSE);
			return;
		}
		skill.position2d = host.position;
		skill.Damage = _proto.Damage;
		skill.CriticalRate = _proto.CriticalRate;
		skill.RB.AddForce(hdir * moveSpeed, ForceMode2D.Impulse);
		_controller.PostSoundEvent(SoundEvents.PLAY_DRONE_ATTACK_THROW_BOMB);
	}
}
