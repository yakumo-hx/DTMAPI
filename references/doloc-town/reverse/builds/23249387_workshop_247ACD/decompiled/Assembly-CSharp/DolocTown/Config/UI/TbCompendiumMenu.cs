using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.UI;

public sealed class TbCompendiumMenu
{
	private readonly List<CompendiumMenuInfo> _dataList;

	private Dictionary<CompendiumLabel, CompendiumMenuInfo> _dataMap_id;

	private Dictionary<string, CompendiumMenuInfo> _dataMap_name;

	public List<CompendiumMenuInfo> DataList => _dataList;

	public TbCompendiumMenu(JSONNode _json)
	{
		_dataList = new List<CompendiumMenuInfo>();
		foreach (JSONNode child in _json.Children)
		{
			CompendiumMenuInfo item = CompendiumMenuInfo.DeserializeCompendiumMenuInfo(child);
			_dataList.Add(item);
		}
		_dataMap_id = new Dictionary<CompendiumLabel, CompendiumMenuInfo>();
		_dataMap_name = new Dictionary<string, CompendiumMenuInfo>();
		foreach (CompendiumMenuInfo data in _dataList)
		{
			if (!_dataMap_id.TryAdd(data.Id, data))
			{
				Debug.LogError($"[Config] Duplicate key: {data.Id} in table: TbCompendiumMenu");
			}
			if (!_dataMap_name.TryAdd(data.Name, data))
			{
				Debug.LogError("[Config] Duplicate key: " + data.Name + " in table: TbCompendiumMenu");
			}
		}
	}

	public CompendiumMenuInfo GetById(CompendiumLabel key)
	{
		if (!_dataMap_id.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public CompendiumMenuInfo GetByName(string key)
	{
		if (!_dataMap_name.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (CompendiumMenuInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (CompendiumMenuInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
