using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourSkillLandmine : MonsterAttackBehaviourSkill
{
	private new readonly ILandmine _proto;

	private readonly MonsterAttackHelperTween _handle;

	public MonsterAttackBehaviourSkillLandmine(Transform host, ILandmine proto, SkillManager skillManager)
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
			DropLandmine(hdir);
		}, _proto.ReadyDuration));
	}

	private void DropLandmine(Vector2 hdir)
	{
		if (!MakeSkill(out Landmine skill))
		{
			host.RaiseEmotion(EmotionName.CONFUSE);
			return;
		}
		skill.position2d = host.position;
		skill.Damage = _proto.Damage;
		skill.CriticalRate = _proto.CriticalRate;
		skill.LiveDuration = _proto.LiveDuration;
		skill.ReflectionTime = _proto.ReflectionTime;
		skill.Throw(hdir * _proto.MoveSpeed);
		_controller.PostSoundEvent(SoundEvents.PLAY_DRONE_ATTACK_THROW_BOMB);
	}
}
