using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.TechTree;

public sealed class TechTreeInfo : BeanBase
{
	public const int __ID__ = -1901238660;

	public string Id { get; private set; }

	public TechPointType TechPoint { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Description { get; private set; }

	public string Description_l10n_key { get; }

	public bool DefaultUnlock { get; private set; }

	public TechTreeInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["tech_point"].IsNumber)
		{
			throw new SerializationException();
		}
		TechPoint = (TechPointType)_json["tech_point"].AsInt;
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
		if (!_json["description"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Description_l10n_key = _json["description"]["key"];
		if (!_json["description"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Description = _json["description"]["text"];
		if (!_json["default_unlock"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultUnlock = _json["default_unlock"];
	}

	public TechTreeInfo(string id, TechPointType tech_point, string title, string description, bool default_unlock)
	{
		Id = id;
		TechPoint = tech_point;
		Title = title;
		Description = description;
		DefaultUnlock = default_unlock;
	}

	public static TechTreeInfo DeserializeTechTreeInfo(JSONNode _json)
	{
		return new TechTreeInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1901238660;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Description = translator(Description_l10n_key, Description);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",TechPoint:" + TechPoint.ToString() + ",Title:" + Title + ",Description:" + Description + ",DefaultUnlock:" + DefaultUnlock + ",}";
	}
}
