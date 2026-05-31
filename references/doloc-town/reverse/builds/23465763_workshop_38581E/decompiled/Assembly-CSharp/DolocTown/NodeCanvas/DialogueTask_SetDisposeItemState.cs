using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/背包")]
[Name("能否丢弃道具", 0)]
[Description("当前是否能丢弃道具(包括扔成掉落物和垃圾桶销毁，ps: editor模式中不会生效)")]
public class DialogueTask_SetDisposeItemState : DialogueTask
{
	public bool value;

	public override string taskTitle
	{
		get
		{
			if (!value)
			{
				return "禁止丢弃道具";
			}
			return "可以丢弃道具";
		}
	}

	public override void DoAction(Graph graph)
	{
		DolocAPI.archiveHandle.farmData.agentData.disableDisposeItem = !value;
	}
}
