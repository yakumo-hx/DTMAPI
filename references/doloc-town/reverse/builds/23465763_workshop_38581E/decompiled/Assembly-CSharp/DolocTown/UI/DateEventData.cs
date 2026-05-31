using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Calendar;
using DolocTown.Config.Time;
using DolocTown.GameData;

namespace DolocTown.UI;

public struct DateEventData : IUIData
{
	public bool notEmpty { get; }

	public int time { get; }

	public string timeStr { get; }

	public string memoContent { get; }

	public bool isPlayerBirthday { get; }

	public string[] eventsTitle { get; }

	public string[] eventsDesc { get; }

	public DateEventData(int month, int day)
	{
		this = default(DateEventData);
		CalendarInfo orDefault = DolocConfig.Tables.TbCalendar.GetOrDefault(month);
		if (orDefault == null || !orDefault.Date_Index.TryGetValue(day, out var value))
		{
			return;
		}
		notEmpty = true;
		time = value.Day;
		timeStr = DolocUtils.Format(DolocConfig.StaticTexts.CalendarPanelDateTitle, month.ToString("D2"), time.ToString("D2"), DolocConfig.GetEnumText((WeekDay)((time - 1) % 7)));
		memoContent = DolocAPI.QueryMemoByDay(day);
		isPlayerBirthday = DolocAPI.archiveHandle.IsPlayerBirthday(month, day);
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		if (isPlayerBirthday)
		{
			list.Add(DolocUtils.Format(DolocConfig.StaticTexts.CalendarPanelPlayerBirthday, DolocAPI.archiveHandle.GetPlayerName()));
			list2.Add(string.Empty);
		}
		DolocTown.Config.Calendar.DateEventInfo[] events_Ref = value.Events_Ref;
		foreach (DolocTown.Config.Calendar.DateEventInfo dateEventInfo in events_Ref)
		{
			if (DolocAPI.archiveHandle.cityData.calendarManager.CheckFestivalUnlocked(dateEventInfo.Id))
			{
				if (dateEventInfo.Type == SpecialEventType.Birthday)
				{
					list.Add(DolocUtils.Format(dateEventInfo.Title, DolocAPI.GetNpcTitle(dateEventInfo.Id, ignoreUnknown: true)));
					list2.Add(dateEventInfo.Description);
				}
				else
				{
					list.Add(dateEventInfo.Title);
					list2.Add(dateEventInfo.Description);
				}
			}
		}
		eventsTitle = ((list.Count > 0) ? list.ToArray() : new string[1] { DolocConfig.StaticTexts.CalendarPanelEmptyHint });
		eventsDesc = ((list2.Count > 0) ? list2.ToArray() : new string[1] { string.Empty });
	}
}
