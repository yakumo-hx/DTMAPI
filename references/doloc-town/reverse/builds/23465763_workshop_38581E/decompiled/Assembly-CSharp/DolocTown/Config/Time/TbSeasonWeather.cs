using System;
using System.Collections.Generic;
using DolocTown.Config.Weather;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Time;

public sealed class TbSeasonWeather
{
	private readonly Dictionary<int, SeasonWeatherInfo> _dataMap;

	private readonly List<SeasonWeatherInfo> _dataList;

	private static Dictionary<Vector2Int, WeatherType> weatherLut;

	private static Dictionary<Vector2Int, (int, WeatherType)> weatherLutOverride;

	public Dictionary<int, SeasonWeatherInfo> DataMap => _dataMap;

	public List<SeasonWeatherInfo> DataList => _dataList;

	public SeasonWeatherInfo this[int key] => _dataMap[key];

	public TbSeasonWeather(JSONNode _json)
	{
		_dataMap = new Dictionary<int, SeasonWeatherInfo>();
		_dataList = new List<SeasonWeatherInfo>();
		foreach (JSONNode child in _json.Children)
		{
			SeasonWeatherInfo seasonWeatherInfo = SeasonWeatherInfo.DeserializeSeasonWeatherInfo(child);
			if (_dataMap.TryAdd(seasonWeatherInfo.Month, seasonWeatherInfo))
			{
				_dataList.Add(seasonWeatherInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {seasonWeatherInfo.Month} in table: TbSeasonWeather");
			}
		}
	}

	public SeasonWeatherInfo GetOrDefault(int key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public SeasonWeatherInfo Get(int key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (SeasonWeatherInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (SeasonWeatherInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	private static Dictionary<Vector2Int, WeatherType> LoadLut()
	{
		Dictionary<Vector2Int, WeatherType> dictionary = new Dictionary<Vector2Int, WeatherType>();
		foreach (KeyValuePair<int, SeasonWeatherInfo> item in DolocConfig.Tables.TbSeasonWeather.DataMap)
		{
			int key = item.Key;
			SeasonWeatherInfo value = item.Value;
			for (int i = 0; i < value.Day.Length; i++)
			{
				dictionary.Add(new Vector2Int(key, value.Day[i]), value.Weather[i]);
			}
		}
		return dictionary;
	}

	private static Dictionary<Vector2Int, (int, WeatherType)> LoadLutOverride()
	{
		Dictionary<Vector2Int, (int, WeatherType)> dictionary = new Dictionary<Vector2Int, (int, WeatherType)>();
		foreach (KeyValuePair<int, SeasonWeatherInfo> item in DolocConfig.Tables.TbSeasonWeather.DataMap)
		{
			int key = item.Key;
			SeasonWeatherInfo value = item.Value;
			for (int i = 0; i < value.Day.Length; i++)
			{
				if (value.WeatherOverride[i] != 0)
				{
					dictionary.Add(new Vector2Int(key, value.Day[i]), (value.Level[i], value.WeatherOverride[i]));
				}
			}
		}
		return dictionary;
	}

	public static bool QueryWeather(int month, int day, out WeatherType weatherType)
	{
		if (weatherLut == null)
		{
			weatherLut = LoadLut();
		}
		return weatherLut.TryGetValue(new Vector2Int(month, day), out weatherType);
	}

	public static bool QueryWeatherOverride(int month, int day, int optimizeLv, out WeatherType weatherType)
	{
		weatherType = WeatherType.NONE;
		if (weatherLutOverride == null)
		{
			weatherLutOverride = LoadLutOverride();
		}
		if (weatherLutOverride.TryGetValue(new Vector2Int(month, day), out var value))
		{
			if (value.Item2 == WeatherType.NONE)
			{
				return false;
			}
			if (value.Item1 <= optimizeLv)
			{
				weatherType = value.Item2;
				return true;
			}
		}
		return false;
	}
}
