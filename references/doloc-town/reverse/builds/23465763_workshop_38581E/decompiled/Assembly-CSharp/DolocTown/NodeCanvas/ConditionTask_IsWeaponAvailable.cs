using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/怪物")]
[Name("检查武器是否就绪", 0)]
[Description("判断自身的某个武器是否CD结束")]
public class ConditionTask_IsWeaponAvailable : ConditionTask
{
	[SerializeField]
	[ExposeField]
	private BBParameter<MonsterAttackId> attackType;

	[SerializeField]
	[ExposeField]
	private BBParameter<MonsterAttackId> buffer;

	protected override string info => "\"" + weaponName + "\"就绪";

	private string weaponName
	{
		get
		{
			if (attackType != null)
			{
				return attackType.value.ToString();
			}
			return "未设置";
		}
	}

	protected override bool OnCheck()
	{
		if (!buffer.isNone)
		{
			buffer.value = attackType.value;
		}
		return base.agent.GetComponent<MonsterController>().IsAttackBehaviourAvailable(attackType.value);
	}
}
