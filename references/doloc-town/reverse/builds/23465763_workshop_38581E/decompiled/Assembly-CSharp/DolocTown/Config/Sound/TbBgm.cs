using System;
using System.Collections.Generic;
using DolocTown.Config.Time;
using DolocTown.Config.Weather;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Sound;

public sealed class TbBgm
{
	private readonly List<BgmInfo> _dataList;

	private Dictionary<(EnvironmentType, DayPeriodType, WeatherType), BgmInfo> _dataMapUnion;

	public List<BgmInfo> DataList => _dataList;

	public TbBgm(JSONNode _json)
	{
		_dataList = new List<BgmInfo>();
		foreach (JSONNode child in _json.Children)
		{
			BgmInfo item = BgmInfo.DeserializeBgmInfo(child);
			_dataList.Add(item);
		}
		_dataMapUnion = new Dictionary<(EnvironmentType, DayPeriodType, WeatherType), BgmInfo>();
		List<BgmInfo> list = new List<BgmInfo>();
		foreach (BgmInfo data in _dataList)
		{
			if (!_dataMapUnion.TryAdd((data.EnvironmentType, data.DaytimeType, data.WeatherType), data))
			{
				list.Add(data);
				Debug.LogError($"[Config] Duplicate key: {(data.EnvironmentType, data.DaytimeType, data.WeatherType)} in table: TbBgm");
			}
		}
		foreach (BgmInfo item2 in list)
		{
			_dataList.Remove(item2);
		}
	}

	public BgmInfo Get(EnvironmentType environment_type, DayPeriodType daytime_type, WeatherType weather_type)
	{
		if (!_dataMapUnion.TryGetValue((environment_type, daytime_type, weather_type), out var value))
		{
			return null;
		}
		return value;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (BgmInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (BgmInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
