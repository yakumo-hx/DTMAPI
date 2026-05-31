using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("任务或成就")]
[Name("检查任务是否完成", 0)]
[Description("给定一个任务链名称和子任务Id, 检查该任务是否完成")]
public class DialogueTask_IsMissionComplete : DialogueConditionTask
{
	[SerializeField]
	public string missionChainId;

	[SerializeField]
	public string missionId;

	public override string taskTitle => "检查任务 <color=#fffde3>" + missionId + "</color> 是否完成";

	protected override bool CheckCondition()
	{
		if (missionId.IsNullOrEmpty() || missionChainId.IsNullOrEmpty())
		{
			return false;
		}
		return DolocAPI.IsMissionComplete(missionId);
	}
}
