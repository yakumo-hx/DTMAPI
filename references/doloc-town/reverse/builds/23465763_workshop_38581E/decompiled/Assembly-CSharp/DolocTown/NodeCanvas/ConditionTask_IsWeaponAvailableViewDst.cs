using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/怪物")]
[Name("检查武器是否就绪(视线距离)", 0)]
[Description("判断自身的某个武器是否就绪")]
public class ConditionTask_IsWeaponAvailableViewDst : ConditionTask
{
	[SerializeField]
	[ExposeField]
	private BBParameter<MonsterAttackId> attackType;

	[SerializeField]
	[ExposeField]
	private BBParameter<int> buffer;

	[SerializeField]
	[ExposeField]
	private float viewDistance;

	[SerializeField]
	[ExposeField]
	private BBParameter<float> viewDistanceBuffer;

	protected override string info => "检查\"" + weaponName + "\"是否就绪";

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
		buffer.value = (int)attackType.value;
		viewDistanceBuffer.value = viewDistance;
		return base.agent.GetComponent<MonsterController>().IsAttackBehaviourAvailable(attackType.value);
	}
}
