using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown;

public abstract class MonsterAttackBehaviourPhysical : MonsterAttackBehaviour
{
	private PhysicalDamageBox _damageBox;

	protected MonsterAttackBehaviourPhysical(Transform host, IMonsterAttackBehaviourPhysical proto)
		: base(host, proto)
	{
	}

	public void SetAttackParams(float damage, float criticalRate = 0f)
	{
		_damageBox = host.GetComponentInChildren<PhysicalDamageBox>(includeInactive: true);
		if (_damageBox != null)
		{
			_damageBox.Damage = damage;
			_damageBox.CriticalRate = criticalRate;
		}
	}

	protected void EnableDamageBox(float damage = 0f, float criticalRate = 0f)
	{
		_damageBox = host.GetComponentInChildren<PhysicalDamageBox>(includeInactive: true);
		if (_damageBox != null)
		{
			_damageBox.Enabled = true;
			_damageBox.Damage = damage;
			_damageBox.CriticalRate = criticalRate;
		}
		else
		{
			Debug.LogWarning(host.name + "未找到物理碰撞盒");
		}
	}

	protected void DisableDamageBox()
	{
		if (!(_damageBox == null))
		{
			_damageBox.Enabled = false;
			_damageBox = null;
		}
	}

	public sealed override void Dispose(BattleSystem bs)
	{
		DisableDamageBox();
	}
}
