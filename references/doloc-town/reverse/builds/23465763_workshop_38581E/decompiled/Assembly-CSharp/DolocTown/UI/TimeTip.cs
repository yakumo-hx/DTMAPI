using DolocTown.Config;
using DolocTown.Config.Time;
using DolocTown.GameData;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class TimeTip : DolocBasicTip
{
	[SerializeField]
	private Text text;

	private string currentDateInfo;

	private string currentSeasonInfo;

	public string TimeInfo
	{
		get
		{
			return text.text;
		}
		set
		{
			text.text = value;
		}
	}

	private string currentFormatClock
	{
		get
		{
			DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
			return DolocUtils.PadZero(dateNow.Hour) + ":" + DolocUtils.PadZero(dateNow.Minute);
		}
	}

	public void UpdateTimeInfo(bool needRefresh = true)
	{
		if (needRefresh)
		{
			RefreshTimeInfoCache();
		}
		TimeInfo = " " + currentFormatClock.Colored(DolocUiColor.TEXTCOLOR_STD) + " " + currentDateInfo + " " + currentSeasonInfo;
	}

	protected override void OnClick()
	{
		base.OnClick();
		if (DolocAPI.archiveHandle.IsCalendarUnlocked())
		{
			DolocAPI.EnterUI<CalendarUiState>();
		}
	}

	private void RefreshTimeInfoCache()
	{
		DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
		currentDateInfo = GetWeekDay(dateNow.WeekDay) + " " + dateNow.GetDateSimpleInfo();
		Color color = (DolocAPI.archiveHandle.Season.IsRainy ? DolocUiColor.SLIENTCOLOR_BLUE : DolocUiColor.SLIENTCOLOR_ORANGE);
		currentSeasonInfo = GetSeasonTitle(DolocAPI.archiveHandle.DateNow.Month).Colored(color);
	}

	private string GetWeekDay(WeekDay day)
	{
		return DolocConfig.GetEnumText(day);
	}

	private string GetSeasonTitle(int month)
	{
		return DolocConfig.Tables.TbSeason.GetByMonth(month).SeasonTitle;
	}
}
