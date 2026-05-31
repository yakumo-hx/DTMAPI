using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Dialogue;

public sealed class DialogueEntityInfo : BeanBase
{
	public const int __ID__ = -1152977153;

	public string Id { get; private set; }

	public DialogueEntityType EntityType { get; private set; }

	public Color SpeakerColor { get; private set; }

	public Color ContentColor { get; private set; }

	public DialogueEntityInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["entity_type"].IsNumber)
		{
			throw new SerializationException();
		}
		EntityType = (DialogueEntityType)_json["entity_type"].AsInt;
		if (!_json["speaker_color"].IsObject)
		{
			throw new SerializationException();
		}
		SpeakerColor = ExternalTypeUtil.ColorConverter(CfgHexColor.DeserializeCfgHexColor(_json["speaker_color"]));
		if (!_json["content_color"].IsObject)
		{
			throw new SerializationException();
		}
		ContentColor = ExternalTypeUtil.ColorConverter(CfgHexColor.DeserializeCfgHexColor(_json["content_color"]));
	}

	public DialogueEntityInfo(string id, DialogueEntityType entity_type, Color speaker_color, Color content_color)
	{
		Id = id;
		EntityType = entity_type;
		SpeakerColor = speaker_color;
		ContentColor = content_color;
	}

	public static DialogueEntityInfo DeserializeDialogueEntityInfo(JSONNode _json)
	{
		return new DialogueEntityInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1152977153;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",EntityType:" + EntityType.ToString() + ",SpeakerColor:" + SpeakerColor.ToString() + ",ContentColor:" + ContentColor.ToString() + ",}";
	}
}
