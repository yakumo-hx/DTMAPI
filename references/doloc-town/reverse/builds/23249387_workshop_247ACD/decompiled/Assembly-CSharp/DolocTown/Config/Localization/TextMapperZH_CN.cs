using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class TextMapperZH_CN : BeanBase
{
	public const int __ID__ = -1260920647;

	public string Key { get; private set; }

	public string Text { get; private set; }

	public TextMapperZH_CN(JSONNode _json)
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

	public TextMapperZH_CN(string key, string text)
	{
		Key = key;
		Text = text;
	}

	public static TextMapperZH_CN DeserializeTextMapperZH_CN(JSONNode _json)
	{
		return new TextMapperZH_CN(_json);
	}

	public override int GetTypeId()
	{
		return -1260920647;
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
