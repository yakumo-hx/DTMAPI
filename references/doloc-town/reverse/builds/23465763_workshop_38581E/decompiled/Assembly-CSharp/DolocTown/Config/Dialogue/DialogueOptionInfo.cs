using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Dialogue;

public sealed class DialogueOptionInfo : BeanBase
{
	public const int __ID__ = 1703608977;

	public string Id { get; private set; }

	public float Order { get; private set; }

	public string IconText { get; private set; }

	public DialogueOptionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["order"].IsNumber)
		{
			throw new SerializationException();
		}
		Order = _json["order"];
		if (!_json["icon_text"].IsString)
		{
			throw new SerializationException();
		}
		IconText = _json["icon_text"];
	}

	public DialogueOptionInfo(string id, float order, string icon_text)
	{
		Id = id;
		Order = order;
		IconText = icon_text;
	}

	public static DialogueOptionInfo DeserializeDialogueOptionInfo(JSONNode _json)
	{
		return new DialogueOptionInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1703608977;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Order:" + Order + ",IconText:" + IconText + ",}";
	}
}
