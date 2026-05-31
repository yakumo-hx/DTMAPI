using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown;

public abstract class MonsterAttackBehaviourSkill : MonsterAttackBehaviour
{
	private new readonly IMonsterAttackBehaviourSkill _proto;

	private SkillManager _skillManager;

	protected MonsterAttackBehaviourSkill(Transform host, IMonsterAttackBehaviourSkill proto, SkillManager skillManager)
		: base(host, proto)
	{
		_proto = proto;
		_skillManager = skillManager;
	}

	protected bool MakeSkill(out Skill skill)
	{
		return _skillManager.Raise(out skill);
	}

	protected bool MakeSkill<T>(out T skill) where T : Skill
	{
		if (_skillManager.Raise(out var skill2) && skill2 is T val)
		{
			skill = val;
			return true;
		}
		skill = null;
		return false;
	}

	public sealed override void Dispose(BattleSystem bs)
	{
		if (_skillManager != null)
		{
			bs.RemoveSkillManager(_skillManager);
			OnDispose();
			_skillManager = null;
		}
	}

	protected virtual void OnDispose()
	{
	}
}
