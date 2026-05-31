using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbPortal
{
	private readonly Dictionary<string, PortalInfo> _dataMap;

	private readonly List<PortalInfo> _dataList;

	public Dictionary<string, PortalInfo> DataMap => _dataMap;

	public List<PortalInfo> DataList => _dataList;

	public PortalInfo this[string key] => _dataMap[key];

	public TbPortal(JSONNode _json)
	{
		_dataMap = new Dictionary<string, PortalInfo>();
		_dataList = new List<PortalInfo>();
		foreach (JSONNode child in _json.Children)
		{
			PortalInfo portalInfo = PortalInfo.DeserializePortalInfo(child);
			if (_dataMap.TryAdd(portalInfo.Id, portalInfo))
			{
				_dataList.Add(portalInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + portalInfo.Id + " in table: TbPortal");
			}
		}
	}

	public PortalInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public PortalInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (PortalInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (PortalInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
