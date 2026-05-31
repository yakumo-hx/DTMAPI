using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("检查金币数量", 0)]
[Description("检查玩家金币数量是否符合条件")]
public class DialogueConditionTask_Money : DialogueConditionTask
{
	[SerializeField]
	public int value = 100;

	[SerializeField]
	public CompareMethod compareMethod;

	public override string taskTitle => $"玩家金币数量 {compareMethod.CompareLabel()} {value}";

	protected override bool CheckCondition()
	{
		return OperationTools.Compare(DolocAPI.archiveHandle.CurrentMoney, value, compareMethod);
	}
}
