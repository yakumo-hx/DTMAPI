using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class TextMapperPT_BR : BeanBase
{
	public const int __ID__ = -1269798392;

	public string Key { get; private set; }

	public string Text { get; private set; }

	public TextMapperPT_BR(JSONNode _json)
	{
		if (!_json["key"].IsString)
		{
			throw new SerializationException();
		}
		Key = _json["key"];
		if (!_json["text"].IsString)
		{
			throw new SerializationException();
		}
		Text = _json["text"];
	}

	public TextMapperPT_BR(string key, string text)
	{
		Key = key;
		Text = text;
	}

	public static TextMapperPT_BR DeserializeTextMapperPT_BR(JSONNode _json)
	{
		return new TextMapperPT_BR(_json);
	}

	public override int GetTypeId()
	{
		return -1269798392;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Key:" + Key + ",Text:" + Text + ",}";
	}
}
