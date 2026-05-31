using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Platform;

public sealed class TbPlatform
{
	private readonly Dictionary<string, PlatformInfo> _dataMap;

	private readonly List<PlatformInfo> _dataList;

	public Dictionary<string, PlatformInfo> DataMap => _dataMap;

	public List<PlatformInfo> DataList => _dataList;

	public PlatformInfo this[string key] => _dataMap[key];

	public TbPlatform(JSONNode _json)
	{
		_dataMap = new Dictionary<string, PlatformInfo>();
		_dataList = new List<PlatformInfo>();
		foreach (JSONNode child in _json.Children)
		{
			PlatformInfo platformInfo = PlatformInfo.DeserializePlatformInfo(child);
			if (_dataMap.TryAdd(platformInfo.Id, platformInfo))
			{
				_dataList.Add(platformInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + platformInfo.Id + " in table: TbPlatform");
			}
		}
	}

	public PlatformInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public PlatformInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (PlatformInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (PlatformInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
