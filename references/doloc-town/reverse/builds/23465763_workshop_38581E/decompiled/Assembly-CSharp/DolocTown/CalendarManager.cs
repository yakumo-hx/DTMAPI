using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Calendar;
using DolocTown.GameData;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class CalendarManager
{
	[JsonProperty]
	private Dictionary<int, string> memoList;

	[JsonProperty]
	private HashSet<string> unlockFestival;

	[JsonConstructor]
	public CalendarManager(Dictionary<int, string> memoList = null, HashSet<string> unlockFestival = null)
	{
		this.memoList = memoList ?? new Dictionary<int, string>();
		this.unlockFestival = unlockFestival ?? new HashSet<string>();
	}

	public void AddMemo(int day, string content)
	{
		if (memoList.ContainsKey(day))
		{
			memoList[day] = content;
		}
		else
		{
			memoList.Add(day, content);
		}
	}

	public void DelMemo(int day)
	{
		if (memoList.ContainsKey(day))
		{
			memoList.Remove(day);
		}
	}

	public bool QueryMemoByDay(int day, out string content)
	{
		return memoList.TryGetValue(day, out content);
	}

	public void ResetMemo()
	{
		memoList.Clear();
	}

	public void UnlockFestival(string id)
	{
		if (DolocConfig.Tables.TbDateEvent.DataMap.ContainsKey(id))
		{
			unlockFestival.Add(id);
		}
	}

	public void LockFestival(string id)
	{
		unlockFestival.Remove(id);
	}

	public bool CheckFestivalUnlocked(string name)
	{
		DateEventInfo orDefault = DolocConfig.Tables.TbDateEvent.GetOrDefault(name);
		if (orDefault == null)
		{
			return false;
		}
		if (!orDefault.DefaultUnlock)
		{
			return unlockFestival.Contains(name);
		}
		return true;
	}

	public void DailyRefresh()
	{
		DateInfo dateNow = DolocAPI.archiveHandle.DateNow;
		if (memoList.TryGetValue(dateNow.Day, out var value))
		{
			DolocAPI.ShowMessageBoxNode(LocSprites.UI_INFOICON_STAR_24PX, DolocConfig.StaticTexts.CalendarMemoMessage.Format(dateNow.Month, dateNow.Day, value));
		}
	}

	public bool QueryNpcBirthdayByName(string npcName, out int month, out int day)
	{
		month = 0;
		day = 0;
		foreach (CalendarInfo data in DolocConfig.Tables.TbCalendar.DataList)
		{
			CalendarDateInfo[] date = data.Date;
			foreach (CalendarDateInfo calendarDateInfo in date)
			{
				if (!(calendarDateInfo.Events_Ref.FirstOrDefault((DateEventInfo x) => x.Type == SpecialEventType.Birthday)?.Id != npcName))
				{
					month = data.Month;
					day = calendarDateInfo.Day;
					return true;
				}
			}
		}
		return false;
	}

	public string QueryNpcBirthdayByDate(int month, int day)
	{
		CalendarInfo orDefault = DolocConfig.Tables.TbCalendar.GetOrDefault(month);
		if (orDefault == null)
		{
			return string.Empty;
		}
		if (!orDefault.Date_Index.TryGetValue(day, out var value))
		{
			return string.Empty;
		}
		return value.Events_Ref.FirstOrDefault((DateEventInfo x) => x.Type == SpecialEventType.Birthday)?.Id;
	}
}
