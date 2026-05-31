using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("任务或成就")]
[Name("检查科技节点是否解锁", 0)]
[Description("检查是否已经解锁某个科技树节点")]
public class DialogueTask_IsTechNodeUnlocked : DialogueConditionTask
{
	[SerializeField]
	private string _techTreeName;

	[SerializeField]
	private string _techNodeName;

	public override string taskTitle => "<b>" + _techTreeName + "</b>.<b>" + _techNodeName + "</b>已经解锁";

	protected override bool CheckCondition()
	{
		return DolocAPI.archiveHandle.GetTechNodeUnlockState(_techNodeName);
	}
}
