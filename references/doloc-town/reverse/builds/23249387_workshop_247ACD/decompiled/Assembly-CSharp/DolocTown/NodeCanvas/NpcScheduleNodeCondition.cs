using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("条件过滤器", 0)]
[ParadoxNotion.Design.Icon("Condition", false, "")]
public class NpcScheduleNodeCondition : NpcScheduleNodeFilter
{
	[SerializeField]
	private DialogueConditionTask condition;

	public override bool IsMatch(NpcScheduleParams npcScheduleParams)
	{
		return condition.IsConditionMet;
	}
}
