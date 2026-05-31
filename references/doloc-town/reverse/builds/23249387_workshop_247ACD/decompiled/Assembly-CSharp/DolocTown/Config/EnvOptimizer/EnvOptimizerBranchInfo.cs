using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.EnvOptimizer;

public sealed class EnvOptimizerBranchInfo : BeanBase
{
	public const int __ID__ = -1266418626;

	public EnvOptimizerBranchType Id { get; private set; }

	public int Limitation { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Description { get; private set; }

	public string Description_l10n_key { get; }

	public string Comment { get; private set; }

	public string Comment_l10n_key { get; }

	public EnvOptimizerBranchInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (EnvOptimizerBranchType)_json["id"].AsInt;
		if (!_json["limitation"].IsNumber)
		{
			throw new SerializationException();
		}
		Limitation = _json["limitation"];
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
		if (!_json["comment"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Comment_l10n_key = _json["comment"]["key"];
		if (!_json["comment"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Comment = _json["comment"]["text"];
	}

	public EnvOptimizerBranchInfo(EnvOptimizerBranchType id, int limitation, string title, string description, string comment)
	{
		Id = id;
		Limitation = limitation;
		Title = title;
		Description = description;
		Comment = comment;
	}

	public static EnvOptimizerBranchInfo DeserializeEnvOptimizerBranchInfo(JSONNode _json)
	{
		return new EnvOptimizerBranchInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1266418626;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Description = translator(Description_l10n_key, Description);
		Comment = translator(Comment_l10n_key, Comment);
	}

	public override string ToString()
	{
		return "{ Id:" + Id.ToString() + ",Limitation:" + Limitation + ",Title:" + Title + ",Description:" + Description + ",Comment:" + Comment + ",}";
	}
}
