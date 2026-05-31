using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbBackgroundHighLevel
{
	private readonly Dictionary<BackgroundHighLevel, BackgroundHighLevelInfo> _dataMap;

	private readonly List<BackgroundHighLevelInfo> _dataList;

	public Dictionary<BackgroundHighLevel, BackgroundHighLevelInfo> DataMap => _dataMap;

	public List<BackgroundHighLevelInfo> DataList => _dataList;

	public BackgroundHighLevelInfo this[BackgroundHighLevel key] => _dataMap[key];

	public TbBackgroundHighLevel(JSONNode _json)
	{
		_dataMap = new Dictionary<BackgroundHighLevel, BackgroundHighLevelInfo>();
		_dataList = new List<BackgroundHighLevelInfo>();
		foreach (JSONNode child in _json.Children)
		{
			BackgroundHighLevelInfo backgroundHighLevelInfo = BackgroundHighLevelInfo.DeserializeBackgroundHighLevelInfo(child);
			if (_dataMap.TryAdd(backgroundHighLevelInfo.Id, backgroundHighLevelInfo))
			{
				_dataList.Add(backgroundHighLevelInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {backgroundHighLevelInfo.Id} in table: TbBackgroundHighLevel");
			}
		}
	}

	public BackgroundHighLevelInfo GetOrDefault(BackgroundHighLevel key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public BackgroundHighLevelInfo Get(BackgroundHighLevel key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (BackgroundHighLevelInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (BackgroundHighLevelInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
