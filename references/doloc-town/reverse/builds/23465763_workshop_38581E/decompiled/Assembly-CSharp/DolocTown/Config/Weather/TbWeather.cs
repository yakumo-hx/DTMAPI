using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Weather;

public sealed class TbWeather
{
	private readonly Dictionary<WeatherType, WeatherInfo> _dataMap;

	private readonly List<WeatherInfo> _dataList;

	public Dictionary<WeatherType, WeatherInfo> DataMap => _dataMap;

	public List<WeatherInfo> DataList => _dataList;

	public WeatherInfo this[WeatherType key] => _dataMap[key];

	public WeatherInfo DefaultWeatherInfo => _dataList.First();

	public TbWeather(JSONNode _json)
	{
		_dataMap = new Dictionary<WeatherType, WeatherInfo>();
		_dataList = new List<WeatherInfo>();
		foreach (JSONNode child in _json.Children)
		{
			WeatherInfo weatherInfo = WeatherInfo.DeserializeWeatherInfo(child);
			if (_dataMap.TryAdd(weatherInfo.Id, weatherInfo))
			{
				_dataList.Add(weatherInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {weatherInfo.Id} in table: TbWeather");
			}
		}
	}

	public WeatherInfo GetOrDefault(WeatherType key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public WeatherInfo Get(WeatherType key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (WeatherInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (WeatherInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	public WeatherInfo GetWeatherInfo(WeatherType type)
	{
		WeatherInfo valueOrDefault = _dataMap.GetValueOrDefault(type, DefaultWeatherInfo);
		if (valueOrDefault.Id != type)
		{
			Debug.LogWarning($"未找到对应的天气信息\"{type}\"");
		}
		return valueOrDefault;
	}

	public bool IsWeatherPropertyChanged(WeatherType L, WeatherType R)
	{
		if (L == R)
		{
			return false;
		}
		return GetWeatherInfo(L).IsMalignantWeather != GetWeatherInfo(R).IsMalignantWeather;
	}

	public bool IsMalignantWeather(WeatherType weatherType)
	{
		return GetWeatherInfo(weatherType).IsMalignantWeather;
	}
}
