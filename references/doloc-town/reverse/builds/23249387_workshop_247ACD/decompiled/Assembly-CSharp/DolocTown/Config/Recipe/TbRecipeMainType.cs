using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Recipe;

public sealed class TbRecipeMainType
{
	private readonly Dictionary<string, RecipeMainTypeInfo> _dataMap;

	private readonly List<RecipeMainTypeInfo> _dataList;

	public Dictionary<string, RecipeMainTypeInfo> DataMap => _dataMap;

	public List<RecipeMainTypeInfo> DataList => _dataList;

	public RecipeMainTypeInfo this[string key] => _dataMap[key];

	public TbRecipeMainType(JSONNode _json)
	{
		_dataMap = new Dictionary<string, RecipeMainTypeInfo>();
		_dataList = new List<RecipeMainTypeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			RecipeMainTypeInfo recipeMainTypeInfo = RecipeMainTypeInfo.DeserializeRecipeMainTypeInfo(child);
			if (_dataMap.TryAdd(recipeMainTypeInfo.Id, recipeMainTypeInfo))
			{
				_dataList.Add(recipeMainTypeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + recipeMainTypeInfo.Id + " in table: TbRecipeMainType");
			}
		}
	}

	public RecipeMainTypeInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public RecipeMainTypeInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (RecipeMainTypeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (RecipeMainTypeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
