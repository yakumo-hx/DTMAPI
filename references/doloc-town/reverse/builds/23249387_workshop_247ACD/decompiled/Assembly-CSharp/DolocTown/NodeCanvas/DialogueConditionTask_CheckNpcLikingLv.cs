using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("好感等级检查", 0)]
[Description("检查目标npc好感度等级是否符合条件")]
public class DialogueConditionTask_CheckNpcLikingLv : DialogueConditionTask
{
	[SerializeField]
	public string npcName = string.Empty;

	[SerializeField]
	public int likingLv;

	[SerializeField]
	public CompareMethod compareMethod = CompareMethod.GreaterThan;

	public override string taskTitle => $"<{npcName}>的好感等级 {compareMethod.CompareLabel()} {likingLv}";

	protected override bool CheckCondition()
	{
		return OperationTools.Compare(DolocAPI.QueryNpcLikingLv(npcName), likingLv, compareMethod);
	}
}
