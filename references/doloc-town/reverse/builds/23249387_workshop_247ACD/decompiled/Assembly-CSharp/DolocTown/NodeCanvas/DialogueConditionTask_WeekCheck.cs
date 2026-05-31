using DolocTown.Config;
using DolocTown.Config.Time;
using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Name("星期检查", 0)]
[Description("如果采用范围，则包含范围边界")]
public class DialogueConditionTask_WeekCheck : DialogueConditionTask
{
	public new bool invert;

	public bool isRange;

	public WeekDay weekDay;

	public WeekDay weekDayEnd;

	public override string taskTitle
	{
		get
		{
			if (!isRange)
			{
				return "如果时间" + NodeInfoInvert + "\"" + GetWeekStr(weekDay) + "\"";
			}
			return "如果时间处于\"" + GetWeekStr(weekDay) + "-" + GetWeekStr(weekDayEnd) + "\"" + NodeInfoInvert;
		}
	}

	private string NodeInfoInvert
	{
		get
		{
			if (!isRange)
			{
				if (!invert)
				{
					return "为";
				}
				return "不为";
			}
			if (!invert)
			{
				return "之内";
			}
			return "之外";
		}
	}

	private string GetWeekStr(WeekDay value)
	{
		return DolocConfig.GetEnumText(value);
	}

	protected override bool CheckCondition()
	{
		WeekDay weekDay = DolocAPI.archiveHandle.timeData.dateNow.WeekDay;
		if (invert)
		{
			if (isRange)
			{
				if (weekDay >= this.weekDay)
				{
					return weekDay > weekDayEnd;
				}
				return true;
			}
			return weekDay != this.weekDay;
		}
		if (isRange)
		{
			if (weekDay >= this.weekDay)
			{
				return weekDay <= weekDayEnd;
			}
			return false;
		}
		return weekDay == this.weekDay;
	}
}
