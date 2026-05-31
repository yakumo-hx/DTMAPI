using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/地牢")]
[Name("启动或关闭对应房间的自然对象状态", 0)]
public class DialogueTask_SetRoomEnvObjectStatus : DialogueTask
{
	private string expendArgs_1 = "default_expend_args";

	private string expendArgs_2 = "default_expend_args";

	public string args;

	public bool block;

	public bool isSuburbRoom;

	public override string taskTitle => $"房间<{args}>是否生成自然对象：<{!block}>";

	public override void DoAction(Graph graph)
	{
		DolocAPI.SetRoomEnvObjectStatus(isSuburbRoom ? ("city_" + args) : args, block);
	}
}
