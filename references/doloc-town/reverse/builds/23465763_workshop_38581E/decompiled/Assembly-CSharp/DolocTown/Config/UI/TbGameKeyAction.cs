using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.UI;

public sealed class TbGameKeyAction
{
	private readonly Dictionary<string, GameKeyActionInfo> _dataMap;

	private readonly List<GameKeyActionInfo> _dataList;

	public Dictionary<string, GameKeyActionInfo> DataMap => _dataMap;

	public List<GameKeyActionInfo> DataList => _dataList;

	public GameKeyActionInfo this[string key] => _dataMap[key];

	public TbGameKeyAction(JSONNode _json)
	{
		_dataMap = new Dictionary<string, GameKeyActionInfo>();
		_dataList = new List<GameKeyActionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			GameKeyActionInfo gameKeyActionInfo = GameKeyActionInfo.DeserializeGameKeyActionInfo(child);
			if (_dataMap.TryAdd(gameKeyActionInfo.ActionName, gameKeyActionInfo))
			{
				_dataList.Add(gameKeyActionInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + gameKeyActionInfo.ActionName + " in table: TbGameKeyAction");
			}
		}
	}

	public GameKeyActionInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public GameKeyActionInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (GameKeyActionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (GameKeyActionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
