using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Archives;

public sealed class CharacterDocumentInfo : BeanBase
{
	public const int __ID__ = -53288971;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Subhead { get; private set; }

	public string Subhead_l10n_key { get; }

	public string Content { get; private set; }

	public string Content_l10n_key { get; }

	public CharacterDocumentInfo(JSONNode _json)
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
		if (!_json["subhead"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Subhead_l10n_key = _json["subhead"]["key"];
		if (!_json["subhead"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Subhead = _json["subhead"]["text"];
		if (!_json["content"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Content_l10n_key = _json["content"]["key"];
		if (!_json["content"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Content = _json["content"]["text"];
	}

	public CharacterDocumentInfo(string id, string title, string subhead, string content)
	{
		Id = id;
		Title = title;
		Subhead = subhead;
		Content = content;
	}

	public static CharacterDocumentInfo DeserializeCharacterDocumentInfo(JSONNode _json)
	{
		return new CharacterDocumentInfo(_json);
	}

	public override int GetTypeId()
	{
		return -53288971;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Subhead = translator(Subhead_l10n_key, Subhead);
		Content = translator(Content_l10n_key, Content);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",Subhead:" + Subhead + ",Content:" + Content + ",}";
	}
}
