using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/背包")]
[Name("发送道具邮件", 0)]
[Description("用邮件发送道具给玩家")]
public class DialogueTask_SendItemAsEmail : DialogueTask
{
	[SerializeField]
	private string itemName;

	[SerializeField]
	[MinValue(1)]
	private int count;

	[SerializeField]
	private string title;

	[SerializeField]
	private string content;

	[SerializeField]
	private string sender;

	[SerializeField]
	private string templateEmail = "send_item_template";

	public override string taskTitle => "发送物品到邮箱";

	public override void DoAction(Graph graph)
	{
		DolocAPI.SendItemAsEmail(itemName, count, title, content, sender);
	}
}
