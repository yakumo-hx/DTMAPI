using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("小时过滤器", 0)]
[Description("根据小时过滤，当设为范围时，范围为左闭右开，即包含起始时间，不包含结束时间")]
public class NpcScheduleNodeFilterHour : NpcScheduleNodeFilter
{
	[SerializeField]
	[ExposeField]
	private bool invert;

	[SerializeField]
	[ExposeField]
	private bool isRange;

	[SerializeField]
	[ExposeField]
	private int hour;

	[SerializeField]
	[ExposeField]
	private int hourEnd;

	[SerializeField]
	[ExposeField]
	private bool enableMinute;

	[SerializeField]
	[ExposeField]
	private int minute;

	[SerializeField]
	[ExposeField]
	private int minuteEnd;

	public override bool IsMatch(NpcScheduleParams npcScheduleParams)
	{
		if (enableMinute)
		{
			return IsMatchByMinute(npcScheduleParams);
		}
		return IsMatchByHour(npcScheduleParams);
	}

	private bool IsMatchByMinute(NpcScheduleParams status)
	{
		if (invert)
		{
			if (isRange)
			{
				if (!IsLessThan(status, hour, minute))
				{
					return IsGreaterOrEqual(status, hourEnd, minuteEnd);
				}
				return true;
			}
			return !IsEqual(status, hour, minute);
		}
		if (isRange)
		{
			if (IsGreaterOrEqual(status, hour, minute))
			{
				return IsLessThan(status, hourEnd, minuteEnd);
			}
			return false;
		}
		return IsEqual(status, hour, minute);
	}

	private bool IsEqual(NpcScheduleParams status, int hour, int minute)
	{
		if (status.date.Hour == hour)
		{
			return status.date.Minute == minute;
		}
		return false;
	}

	private bool IsLessThan(NpcScheduleParams status, int hour, int minute)
	{
		if (status.date.Hour == hour)
		{
			return status.date.Minute < minute;
		}
		return status.date.Hour < hour;
	}

	private bool IsGreaterOrEqual(NpcScheduleParams status, int hour, int minute)
	{
		if (status.date.Hour == hour)
		{
			return status.date.Minute >= minute;
		}
		return status.date.Hour >= hour;
	}

	private bool IsMatchByHour(NpcScheduleParams status)
	{
		int num = status.date.Hour;
		if (invert)
		{
			if (isRange)
			{
				if (num >= hour)
				{
					return num >= hourEnd;
				}
				return true;
			}
			return num != hour;
		}
		if (isRange)
		{
			if (num >= hour)
			{
				return num < hourEnd;
			}
			return false;
		}
		return num == hour;
	}
}
