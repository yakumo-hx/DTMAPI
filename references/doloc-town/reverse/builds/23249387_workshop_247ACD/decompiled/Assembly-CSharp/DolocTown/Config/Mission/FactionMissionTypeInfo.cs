using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class FactionMissionTypeInfo : BeanBase
{
	public const int __ID__ = 1885059546;

	public FactionMissionType Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public FactionMissionTypeInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (FactionMissionType)_json["id"].AsInt;
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

	public FactionMissionTypeInfo(FactionMissionType id, string title)
	{
		Id = id;
		Title = title;
	}

	public static FactionMissionTypeInfo DeserializeFactionMissionTypeInfo(JSONNode _json)
	{
		return new FactionMissionTypeInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1885059546;
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
