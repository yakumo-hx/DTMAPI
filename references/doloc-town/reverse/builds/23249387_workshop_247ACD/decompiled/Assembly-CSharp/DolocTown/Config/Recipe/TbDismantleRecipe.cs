using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Recipe;

public sealed class TbDismantleRecipe
{
	private readonly Dictionary<string, DismantleRecipeInfo> _dataMap;

	private readonly List<DismantleRecipeInfo> _dataList;

	public Dictionary<string, DismantleRecipeInfo> DataMap => _dataMap;

	public List<DismantleRecipeInfo> DataList => _dataList;

	public DismantleRecipeInfo this[string key] => _dataMap[key];

	public TbDismantleRecipe(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DismantleRecipeInfo>();
		_dataList = new List<DismantleRecipeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DismantleRecipeInfo dismantleRecipeInfo = DismantleRecipeInfo.DeserializeDismantleRecipeInfo(child);
			if (_dataMap.TryAdd(dismantleRecipeInfo.Id, dismantleRecipeInfo))
			{
				_dataList.Add(dismantleRecipeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + dismantleRecipeInfo.Id + " in table: TbDismantleRecipe");
			}
		}
	}

	public DismantleRecipeInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DismantleRecipeInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DismantleRecipeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DismantleRecipeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
