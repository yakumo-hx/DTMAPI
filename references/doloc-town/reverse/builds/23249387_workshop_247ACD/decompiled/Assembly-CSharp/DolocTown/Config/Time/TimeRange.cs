using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Time;

public sealed class TimeRange : BeanBase
{
	public const int __ID__ = 1400235151;

	public int StartTime { get; private set; }

	public int EndTime { get; private set; }

	public TimeRange(JSONNode _json)
	{
		if (!_json["start_time"].IsNumber)
		{
			throw new SerializationException();
		}
		StartTime = _json["start_time"];
		if (!_json["end_time"].IsNumber)
		{
			throw new SerializationException();
		}
		EndTime = _json["end_time"];
	}

	public TimeRange(int start_time, int end_time)
	{
		StartTime = start_time;
		EndTime = end_time;
	}

	public static TimeRange DeserializeTimeRange(JSONNode _json)
	{
		return new TimeRange(_json);
	}

	public override int GetTypeId()
	{
		return 1400235151;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ StartTime:" + StartTime + ",EndTime:" + EndTime + ",}";
	}

	public bool InRange(int hour, bool ignoreEqual = true)
	{
		if (StartTime == EndTime)
		{
			if (!ignoreEqual)
			{
				return StartTime == hour;
			}
			return true;
		}
		if (StartTime < EndTime)
		{
			if (StartTime <= hour)
			{
				return hour <= EndTime;
			}
			return false;
		}
		if (StartTime > hour)
		{
			return hour <= EndTime;
		}
		return true;
	}
}
