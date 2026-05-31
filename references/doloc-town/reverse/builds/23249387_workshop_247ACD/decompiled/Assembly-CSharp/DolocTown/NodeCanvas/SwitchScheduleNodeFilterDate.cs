using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("日期过滤器", 0)]
public class SwitchScheduleNodeFilterDate : SwitchScheduleNodeFilter
{
	private struct Date
	{
		public int month;

		public int day;
	}

	[SerializeField]
	[ExposeField]
	private bool invert;

	[SerializeField]
	[ExposeField]
	private bool isRange;

	[SerializeField]
	[ExposeField]
	private bool ignoreMonth;

	[SerializeField]
	[ExposeField]
	private bool ignoreDay;

	[SerializeField]
	[ExposeField]
	private Date date;

	[SerializeField]
	[ExposeField]
	private Date dateEnd;

	public override bool IsMatch(SwitchScheduleParams npcScheduleParams)
	{
		if (ignoreMonth && ignoreDay)
		{
			return true;
		}
		int month = npcScheduleParams.date.Month;
		int day = npcScheduleParams.date.Day;
		if (invert)
		{
			if (ignoreDay)
			{
				return !_IsMatchMonth(month);
			}
			if (ignoreMonth)
			{
				return !_IsMatchDay(day);
			}
			return !_IsMatch(month, day);
		}
		if (ignoreDay)
		{
			return _IsMatchMonth(month);
		}
		if (ignoreMonth)
		{
			return _IsMatchDay(day);
		}
		return _IsMatch(month, day);
	}

	private bool _IsMatch(int month, int day)
	{
		if (isRange)
		{
			if (month < date.month || month > dateEnd.month)
			{
				return false;
			}
			if (month == date.month)
			{
				return day >= date.day;
			}
			if (month == dateEnd.month)
			{
				return day <= date.day;
			}
			return true;
		}
		if (month != date.month)
		{
			return false;
		}
		return day == date.day;
	}

	private bool _IsMatchMonth(int month)
	{
		if (isRange)
		{
			if (month >= date.month)
			{
				return month <= dateEnd.month;
			}
			return false;
		}
		return month == date.month;
	}

	private bool _IsMatchDay(int day)
	{
		if (isRange)
		{
			if (day >= date.day)
			{
				return day <= dateEnd.day;
			}
			return false;
		}
		return day == date.day;
	}
}
