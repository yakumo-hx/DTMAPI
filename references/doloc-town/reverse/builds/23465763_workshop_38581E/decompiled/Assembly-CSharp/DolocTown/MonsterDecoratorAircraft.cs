using UnityEngine;

namespace DolocTown;

public class MonsterDecoratorAircraft : MonsterDecorator
{
	public override bool OnAttacked(float attack, bool isCritical, Vector2 pos, out bool isDead)
	{
		isDead = false;
		if (base.Monster == null)
		{
			return false;
		}
		isDead = base.Monster.Damage(attack, isCritical, out var value);
		DolocAPI.RaiseDamageTip(value, pos, isCritical);
		return true;
	}
}
