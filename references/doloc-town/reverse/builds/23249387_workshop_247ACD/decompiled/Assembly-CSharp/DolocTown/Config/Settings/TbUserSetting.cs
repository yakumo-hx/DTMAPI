using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Settings;

public sealed class TbUserSetting
{
	private readonly Dictionary<UserSettingType, UserSettingInfo> _dataMap;

	private readonly List<UserSettingInfo> _dataList;

	public Dictionary<UserSettingType, UserSettingInfo> DataMap => _dataMap;

	public List<UserSettingInfo> DataList => _dataList;

	public UserSettingInfo this[UserSettingType key] => _dataMap[key];

	public TbUserSetting(JSONNode _json)
	{
		_dataMap = new Dictionary<UserSettingType, UserSettingInfo>();
		_dataList = new List<UserSettingInfo>();
		foreach (JSONNode child in _json.Children)
		{
			UserSettingInfo userSettingInfo = UserSettingInfo.DeserializeUserSettingInfo(child);
			if (_dataMap.TryAdd(userSettingInfo.Id, userSettingInfo))
			{
				_dataList.Add(userSettingInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {userSettingInfo.Id} in table: TbUserSetting");
			}
		}
	}

	public UserSettingInfo GetOrDefault(UserSettingType key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public UserSettingInfo Get(UserSettingType key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (UserSettingInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (UserSettingInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
