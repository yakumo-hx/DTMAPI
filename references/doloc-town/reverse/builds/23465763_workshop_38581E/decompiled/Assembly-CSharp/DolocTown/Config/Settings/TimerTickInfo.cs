using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Settings;

public sealed class TimerTickInfo : BeanBase
{
	public const int __ID__ = 1763820485;

	public float Interval { get; private set; }

	public int RepeatTime { get; private set; }

	public TimerTickInfo(JSONNode _json)
	{
		if (!_json["interval"].IsNumber)
		{
			throw new SerializationException();
		}
		Interval = _json["interval"];
		if (!_json["repeat_time"].IsNumber)
		{
			throw new SerializationException();
		}
		RepeatTime = _json["repeat_time"];
	}

	public TimerTickInfo(float interval, int repeat_time)
	{
		Interval = interval;
		RepeatTime = repeat_time;
	}

	public static TimerTickInfo DeserializeTimerTickInfo(JSONNode _json)
	{
		return new TimerTickInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1763820485;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Interval:" + Interval + ",RepeatTime:" + RepeatTime + ",}";
	}
}
