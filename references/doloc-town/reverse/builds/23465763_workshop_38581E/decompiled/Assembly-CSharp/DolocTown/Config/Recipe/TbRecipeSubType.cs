using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Recipe;

public sealed class TbRecipeSubType
{
	private readonly Dictionary<string, RecipeSubTypeInfo> _dataMap;

	private readonly List<RecipeSubTypeInfo> _dataList;

	public Dictionary<string, RecipeSubTypeInfo> DataMap => _dataMap;

	public List<RecipeSubTypeInfo> DataList => _dataList;

	public RecipeSubTypeInfo this[string key] => _dataMap[key];

	public TbRecipeSubType(JSONNode _json)
	{
		_dataMap = new Dictionary<string, RecipeSubTypeInfo>();
		_dataList = new List<RecipeSubTypeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			RecipeSubTypeInfo recipeSubTypeInfo = RecipeSubTypeInfo.DeserializeRecipeSubTypeInfo(child);
			if (_dataMap.TryAdd(recipeSubTypeInfo.Id, recipeSubTypeInfo))
			{
				_dataList.Add(recipeSubTypeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + recipeSubTypeInfo.Id + " in table: TbRecipeSubType");
			}
		}
	}

	public RecipeSubTypeInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public RecipeSubTypeInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (RecipeSubTypeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (RecipeSubTypeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
