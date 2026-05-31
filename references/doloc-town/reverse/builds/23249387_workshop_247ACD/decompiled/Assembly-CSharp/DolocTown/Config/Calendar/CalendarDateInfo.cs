using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Calendar;

public sealed class CalendarDateInfo : BeanBase
{
	public const int __ID__ = 1143157642;

	public int Day { get; private set; }

	public string[] Events { get; private set; }

	public DateEventInfo[] Events_Ref { get; private set; }

	public CalendarDateInfo(JSONNode _json)
	{
		if (!_json["day"].IsNumber)
		{
			throw new SerializationException();
		}
		Day = _json["day"];
		JSONNode jSONNode = _json["events"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Events = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			Events[num++] = text;
		}
	}

	public CalendarDateInfo(int day, string[] events)
	{
		Day = day;
		Events = events;
	}

	public static CalendarDateInfo DeserializeCalendarDateInfo(JSONNode _json)
	{
		return new CalendarDateInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1143157642;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		int num = Events.Length;
		TbDateEvent tbDateEvent = (TbDateEvent)_tables["Calendar.TbDateEvent"];
		Events_Ref = new DateEventInfo[num];
		for (int i = 0; i < num; i++)
		{
			Events_Ref[i] = tbDateEvent.GetOrDefault(Events[i]);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Day:" + Day + ",Events:" + StringUtil.CollectionToString(Events) + ",}";
	}
}
