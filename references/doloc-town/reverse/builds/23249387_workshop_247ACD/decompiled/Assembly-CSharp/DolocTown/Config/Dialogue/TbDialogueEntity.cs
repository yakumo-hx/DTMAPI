using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Dialogue;

public sealed class TbDialogueEntity
{
	private readonly Dictionary<string, DialogueEntityInfo> _dataMap;

	private readonly List<DialogueEntityInfo> _dataList;

	public Dictionary<string, DialogueEntityInfo> DataMap => _dataMap;

	public List<DialogueEntityInfo> DataList => _dataList;

	public DialogueEntityInfo this[string key] => _dataMap[key];

	public TbDialogueEntity(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DialogueEntityInfo>();
		_dataList = new List<DialogueEntityInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DialogueEntityInfo dialogueEntityInfo = DialogueEntityInfo.DeserializeDialogueEntityInfo(child);
			if (_dataMap.TryAdd(dialogueEntityInfo.Id, dialogueEntityInfo))
			{
				_dataList.Add(dialogueEntityInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + dialogueEntityInfo.Id + " in table: TbDialogueEntity");
			}
		}
	}

	public DialogueEntityInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DialogueEntityInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DialogueEntityInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DialogueEntityInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
