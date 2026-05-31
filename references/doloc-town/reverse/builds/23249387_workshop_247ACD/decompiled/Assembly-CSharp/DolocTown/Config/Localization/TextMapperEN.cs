using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class TextMapperEN : BeanBase
{
	public const int __ID__ = -211107796;

	public string Key { get; private set; }

	public string Text { get; private set; }

	public TextMapperEN(JSONNode _json)
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

	public TextMapperEN(string key, string text)
	{
		Key = key;
		Text = text;
	}

	public static TextMapperEN DeserializeTextMapperEN(JSONNode _json)
	{
		return new TextMapperEN(_json);
	}

	public override int GetTypeId()
	{
		return -211107796;
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
