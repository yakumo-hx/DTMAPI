using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/怪物")]
[Name("检查技能冷却", 0)]
[Description("检查特定技能是否冷却结束")]
public class IsAttackBehaviourAvailable : ConditionTask<MonsterController>
{
	[SerializeField]
	[ExposeField]
	private BBParameter<MonsterAttackId> attackIdVariable;

	protected override string info => $"\"{attackIdVariable.value}\"冷却结束";

	protected override bool OnCheck()
	{
		return base.agent.IsAttackBehaviourAvailable(attackIdVariable.value);
	}
}
