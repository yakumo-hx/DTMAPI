using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class TextMapperFR : BeanBase
{
	public const int __ID__ = -211107761;

	public string Key { get; private set; }

	public string Text { get; private set; }

	public TextMapperFR(JSONNode _json)
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

	public TextMapperFR(string key, string text)
	{
		Key = key;
		Text = text;
	}

	public static TextMapperFR DeserializeTextMapperFR(JSONNode _json)
	{
		return new TextMapperFR(_json);
	}

	public override int GetTypeId()
	{
		return -211107761;
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
