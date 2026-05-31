using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("检查是否与自定义事件对象接触", 0)]
[Description("检查主角与给定的可交互对象是否处于接触状态(通过目标对象的自定义事件来标记该对象)")]
public class DialogueTask_TouchCustomEventObject : DialogueConditionTask
{
	[SerializeField]
	private string positionStatus = "";

	public override string taskTitle => "检查是否与自定义事件对象\"" + positionStatus + "\"接触";

	protected override bool CheckCondition()
	{
		if (positionStatus.IsNullOrEmpty())
		{
			return false;
		}
		if (DolocAPI.CurrentInteractableObject == null)
		{
			return false;
		}
		return DolocAPI.CurrentInteractableObject.CustomEventId == positionStatus;
	}
}
