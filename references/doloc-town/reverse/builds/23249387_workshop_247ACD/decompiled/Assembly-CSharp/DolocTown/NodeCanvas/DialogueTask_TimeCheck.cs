using System;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("时间检查", 0)]
[Description("时间判断，如果采用范围，则包含范围边界")]
public class DialogueTask_TimeCheck : DialogueConditionTask
{
	public enum TimeCheckType
	{
		TotalDays,
		Month,
		WeekDay,
		Date,
		Hour
	}

	[SerializeField]
	private int timeStart;

	[SerializeField]
	private int timeEnd;

	[SerializeField]
	private bool hasRange;

	[SerializeField]
	private bool invertRange;

	[SerializeField]
	private CompareMethod compareMethod;

	[SerializeField]
	private TimeCheckType timeCheckType;

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

	private string timeTargetLabel => timeCheckType switch
	{
		TimeCheckType.TotalDays => "总天数", 
		TimeCheckType.Month => "月份", 
		TimeCheckType.WeekDay => "星期", 
		TimeCheckType.Date => "日期", 
		TimeCheckType.Hour => "小时", 
		_ => throw new ArgumentOutOfRangeException(), 
	};

	private string taskTitleOfRange
	{
		get
		{
			string text = (invertRange ? "之外" : "之间");
			return "当前<" + timeTargetLabel + ">是否在 " + DolocUtils.PadZero(timeStart) + " : " + DolocUtils.PadZero(timeEnd) + " " + text;
		}
	}

	private string taskTitleOfCompare => $"当前<{timeTargetLabel}>是否 {compareMethod.CompareLabel()} {timeStart}";

	protected override bool CheckCondition()
	{
		int num = 0;
		num = timeCheckType switch
		{
			TimeCheckType.TotalDays => DolocAPI.archiveHandle.DateNow.TotalDays, 
			TimeCheckType.Month => DolocAPI.archiveHandle.DateNow.Month, 
			TimeCheckType.WeekDay => (int)(DolocAPI.archiveHandle.CurrentWeekDay + 1), 
			TimeCheckType.Date => DolocAPI.archiveHandle.DateNow.Day, 
			TimeCheckType.Hour => DolocAPI.archiveHandle.DateNow.Hour, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		if (hasRange)
		{
			bool flag = num >= timeStart && num <= timeEnd;
			if (invertRange)
			{
				return !flag;
			}
			return flag;
		}
		return OperationTools.Compare(num, timeStart, compareMethod);
	}
}
