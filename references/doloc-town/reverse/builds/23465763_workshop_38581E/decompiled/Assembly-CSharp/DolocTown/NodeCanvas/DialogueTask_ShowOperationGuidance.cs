using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/UI")]
[Name("添加一个操作引导", 0)]
[Description("显示一个常驻的引导，只有玩家完成指定操作后才会消失")]
public class DialogueTask_ShowOperationGuidance : DialogueTask
{
	[SerializeField]
	[RequiredField]
	public string gameEventString = OperationEventType.NONE.ToString();

	[SerializeField]
	[RequiredField]
	public string textKey;

	[SerializeField]
	[RequiredField]
	public string keyCodeKey;

	public override string taskTitle => "操作引导<" + gameEventString + ">";

	public override void DoAction(Graph graph)
	{
		DolocAPI.uiSystem.GuidanceTips.RegisterEvent(gameEventString, textKey, keyCodeKey);
	}
}
