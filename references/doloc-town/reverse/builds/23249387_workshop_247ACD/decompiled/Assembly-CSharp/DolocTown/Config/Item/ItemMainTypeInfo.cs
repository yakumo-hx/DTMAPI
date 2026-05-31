using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemMainTypeInfo : BeanBase
{
	public const int __ID__ = 339790863;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public ItemMainTypeInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
	}

	public ItemMainTypeInfo(string id, string title)
	{
		Id = id;
		Title = title;
	}

	public static ItemMainTypeInfo DeserializeItemMainTypeInfo(JSONNode _json)
	{
		return new ItemMainTypeInfo(_json);
	}

	public override int GetTypeId()
	{
		return 339790863;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",}";
	}
}
