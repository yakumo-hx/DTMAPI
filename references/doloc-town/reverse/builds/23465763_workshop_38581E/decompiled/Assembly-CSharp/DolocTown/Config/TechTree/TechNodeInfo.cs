using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.TechTree;

public sealed class TechNodeInfo : BeanBase
{
	public const int __ID__ = 2130628320;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Description { get; private set; }

	public string Description_l10n_key { get; }

	public string LockPrompt { get; private set; }

	public string LockPrompt_l10n_key { get; }

	public TechNodeInfo(JSONNode _json)
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
		if (!_json["lock_prompt"]["key"].IsString)
		{
			throw new SerializationException();
		}
		LockPrompt_l10n_key = _json["lock_prompt"]["key"];
		if (!_json["lock_prompt"]["text"].IsString)
		{
			throw new SerializationException();
		}
		LockPrompt = _json["lock_prompt"]["text"];
	}

	public TechNodeInfo(string id, string title, string description, string lock_prompt)
	{
		Id = id;
		Title = title;
		Description = description;
		LockPrompt = lock_prompt;
	}

	public static TechNodeInfo DeserializeTechNodeInfo(JSONNode _json)
	{
		return new TechNodeInfo(_json);
	}

	public override int GetTypeId()
	{
		return 2130628320;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Description = translator(Description_l10n_key, Description);
		LockPrompt = translator(LockPrompt_l10n_key, LockPrompt);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",Description:" + Description + ",LockPrompt:" + LockPrompt + ",}";
	}
}
