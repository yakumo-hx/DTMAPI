using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/怪物")]
[Name("技能是否起作用", 0)]
[Description("检查玩家是否处于目标技能的攻击范围内")]
public class IsAttackBehaviourWork : ConditionTask<MonsterController>
{
	[RequiredField]
	public BBParameter<MonsterAttackId> attackId;

	[RequiredField]
	public BBParameter<Transform> target;

	protected override string info => $"\"{target.name}\"处于\"{attackId}\"攻击范围内";

	protected override bool OnCheck()
	{
		if (attackId == null)
		{
			return false;
		}
		if (target == null || target.value == null)
		{
			return false;
		}
		return base.agent.IsAttackBehaviourWork(attackId.value, target.value);
	}
}
