using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.UI;

public sealed class TbUIEntityGroup
{
	private readonly Dictionary<string, UIEntityGroupInfo> _dataMap;

	private readonly List<UIEntityGroupInfo> _dataList;

	public Dictionary<string, UIEntityGroupInfo> DataMap => _dataMap;

	public List<UIEntityGroupInfo> DataList => _dataList;

	public UIEntityGroupInfo this[string key] => _dataMap[key];

	public TbUIEntityGroup(JSONNode _json)
	{
		_dataMap = new Dictionary<string, UIEntityGroupInfo>();
		_dataList = new List<UIEntityGroupInfo>();
		foreach (JSONNode child in _json.Children)
		{
			UIEntityGroupInfo uIEntityGroupInfo = UIEntityGroupInfo.DeserializeUIEntityGroupInfo(child);
			if (_dataMap.TryAdd(uIEntityGroupInfo.Id, uIEntityGroupInfo))
			{
				_dataList.Add(uIEntityGroupInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + uIEntityGroupInfo.Id + " in table: TbUIEntityGroup");
			}
		}
	}

	public UIEntityGroupInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public UIEntityGroupInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (UIEntityGroupInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (UIEntityGroupInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
