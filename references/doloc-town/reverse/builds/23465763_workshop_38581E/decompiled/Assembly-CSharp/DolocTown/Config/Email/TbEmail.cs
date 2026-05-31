using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Email;

public sealed class TbEmail
{
	private readonly Dictionary<string, EmailInfo> _dataMap;

	private readonly List<EmailInfo> _dataList;

	public Dictionary<string, EmailInfo> DataMap => _dataMap;

	public List<EmailInfo> DataList => _dataList;

	public EmailInfo this[string key] => _dataMap[key];

	public TbEmail(JSONNode _json)
	{
		_dataMap = new Dictionary<string, EmailInfo>();
		_dataList = new List<EmailInfo>();
		foreach (JSONNode child in _json.Children)
		{
			EmailInfo emailInfo = EmailInfo.DeserializeEmailInfo(child);
			if (_dataMap.TryAdd(emailInfo.Id, emailInfo))
			{
				_dataList.Add(emailInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + emailInfo.Id + " in table: TbEmail");
			}
		}
	}

	public EmailInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public EmailInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (EmailInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (EmailInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
