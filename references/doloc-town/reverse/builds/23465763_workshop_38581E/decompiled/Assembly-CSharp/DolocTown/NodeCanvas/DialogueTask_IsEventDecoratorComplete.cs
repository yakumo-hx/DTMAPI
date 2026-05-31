using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Category("任务或成就")]
[Name("检查事件装饰器", 0)]
[Description("指定一个事件装饰器Id并检查该装饰器是否已经完成")]
public class DialogueTask_IsEventDecoratorComplete : DialogueConditionTask
{
	[SerializeField]
	private string decoratorId;

	public override string taskTitle => "是否完成事件装饰器:" + decoratorId;

	protected override bool CheckCondition()
	{
		return DolocAPI.IsEventDecoratorComplete(decoratorId);
	}
}
