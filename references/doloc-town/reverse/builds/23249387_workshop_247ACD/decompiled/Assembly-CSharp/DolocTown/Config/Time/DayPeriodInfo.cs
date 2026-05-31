using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Time;

public sealed class DayPeriodInfo : BeanBase
{
	public const int __ID__ = -587456150;

	public int DayHour { get; private set; }

	public DayPeriodType DayPeriodType { get; private set; }

	public DayPeriodInfo(JSONNode _json)
	{
		if (!_json["day_hour"].IsNumber)
		{
			throw new SerializationException();
		}
		DayHour = _json["day_hour"];
		if (!_json["day_period_type"].IsNumber)
		{
			throw new SerializationException();
		}
		DayPeriodType = (DayPeriodType)_json["day_period_type"].AsInt;
	}

	public DayPeriodInfo(int day_hour, DayPeriodType day_period_type)
	{
		DayHour = day_hour;
		DayPeriodType = day_period_type;
	}

	public static DayPeriodInfo DeserializeDayPeriodInfo(JSONNode _json)
	{
		return new DayPeriodInfo(_json);
	}

	public override int GetTypeId()
	{
		return -587456150;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ DayHour:" + DayHour + ",DayPeriodType:" + DayPeriodType.ToString() + ",}";
	}
}
