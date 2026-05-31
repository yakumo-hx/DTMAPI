using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("检查背包中道具数量", 0)]
[Description("检查玩家背包中道具数量是否符合条件")]
public class DialogueTask_ItemCountCheck : DialogueConditionTask
{
	[SerializeField]
	public int value = 10;

	[SerializeField]
	public CompareMethod compareMethod = CompareMethod.GreaterThan;

	public override string taskTitle => $"玩家背包中道具数量 {compareMethod.CompareLabel()} {value}";

	protected override bool CheckCondition()
	{
		return OperationTools.Compare(DolocAPI.archiveHandle.InventorySystem.inventory.filledCount, value, compareMethod);
	}
}
