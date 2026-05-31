using NodeCanvas.Framework;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/任务")]
[Name("完成事件装饰器", 0)]
[Description("完成或移除事件装饰器")]
public class DialogueTask_AddEventDecorator : DialogueTask
{
	[SerializeField]
	public bool remove;

	[SerializeField]
	private string decoratorId;

	public override string taskTitle => prefix + "事件装饰器:" + decoratorId;

	private string prefix
	{
		get
		{
			if (!remove)
			{
				return "添加";
			}
			return "移除";
		}
	}

	public override void DoAction(Graph graph)
	{
		if (remove ? DolocAPI.RemoveEventDecorator(decoratorId) : DolocAPI.AddEventDecorator(decoratorId))
		{
			DolocAPI.outputSuccess("事件装饰器:" + decoratorId + prefix + "成功");
		}
		else
		{
			DolocAPI.outputError("事件装饰器:" + decoratorId + prefix + "失败");
		}
	}
}
