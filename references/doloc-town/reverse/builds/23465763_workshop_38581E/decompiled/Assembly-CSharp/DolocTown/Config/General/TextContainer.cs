using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.General;

public sealed class TextContainer : BeanBase
{
	public const int __ID__ = -673073394;

	public string Id { get; private set; }

	public string Text { get; private set; }

	public string Text_l10n_key { get; }

	public TextContainer(JSONNode _json)
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

	public TextContainer(string id, string text)
	{
		Id = id;
		Text = text;
	}

	public static TextContainer DeserializeTextContainer(JSONNode _json)
	{
		return new TextContainer(_json);
	}

	public override int GetTypeId()
	{
		return -673073394;
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
