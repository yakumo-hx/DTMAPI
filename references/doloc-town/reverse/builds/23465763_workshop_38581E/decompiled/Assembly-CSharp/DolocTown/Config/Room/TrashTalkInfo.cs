using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Room;

public sealed class TrashTalkInfo : BeanBase
{
	public const int __ID__ = -1981480193;

	public string Id { get; private set; }

	public string[] Groups { get; private set; }

	public string TrashTalk { get; private set; }

	public string TrashTalk_l10n_key { get; }

	public TrashTalkInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["groups"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Groups = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			Groups[num++] = text;
		}
		if (!_json["trash_talk"]["key"].IsString)
		{
			throw new SerializationException();
		}
		TrashTalk_l10n_key = _json["trash_talk"]["key"];
		if (!_json["trash_talk"]["text"].IsString)
		{
			throw new SerializationException();
		}
		TrashTalk = _json["trash_talk"]["text"];
	}

	public TrashTalkInfo(string id, string[] groups, string trash_talk)
	{
		Id = id;
		Groups = groups;
		TrashTalk = trash_talk;
	}

	public static TrashTalkInfo DeserializeTrashTalkInfo(JSONNode _json)
	{
		return new TrashTalkInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1981480193;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		TrashTalk = translator(TrashTalk_l10n_key, TrashTalk);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Groups:" + StringUtil.CollectionToString(Groups) + ",TrashTalk:" + TrashTalk + ",}";
	}
}
