using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.TechTree;

public sealed class TbTechTree
{
	private readonly List<TechTreeInfo> _dataList;

	private Dictionary<string, TechTreeInfo> _dataMap_id;

	private Dictionary<TechPointType, TechTreeInfo> _dataMap_tech_point;

	public List<TechTreeInfo> DataList => _dataList;

	public TbTechTree(JSONNode _json)
	{
		_dataList = new List<TechTreeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			TechTreeInfo item = TechTreeInfo.DeserializeTechTreeInfo(child);
			_dataList.Add(item);
		}
		_dataMap_id = new Dictionary<string, TechTreeInfo>();
		_dataMap_tech_point = new Dictionary<TechPointType, TechTreeInfo>();
		foreach (TechTreeInfo data in _dataList)
		{
			if (!_dataMap_id.TryAdd(data.Id, data))
			{
				Debug.LogError("[Config] Duplicate key: " + data.Id + " in table: TbTechTree");
			}
			if (!_dataMap_tech_point.TryAdd(data.TechPoint, data))
			{
				Debug.LogError($"[Config] Duplicate key: {data.TechPoint} in table: TbTechTree");
			}
		}
	}

	public TechTreeInfo GetById(string key)
	{
		if (!_dataMap_id.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TechTreeInfo GetByTechPoint(TechPointType key)
	{
		if (!_dataMap_tech_point.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TechTreeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TechTreeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
