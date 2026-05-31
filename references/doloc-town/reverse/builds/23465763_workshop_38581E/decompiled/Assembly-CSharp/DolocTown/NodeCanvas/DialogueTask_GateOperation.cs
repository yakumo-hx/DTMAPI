using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/城镇")]
[Name("禁用或启用传送门", 0)]
[Description("指定一个城镇房间，禁用其中的某个传送门")]
public class DialogueTask_GateOperation : DialogueTask
{
	[SerializeField]
	[RequiredField]
	public bool isDisable;

	[SerializeField]
	[RequiredField]
	public string gateName;

	public override string taskTitle => operationName + "<" + gateName + ">";

	protected string operationName
	{
		get
		{
			if (!isDisable)
			{
				return "启用";
			}
			return "禁用";
		}
	}

	public override void DoAction(Graph graph)
	{
		if (isDisable)
		{
			DolocAPI.DisableGate(gateName);
		}
		else
		{
			DolocAPI.EnableGate(gateName);
		}
	}
}
