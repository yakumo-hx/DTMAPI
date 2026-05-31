using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Animal;

public sealed class AnimalStateInfo : BeanBase
{
	public const int __ID__ = -1662987695;

	public AnimalState Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Description { get; private set; }

	public string Description_l10n_key { get; }

	public AnimalStateInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (AnimalState)_json["id"].AsInt;
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
	}

	public AnimalStateInfo(AnimalState id, string title, string description)
	{
		Id = id;
		Title = title;
		Description = description;
	}

	public static AnimalStateInfo DeserializeAnimalStateInfo(JSONNode _json)
	{
		return new AnimalStateInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1662987695;
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
		return "{ Id:" + Id.ToString() + ",Title:" + Title + ",Description:" + Description + ",}";
	}
}
