using DolocTown.Config.Time;

namespace DolocTown.GameData;

public static class TimeUtils
{
	public static DateInfo CalcDateInfo(int totalSeconds, int tuLength, int tu2Min, int hour2Min, int day2Hour, int month2Day, int year2Month)
	{
		int num = totalSeconds / tuLength;
		int num2 = num * tu2Min;
		int num3 = num2 / hour2Min;
		int num4 = num3 / day2Hour;
		int num5 = num4 / month2Day;
		int year = num5 / year2Month;
		int minute = num2 % hour2Min;
		int hour = num3 % day2Hour;
		int day = num4 % month2Day;
		int month = num5 % year2Month;
		return new DateInfo(num, num4, minute, hour, day, month, year, WeekDay.MONDAY);
	}

	public static WeekDay TraceBackWeekday(WeekDay today, int n)
	{
		return (WeekDay)((int)(today + 7 - n % 7) % 7);
	}
}
