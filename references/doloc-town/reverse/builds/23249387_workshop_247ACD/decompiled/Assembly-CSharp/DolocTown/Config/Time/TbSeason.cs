using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Time;

public sealed class TbSeason
{
	private readonly List<SeasonInfo> _dataList;

	private Dictionary<int, SeasonInfo> _dataMap_index;

	private Dictionary<int, SeasonInfo> _dataMap_month;

	public List<SeasonInfo> DataList => _dataList;

	public TbSeason(JSONNode _json)
	{
		_dataList = new List<SeasonInfo>();
		foreach (JSONNode child in _json.Children)
		{
			SeasonInfo item = SeasonInfo.DeserializeSeasonInfo(child);
			_dataList.Add(item);
		}
		_dataMap_index = new Dictionary<int, SeasonInfo>();
		_dataMap_month = new Dictionary<int, SeasonInfo>();
		foreach (SeasonInfo data in _dataList)
		{
			if (!_dataMap_index.TryAdd(data.Index, data))
			{
				Debug.LogError($"[Config] Duplicate key: {data.Index} in table: TbSeason");
			}
			if (!_dataMap_month.TryAdd(data.Month, data))
			{
				Debug.LogError($"[Config] Duplicate key: {data.Month} in table: TbSeason");
			}
		}
	}

	public SeasonInfo GetByIndex(int key)
	{
		if (!_dataMap_index.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public SeasonInfo GetByMonth(int key)
	{
		if (!_dataMap_month.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (SeasonInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (SeasonInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
