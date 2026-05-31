using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbLockableObject
{
	private readonly Dictionary<string, LockableObjectInfo> _dataMap;

	private readonly List<LockableObjectInfo> _dataList;

	public Dictionary<string, LockableObjectInfo> DataMap => _dataMap;

	public List<LockableObjectInfo> DataList => _dataList;

	public LockableObjectInfo this[string key] => _dataMap[key];

	public TbLockableObject(JSONNode _json)
	{
		_dataMap = new Dictionary<string, LockableObjectInfo>();
		_dataList = new List<LockableObjectInfo>();
		foreach (JSONNode child in _json.Children)
		{
			LockableObjectInfo lockableObjectInfo = LockableObjectInfo.DeserializeLockableObjectInfo(child);
			if (_dataMap.TryAdd(lockableObjectInfo.Id, lockableObjectInfo))
			{
				_dataList.Add(lockableObjectInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + lockableObjectInfo.Id + " in table: TbLockableObject");
			}
		}
	}

	public LockableObjectInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public LockableObjectInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (LockableObjectInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (LockableObjectInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
