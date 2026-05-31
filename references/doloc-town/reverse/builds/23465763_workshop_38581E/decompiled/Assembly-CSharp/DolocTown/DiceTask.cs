using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown;

[Category("多洛可小镇")]
[Name("骰子", 0)]
[Description("以给定的概率产出一个True值")]
public class DiceTask : ConditionTask
{
	[RequiredField]
	public float probability;

	protected override bool OnCheck()
	{
		return Random.value < probability;
	}
}
