using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/UI")]
[Name("打开道具提交界面", 0)]
[Description("打开道具提交界面, 将任务相关的道具提交给npc")]
public class DialogueTask_SubmitMissionItem : DialogueTask
{
	[RequiredField]
	public string itemName;

	[RequiredField]
	public int itemCount = 1;

	[RequiredField]
	public bool shouldCostItem = true;

	public override string taskTitle => "提交<" + itemName + ">";

	public override void DoAction(Graph graph)
	{
		DolocAPI.EnterUI((SubmitItemToNpcUiState state) => state.HandleStartUpArgs(new SubmitItemFilter(itemName, itemCount, shouldCostItem)));
	}
}
