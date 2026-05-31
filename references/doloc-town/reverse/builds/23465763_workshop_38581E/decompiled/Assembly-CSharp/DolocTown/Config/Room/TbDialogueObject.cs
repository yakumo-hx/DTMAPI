using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbDialogueObject
{
	private readonly Dictionary<string, DialogueObjectInfo> _dataMap;

	private readonly List<DialogueObjectInfo> _dataList;

	public Dictionary<string, DialogueObjectInfo> DataMap => _dataMap;

	public List<DialogueObjectInfo> DataList => _dataList;

	public DialogueObjectInfo this[string key] => _dataMap[key];

	public TbDialogueObject(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DialogueObjectInfo>();
		_dataList = new List<DialogueObjectInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DialogueObjectInfo dialogueObjectInfo = DialogueObjectInfo.DeserializeDialogueObjectInfo(child);
			if (_dataMap.TryAdd(dialogueObjectInfo.Id, dialogueObjectInfo))
			{
				_dataList.Add(dialogueObjectInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + dialogueObjectInfo.Id + " in table: TbDialogueObject");
			}
		}
	}

	public DialogueObjectInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DialogueObjectInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DialogueObjectInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DialogueObjectInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
