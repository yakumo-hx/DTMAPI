using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Calendar;

public sealed class CalendarInfo : BeanBase
{
	public readonly Dictionary<int, CalendarDateInfo> Date_Index = new Dictionary<int, CalendarDateInfo>();

	public const int __ID__ = -1728737860;

	public int Month { get; private set; }

	public CalendarDateInfo[] Date { get; private set; }

	public CalendarInfo(JSONNode _json)
	{
		if (!_json["month"].IsNumber)
		{
			throw new SerializationException();
		}
		Month = _json["month"];
		JSONNode jSONNode = _json["date"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Date = new CalendarDateInfo[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			CalendarDateInfo calendarDateInfo = CalendarDateInfo.DeserializeCalendarDateInfo(child);
			Date[num++] = calendarDateInfo;
		}
		CalendarDateInfo[] date = Date;
		foreach (CalendarDateInfo calendarDateInfo2 in date)
		{
			Date_Index.Add(calendarDateInfo2.Day, calendarDateInfo2);
		}
	}

	public CalendarInfo(int month, CalendarDateInfo[] date)
	{
		Month = month;
		Date = date;
		CalendarDateInfo[] date2 = Date;
		foreach (CalendarDateInfo calendarDateInfo in date2)
		{
			Date_Index.Add(calendarDateInfo.Day, calendarDateInfo);
		}
	}

	public static CalendarInfo DeserializeCalendarInfo(JSONNode _json)
	{
		return new CalendarInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1728737860;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		CalendarDateInfo[] date = Date;
		for (int i = 0; i < date.Length; i++)
		{
			date[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		CalendarDateInfo[] date = Date;
		for (int i = 0; i < date.Length; i++)
		{
			date[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Month:" + Month + ",Date:" + StringUtil.CollectionToString(Date) + ",}";
	}
}
