using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Recipe;

public sealed class TbDismantleRecipeGroup
{
	private readonly Dictionary<string, DismantleRecipeGroupInfo> _dataMap;

	private readonly List<DismantleRecipeGroupInfo> _dataList;

	public Dictionary<string, DismantleRecipeGroupInfo> DataMap => _dataMap;

	public List<DismantleRecipeGroupInfo> DataList => _dataList;

	public DismantleRecipeGroupInfo this[string key] => _dataMap[key];

	public TbDismantleRecipeGroup(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DismantleRecipeGroupInfo>();
		_dataList = new List<DismantleRecipeGroupInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DismantleRecipeGroupInfo dismantleRecipeGroupInfo = DismantleRecipeGroupInfo.DeserializeDismantleRecipeGroupInfo(child);
			if (_dataMap.TryAdd(dismantleRecipeGroupInfo.Id, dismantleRecipeGroupInfo))
			{
				_dataList.Add(dismantleRecipeGroupInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + dismantleRecipeGroupInfo.Id + " in table: TbDismantleRecipeGroup");
			}
		}
	}

	public DismantleRecipeGroupInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DismantleRecipeGroupInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DismantleRecipeGroupInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DismantleRecipeGroupInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
