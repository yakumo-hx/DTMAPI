using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourSkillBomb : MonsterAttackBehaviourSkill
{
	private new readonly IBomb _proto;

	private readonly MonsterAttackHelperTween _handle;

	public MonsterAttackBehaviourSkillBomb(Transform transform, IBomb proto, SkillManager manager)
		: base(transform, proto, manager)
	{
		_proto = proto;
		_handle = new MonsterAttackHelperTween(base.End);
	}

	public override bool IsTargetInAttackRange(Transform target)
	{
		if (host.position.y - target.position.y < _proto.HeightDstThreshold)
		{
			return false;
		}
		return _controller.RayTest(target, _proto.ValidDistance);
	}

	public override void Invoke()
	{
		Begin();
		if (_proto.HasReadyAction)
		{
			_controller.RaiseDangerWarning01();
			_handle.SetTween(DoReadyAction(_proto.ReadyDuration, StartDropBomb));
		}
		else
		{
			StartDropBomb();
		}
	}

	private void StartDropBomb()
	{
		Vector2 dirToTargetHorizontal = base.DirToTargetHorizontal;
		_handle.SetFinalTween(DoRecoil(dirToTargetHorizontal, DropBomb, 1.2f));
	}

	private void DropBomb()
	{
		if (!MakeSkill(out Bomb skill))
		{
			host.RaiseEmotion(EmotionName.CONFUSE);
			return;
		}
		Vector3 position = host.position;
		Vector3 position2 = base.target.position;
		skill.position2d = position;
		skill.Damage = _proto.Damage;
		skill.CriticalRate = _proto.CriticalRate;
		float f = Physics2D.gravity.y * skill._rigidbody2D.gravityScale;
		f = Mathf.Abs(f);
		float num = Mathf.Sqrt((position.y - position2.y) * 2f / f);
		float x = (position2.x - position.x) / num;
		skill.Velocity = new Vector2(x, 0f);
		_controller.PostSoundEvent(SoundEvents.PLAY_DRONE_ATTACK_THROW_BOMB);
	}

	public override bool OnUpdate(float dt)
	{
		return _handle.Update(dt);
	}
}
