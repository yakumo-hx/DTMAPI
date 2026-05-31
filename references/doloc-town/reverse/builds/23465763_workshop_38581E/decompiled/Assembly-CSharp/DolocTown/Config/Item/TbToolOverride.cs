using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class TbToolOverride
{
	private readonly Dictionary<string, ToolOverrideInfo> _dataMap;

	private readonly List<ToolOverrideInfo> _dataList;

	public Dictionary<string, ToolOverrideInfo> DataMap => _dataMap;

	public List<ToolOverrideInfo> DataList => _dataList;

	public ToolOverrideInfo this[string key] => _dataMap[key];

	public TbToolOverride(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ToolOverrideInfo>();
		_dataList = new List<ToolOverrideInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ToolOverrideInfo toolOverrideInfo = ToolOverrideInfo.DeserializeToolOverrideInfo(child);
			if (_dataMap.TryAdd(toolOverrideInfo.Id, toolOverrideInfo))
			{
				_dataList.Add(toolOverrideInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + toolOverrideInfo.Id + " in table: TbToolOverride");
			}
		}
	}

	public ToolOverrideInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ToolOverrideInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ToolOverrideInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ToolOverrideInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
