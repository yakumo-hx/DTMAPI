using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("小时检查", 0)]
[Description("小时数判断，如果采用范围，则包含范围边界")]
public class DialogueTask_HourCheck : DialogueConditionTask
{
	[SerializeField]
	private int hourStart;

	[SerializeField]
	private int hourEnd;

	[SerializeField]
	private bool hasRange;

	[SerializeField]
	private bool invertRange;

	[SerializeField]
	private CompareMethod compareMethod;

	public override string taskTitle
	{
		get
		{
			if (!hasRange)
			{
				return taskTitleOfCompare;
			}
			return taskTitleOfRange;
		}
	}

	private string taskTitleOfRange
	{
		get
		{
			string text = (invertRange ? "之外" : "之间");
			return "当前小时数是否在 " + DolocUtils.PadZero(hourStart) + " - " + DolocUtils.PadZero(hourEnd) + " " + text;
		}
	}

	private string taskTitleOfCompare => $"当前小时是否 {compareMethod.CompareLabel()} {hourStart}";

	protected override bool CheckCondition()
	{
		int hour = DolocAPI.archiveHandle.DateNow.Hour;
		if (hasRange)
		{
			bool flag = hour >= hourStart && hour <= hourEnd;
			if (invertRange)
			{
				return !flag;
			}
			return flag;
		}
		return OperationTools.Compare(hour, hourStart, compareMethod);
	}
}
