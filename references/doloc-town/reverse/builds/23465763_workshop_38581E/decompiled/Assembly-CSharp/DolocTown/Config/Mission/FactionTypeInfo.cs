using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class FactionTypeInfo : BeanBase
{
	public const int __ID__ = 1783248034;

	public FactionType Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public FactionTypeInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (FactionType)_json["id"].AsInt;
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

	public FactionTypeInfo(FactionType id, string title)
	{
		Id = id;
		Title = title;
	}

	public static FactionTypeInfo DeserializeFactionTypeInfo(JSONNode _json)
	{
		return new FactionTypeInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1783248034;
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
		return "{ Id:" + Id.ToString() + ",Title:" + Title + ",}";
	}
}
