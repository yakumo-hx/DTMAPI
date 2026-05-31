using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Recipe;

public sealed class TbRecipe
{
	private readonly Dictionary<string, RecipeInfo> _dataMap;

	private readonly List<RecipeInfo> _dataList;

	public Dictionary<string, RecipeInfo> DataMap => _dataMap;

	public List<RecipeInfo> DataList => _dataList;

	public RecipeInfo this[string key] => _dataMap[key];

	public TbRecipe(JSONNode _json)
	{
		_dataMap = new Dictionary<string, RecipeInfo>();
		_dataList = new List<RecipeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			RecipeInfo recipeInfo = RecipeInfo.DeserializeRecipeInfo(child);
			if (_dataMap.TryAdd(recipeInfo.Id, recipeInfo))
			{
				_dataList.Add(recipeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + recipeInfo.Id + " in table: TbRecipe");
			}
		}
	}

	public RecipeInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public RecipeInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (RecipeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (RecipeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
