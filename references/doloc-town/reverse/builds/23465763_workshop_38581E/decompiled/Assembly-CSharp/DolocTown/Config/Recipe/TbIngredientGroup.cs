using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Recipe;

public sealed class TbIngredientGroup
{
	private readonly Dictionary<string, IngredientGroupInfo> _dataMap;

	private readonly List<IngredientGroupInfo> _dataList;

	public Dictionary<string, IngredientGroupInfo> DataMap => _dataMap;

	public List<IngredientGroupInfo> DataList => _dataList;

	public IngredientGroupInfo this[string key] => _dataMap[key];

	public TbIngredientGroup(JSONNode _json)
	{
		_dataMap = new Dictionary<string, IngredientGroupInfo>();
		_dataList = new List<IngredientGroupInfo>();
		foreach (JSONNode child in _json.Children)
		{
			IngredientGroupInfo ingredientGroupInfo = IngredientGroupInfo.DeserializeIngredientGroupInfo(child);
			if (_dataMap.TryAdd(ingredientGroupInfo.Id, ingredientGroupInfo))
			{
				_dataList.Add(ingredientGroupInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + ingredientGroupInfo.Id + " in table: TbIngredientGroup");
			}
		}
	}

	public IngredientGroupInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public IngredientGroupInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (IngredientGroupInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (IngredientGroupInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
