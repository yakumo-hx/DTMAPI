using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.UI;

public sealed class TbEmailMenu
{
	private readonly Dictionary<EmailMenuType, EmailMenuInfo> _dataMap;

	private readonly List<EmailMenuInfo> _dataList;

	public Dictionary<EmailMenuType, EmailMenuInfo> DataMap => _dataMap;

	public List<EmailMenuInfo> DataList => _dataList;

	public EmailMenuInfo this[EmailMenuType key] => _dataMap[key];

	public TbEmailMenu(JSONNode _json)
	{
		_dataMap = new Dictionary<EmailMenuType, EmailMenuInfo>();
		_dataList = new List<EmailMenuInfo>();
		foreach (JSONNode child in _json.Children)
		{
			EmailMenuInfo emailMenuInfo = EmailMenuInfo.DeserializeEmailMenuInfo(child);
			if (_dataMap.TryAdd(emailMenuInfo.Id, emailMenuInfo))
			{
				_dataList.Add(emailMenuInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {emailMenuInfo.Id} in table: TbEmailMenu");
			}
		}
	}

	public EmailMenuInfo GetOrDefault(EmailMenuType key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public EmailMenuInfo Get(EmailMenuType key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (EmailMenuInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (EmailMenuInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
