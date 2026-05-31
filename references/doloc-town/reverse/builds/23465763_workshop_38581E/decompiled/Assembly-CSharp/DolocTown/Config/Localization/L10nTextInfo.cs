using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class L10nTextInfo : BeanBase
{
	public const int __ID__ = 28991507;

	public string Id { get; private set; }

	public string Text { get; private set; }

	public string Text_l10n_key { get; }

	public L10nTextInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["text"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Text_l10n_key = _json["text"]["key"];
		if (!_json["text"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Text = _json["text"]["text"];
	}

	public L10nTextInfo(string id, string text)
	{
		Id = id;
		Text = text;
	}

	public static L10nTextInfo DeserializeL10nTextInfo(JSONNode _json)
	{
		return new L10nTextInfo(_json);
	}

	public override int GetTypeId()
	{
		return 28991507;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Text = translator(Text_l10n_key, Text);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Text:" + Text + ",}";
	}
}
