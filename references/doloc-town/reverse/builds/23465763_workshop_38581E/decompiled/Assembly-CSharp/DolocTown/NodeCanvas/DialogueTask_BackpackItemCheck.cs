using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("检查背包中道具", 0)]
[Description("检查玩家背包中指定道具数量是否符合条件")]
public class DialogueTask_BackpackItemCheck : DialogueConditionTask
{
	[SerializeField]
	public string itemName = "";

	[SerializeField]
	public int value = 10;

	[SerializeField]
	public bool useBox;

	[SerializeField]
	public CompareMethod compareMethod = CompareMethod.GreaterThan;

	public override string taskTitle => $"玩家背包中<{itemName}>数量 {compareMethod.CompareLabel()} {value}";

	protected override bool CheckCondition()
	{
		return OperationTools.Compare(DolocAPI.CountItem(itemName.ToLower(), useBox), value, compareMethod);
	}
}
