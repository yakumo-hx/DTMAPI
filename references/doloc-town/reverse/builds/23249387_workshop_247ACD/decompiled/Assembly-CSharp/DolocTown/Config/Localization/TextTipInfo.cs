using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class TextTipInfo : BeanBase
{
	public const int __ID__ = -632318425;

	public string Id { get; private set; }

	public TipTextArgsBase TipArgs { get; private set; }

	public TextTipInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["tip_args"].IsObject)
		{
			throw new SerializationException();
		}
		TipArgs = TipTextArgsBase.DeserializeTipTextArgsBase(_json["tip_args"]);
	}

	public TextTipInfo(string id, TipTextArgsBase tip_args)
	{
		Id = id;
		TipArgs = tip_args;
	}

	public static TextTipInfo DeserializeTextTipInfo(JSONNode _json)
	{
		return new TextTipInfo(_json);
	}

	public override int GetTypeId()
	{
		return -632318425;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		TipArgs?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		TipArgs?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",TipArgs:" + TipArgs?.ToString() + ",}";
	}
}
