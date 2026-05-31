using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Recipe;

public sealed class TbRecipeGroup
{
	private readonly Dictionary<string, RecipeGroupInfo> _dataMap;

	private readonly List<RecipeGroupInfo> _dataList;

	private readonly Dictionary<string, HashSet<string>> synthesizerMap = new Dictionary<string, HashSet<string>>();

	public Dictionary<string, RecipeGroupInfo> DataMap => _dataMap;

	public List<RecipeGroupInfo> DataList => _dataList;

	public RecipeGroupInfo this[string key] => _dataMap[key];

	public TbRecipeGroup(JSONNode _json)
	{
		_dataMap = new Dictionary<string, RecipeGroupInfo>();
		_dataList = new List<RecipeGroupInfo>();
		foreach (JSONNode child in _json.Children)
		{
			RecipeGroupInfo recipeGroupInfo = RecipeGroupInfo.DeserializeRecipeGroupInfo(child);
			if (_dataMap.TryAdd(recipeGroupInfo.Id, recipeGroupInfo))
			{
				_dataList.Add(recipeGroupInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + recipeGroupInfo.Id + " in table: TbRecipeGroup");
			}
		}
	}

	public RecipeGroupInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public RecipeGroupInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (RecipeGroupInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (RecipeGroupInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	private void PostResolve()
	{
		synthesizerMap.Clear();
		foreach (RecipeGroupInfo data in DataList)
		{
			foreach (string recipeId in data.RecipeIds)
			{
				if (!synthesizerMap.TryGetValue(recipeId, out var value))
				{
					value = new HashSet<string>();
					synthesizerMap[recipeId] = value;
				}
				value.Add(data.Id);
			}
		}
	}

	public bool TryGetRecipeMachineName(string recipeId, out HashSet<string> machineNames)
	{
		return synthesizerMap.TryGetValue(recipeId, out machineNames);
	}
}
