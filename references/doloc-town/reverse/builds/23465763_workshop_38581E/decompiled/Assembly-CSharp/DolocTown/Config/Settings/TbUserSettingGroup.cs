using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Settings;

public sealed class TbUserSettingGroup
{
	private readonly Dictionary<string, UserSettingGroupInfo> _dataMap;

	private readonly List<UserSettingGroupInfo> _dataList;

	public Dictionary<string, UserSettingGroupInfo> DataMap => _dataMap;

	public List<UserSettingGroupInfo> DataList => _dataList;

	public UserSettingGroupInfo this[string key] => _dataMap[key];

	public TbUserSettingGroup(JSONNode _json)
	{
		_dataMap = new Dictionary<string, UserSettingGroupInfo>();
		_dataList = new List<UserSettingGroupInfo>();
		foreach (JSONNode child in _json.Children)
		{
			UserSettingGroupInfo userSettingGroupInfo = UserSettingGroupInfo.DeserializeUserSettingGroupInfo(child);
			if (_dataMap.TryAdd(userSettingGroupInfo.Id, userSettingGroupInfo))
			{
				_dataList.Add(userSettingGroupInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + userSettingGroupInfo.Id + " in table: TbUserSettingGroup");
			}
		}
	}

	public UserSettingGroupInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public UserSettingGroupInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (UserSettingGroupInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (UserSettingGroupInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
