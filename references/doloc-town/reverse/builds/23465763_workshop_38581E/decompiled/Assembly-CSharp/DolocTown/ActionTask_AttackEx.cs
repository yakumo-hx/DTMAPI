using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown;

[Category("多洛可小镇/怪物")]
[Name("释放技能", 0)]
[Description("调用自身的武器系统进行攻击，执行该函数前需要进行武器CD以及范围检测，如果不进行检测则认为需要强制攻击")]
public class ActionTask_AttackEx : ActionTask
{
	[SerializeField]
	[ExposeField]
	private BBParameter<MonsterAttackId> attackType;

	[SerializeField]
	[ExposeField]
	private bool hasTarget = true;

	[SerializeField]
	[ExposeField]
	[ShowIf("hasTarget", 1)]
	private BBParameter<Transform> target;

	protected override string info
	{
		get
		{
			if (hasTarget)
			{
				string arg = ((target.value == null) ? target.name : target.value.name);
				return $"向\"{arg}\"释放技能\"{attackType}\"";
			}
			return $"释放技能:\"{attackType}\"";
		}
	}

	protected override void OnExecute()
	{
		MonsterController component = base.agent.GetComponent<MonsterController>();
		if (component == null)
		{
			EndAction(success: false);
		}
		else
		{
			component.Attack(attackType.value, hasTarget ? target.value : null, base.EndAction);
		}
	}
}
