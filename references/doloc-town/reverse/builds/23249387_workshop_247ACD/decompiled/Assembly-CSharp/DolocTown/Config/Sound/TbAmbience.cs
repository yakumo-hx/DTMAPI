using System;
using System.Collections.Generic;
using DolocTown.Config.Weather;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Sound;

public sealed class TbAmbience
{
	private readonly List<AmbienceInfo> _dataList;

	private Dictionary<(bool, WeatherType), AmbienceInfo> _dataMapUnion;

	public List<AmbienceInfo> DataList => _dataList;

	public TbAmbience(JSONNode _json)
	{
		_dataList = new List<AmbienceInfo>();
		foreach (JSONNode child in _json.Children)
		{
			AmbienceInfo item = AmbienceInfo.DeserializeAmbienceInfo(child);
			_dataList.Add(item);
		}
		_dataMapUnion = new Dictionary<(bool, WeatherType), AmbienceInfo>();
		List<AmbienceInfo> list = new List<AmbienceInfo>();
		foreach (AmbienceInfo data in _dataList)
		{
			if (!_dataMapUnion.TryAdd((data.IsIndoor, data.WeatherType), data))
			{
				list.Add(data);
				Debug.LogError($"[Config] Duplicate key: {(data.IsIndoor, data.WeatherType)} in table: TbAmbience");
			}
		}
		foreach (AmbienceInfo item2 in list)
		{
			_dataList.Remove(item2);
		}
	}

	public AmbienceInfo Get(bool is_indoor, WeatherType weather_type)
	{
		if (!_dataMapUnion.TryGetValue((is_indoor, weather_type), out var value))
		{
			return null;
		}
		return value;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (AmbienceInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (AmbienceInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
