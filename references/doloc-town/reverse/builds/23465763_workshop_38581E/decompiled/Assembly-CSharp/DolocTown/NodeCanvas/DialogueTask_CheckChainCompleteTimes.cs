using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("任务链完成次数检查", 0)]
[Category("任务或成就")]
[Description("检查目标任务链完成的次数")]
public class DialogueTask_CheckChainCompleteTimes : DialogueConditionTask
{
	[SerializeField]
	private string chainId;

	[SerializeField]
	private int completeTimes;

	[SerializeField]
	private CompareMethod compareMethod;

	public override string taskTitle => $"任务链 {chainId} 完成次数 {compareMethod.CompareLabel()} {completeTimes}";

	protected override bool CheckCondition()
	{
		return OperationTools.Compare(DolocAPI.archiveHandle.farmData.missionChainManager.GetMissionChainCompletedCount(chainId), completeTimes, compareMethod);
	}
}
