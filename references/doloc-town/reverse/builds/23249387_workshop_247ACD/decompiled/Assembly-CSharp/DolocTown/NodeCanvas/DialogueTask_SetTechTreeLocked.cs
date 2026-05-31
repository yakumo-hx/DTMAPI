using DolocTown.GameData;
using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/UI")]
[Name("锁定科技树", 0)]
[Description("将科技树能够解锁的节点锁定于一个给定的节点")]
public class DialogueTask_SetTechTreeLocked : DialogueTask
{
	[SerializeField]
	[ExposeField]
	private string lockedTechTreeName;

	[SerializeField]
	[ExposeField]
	private string lockedNodeName;

	public override string taskTitle => "锁定科技树:\"" + lockedTechTreeName + "." + lockedNodeName + "\"";

	public override void DoAction(Graph graph)
	{
		DolocAPI.archiveHandle.LockTechTreeNode(lockedNodeName);
	}
}
