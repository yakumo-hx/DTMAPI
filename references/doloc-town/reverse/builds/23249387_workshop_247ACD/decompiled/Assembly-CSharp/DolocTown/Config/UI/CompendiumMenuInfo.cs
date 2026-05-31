using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.UI;

public sealed class CompendiumMenuInfo : BeanBase
{
	public const int __ID__ = 1242062172;

	public CompendiumLabel Id { get; private set; }

	public string Name { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public bool DefaultUnlock { get; private set; }

	public CompendiumMenuInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (CompendiumLabel)_json["id"].AsInt;
		if (!_json["name"].IsString)
		{
			throw new SerializationException();
		}
		Name = _json["name"];
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
		if (!_json["default_unlock"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultUnlock = _json["default_unlock"];
	}

	public CompendiumMenuInfo(CompendiumLabel id, string name, string title, bool default_unlock)
	{
		Id = id;
		Name = name;
		Title = title;
		DefaultUnlock = default_unlock;
	}

	public static CompendiumMenuInfo DeserializeCompendiumMenuInfo(JSONNode _json)
	{
		return new CompendiumMenuInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1242062172;
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
		return "{ Id:" + Id.ToString() + ",Name:" + Name + ",Title:" + Title + ",DefaultUnlock:" + DefaultUnlock + ",}";
	}
}
