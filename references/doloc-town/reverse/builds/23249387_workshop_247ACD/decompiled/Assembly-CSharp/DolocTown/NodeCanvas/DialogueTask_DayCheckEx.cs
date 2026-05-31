using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("总天数检查(扩展)", 0)]
[Description("判断当前总天数与目标参数的关系")]
public class DialogueTask_DayCheckEx : DialogueConditionTask
{
	[SerializeField]
	private int _day;

	[SerializeField]
	private CompareMethod _compareMethod;

	public override string taskTitle => $"总天数 {_compareMethod.CompareLabel()} {_day}";

	protected override bool CheckCondition()
	{
		return OperationTools.Compare(DolocAPI.archiveHandle.timeData.TotalDays, _day, _compareMethod);
	}
}
