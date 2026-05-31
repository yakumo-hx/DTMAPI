using System.Linq;
using DolocTown.Config.Calendar;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown.UI;

public struct DateData : IUIData
{
	public bool notEmpty { get; }

	public int time { get; }

	public bool isSameDay { get; }

	public bool hasMemo { get; }

	public bool isPlayerBirthday { get; }

	public Sprite birthdayIcon { get; }

	public Sprite festivalIcon { get; }

	public DateData(int month, CalendarDateInfo calendarInfo)
	{
		this = default(DateData);
		if (calendarInfo != null)
		{
			notEmpty = true;
			time = calendarInfo.Day;
			DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
			isSameDay = month == dateNow.Month && time == dateNow.Day;
			CalendarManager calendarManager = DolocAPI.archiveHandle.cityData.calendarManager;
			hasMemo = calendarManager.QueryMemoByDay(time, out var _);
			isPlayerBirthday = DolocAPI.archiveHandle.IsPlayerBirthday(month, time);
			birthdayIcon = calendarInfo.Events_Ref.FirstOrDefault((DolocTown.Config.Calendar.DateEventInfo info) => info.Type == SpecialEventType.Birthday && calendarManager.CheckFestivalUnlocked(info.Id))?.IconAsset.Asset;
			festivalIcon = calendarInfo.Events_Ref.FirstOrDefault((DolocTown.Config.Calendar.DateEventInfo info) => info.Type == SpecialEventType.Festival && calendarManager.CheckFestivalUnlocked(info.Id))?.IconAsset.Asset;
		}
	}
}
