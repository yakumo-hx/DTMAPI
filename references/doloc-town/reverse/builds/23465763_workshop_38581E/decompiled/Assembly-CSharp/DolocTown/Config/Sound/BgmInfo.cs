using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Time;
using DolocTown.Config.Weather;
using SimpleJSON;

namespace DolocTown.Config.Sound;

public sealed class BgmInfo : BeanBase
{
	public const int __ID__ = -1660365961;

	public EnvironmentType EnvironmentType { get; private set; }

	public DayPeriodType DaytimeType { get; private set; }

	public WeatherType WeatherType { get; private set; }

	public string SoundEvent { get; private set; }

	public BgmInfo(JSONNode _json)
	{
		if (!_json["environment_type"].IsNumber)
		{
			throw new SerializationException();
		}
		EnvironmentType = (EnvironmentType)_json["environment_type"].AsInt;
		if (!_json["daytime_type"].IsNumber)
		{
			throw new SerializationException();
		}
		DaytimeType = (DayPeriodType)_json["daytime_type"].AsInt;
		if (!_json["weather_type"].IsNumber)
		{
			throw new SerializationException();
		}
		WeatherType = (WeatherType)_json["weather_type"].AsInt;
		if (!_json["sound_event"].IsString)
		{
			throw new SerializationException();
		}
		SoundEvent = _json["sound_event"];
	}

	public BgmInfo(EnvironmentType environment_type, DayPeriodType daytime_type, WeatherType weather_type, string sound_event)
	{
		EnvironmentType = environment_type;
		DaytimeType = daytime_type;
		WeatherType = weather_type;
		SoundEvent = sound_event;
	}

	public static BgmInfo DeserializeBgmInfo(JSONNode _json)
	{
		return new BgmInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1660365961;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ EnvironmentType:" + EnvironmentType.ToString() + ",DaytimeType:" + DaytimeType.ToString() + ",WeatherType:" + WeatherType.ToString() + ",SoundEvent:" + SoundEvent + ",}";
	}
}
