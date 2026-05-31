using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Weather;
using SimpleJSON;

namespace DolocTown.Config.Time;

public sealed class SeasonWeatherInfo : BeanBase
{
	public const int __ID__ = 829570814;

	public int Month { get; private set; }

	public int[] Day { get; private set; }

	public WeatherType[] Weather { get; private set; }

	public WeatherType[] WeatherOverride { get; private set; }

	public int[] Level { get; private set; }

	public SeasonWeatherInfo(JSONNode _json)
	{
		if (!_json["month"].IsNumber)
		{
			throw new SerializationException();
		}
		Month = _json["month"];
		JSONNode jSONNode = _json["day"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Day = new int[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsNumber)
			{
				throw new SerializationException();
			}
			int num2 = child;
			Day[num++] = num2;
		}
		JSONNode jSONNode2 = _json["weather"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		Weather = new WeatherType[count2];
		int num3 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsNumber)
			{
				throw new SerializationException();
			}
			WeatherType asInt = (WeatherType)child2.AsInt;
			Weather[num3++] = asInt;
		}
		JSONNode jSONNode3 = _json["weather_override"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count3 = jSONNode3.Count;
		WeatherOverride = new WeatherType[count3];
		int num4 = 0;
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsNumber)
			{
				throw new SerializationException();
			}
			WeatherType asInt2 = (WeatherType)child3.AsInt;
			WeatherOverride[num4++] = asInt2;
		}
		JSONNode jSONNode4 = _json["level"];
		if (!jSONNode4.IsArray)
		{
			throw new SerializationException();
		}
		int count4 = jSONNode4.Count;
		Level = new int[count4];
		int num5 = 0;
		foreach (JSONNode child4 in jSONNode4.Children)
		{
			if (!child4.IsNumber)
			{
				throw new SerializationException();
			}
			int num6 = child4;
			Level[num5++] = num6;
		}
	}

	public SeasonWeatherInfo(int month, int[] day, WeatherType[] weather, WeatherType[] weather_override, int[] level)
	{
		Month = month;
		Day = day;
		Weather = weather;
		WeatherOverride = weather_override;
		Level = level;
	}

	public static SeasonWeatherInfo DeserializeSeasonWeatherInfo(JSONNode _json)
	{
		return new SeasonWeatherInfo(_json);
	}

	public override int GetTypeId()
	{
		return 829570814;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Month:" + Month + ",Day:" + StringUtil.CollectionToString(Day) + ",Weather:" + StringUtil.CollectionToString(Weather) + ",WeatherOverride:" + StringUtil.CollectionToString(WeatherOverride) + ",Level:" + StringUtil.CollectionToString(Level) + ",}";
	}
}
