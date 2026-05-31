using DolocTown.Config.Time;
using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("星期过滤器", 0)]
public class SwitchScheduleNodeFilterWeek : SwitchScheduleNodeFilter
{
	[SerializeField]
	[ExposeField]
	private bool invert;

	[SerializeField]
	[ExposeField]
	private bool isRange;

	[SerializeField]
	[ExposeField]
	private WeekDay weekDay;

	[SerializeField]
	[ExposeField]
	private WeekDay weekDayEnd;

	private static readonly string[] weekStrs = new string[7] { "一", "二", "三", "四", "五", "六", "日" };

	public override bool IsMatch(SwitchScheduleParams param)
	{
		WeekDay weekDay = param.date.WeekDay;
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
