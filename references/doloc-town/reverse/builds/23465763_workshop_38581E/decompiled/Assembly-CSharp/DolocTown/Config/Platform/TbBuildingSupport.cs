using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Platform;

public sealed class TbBuildingSupport
{
	private readonly Dictionary<string, BuildingSupportInfo> _dataMap;

	private readonly List<BuildingSupportInfo> _dataList;

	public Dictionary<string, BuildingSupportInfo> DataMap => _dataMap;

	public List<BuildingSupportInfo> DataList => _dataList;

	public BuildingSupportInfo this[string key] => _dataMap[key];

	public TbBuildingSupport(JSONNode _json)
	{
		_dataMap = new Dictionary<string, BuildingSupportInfo>();
		_dataList = new List<BuildingSupportInfo>();
		foreach (JSONNode child in _json.Children)
		{
			BuildingSupportInfo buildingSupportInfo = BuildingSupportInfo.DeserializeBuildingSupportInfo(child);
			if (_dataMap.TryAdd(buildingSupportInfo.Id, buildingSupportInfo))
			{
				_dataList.Add(buildingSupportInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + buildingSupportInfo.Id + " in table: TbBuildingSupport");
			}
		}
	}

	public BuildingSupportInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public BuildingSupportInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (BuildingSupportInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (BuildingSupportInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
