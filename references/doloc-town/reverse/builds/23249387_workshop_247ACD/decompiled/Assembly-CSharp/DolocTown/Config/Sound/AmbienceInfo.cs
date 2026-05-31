using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Weather;
using SimpleJSON;

namespace DolocTown.Config.Sound;

public sealed class AmbienceInfo : BeanBase
{
	public const int __ID__ = -1843164341;

	public bool IsIndoor { get; private set; }

	public WeatherType WeatherType { get; private set; }

	public string SoundEvent { get; private set; }

	public AmbienceInfo(JSONNode _json)
	{
		if (!_json["is_indoor"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsIndoor = _json["is_indoor"];
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

	public AmbienceInfo(bool is_indoor, WeatherType weather_type, string sound_event)
	{
		IsIndoor = is_indoor;
		WeatherType = weather_type;
		SoundEvent = sound_event;
	}

	public static AmbienceInfo DeserializeAmbienceInfo(JSONNode _json)
	{
		return new AmbienceInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1843164341;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ IsIndoor:" + IsIndoor + ",WeatherType:" + WeatherType.ToString() + ",SoundEvent:" + SoundEvent + ",}";
	}
}
