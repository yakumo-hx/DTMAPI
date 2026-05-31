using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/怪物")]
[Name("生命值检查", 0)]
public class CheckHealth : ConditionTask<MonsterController>
{
	[SerializeField]
	[ExposeField]
	public float healthPercent;

	[SerializeField]
	[ExposeField]
	public float floatingPoint = 0.05f;

	[SerializeField]
	[ExposeField]
	public CompareMethod comparison;

	protected override string info => $"Health {comparison} {healthPercent}";

	protected override bool OnCheck()
	{
		return OperationTools.Compare(base.agent.HealthPercent, healthPercent, comparison, floatingPoint);
	}
}
