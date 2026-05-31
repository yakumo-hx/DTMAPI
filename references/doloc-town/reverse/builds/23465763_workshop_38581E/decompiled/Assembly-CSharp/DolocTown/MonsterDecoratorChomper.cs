using UnityEngine;

namespace DolocTown;

public class MonsterDecoratorChomper : MonsterDecorator
{
	public override bool ShieldBullet => true;

	public Vector2 EffectsPosition => base.transform.position + new Vector3(0f, 0.75f);

	public override bool OnAttacked(float attack, bool criticalRate, Vector2 pos, out bool isDead)
	{
		isDead = false;
		return false;
	}

	public override bool OnSwordAttack(float atk, bool isCritical, Vector2 pos, out bool isDead)
	{
		isDead = false;
		if (!base.Controller.Monster.Damage(atk, critical: false, out var value))
		{
			base.Controller.Hurt(DolocAPI.AgentTransform);
			return true;
		}
		DolocAPI.RaiseDamageTip(value, pos);
		isDead = true;
		return true;
	}

	public override bool OnFell(ItemTool tool, Vector2 pos)
	{
		base.Controller.attackBehaviourManager.ReduceCD(MonsterAttackId.AtkNormal, 0.5f);
		return base.OnFell(tool, pos);
	}
}
