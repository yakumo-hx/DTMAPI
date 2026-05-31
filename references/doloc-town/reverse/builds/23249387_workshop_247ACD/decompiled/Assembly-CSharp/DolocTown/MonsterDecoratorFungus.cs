using UnityEngine;

namespace DolocTown;

public class MonsterDecoratorFungus : MonsterDecorator
{
	public override bool ShieldBullet => true;

	public Vector2 EffectsPosition => base.transform.position + new Vector3(0f, 0.75f);

	public override bool OnAttacked(float attack, bool criticalRate, Vector2 pos, out bool isDead)
	{
		isDead = false;
		return false;
	}

	public override bool OnFell(ItemTool tool, Vector2 pos)
	{
		base.Controller.attackBehaviourManager.ReduceCD(MonsterAttackId.AtkNormal, 1f);
		return base.OnFell(tool, pos);
	}
}
