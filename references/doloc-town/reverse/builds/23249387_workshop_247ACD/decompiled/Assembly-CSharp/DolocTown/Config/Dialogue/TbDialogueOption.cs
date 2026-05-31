using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Dialogue;

public sealed class TbDialogueOption
{
	private readonly Dictionary<string, DialogueOptionInfo> _dataMap;

	private readonly List<DialogueOptionInfo> _dataList;

	public Dictionary<string, DialogueOptionInfo> DataMap => _dataMap;

	public List<DialogueOptionInfo> DataList => _dataList;

	public DialogueOptionInfo this[string key] => _dataMap[key];

	public TbDialogueOption(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DialogueOptionInfo>();
		_dataList = new List<DialogueOptionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DialogueOptionInfo dialogueOptionInfo = DialogueOptionInfo.DeserializeDialogueOptionInfo(child);
			if (_dataMap.TryAdd(dialogueOptionInfo.Id, dialogueOptionInfo))
			{
				_dataList.Add(dialogueOptionInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + dialogueOptionInfo.Id + " in table: TbDialogueOption");
			}
		}
	}

	public DialogueOptionInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DialogueOptionInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DialogueOptionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DialogueOptionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
