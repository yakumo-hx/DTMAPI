using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("天数检查", 0)]
[Description("检查天数是否达到指定值")]
public class DialogueTask_DayCheck : DialogueConditionTask
{
	[SerializeField]
	private int _day;

	public override string taskTitle => $"天数是否达到:{_day}";

	protected override bool CheckCondition()
	{
		return DolocAPI.archiveHandle.timeData.TotalDays >= _day;
	}
}
