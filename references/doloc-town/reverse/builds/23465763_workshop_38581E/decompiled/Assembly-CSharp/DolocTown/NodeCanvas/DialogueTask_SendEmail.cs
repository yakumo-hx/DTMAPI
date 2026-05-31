using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/任务")]
[Name("发送邮件", 0)]
[Description("指定一个邮件并发送给玩家")]
public class DialogueTask_SendEmail : DialogueTask
{
	[SerializeField]
	public string emailId;

	[SerializeField]
	public bool notRepeat;

	public override string taskTitle => "发送邮件<" + emailId + ">[" + (notRepeat ? "唯一" : "可重复") + "]";

	public override void DoAction(Graph graph)
	{
		DolocAPI.SendEmail(emailId, !notRepeat);
	}
}
