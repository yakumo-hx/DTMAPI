using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Archives;

public sealed class DocumentMissionTip : BeanBase
{
	public const int __ID__ = 1136164871;

	public string Id { get; private set; }

	public int LikingLevel { get; private set; }

	public string Tip { get; private set; }

	public string Tip_l10n_key { get; }

	public DocumentMissionTip(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["liking_level"].IsNumber)
		{
			throw new SerializationException();
		}
		LikingLevel = _json["liking_level"];
		if (!_json["tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Tip_l10n_key = _json["tip"]["key"];
		if (!_json["tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Tip = _json["tip"]["text"];
	}

	public DocumentMissionTip(string id, int liking_level, string tip)
	{
		Id = id;
		LikingLevel = liking_level;
		Tip = tip;
	}

	public static DocumentMissionTip DeserializeDocumentMissionTip(JSONNode _json)
	{
		return new DocumentMissionTip(_json);
	}

	public override int GetTypeId()
	{
		return 1136164871;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Tip = translator(Tip_l10n_key, Tip);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",LikingLevel:" + LikingLevel + ",Tip:" + Tip + ",}";
	}
}
