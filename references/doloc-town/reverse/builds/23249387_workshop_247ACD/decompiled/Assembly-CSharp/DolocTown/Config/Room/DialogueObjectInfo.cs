using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Room;

public sealed class DialogueObjectInfo : BeanBase
{
	public const int __ID__ = -1364585352;

	public string Id { get; private set; }

	public ObjectRefreshFrequency RefreshType { get; private set; }

	public bool LoopSequence { get; private set; }

	public string[] DialogueSequence { get; private set; }

	public DialogueObjectInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["refresh_type"].IsNumber)
		{
			throw new SerializationException();
		}
		RefreshType = (ObjectRefreshFrequency)_json["refresh_type"].AsInt;
		if (!_json["loop_sequence"].IsBoolean)
		{
			throw new SerializationException();
		}
		LoopSequence = _json["loop_sequence"];
		JSONNode jSONNode = _json["dialogue_sequence"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		DialogueSequence = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			DialogueSequence[num++] = text;
		}
	}

	public DialogueObjectInfo(string id, ObjectRefreshFrequency refresh_type, bool loop_sequence, string[] dialogue_sequence)
	{
		Id = id;
		RefreshType = refresh_type;
		LoopSequence = loop_sequence;
		DialogueSequence = dialogue_sequence;
	}

	public static DialogueObjectInfo DeserializeDialogueObjectInfo(JSONNode _json)
	{
		return new DialogueObjectInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1364585352;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",RefreshType:" + RefreshType.ToString() + ",LoopSequence:" + LoopSequence + ",DialogueSequence:" + StringUtil.CollectionToString(DialogueSequence) + ",}";
	}
}
