using System;
using System.Collections.Generic;
using RedSaw;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbTrashTalk
{
	private readonly Dictionary<string, TrashTalkInfo> _dataMap;

	private readonly List<TrashTalkInfo> _dataList;

	private Dictionary<string, List<string>> groupMap = new Dictionary<string, List<string>>();

	public Dictionary<string, TrashTalkInfo> DataMap => _dataMap;

	public List<TrashTalkInfo> DataList => _dataList;

	public TrashTalkInfo this[string key] => _dataMap[key];

	public TbTrashTalk(JSONNode _json)
	{
		_dataMap = new Dictionary<string, TrashTalkInfo>();
		_dataList = new List<TrashTalkInfo>();
		foreach (JSONNode child in _json.Children)
		{
			TrashTalkInfo trashTalkInfo = TrashTalkInfo.DeserializeTrashTalkInfo(child);
			if (_dataMap.TryAdd(trashTalkInfo.Id, trashTalkInfo))
			{
				_dataList.Add(trashTalkInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + trashTalkInfo.Id + " in table: TbTrashTalk");
			}
		}
	}

	public TrashTalkInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TrashTalkInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TrashTalkInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TrashTalkInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	private void PostResolve()
	{
		foreach (TrashTalkInfo data in _dataList)
		{
			string[] groups = data.Groups;
			foreach (string key in groups)
			{
				groupMap.TryAdd(key, new List<string>());
				groupMap[key].Add(data.Id);
			}
		}
	}

	public string GetRandomTrashTalk(string group = "")
	{
		if (group == null)
		{
			group = string.Empty;
		}
		if (groupMap.TryGetValue(group, out var value))
		{
			int index = RandomUtils.DiceCount(0, value.Count - 1);
			string key = value[index];
			return Get(key).TrashTalk;
		}
		int index2 = RandomUtils.DiceCount(0, _dataList.Count - 1);
		return _dataList[index2].TrashTalk;
	}
}
