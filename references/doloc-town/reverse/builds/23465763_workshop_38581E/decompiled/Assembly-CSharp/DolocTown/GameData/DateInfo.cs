using DolocTown.Config;
using DolocTown.Config.Time;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown.GameData;

[DebugObject]
[JsonObject(MemberSerialization.OptIn)]
public struct DateInfo
{
	[JsonProperty]
	[DebugInfo("总时间单位数")]
	public int TotalTUs;

	[JsonProperty]
	[DebugInfo("总天数")]
	public int TotalDays;

	[JsonProperty]
	[DebugInfo("分/某天")]
	public int Minute;

	[JsonProperty]
	[DebugInfo("小时/某天")]
	public int Hour;

	[JsonProperty]
	[DebugInfo("天/某月")]
	public int Day;

	[JsonProperty]
	[DebugInfo("月/某年")]
	public int Month;

	[JsonProperty]
	[DebugInfo("年")]
	public int Year;

	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public WeekDay WeekDay;

	private int YearOffset;

	private int MonthOffset;

	private int DayOffset;

	public int YearShown => Year + YearOffset;

	public int MonthShown => Month + MonthOffset;

	public int DayShown => Day + DayOffset;

	public int TotalMonth => Year * DolocAPI.GlobalParameter.Year2Month + Month;

	public int NextMonth
	{
		get
		{
			if (Month + 1 <= DolocAPI.GlobalParameter.Year2Month)
			{
				return Month + 1;
			}
			return 1;
		}
	}

	public int LastMonth
	{
		get
		{
			if (Month - 1 >= 1)
			{
				return Month - 1;
			}
			return DolocAPI.GlobalParameter.Year2Month;
		}
	}

	public Vector2Int CurrentWeatherKey
	{
		get
		{
			int hour = Hour;
			if (hour >= 6)
			{
				if (hour < 18)
				{
					return new Vector2Int(Day, 6);
				}
				return new Vector2Int(Day, 18);
			}
			return new Vector2Int(Day - 1, 18);
		}
	}

	private DateConfig dateConfig => DolocAPI.GlobalParameter.DateConfig;

	[JsonConstructor]
	public DateInfo(int totalTUs, int totalDays, int minute, int hour, int day, int month, int year, WeekDay weekDay)
	{
		TotalTUs = totalTUs;
		TotalDays = totalDays;
		Minute = minute;
		Hour = hour;
		Day = day;
		Month = month;
		Year = year;
		WeekDay = weekDay;
		int totalDayOffset = DolocAPI.GlobalParameter.TotalDayOffset;
		int month2Day = DolocAPI.GlobalParameter.Month2Day;
		int num = month2Day * DolocAPI.GlobalParameter.Year2Month;
		YearOffset = totalDayOffset / num;
		MonthOffset = totalDayOffset % num / month2Day;
		DayOffset = totalDayOffset - YearOffset * num - MonthOffset * month2Day;
	}

	public static WeekDay NextWeekDay(WeekDay day)
	{
		return day switch
		{
			WeekDay.MONDAY => WeekDay.TUESDAY, 
			WeekDay.TUESDAY => WeekDay.WEDNESDAY, 
			WeekDay.WEDNESDAY => WeekDay.THURSDAY, 
			WeekDay.THURSDAY => WeekDay.FRIDAY, 
			WeekDay.FRIDAY => WeekDay.SATURDAY, 
			WeekDay.SATURDAY => WeekDay.SUNDAY, 
			WeekDay.SUNDAY => WeekDay.MONDAY, 
			_ => WeekDay.MONDAY, 
		};
	}

	public DateInfo Copy()
	{
		return new DateInfo(TotalTUs, TotalDays, Minute, Hour, Day, Month, Year, WeekDay);
	}

	public string GetDateSimpleInfo()
	{
		return "X" + GetDateInfo();
	}

	public string GetDetailDateInfo()
	{
		return "X" + GetDateInfo() + " " + DolocUtils.PadZero(Hour) + ":" + DolocUtils.PadZero(Minute);
	}

	public string GetDateInfo()
	{
		return DolocUtils.Format(DolocConfig.StaticTexts.UiTextTimeFormat, DolocUtils.PadZero(YearShown), DolocUtils.PadZero(MonthShown), DolocUtils.PadZero(DayShown));
	}

	public int GetTotalHour(DateConfig config)
	{
		return TotalDays * config.Day2Hour + Hour;
	}

	public int CalMinuteDiff(DateInfo target)
	{
		return Mathf.Abs(TotalTUs - target.TotalTUs) * dateConfig.TU2Min;
	}

	public int CalHourDiff(DateInfo target)
	{
		return Mathf.Abs(GetTotalHour(dateConfig) - target.GetTotalHour(dateConfig));
	}

	public string GetWeekDayOfDay(int day)
	{
		return DolocConfig.GetEnumText((WeekDay)((int)(WeekDay + day) % 7));
	}

	private void AddTimeUnit()
	{
		TotalTUs++;
		Minute += dateConfig.TU2Min;
		if (Minute < dateConfig.Hour2Min)
		{
			return;
		}
		Minute %= dateConfig.Hour2Min;
		Hour++;
		if (Hour < dateConfig.Day2Hour)
		{
			return;
		}
		Hour = 0;
		TotalDays++;
		Day++;
		WeekDay = NextWeekDay(WeekDay);
		if (Day > dateConfig.Month2Day)
		{
			Day = 1;
			Month++;
			if (Month > dateConfig.Year2Month)
			{
				Month = 1;
				Year++;
			}
		}
	}

	private void SubTimeUnit()
	{
		if (TotalTUs == 0)
		{
			return;
		}
		TotalTUs--;
		Minute -= dateConfig.TU2Min;
		if (Minute >= 0)
		{
			return;
		}
		Minute += dateConfig.Hour2Min;
		Hour--;
		if (Hour >= 0)
		{
			return;
		}
		Hour = dateConfig.Day2Hour - 1;
		TotalDays--;
		Day--;
		WeekDay = TimeUtils.TraceBackWeekday(WeekDay, 1);
		if (Day <= 0)
		{
			Day = dateConfig.Month2Day;
			Month--;
			if (Month <= 0)
			{
				Month = dateConfig.Year2Month;
				Year--;
			}
		}
	}

	public DateInfo CopyWithDayOffset(int dayOffset)
	{
		DateInfo result = Copy();
		int num = dateConfig.Day2Hour * dateConfig.Hour2Min / dateConfig.TU2Min * dayOffset;
		if (num == 0)
		{
			return result;
		}
		for (int i = 0; i < Mathf.Abs(num); i++)
		{
			if (num > 0)
			{
				result.AddTimeUnit();
			}
			else
			{
				result.SubTimeUnit();
			}
		}
		return result;
	}
}
